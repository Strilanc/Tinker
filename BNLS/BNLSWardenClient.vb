Imports System.Net.Sockets

Namespace BNLS
    Public Enum BNLSPacketId As Byte
        Null = &H0
        CDKey = &H1
        LogOnChallenge = &H2
        LogOnProof = &H3
        CreateAccount = &H4
        ChangeChallenge = &H5
        ChangeProof = &H6
        UpgradeChallenge = &H7
        UpgradeProof = &H8
        VersionCheck = &H9
        ConfirmLogOn = &HA
        HashData = &HB
        CDKeyEx = &HC
        ChooseNlsRevision = &HD
        Authorize = &HE
        AuthorizeProof = &HF
        RequestVersionByte = &H10
        VerifyServer = &H11
        ReserveServerSlots = &H12
        ServerLogOnChallenge = &H13
        ServerLogOnProof = &H14
        VersionCheckEx = &H18
        VersionCheckEx2 = &H1A
        Warden = &H7D
    End Enum

    Public NotInheritable Class BNLSWardenClient
        Implements IDisposable

        Private ReadOnly keepAlive As New DeadManSwitch(period:=60.Seconds, initiallyArmed:=True)
        Private WithEvents socket As PacketSocket
        Public ReadOnly logger As Logger
        Private ReadOnly cookie As UInteger
        Public Event Send(ByVal data As Byte())
        Public Event Fail(ByVal e As Exception)
        Public Event Disconnect(ByVal sender As BNLSWardenClient, ByVal expected As Boolean, ByVal reason As String)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(logger IsNot Nothing)
            Contract.Invariant(socket IsNot Nothing)
        End Sub

        ''' <summary>
        ''' Constructs a BNLSWardenClient
        ''' </summary>
        ''' <param name="socket">A socket already connected and introduced to a BNLS server.</param>
        ''' <param name="cookie">The cookie value the server associates with this instance.</param>
        ''' <param name="logger">Logger</param>
        Private Sub New(ByVal socket As PacketSocket,
                        ByVal cookie As UInteger,
                        Optional ByVal logger As Logger = Nothing)
            Contract.Assume(socket IsNot Nothing)
            logger = If(logger, New Logger())
            Me.logger = logger
            Me.socket = socket
            Me.cookie = cookie
            BeginReading()
        End Sub

        '''<summary>Asynchronously reads packets until an exception occurs, raising events to the outside.</summary>
        Private Sub BeginReading()
            AsyncProduceConsumeUntilError2(
                producer:=Function() AsyncReadBnlsPacket(socket, logger),
                consumer:=Sub(packet)
                              'Check packet type
                              If packet.id <> BNLSWardenPacketId.FullServiceHandleWardenPacket Then
                                  Throw New IO.InvalidDataException("Incorrect packet type received from BNLS server.")
                              End If

                              'Check packet contents
                              Dim vals = CType(packet.Payload, IPickle(Of Dictionary(Of InvariantString, Object))).Value
                              Contract.Assume(vals IsNot Nothing)
                              If CUInt(vals("cookie")) <> cookie Then
                                  Throw New IO.InvalidDataException("Incorrect cookie from BNLS server.")
                              ElseIf CUInt(vals("result")) <> 0 Then
                                  Throw New IO.IOException("BNLS server indicated there was a failure.")
                              End If

                              'Warden response data for bnet client
                              RaiseEvent Send(CType(vals("data"), Byte()))
                          End Sub,
                errorHandler:=Sub(exception) RaiseEvent Fail(exception)
            )
        End Sub

        '''<summary>Asynchronously connects to a BNLS server.</summary>
        Public Shared Function AsyncConnectToBNLSServer(ByVal hostName As String,
                                                        ByVal port As UShort,
                                                        ByVal seed As UInteger,
                                                        Optional ByVal logger As Logger = Nothing) As IFuture(Of BNLSWardenClient)
            Contract.Requires(hostName IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of BNLSWardenClient))() IsNot Nothing)
            logger = If(logger, New Logger())
            Dim cookie = seed

            'Connect and send first packet
            Dim futurePacketSocket = AsyncTcpConnect(hostName, port).Select(
                Function(tcpClient)
                    Dim socket = New PacketSocket(stream:=tcpClient.GetStream,
                                                  localendpoint:=CType(tcpClient.Client.LocalEndPoint, Net.IPEndPoint),
                                                  remoteendpoint:=CType(tcpClient.Client.RemoteEndPoint, Net.IPEndPoint),
                                                  timeout:=5.Minutes,
                                                  numBytesBeforeSize:=0,
                                                  numSizeBytes:=2,
                                                  logger:=logger,
                                                  name:="BNLS")
                    WritePacket(socket, logger, BNLSWardenPacket.MakeFullServiceConnect(cookie, seed))
                    Return socket
                End Function
            )

            'Read response packet
            Dim futureReadPacket = futurePacketSocket.Select(
                Function(packetSocket) AsyncReadBnlsPacket(packetSocket, logger)
            ).Defuturized

            'Process response packet and construct bnls client on success
            Dim futureBnlsClient = futureReadPacket.Select(
                Function(packet)
                    'Check type
                    If packet.id <> BNLSWardenPacketId.FullServiceConnect Then
                        Throw New IO.InvalidDataException("Bnls server responded with a non-FullServiceConnect packet.")
                    End If

                    'Check content
                    Dim vals = CType(packet.Payload, IPickle(Of Dictionary(Of InvariantString, Object))).Value
                    If CUInt(vals("cookie")) <> cookie Then
                        Throw New IO.InvalidDataException("Incorrect cookie from server.")
                    ElseIf CUInt(vals("result")) <> 0 Then
                        Throw New IO.InvalidDataException("Server result was non-zero: {0}".Frmt(CUInt(vals("result"))))
                    End If

                    Return New BNLSWardenClient(futurePacketSocket.Value, cookie, logger)
                End Function
            )
            futureBnlsClient.Catch(
                Sub(exception)
                    If futurePacketSocket.State = FutureState.Succeeded Then
                        futurePacketSocket.Value.Disconnect(expected:=False, reason:=exception.Message)
                    End If
                End Sub
            )

            Return futureBnlsClient
        End Function

        Private Shared Sub WritePacket(ByVal socket As PacketSocket,
                                       ByVal logger As Logger,
                                       ByVal packet As BNLSWardenPacket)
            Contract.Requires(packet IsNot Nothing)
            Contract.Requires(logger IsNot Nothing)
            Contract.Requires(socket IsNot Nothing)

            Try
                logger.Log(Function() "Sending {0} to {1}".Frmt(packet.id, socket.Name), LogMessageType.DataEvent)
                logger.Log(packet.Payload.Description, LogMessageType.DataParsed)
                socket.WritePacket(Concat(Of Byte)({0, 0, BNLSPacketId.Warden, packet.id}, packet.Payload.Data.ToArray))

            Catch e As Exception
                If Not (TypeOf e Is SocketException OrElse
                        TypeOf e Is ObjectDisposedException OrElse
                        TypeOf e Is IO.IOException OrElse
                        TypeOf e Is PicklingException) Then
                    e.RaiseAsUnexpected("Error sending {0} to {1}.".Frmt(packet.id, socket.Name))
                End If
                socket.Disconnect(expected:=False, reason:="Error sending {0} for {1}: {2}".Frmt(packet.id, socket.Name, e))
                Throw
            End Try
        End Sub

        Private Shared Function AsyncReadBnlsPacket(ByVal socket As PacketSocket,
                                                    ByVal logger As Logger) As IFuture(Of BNLSWardenPacket)
            Return socket.AsyncReadPacket().Select(
                Function(packetData)
                    'Check
                    If packetData.Count < 3 Then
                        Throw New IO.InvalidDataException("Packet doesn't have a header.")
                    ElseIf packetData(2) <> BNLSPacketId.Warden Then
                        Throw New IO.InvalidDataException("Not a bnls warden packet.")
                    End If

                    'Parse, log, return
                    Dim pk = BNLSWardenPacket.FromServerData(packetData.SubView(3))
                    logger.Log(Function() "Received {0} from {1}".Frmt(pk.id, socket.Name), LogMessageType.DataEvent)
                    logger.Log(pk.Payload.Description, LogMessageType.DataParsed)
                    Return pk
                End Function)
        End Function

        Public Sub ProcessWardenPacket(ByVal data As IReadableList(Of Byte))
            Contract.Requires(data IsNot Nothing)
            keepAlive.Reset()
            WritePacket(socket, logger, BNLSWardenPacket.MakeFullServiceHandleWardenPacket(cookie, data))
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            socket.Disconnect(expected:=True, reason:="Disposed")
            GC.SuppressFinalize(Me)
        End Sub

        Private Sub OnSocketDisconnect(ByVal sender As PacketSocket, ByVal expected As Boolean, ByVal reason As String) Handles socket.Disconnected
            RaiseEvent Disconnect(Me, expected, reason)
        End Sub
    End Class
End Namespace