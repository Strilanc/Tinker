Imports System.Net
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

        Private Sub New(ByVal socket As PacketSocket,
                        ByVal cookie As UInteger,
                        Optional ByVal logger As Logger = Nothing)
            logger = If(logger, New Logger())
            Me.logger = logger
            Me.socket = socket
            Me.cookie = cookie
            BeginReading()
        End Sub

        '''<summary>Asynchronously reads packets until an exception occurs, raising events to the outside.</summary>
        Private Sub BeginReading()
            'Read packets until an exception occurs
            Dim readLoop = FutureIterateExcept(Function() FutureReadBnlsPacket(socket, logger),
                Sub(packet)
                    Contract.Assume(packet IsNot Nothing)

                    'Check packet type
                    If packet.id <> BNLSWardenPacketId.FullServiceHandleWardenPacket Then
                        Throw New IO.InvalidDataException("Incorrect packet type received from server.")
                    End If

                    'Check packet contents
                    Dim vals = CType(packet.Payload, IPickle(Of Dictionary(Of String, Object))).Value
                    Contract.Assume(vals IsNot Nothing)
                    If CUInt(vals("cookie")) <> cookie Then
                        Throw New IO.InvalidDataException("Incorrect cookie from server.")
                    ElseIf CUInt(vals("result")) <> 0 Then
                        Throw New IO.IOException("Server indicated there was a failure.")
                    End If

                    'Pass warden data out (and continue reading)
                    RaiseEvent Send(CType(vals("data"), Byte()))
                End Sub
            )

            'Pass exception out when readLoop fails
            readLoop.CallWhenReady(
                Sub(exception)
                    If exception IsNot Nothing Then
                        RaiseEvent Fail(exception)
                    End If
                End Sub
            )
        End Sub

        '''<summary>Asynchronously connects to a BNLS server.</summary>
        Public Shared Function FutureConnectToBNLSServer(ByVal hostName As String,
                                                         ByVal port As UShort,
                                                         ByVal seed As UInteger,
                                                         Optional ByVal logger As Logger = Nothing) As IFuture(Of BNLSWardenClient)
            Contract.Requires(hostName IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of BNLSWardenClient))() IsNot Nothing)
            logger = If(logger, New Logger())
            Dim cookie = seed

            'Connect and send first packet
            Dim futurePacketSocket = FutureCreateConnectedTcpClient(hostName, port).Select(
                Function(tcpClient)
                    Contract.Assume(tcpClient IsNot Nothing)
                    Dim socket = New PacketSocket(tcpClient,
                                                  timeout:=5.Minutes,
                                                  numBytesBeforeSize:=0,
                                                  numSizeBytes:=2,
                                                  logger:=logger,
                                                  name:="BNLS")
                    WritePacket(socket, logger, BNLSWardenPacket.MakeFullServiceConnect(seed, seed))
                    Return socket
                End Function
            )

            'Read response packet
            Dim futureReadPacket = futurePacketSocket.Select(
                Function(packetSocket) FutureReadBnlsPacket(packetSocket, logger)
            ).Defuturized

            'Process response packet and construct bnls client on success
            Dim futureBnlsClient = futureReadPacket.Select(
                Function(packet)
                    Contract.Assume(packet IsNot Nothing)
                    Dim packetSocket = futurePacketSocket.Value
                    Contract.Assume(packetSocket IsNot Nothing)

                    'Check packet type
                    If packet.id <> BNLSWardenPacketId.FullServiceConnect Then
                        Dim msg = "Bnls server responded with a non-FullServiceConnect packet."
                        packetSocket.Disconnect(expected:=False, reason:=msg)
                        Throw New IO.InvalidDataException(msg)
                    End If

                    'Check packet contents
                    Dim vals = CType(packet.Payload, IPickle(Of Dictionary(Of String, Object))).Value
                    If CUInt(vals("cookie")) <> cookie Then
                        Dim msg = "Incorrect cookie from server."
                        packetSocket.Disconnect(expected:=False, reason:=msg)
                        Throw New IO.InvalidDataException(msg)
                    End If

                    Return New BNLSWardenClient(packetSocket, cookie, logger)
                End Function
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

        Private Shared Function FutureReadBnlsPacket(ByVal socket As PacketSocket,
                                                     ByVal logger As Logger) As IFuture(Of BNLSWardenPacket)
            Return socket.FutureReadPacket().Select(
                Function(packetData)
                    Contract.Assume(packetData IsNot Nothing)

                    'Check packet
                    If packetData.Length < 3 Then
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

        Public Sub ProcessWardenPacket(ByVal data As ViewableList(Of Byte))
            Contract.Requires(data IsNot Nothing)
            WritePacket(socket, logger, BNLSWardenPacket.MakeFullServiceHandleWardenPacket(cookie, data.ToArray))
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