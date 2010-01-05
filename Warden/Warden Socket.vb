Namespace Warden
    Public NotInheritable Class Socket
        Inherits FutureDisposable

        Public Event ReceivedWardenData(ByVal sender As Warden.Socket, ByVal wardenData As IReadableList(Of Byte))
        Public Event Failed(ByVal sender As Warden.Socket, ByVal e As Exception)
        Public Event Disconnected(ByVal sender As Warden.Socket, ByVal expected As Boolean, ByVal reason As String)

        Private ReadOnly inQueue As ICallQueue = New TaskedCallQueue
        Private ReadOnly outQueue As ICallQueue = New TaskedCallQueue
        Private ReadOnly _socket As PacketSocket
        Private ReadOnly _logger As Logger
        Private ReadOnly _cookie As UInt32
        Private ReadOnly _seed As UInt32
        Private ReadOnly _keepAlive As New DeadManSwitch(period:=30.Seconds)
        Private _connected As Boolean

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(inQueue IsNot Nothing)
            Contract.Invariant(outQueue IsNot Nothing)
            Contract.Invariant(_socket IsNot Nothing)
            Contract.Invariant(_logger IsNot Nothing)
            Contract.Invariant(_keepAlive IsNot Nothing)
        End Sub

        Public Sub New(ByVal socket As PacketSocket,
                       ByVal seed As UInt32,
                       ByVal cookie As UInt32,
                       Optional ByVal logger As Logger = Nothing)
            Contract.Assume(socket IsNot Nothing)
            Me._logger = If(logger, New Logger())
            Me._socket = socket
            Me._cookie = cookie
            Me._seed = seed

            AddHandler _socket.Disconnected, Sub(sender, expected, reason) outQueue.QueueAction(Sub() RaiseEvent Disconnected(Me, expected, reason))
            AddHandler _keepAlive.Triggered, Sub()
                                                 _keepAlive.Arm()
                                                 _socket.WritePacket({0, 0, BNLSPacketId.Null})
                                             End Sub

            Start()
        End Sub

        Private Sub Start()
            Me._keepAlive.Arm()
            WritePacket(ClientPacket.MakeFullServiceConnect(_cookie, _seed))
            BeginReading()
        End Sub

        '''<summary>Asynchronously reads packets until an exception occurs, raising events to the outside.</summary>
        Private Sub BeginReading()
            AsyncProduceConsumeUntilError(
                producer:=AddressOf AsyncReadPacket,
                consumer:=Function(packet) inQueue.QueueAction(Sub() OnReceivePacket(packet)),
                errorHandler:=Sub(exception) outQueue.QueueAction(Sub() RaiseEvent Failed(Me, exception))
            )
        End Sub
        Private Function AsyncReadPacket() As IFuture(Of ServerPacket)
            Return _socket.AsyncReadPacket().Select(
                Function(packetData)
                    'Check
                    If packetData.Count < 3 Then
                        Throw New IO.InvalidDataException("Packet doesn't have a header.")
                    ElseIf packetData(2) <> BNLSPacketId.Warden Then
                        Throw New IO.InvalidDataException("Not a bnls warden packet.")
                    End If

                    'Parse, log, return
                    Dim pk = ServerPacket.FromData(packetData.SubView(3))
                    _logger.Log(Function() "Received {0} from {1}".Frmt(pk.Id, _socket.Name), LogMessageType.DataEvent)
                    _logger.Log(pk.ToString, LogMessageType.DataParsed)
                    Return pk
                End Function)
        End Function
        Private Sub OnReceivePacket(ByVal packet As ServerPacket)
            Contract.Requires(packet IsNot Nothing)

            If packet.Cookie <> _cookie Then
                Throw New IO.InvalidDataException("Incorrect cookie from BNLS server.")
            ElseIf packet.Result <> 0 Then
                Throw New IO.IOException("BNLS server indicated there was a failure: {0}: ""{1}"".".Frmt(packet.Result,
                                                                                                     packet.ResponseData.ParseChrString(nullTerminated:=False)))
            End If

            Select Case packet.Id
                Case WardenPacketId.FullServiceConnect
                    If _connected Then Throw New IO.InvalidDataException("Unexpected {0} from {1}.".Frmt(packet.Id, _socket.Name))
                    _connected = True
                Case WardenPacketId.FullServiceHandleWardenPacket
                    If Not _connected Then Throw New IO.InvalidDataException("Unexpected {0} from {1}.".Frmt(packet.Id, _socket.Name))
                    outQueue.QueueAction(Sub() RaiseEvent ReceivedWardenData(Me, packet.ResponseData))
                Case Else
                    Throw New IO.InvalidDataException("Unrecognized packet type received from {0}: {1}.".Frmt(_socket.Name, packet.Id))
            End Select
        End Sub

        Private Sub WritePacket(ByVal packet As ClientPacket)
            Contract.Requires(packet IsNot Nothing)

            Try
                _logger.Log(Function() "Sending {0} to {1}".Frmt(packet.Id, _socket.Name), LogMessageType.DataEvent)
                _logger.Log(packet.Payload.Description, LogMessageType.DataParsed)
                _socket.WritePacket(Concat(Of Byte)({0, 0, BNLSPacketId.Warden, packet.Id}, packet.Payload.Data.ToArray))
                _keepAlive.Reset()

            Catch e As Exception
                e.RaiseAsUnexpected("Sending {0} to {1}".Frmt(packet.Id, _socket.Name))
                _socket.Disconnect(expected:=False, reason:="Error sending {0} for {1}: {2}".Frmt(packet.Id, _socket.Name, e))
                Throw
            End Try
        End Sub
        Public Function QueueSendWardenData(ByVal wardenData As IReadableList(Of Byte)) As ifuture
            Contract.Requires(wardenData IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ifuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() WritePacket(ClientPacket.MakeFullServiceHandleWardenPacket(_cookie, wardenData)))
        End Function

        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As Strilbrary.Threading.IFuture
            If finalizing Then Return Nothing
            Return inQueue.QueueAction(Sub()
                                           _socket.Disconnect(expected:=True, reason:="Disposed")
                                       End Sub)
        End Function
    End Class
End Namespace
