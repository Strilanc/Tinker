Imports Tinker.Bnet

Namespace CKL
    '''<summary>Provides answers to bnet cd key authentication challenges, allowing clients to login to bnet once with the server's keys.</summary>
    Public NotInheritable Class Server
        Private Shared ReadOnly jar As New Bnet.Packet.ProductCredentialsJar("ckl")
        Public Const PacketPrefixValue As Byte = 1

        Private ReadOnly inQueue As ICallQueue = New TaskedCallQueue
        Private ReadOnly outQueue As ICallQueue = New TaskedCallQueue

        Public ReadOnly name As InvariantString
        Private WithEvents Accepter As New ConnectionAccepter()
        Private ReadOnly logger As New Logger()
        Private ReadOnly keys As New AsyncViewableCollection(Of CKL.KeyEntry)(outQueue:=outQueue)
        Private keyIndex As Integer
        Private ReadOnly portHandle As PortPool.PortHandle

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(Accepter IsNot Nothing)
            Contract.Invariant(logger IsNot Nothing)
            Contract.Invariant(keys IsNot Nothing)
            Contract.Invariant(inQueue IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As InvariantString,
                       ByVal listenPort As PortPool.PortHandle)
            Contract.Assume(listenPort IsNot Nothing)
            Me.name = name
            Me.portHandle = listenPort
            Me.Accepter.OpenPort(listenPort.Port)
        End Sub
        Public Sub New(ByVal name As InvariantString,
                       ByVal listenPort As UShort)
            Me.name = name
            Me.Accepter.OpenPort(listenPort)
        End Sub

        Public Function AddKey(ByVal keyName As InvariantString, ByVal cdKeyROC As String, ByVal cdKeyTFT As String) As IFuture
            Contract.Requires(cdKeyROC IsNot Nothing)
            Contract.Requires(cdKeyTFT IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ifuture)() IsNot Nothing)
            Return inQueue.QueueAction(
                Sub()
                    If (From k In keys Where k.Name = keyName).Any Then
                        Throw New InvalidOperationException("A key with the name '{0}' already exists.".Frmt(keyName))
                    End If
                    If cdKeyROC.ToWC3CDKeyCredentials({}, {}).Product <> ProductType.Warcraft3ROC Then Throw New ArgumentException("Not a ROC cd key.", "cdKeyROC")
                    If cdKeyTFT.ToWC3CDKeyCredentials({}, {}).Product <> ProductType.Warcraft3TFT Then Throw New ArgumentException("Not a TFT cd key.", "cdKeyTFT")
                    Dim key = New CKL.KeyEntry(keyName, cdKeyROC, cdKeyTFT)
                    keys.Add(key)
                End Sub
            )
        End Function
        Public Function RemoveKey(ByVal keyName As InvariantString) As IFuture
            Contract.Ensures(Contract.Result(Of ifuture)() IsNot Nothing)
            Return inQueue.QueueAction(
                Sub()
                    Dim key = (From k In keys Where k.Name = keyName).FirstOrDefault
                    If key Is Nothing Then Throw New InvalidOperationException("No key found with the name '{0}'.".Frmt(keyName))
                    keys.Remove(key)
                End Sub
            )
        End Function

        Private Sub OnAcceptedConnection(ByVal sender As ConnectionAccepter,
                                         ByVal acceptedClient As Net.Sockets.TcpClient) Handles Accepter.AcceptedConnection
            Contract.Requires(sender IsNot Nothing)
            Contract.Requires(acceptedClient IsNot Nothing)
            Contract.Assume(acceptedClient.Client IsNot Nothing)
            Dim socket = New PacketSocket(stream:=acceptedClient.GetStream,
                                          localendpoint:=CType(acceptedClient.Client.LocalEndPoint, Net.IPEndPoint),
                                          remoteendpoint:=CType(acceptedClient.Client.RemoteEndPoint, Net.IPEndPoint),
                                          timeout:=10.Seconds,
                                          logger:=Me.logger)
            logger.Log("Connection from {0}.".Frmt(socket.Name), LogMessageType.Positive)

            AsyncProduceConsumeUntilError(
                producer:=AddressOf socket.AsyncReadPacket,
                consumer:=Function(packetData) inQueue.QueueAction(
                    Sub()
                        Dim flag = packetData(0)
                        Dim id = packetData(0)
                        Dim data = packetData.SubView(4)
                        Dim responseData As Byte() = Nothing
                        Dim errorMessage As String = Nothing
                        If flag <> PacketPrefixValue Then
                            errorMessage = "Invalid header id."
                        Else
                            Select Case CType(id, CKLPacketId)
                                Case CKLPacketId.[Error]
                                    'ignore
                                Case CKLPacketId.Keys
                                    If keys.Count <= 0 Then
                                        errorMessage = "No keys to lend."
                                    ElseIf data.Count <> 8 Then
                                        errorMessage = "Invalid length. Require client token [4] + server token [4]."
                                    Else
                                        If keyIndex >= keys.Count Then keyIndex = 0
                                        Dim credentials = keys(keyIndex).GenerateCredentials(clientToken:=data.SubView(0, 4).ToUInt32,
                                                                                             serverToken:=data.SubView(4, 4).ToUInt32)
                                        responseData = {jar.Pack(credentials.AuthenticationROC).Data,
                                                        jar.Pack(credentials.AuthenticationTFT).Data
                                                       }.Fold.ToArray
                                        logger.Log("Provided key '{0}' to {1}".Frmt(keys(keyIndex).Name, socket.Name), LogMessageType.Positive)
                                        keyIndex += 1
                                    End If
                                Case Else
                                    errorMessage = "Invalid packet id."
                            End Select
                        End If

                        If responseData IsNot Nothing Then
                            socket.WritePacket(Concat({PacketPrefixValue, id, 0, 0}, responseData))
                        End If
                        If errorMessage IsNot Nothing Then
                            logger.Log("Error parsing data from client: " + errorMessage, LogMessageType.Negative)
                            socket.WritePacket(Concat({PacketPrefixValue, CKLPacketId.[Error]}, System.Text.UTF8Encoding.UTF8.GetBytes(errorMessage)))
                        End If
                    End Sub),
                errorHandler:=Sub(exception) logger.Log("Error receiving from {0}: {1}".Frmt(socket.Name, exception.Message), LogMessageType.Problem)
            )
        End Sub

        Public Sub [Stop]()
            Accepter.CloseAllPorts()
            If portHandle IsNot Nothing Then portHandle.Dispose()
        End Sub
    End Class
End Namespace
