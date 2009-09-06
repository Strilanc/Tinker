Imports HostBot.Warcraft3.W3PacketId
Imports System.Runtime.CompilerServices
Imports System.Net
Imports System.Net.Sockets

Namespace Warcraft3
    Public Class W3Socket
        Private WithEvents socket As PacketSocket
        Public Event Disconnected(ByVal sender As W3Socket, ByVal reason As String)

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
            'contract bug wrt interface event implementation requires this:
            'Contract.Requires(socket IsNot Nothing)
            'Contract.Requires(initialSlack >= 0)
            'Contract.Requires(costPerPacket >= 0)
            'Contract.Requires(costPerPacketData >= 0)
            'Contract.Requires(costPerNonGameAction >= 0)
            'Contract.Requires(costPerNonGameActionData >= 0)
            'Contract.Requires(costLimit >= 0)
            'Contract.Requires(costRecoveredPerSecond > 0)
            Contract.Assume(socket IsNot Nothing)
            Contract.Assume(initialSlack >= 0)
            Contract.Assume(costPerPacket >= 0)
            Contract.Assume(costPerPacketData >= 0)
            Contract.Assume(costPerNonGameAction >= 0)
            Contract.Assume(costPerNonGameActionData >= 0)
            Contract.Assume(costLimit >= 0)
            Contract.Assume(costRecoveredPerSecond > 0)

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

        Private Sub CatchDisconnected(ByVal sender As PacketSocket, ByVal reason As String) Handles socket.Disconnected
            RaiseEvent Disconnected(Me, reason)
        End Sub
        Public Sub disconnect(ByVal reason As String)
            Contract.Requires(reason IsNot Nothing)
            socket.Disconnect(reason)
        End Sub

        Public Function SendPacket(ByVal pk As W3Packet) As Outcome
            Contract.Requires(pk IsNot Nothing)

            Try
                'Validate
                If socket Is Nothing OrElse Not socket.IsConnected OrElse pk Is Nothing Then
                    Return failure("Socket is not connected")
                End If

                'Log
                Logger.log(Function() "Sending {0} to {1}".frmt(pk.id, Name), LogMessageType.DataEvent)
                Logger.log(pk.payload.Description, LogMessageType.DataParsed)

                'Send
                socket.WritePacket(Concat({W3Packet.PACKET_PREFIX, pk.id, 0, 0}, pk.payload.Data.ToArray))
                Return success("Sent")

            Catch e As Pickling.PicklingException
                Dim msg = "Error packing {0} for {1}: {2}".Frmt(pk.id, Name, e)
                Logger.log(msg, LogMessageType.Problem)
                Return failure(msg)

            Catch e As Exception
                If Not (TypeOf e Is SocketException OrElse
                        TypeOf e Is ObjectDisposedException OrElse
                        TypeOf e Is IO.IOException) Then
                    LogUnexpectedException("Error sending {0} to {1}.".frmt(pk.id, Name), e)
                End If
                Dim msg = "Error sending {0} to {1}: {2}".frmt(pk.id, Name, e)
                socket.Disconnect(reason:=msg)
                Return failure(msg)
            End Try
        End Function

        Public Function FutureReadPacket() As IFuture(Of PossibleException(Of W3Packet, Exception))
            Dim f = New Future(Of PossibleException(Of W3Packet, Exception))
            socket.FutureReadPacket().CallWhenValueReady(
                Sub(result)
                    If result.Exception IsNot Nothing Then
                        f.SetValue(result.Exception)
                        Return
                    End If

                    Dim data = result.Value
                    If data(0) <> W3Packet.PACKET_PREFIX OrElse data.Length < 4 Then
                        disconnect("Invalid packet prefix")
                        f.SetValue(New IO.IOException("Invalid packet prefix"))
                        Return
                    End If
                    Dim id = CType(data(1), W3PacketId)
                    data = data.SubView(4)

                    Try
                        'Anti-flood
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
                        f.SetValue(pk)

                    Catch e As Pickling.PicklingException
                        Dim msg = "Error parsing {0} from {1}: {2} ({3})".Frmt(id, Name, e, data.ToHexString())
                        Logger.Log(msg, LogMessageType.Negative)
                        f.SetValue(e)

                    Catch e As Exception
                        If Not (TypeOf e Is SocketException OrElse
                                TypeOf e Is ObjectDisposedException OrElse
                                TypeOf e Is IO.IOException) Then
                            LogUnexpectedException("Error receiving {0} from {1} ({2}).".Frmt(id, Name, data.ToHexString()), e)
                        End If
                        Dim msg = "Error receiving {0} from {1}: {2} ({3})".Frmt(id, Name, e, data.ToHexString())
                        Logger.Log(msg, LogMessageType.Problem)
                        socket.Disconnect(reason:=msg)
                        f.SetValue(e)
                    End Try
                End Sub
            )
            Return f
        End Function
    End Class
End Namespace