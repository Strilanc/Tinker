Imports Tinker.Pickling

Namespace Warden
    Public NotInheritable Class Socket
        Inherits DisposableWithTask

        Private ReadOnly inQueue As CallQueue = MakeTaskedCallQueue()
        Private ReadOnly outQueue As CallQueue = MakeTaskedCallQueue
        Private ReadOnly _socket As PacketSocket
        Private ReadOnly _logger As Logger
        Private ReadOnly _cookie As UInt32
        Private ReadOnly _seed As UInt32
        Private _connected As Boolean

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(inQueue IsNot Nothing)
            Contract.Invariant(outQueue IsNot Nothing)
            Contract.Invariant(_socket IsNot Nothing)
            Contract.Invariant(_logger IsNot Nothing)
        End Sub

        Public Sub New(socket As PacketSocket,
                       seed As UInt32,
                       cookie As UInt32,
                       Optional logger As Logger = Nothing)
            Contract.Assume(socket IsNot Nothing)
            Me._logger = If(logger, New Logger())
            Me._socket = socket
            Me._cookie = cookie
            Me._seed = seed
        End Sub

        Public Async Function QueueRunAsync(ct As CancellationToken, callback As Action(Of IRist(Of Byte))) As Task
            Await inQueue
            If ct.IsCancellationRequested Then Return
            WritePacket(ClientPacket.MakeFullServiceConnect(_cookie, _seed))
            Do
                Dim packet = Await AsyncReadPacket(ct)
                If ct.IsCancellationRequested Then Return

                If packet.Cookie <> _cookie Then
                    Throw New IO.InvalidDataException("Incorrect cookie from BNLS server.")
                ElseIf packet.Result <> 0 Then
                    Throw New IO.IOException("BNLS server indicated there was a failure: {0}: ""{1}"".".Frmt(packet.Result, packet.ResponseData.ToAsciiChars.AsString))
                End If

                Select Case packet.Id
                    Case WardenPacketId.FullServiceConnect
                        If _connected Then Throw New IO.InvalidDataException("Unexpected {0} from {1}.".Frmt(packet.Id, _socket.Name))
                        _connected = True
                    Case WardenPacketId.FullServiceHandleWardenPacket
                        If Not _connected Then Throw New IO.InvalidDataException("Unexpected {0} from {1}.".Frmt(packet.Id, _socket.Name))
                        Call Async Sub() Await outQueue.QueueAction(Sub() callback(packet.ResponseData))
                    Case Else
                        Throw New IO.InvalidDataException("Unrecognized packet type received from {0}: {1}.".Frmt(_socket.Name, packet.Id))
                End Select
            Loop
        End Function
        Private Async Function AsyncReadPacket(ct As CancellationToken) As Task(Of ServerPacket)
            Dim packetData = Await _socket.AsyncReadPacket()
            If ct.IsCancellationRequested Then Return Nothing

            'Check
            If packetData.Count < 3 Then
                Throw New IO.InvalidDataException("Packet doesn't have a header.")
            ElseIf packetData(2) <> BNLSPacketId.Warden Then
                Throw New IO.InvalidDataException("Not a bnls warden packet.")
            End If

            'Parse, log, return
            Dim pk = ServerPacket.FromData(packetData.SkipExact(3))
            _logger.Log(Function() "Received {0} from {1}".Frmt(pk.Id, _socket.Name), LogMessageType.DataEvent)
            _logger.Log(Function() "Received {0} from {1}: {2}".Frmt(pk.Id, _socket.Name, pk), LogMessageType.DataParsed)
            Return pk
        End Function

        Private Sub WritePacket(packet As ClientPacket)
            Contract.Requires(packet IsNot Nothing)

            Try
                _logger.Log(Function() "Sending {0} to {1}".Frmt(packet.Id, _socket.Name), LogMessageType.DataEvent)
                _logger.Log(Function() "Sending {0} to {1}: {2}".Frmt(packet.Id, _socket.Name, packet.Payload.Description), LogMessageType.DataParsed)
                _socket.WritePacket({}, New Byte() {BNLSPacketId.Warden, packet.Id}.Concat(packet.Payload.Data))

            Catch e As Exception
                e.RaiseAsUnexpected("Sending {0} to {1}".Frmt(packet.Id, _socket.Name))
                _socket.QueueDisconnect(expected:=False, reason:="Error sending {0} for {1}: {2}".Frmt(packet.Id, _socket.Name, e))
                Throw
            End Try
        End Sub
        Public Function QueueSendWardenData(wardenData As IRist(Of Byte)) As Task
            Contract.Requires(wardenData IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() WritePacket(ClientPacket.MakeFullServiceHandleWardenPacket(_cookie, wardenData)))
        End Function

        Protected Overrides Async Function PerformDispose(finalizing As Boolean) As Task
            If finalizing Then Return
            Await inQueue
            Call Async Sub() Await _socket.QueueDisconnect(expected:=True, reason:="Disposed")
        End Function

        Public Shared Async Function ConnectToAsync(remoteHost As InvariantString,
                                                    remotePort As UInt16,
                                                    seed As UInt32,
                                                    cookie As UInt32,
                                                    clock As IClock,
                                                    logger As Logger) As Task(Of Warden.Socket)
            Contract.Assume(clock IsNot Nothing)
            Contract.Assume(logger IsNot Nothing)

            'Initiate connection
            Dim tcpClient = Await AsyncTcpConnect(remoteHost, remotePort)
            Dim packetSocket = New PacketSocket(stream:=tcpClient.GetStream,
                                                localendpoint:=DirectCast(tcpClient.Client.LocalEndPoint, Net.IPEndPoint),
                                                remoteendpoint:=DirectCast(tcpClient.Client.RemoteEndPoint, Net.IPEndPoint),
                                                Timeout:=5.Minutes,
                                                preheaderLength:=0,
                                                sizeHeaderLength:=2,
                                                logger:=logger,
                                                Name:="BNLS",
                                                clock:=clock)
            logger.Log("Connected to bnls server.", LogMessageType.Positive)
            Return New Warden.Socket(Socket:=packetSocket,
                                        seed:=seed,
                                        cookie:=cookie,
                                        logger:=logger)
        End Function
    End Class
End Namespace
