Imports System.Net
Imports System.Net.Sockets

Namespace BattleNetLogonServer
    Public Enum BnlsPacketId As Byte
        Null = &H0
        CdKey = &H1
        LogonChallenge = &H2
        LogonProof = &H3
        CreateAccount = &H4
        ChangeChallenge = &H5
        ChangeProof = &H6
        UpgradeChallenge = &H7
        UpgradeProof = &H8
        VersionCheck = &H9
        ConfirmLogon = &HA
        HashData = &HB
        CdKeyEx = &HC
        ChooseNlsRevision = &HD
        Authorize = &HE
        AuthorizeProof = &HF
        RequestVersionByte = &H10
        VerifyServer = &H11
        ReserveServerSlots = &H12
        ServerLogonChallenge = &H13
        ServerLogonProof = &H14
        Reserved0 = &H15
        Reserved1 = &H16
        Reserved2 = &H17
        VersionCheckEx = &H18
        Reserved3 = &H19
        VersionCheckEx2 = &H1A
        Warden = &H7D
    End Enum

    Public Class BnlsClient
        Implements IDisposable

        Private WithEvents socket As PacketSocket
        Public ReadOnly logger As Logger
        Private ReadOnly cookie As UInteger
        Public Event Send(ByVal data As Byte())
        Public Event Fail(ByVal e As Exception)

        Private Sub New(ByVal socket As PacketSocket, ByVal cookie As UInteger, Optional ByVal logger As Logger = Nothing)
            logger = If(logger, New Logger())
            Me.logger = logger
            Me.socket = socket
            Me.cookie = cookie
            BeginReading()
        End Sub

        Private Sub BeginReading()
            FutureIterate(Function() FutureReadPacket(socket, logger),
                Function(packetResult)
                    If packetResult.Exception IsNot Nothing Then
                        RaiseEvent Fail(packetResult.Exception)
                        Return False.Futurize
                    End If

                    Dim pk = packetResult.Value
                    If pk.id <> BnlsWardenPacketId.FullServiceHandleWardenPacket Then
                        RaiseEvent Fail(New IO.IOException("Incorrect packet type received from server."))
                        Return False.Futurize
                    End If

                    Dim vals = CType(pk.payload, IPickle(Of Dictionary(Of String, Object))).Value
                    If CUInt(vals("cookie")) <> cookie Then
                        RaiseEvent Fail(New IO.IOException("Incorrect cookie from server."))
                        Return False.Futurize
                    ElseIf CUInt(vals("result")) <> 0 Then
                        RaiseEvent Fail(New IO.IOException("Server indicated there was a failure."))
                        Return False.Futurize
                    End If

                    RaiseEvent Send(CType(vals("data"), Byte()))
                    Return True.Futurize
                End Function
            )
        End Sub

        Public Shared Function FutureConnectToBnlsServer(ByVal hostname As String,
                                                         ByVal port As UShort,
                                                         ByVal seed As UInteger,
                                                         Optional ByVal logger As Logger = Nothing) As IFuture(Of PossibleException(Of BnlsClient))
            Contract.Requires(hostname IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of PossibleException(Of BnlsClient)))() IsNot Nothing)
            logger = If(logger, New Logger())

            Dim f = New Future(Of PossibleException(Of BnlsClient))
            FutureConnectTo(hostname, port).CallWhenValueReady(
                Sub(result)
                    If result.Exception IsNot Nothing Then
                        f.SetValue(result.Exception)
                        Return
                    End If

                    Dim cookie = seed
                    Dim socket = New PacketSocket(result.Value,
                                                  timeout:=5.Minutes,
                                                  numByteBeforeSize:=0,
                                                  numSizeBytes:=2,
                                                  logger:=logger)
                    socket.Name = "BNLS"

                    Dim writeException = WritePacket(socket, logger, BnlsWardenPacket.MakeFullServiceConnect(seed, seed))
                    If writeException IsNot Nothing Then
                        f.SetValue(writeException)
                        socket.Disconnect("server error")
                        Return
                    End If

                    FutureReadPacket(socket, logger).CallWhenValueReady(
                        Sub(packetResult)
                            If packetResult.Exception IsNot Nothing Then
                                f.SetValue(packetResult.Exception)
                                Return
                            End If

                            Dim pk = packetResult.Value
                            If pk.id <> BnlsWardenPacketId.FullServiceConnect Then
                                f.SetValue(New IO.IOException("Incorrect usage mode from server."))
                                socket.Disconnect("server error")
                                Return
                            End If

                            Dim vals = CType(pk.payload, IPickle(Of Dictionary(Of String, Object))).Value
                            If CUInt(vals("cookie")) <> cookie Then
                                f.SetValue(New IO.IOException("Incorrect cookie from server."))
                                socket.Disconnect("server error")
                                Return
                            End If

                            f.SetValue(New BnlsClient(socket, cookie, logger))
                        End Sub
                    )
                End Sub
            )
            Return f
        End Function

        Private Shared Function WritePacket(ByVal socket As PacketSocket,
                                            ByVal logger As Logger,
                                            ByVal pk As BnlsWardenPacket) As Exception
            Contract.Requires(pk IsNot Nothing)

            Try
                logger.Log(Function() "Sending {0} to {1}".Frmt(pk.id, socket.Name), LogMessageTypes.DataEvent)
                logger.Log(pk.payload.Description, LogMessageTypes.DataParsed)
                socket.WritePacket(Concat(Of Byte)({0, 0, BnlsPacketId.Warden, pk.id}, pk.payload.Data.ToArray))
                Return Nothing

            Catch e As Exception
                If Not (TypeOf e Is SocketException OrElse
                        TypeOf e Is ObjectDisposedException OrElse
                        TypeOf e Is IO.IOException OrElse
                        TypeOf e Is PicklingException) Then
                    LogUnexpectedException("Error sending {0} to {1}.".Frmt(pk.id, socket.Name), e)
                End If
                logger.Log("Error sending {0} for {1}: {2}".Frmt(pk.id, socket.Name, e), LogMessageTypes.Problem)
                Return e
            End Try
        End Function

        Private Shared Function FutureReadPacket(ByVal socket As PacketSocket, ByVal logger As Logger) As IFuture(Of PossibleException(Of BnlsWardenPacket))
            Dim f = New Future(Of PossibleException(Of BnlsWardenPacket))
            socket.FutureReadPacket().CallWhenValueReady(
                Sub(result)
                    If result.Exception IsNot Nothing Then
                        f.SetValue(result.Exception)
                        Return
                    End If
                    If result.Value.Length < 3 OrElse result.Value(2) <> BnlsPacketId.Warden Then
                        f.SetValue(New IO.IOException("Not a bnls warden server."))
                        Return
                    End If
                    Try
                        Dim pk = BnlsWardenPacket.FromServerData(result.Value.SubView(3))
                        logger.Log(Function() "Received {0} from {1}".Frmt(pk.id, socket.Name), LogMessageTypes.DataEvent)
                        logger.Log(pk.payload.Description, LogMessageTypes.DataParsed)
                        f.SetValue(pk)
                    Catch e As Exception
                        f.SetValue(e)
                    End Try
                End Sub
            )
            Return f
        End Function

        Public Sub ProcessWardenPacket(ByVal data As ViewableList(Of Byte))
            WritePacket(socket, logger, BnlsWardenPacket.MakeFullServiceHandleWardenPacket(cookie, data.ToArray))
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            socket.Disconnect("Disposed")
            GC.SuppressFinalize(Me)
        End Sub
    End Class
End Namespace