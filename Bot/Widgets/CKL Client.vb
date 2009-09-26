Namespace CKL
    '''<summary>Asynchronously connects to a CKLServer and requests a response to a cd key authentication challenge from bnet.</summary>
    Public Class CklClient
        Public Shared Function BeginBorrowKeys(ByVal remoteHost As String,
                                               ByVal remotePort As UShort,
                                               ByVal clientCdKeySalt As Byte(),
                                               ByVal serverCdKeySalt As Byte()) As IFuture(Of CklEncodedKey)
            Contract.Requires(remoteHost IsNot Nothing)
            Contract.Requires(clientCdKeySalt IsNot Nothing)
            Contract.Requires(serverCdKeySalt IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of CklEncodedKey))() IsNot Nothing)
            Dim requestPacket = Concat({CklServer.PACKET_ID, CklPacketId.keys, 0, 0},
                                       clientCdKeySalt,
                                       serverCdKeySalt)

            'Connect to CKL server and send request
            Dim futureSocket = FutureCreateConnectedTcpClient(remoteHost, remotePort).Select(
                Function(tcpClient)
                    Dim socket = New PacketSocket(tcpClient, timeout:=10.Seconds)
                    socket.WritePacket(requestPacket)
                    Return socket
                End Function)

            'Read response
            Dim futureReadPacket = futureSocket.Select(
                Function(socket) socket.FutureReadPacket()
            ).Defuturized

            'Process response
            Dim futureKeys = futureReadPacket.Select(
                Function(packetData) As CklEncodedKey
                    futureSocket.Value.Disconnect(expected:=True, reason:="Received response")

                    'Read header
                    Contract.Assume(packetData.Length >= 4)
                    Dim flag = packetData(0)
                    Dim id = packetData(1)
                    If flag <> CklServer.PACKET_ID Then
                        Throw New IO.IOException("Incorrect header id in data returned from CKL server.")
                    End If

                    'Read body
                    Dim body = packetData.SubView(4)
                    Select Case CType(id, CklPacketId)
                        Case CklPacketId.error
                            Throw New IO.IOException("CKL server returned an error: {0}.".Frmt(System.Text.UTF8Encoding.UTF8.GetString(body.ToArray)))
                        Case CklPacketId.keys
                            Dim keyLength = body.Length \ 2
                            Dim rocKeyData = body.SubView(0, keyLength).ToArray
                            Dim tftKeyData = body.SubView(keyLength, keyLength).ToArray
                            Return New CklEncodedKey(Bnet.BnetPacket.CDKeyJar.packBorrowedCdKey(rocKeyData),
                                                     Bnet.BnetPacket.CDKeyJar.packBorrowedCdKey(tftKeyData))
                        Case Else
                            Throw New IO.IOException("Incorrect packet id in data returned from CKL server.")
                    End Select
                End Function)

            Return futureKeys
        End Function
    End Class
End Namespace