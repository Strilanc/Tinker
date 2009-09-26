Imports HostBot.Warcraft3.W3PacketId
Imports System.Runtime.CompilerServices
Imports System.Net
Imports System.Net.Sockets

Namespace Warcraft3
    Public Class W3Socket
        Private WithEvents socket As PacketSocket
        Public Event Disconnected(ByVal sender As W3Socket, ByVal expected As Boolean, ByVal reason As String)

        Private ReadOnly costPerPacket As Double
        Private ReadOnly costPerPacketData As Double
        Private ReadOnly costPerNonGameAction As Double
        Private ReadOnly costPerNonGameActionData As Double
        Private ReadOnly costLimit As Double
        Private ReadOnly recoveryRate As Double

        Private availableSlack As Double
        Private usedCost As Double
        Private lastReadTime As Date = DateTime.Now()

        Public Sub New(ByVal socket As PacketSocket,
                       Optional ByVal initialSlack As Double = 0,
                       Optional ByVal costPerPacket As Double = 0,
                       Optional ByVal costPerPacketData As Double = 0,
                       Optional ByVal costPerNonGameAction As Double = 0,
                       Optional ByVal costPerNonGameActionData As Double = 0,
                       Optional ByVal costLimit As Double = 0,
                       Optional ByVal costRecoveredPerSecond As Double = 1)
            Contract.Requires(socket IsNot Nothing)
            Contract.Requires(initialSlack >= 0)
            Contract.Requires(costPerPacket >= 0)
            Contract.Requires(costPerPacketData >= 0)
            Contract.Requires(costPerNonGameAction >= 0)
            Contract.Requires(costPerNonGameActionData >= 0)
            Contract.Requires(costLimit >= 0)
            Contract.Requires(costRecoveredPerSecond > 0)

            Me.socket = socket
            Me.availableSlack = initialSlack
            Me.costPerNonGameAction = costPerNonGameActionData
            Me.costPerNonGameActionData = costPerNonGameActionData
            Me.costPerPacket = costPerPacket
            Me.costPerPacketData = costPerPacketData
            Me.costLimit = costLimit
            Me.recoveryRate = costRecoveredPerSecond / TimeSpan.TicksPerSecond
        End Sub

        Public Property Logger() As Logger
            Get
                Return socket.logger
            End Get
            Set(ByVal value As Logger)
                socket.logger = value
            End Set
        End Property
        Public Property Name() As String
            Get
                Return socket.Name
            End Get
            Set(ByVal value As String)
                socket.Name = value
            End Set
        End Property

        Public ReadOnly Property LocalEndPoint As IPEndPoint
            Get
                Contract.Ensures(Contract.Result(Of IPEndPoint)() IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IPEndPoint)().Address IsNot Nothing)
                Return socket.LocalEndPoint
            End Get
        End Property
        Public ReadOnly Property RemoteEndPoint As IPEndPoint
            Get
                Contract.Ensures(Contract.Result(Of IPEndPoint)() IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IPEndPoint)().Address IsNot Nothing)
                Return socket.RemoteEndPoint
            End Get
        End Property
        Public Function connected() As Boolean
            Return socket.IsConnected
        End Function

        Private Sub OnSocketDisconnect(ByVal sender As PacketSocket, ByVal expected As Boolean, ByVal reason As String) Handles socket.Disconnected
            RaiseEvent Disconnected(Me, expected, reason)
        End Sub
        Public Sub Disconnect(ByVal expected As Boolean, ByVal reason As String)
            Contract.Requires(reason IsNot Nothing)
            socket.Disconnect(expected, reason)
        End Sub

        Public Sub SendPacket(ByVal pk As W3Packet)
            Contract.Requires(pk IsNot Nothing)

            Try
                'Validate
                If socket Is Nothing OrElse Not socket.IsConnected OrElse pk Is Nothing Then
                    Throw New InvalidOperationException("Socket is not connected")
                End If

                'Log
                Logger.Log(Function() "Sending {0} to {1}".Frmt(pk.id, Name), LogMessageType.DataEvent)
                Logger.Log(pk.payload.Description, LogMessageType.DataParsed)

                'Send
                socket.WritePacket(Concat({W3Packet.PACKET_PREFIX, pk.id, 0, 0}, pk.payload.Data.ToArray))

            Catch e As Pickling.PicklingException
                Dim msg = "Error packing {0} for {1}: {2}".Frmt(pk.id, Name, e)
                Logger.Log(msg, LogMessageType.Problem)
                Throw

            Catch e As Exception
                If Not (TypeOf e Is SocketException OrElse
                        TypeOf e Is ObjectDisposedException OrElse
                        TypeOf e Is IO.IOException) Then
                    LogUnexpectedException("Error sending {0} to {1}.".Frmt(pk.id, Name), e)
                End If
                Dim msg = "Error sending {0} to {1}: {2}".Frmt(pk.id, Name, e)
                socket.Disconnect(expected:=False, reason:=msg)
                Throw
            End Try
        End Sub

        Public Function FutureReadPacket() As IFuture(Of W3Packet)
            Return socket.FutureReadPacket().Select(
                Function(data)
                    If data(0) <> W3Packet.PACKET_PREFIX OrElse data.Length < 4 Then
                        Disconnect(expected:=False, reason:="Invalid packet prefix")
                        Throw New IO.IOException("Invalid packet prefix")
                    End If
                    Dim id = CType(data(1), W3PacketId)
                    data = data.SubView(4)

                    Try
                        'Anti-flood [uh... is this half done?]
                        If id = NonGameAction Then
                            usedCost += costPerNonGameAction
                            usedCost += costPerNonGameActionData * data.Length
                        Else
                            usedCost += costPerPacket
                            usedCost += costPerPacketData * data.Length
                        End If
                        Dim t = Now()
                        Dim dt = t - lastReadTime
                        lastReadTime = t
                        usedCost -= dt.Ticks * recoveryRate
                        If usedCost < 0 Then  usedCost = 0
                        If availableSlack > 0 Then
                            Dim x = Math.Min(availableSlack, usedCost)
                            usedCost -= x
                            availableSlack -= x
                        End If

                        'Handle
                        Logger.Log(Function() "Received {0} from {1}".Frmt(id, Name), LogMessageType.DataEvent)
                        Dim pk = W3Packet.FromData(id, data)
                        If pk.payload.Data.Length <> data.Length Then
                            Throw New Pickling.PicklingException("Data left over after parsing.")
                        End If
                        Logger.Log(pk.payload.Description, LogMessageType.DataParsed)
                        Return pk

                    Catch e As Pickling.PicklingException
                        Dim msg = "Error parsing {0} from {1}: {2} ({3})".Frmt(id, Name, e, data.ToHexString())
                        Logger.Log(msg, LogMessageType.Negative)
                        Throw

                    Catch e As Exception
                        If Not (TypeOf e Is SocketException OrElse
                                TypeOf e Is ObjectDisposedException OrElse
                                TypeOf e Is IO.IOException) Then
                            LogUnexpectedException("Error receiving {0} from {1} ({2}).".Frmt(id, Name, data.ToHexString()), e)
                        End If
                        Dim msg = "Error receiving {0} from {1}: {2} ({3})".Frmt(id, Name, e, data.ToHexString())
                        Logger.Log(msg, LogMessageType.Problem)
                        socket.Disconnect(expected:=False, reason:=msg)
                        Throw
                    End Try
                End Function
            )
        End Function
    End Class
End Namespace