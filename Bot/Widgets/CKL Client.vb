Namespace CKL
    '''<summary>Asynchronously connects to a CKLServer and requests a response to a cd key authentication challenge from bnet.</summary>
    Public NotInheritable Class CKLClient
        Private Sub New()
        End Sub
        Public Shared Function AsyncBorrowKeys(ByVal remoteHost As String,
                                               ByVal remotePort As UShort,
                                               ByVal clientCDKeySalt As UInt32,
                                               ByVal serverCDKeySalt As UInt32) As IFuture(Of CKLEncodedKey)
            Contract.Requires(remoteHost IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of CKLEncodedKey))() IsNot Nothing)
            Dim requestPacket = Concat({CKLServer.PacketPrefixValue, CKLPacketId.Keys, 0, 0},
                                       clientCDKeySalt.Bytes,
                                       serverCDKeySalt.Bytes)

            'Connect to CKL server and send request
            Dim futureSocket = AsyncTcpConnect(remoteHost, remotePort).Select(
                Function(tcpClient)
                    Dim socket = New PacketSocket(stream:=tcpClient.GetStream,
                                                  localendpoint:=CType(tcpClient.Client.LocalEndPoint, Net.IPEndPoint),
                                                  remoteendpoint:=CType(tcpClient.Client.RemoteEndPoint, Net.IPEndPoint),
                                                  timeout:=10.Seconds)
                    socket.WritePacket(requestPacket)
                    Return socket
                End Function)

            'Read response
            Dim futureReadPacket = futureSocket.Select(
                Function(socket) socket.AsyncReadPacket()
            ).Defuturized

            'Process response
            Dim futureKeys = futureReadPacket.Select(
                Function(packetData) As CKLEncodedKey
                    futureSocket.Value.Disconnect(expected:=True, reason:="Received response")

                    'Read header
                    Contract.Assume(packetData.Length >= 4)
                    Dim flag = packetData(0)
                    Dim id = packetData(1)
                    If flag <> CKLServer.PacketPrefixValue Then
                        Throw New IO.InvalidDataException("Incorrect header id in data returned from CKL server.")
                    End If

                    'Read body
                    Dim body = packetData.SubView(4)
                    Select Case CType(id, CKLPacketId)
                        Case CKLPacketId.[Error]
                            Throw New IO.IOException("CKL server returned an error: {0}.".Frmt(System.Text.UTF8Encoding.UTF8.GetString(body.ToArray)))
                        Case CKLPacketId.Keys
                            Dim keyLength = body.Length \ 2
                            Dim rocKeyData = body.SubView(0, keyLength).ToArray
                            Dim tftKeyData = body.SubView(keyLength, keyLength).ToArray
                            Return New CKLEncodedKey(Bnet.Packet.CDKeyJar.PackBorrowedCDKey(rocKeyData),
                                                     Bnet.Packet.CDKeyJar.PackBorrowedCDKey(tftKeyData))
                        Case Else
                            Throw New IO.InvalidDataException("Incorrect packet id in data returned from CKL server.")
                    End Select
                End Function)

            Return futureKeys
        End Function
    End Class
End Namespace