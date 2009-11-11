Imports System.Net.Sockets

Namespace CKL
    '''<summary>Provides answers to bnet cd key authentication challenges, allowing clients to login to bnet once with the server's keys.</summary>
    Public Class CKLServer
        Public Const PacketPrefixValue As Byte = 1

        Public ReadOnly name As String
        Protected WithEvents Accepter As New ConnectionAccepter()
        Protected ReadOnly logger As New Logger()
        Protected ReadOnly keys As New List(Of CKLKey)
        Protected ReadOnly ref As ICallQueue = New TaskedCallQueue
        Private keyIndex As Integer
        Private ReadOnly portHandle As PortPool.PortHandle
        Public Event KeyAdded(ByVal sender As CKLServer, ByVal key As CKLKey)
        Public Event KeyRemoved(ByVal sender As CKLServer, ByVal key As CKLKey)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(name IsNot Nothing)
            Contract.Invariant(Accepter IsNot Nothing)
            Contract.Invariant(logger IsNot Nothing)
            Contract.Invariant(keys IsNot Nothing)
            Contract.Invariant(ref IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As String,
                       ByVal listenPort As PortPool.PortHandle)
            Contract.Assume(name IsNot Nothing) 'bug in contracts required not using requires here
            Contract.Assume(listenPort IsNot Nothing)
            Me.name = name
            Me.portHandle = listenPort
            Me.Accepter.OpenPort(listenPort.Port)
        End Sub
        Public Sub New(ByVal name As String,
                       ByVal listenPort As UShort)
            Contract.Assume(name IsNot Nothing) 'bug in contracts required not using requires here
            Me.name = name
            Me.Accepter.OpenPort(listenPort)
        End Sub

        Public Function AddKey(ByVal keyName As String, ByVal cdKeyROC As String, ByVal cdKeyTFT As String) As IFuture
            Contract.Requires(keyName IsNot Nothing)
            Contract.Requires(cdKeyROC IsNot Nothing)
            Contract.Requires(cdKeyTFT IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ifuture)() IsNot Nothing)
            Return ref.QueueAction(
                Sub()
                    If (From k In keys Where k.Name.ToUpperInvariant = keyName.ToUpperInvariant).Any Then
                        Throw New InvalidOperationException("A key with the name '{0}' already exists.".Frmt(keyName))
                    End If
                    Dim key = New CKLKey(keyName, cdKeyROC, cdKeyTFT)
                    keys.Add(key)
                    RaiseEvent KeyAdded(Me, key)
                End Sub
            )
        End Function
        Public Function RemoveKey(ByVal keyName As String) As IFuture
            Contract.Requires(keyName IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ifuture)() IsNot Nothing)
            Return ref.QueueAction(
                Sub()
                    Dim key = (From k In keys
                               Where k.Name.ToUpperInvariant = keyName.ToUpperInvariant).
                               FirstOrDefault
                    If key Is Nothing Then  Throw New InvalidOperationException("No key found with the name '{0}'.".Frmt(keyName))
                    keys.Remove(key)
                    RaiseEvent KeyRemoved(Me, key)
                End Sub
            )
        End Function

        Private Sub OnAcceptedConnection(ByVal sender As ConnectionAccepter,
                                         ByVal acceptedClient As Net.Sockets.TcpClient) Handles Accepter.AcceptedConnection
            Contract.Requires(sender IsNot Nothing)
            Contract.Requires(acceptedClient IsNot Nothing)
            Dim socket = New PacketSocket(acceptedClient, 10.Seconds, Me.logger)
            FutureIterateExcept(AddressOf socket.FutureReadPacket, Sub(packetData) ref.QueueAction(
                Sub()
                    Contract.Assume(packetData IsNot Nothing)
                    Contract.Assume(packetData.Length >= 4)
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
                                ElseIf data.Length <> 8 Then
                                    errorMessage = "Invalid length. Require client token [4] + server token [4]."
                                Else
                                    If keyIndex >= keys.Count Then  keyIndex = 0
                                    responseData = keys(keyIndex).Pack(clientToken:=data.SubView(0, 4),
                                                                       serverToken:=data.SubView(4, 4)).ToArray
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
                End Sub
            ))
            logger.Log("Connection from " + socket.Name, LogMessageType.Positive)
        End Sub

        Public Overridable Sub [Stop]()
            Accepter.CloseAllPorts()
            If portHandle IsNot Nothing Then portHandle.Dispose()
        End Sub
    End Class
End Namespace
