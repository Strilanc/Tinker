Namespace CKL
    '''<summary>Asynchronously connects to a CKLServer and requests a response to a cd key authentication challenge from bnet.</summary>
    Public NotInheritable Class CKLClient
        Private Shared ReadOnly jar As New Bnet.Packet.ProductCredentialsJar("authentication")

        Private Sub New()
        End Sub
        Public Shared Function AsyncBorrowCredentials(ByVal remoteHost As String,
                                                      ByVal remotePort As UShort,
                                                      ByVal clientCDKeySalt As UInt32,
                                                      ByVal serverCDKeySalt As UInt32) As IFuture(Of WC3CredentialPair)
            Contract.Requires(remoteHost IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of WC3CredentialPair))() IsNot Nothing)
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
                Function(packetData)
                    futureSocket.Value.Disconnect(expected:=True, reason:="Received response")

                    'Read header
                    Contract.Assume(packetData.Count >= 4)
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
                            Dim rocAuthentication = jar.Parse(body.SubView(0, body.Count \ 2)).Value
                            Dim tftAuthentication = jar.Parse(body.SubView(body.Count \ 2)).Value
                            If rocAuthentication.Product <> Bnet.ProductType.Warcraft3ROC _
                                        OrElse tftAuthentication.Product <> Bnet.ProductType.Warcraft3TFT Then
                                Throw New IO.InvalidDataException("CKL server returned invalid credentials.")
                            End If
                            Return New WC3CredentialPair(rocAuthentication, tftAuthentication)
                        Case Else
                            Throw New IO.InvalidDataException("Incorrect packet id in data returned from CKL server.")
                    End Select
                End Function)

            Return futureKeys
        End Function
    End Class
End Namespace
