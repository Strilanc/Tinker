Imports System.Net.Sockets

Namespace CKL
    '''<summary>Provides answers to bnet cd key authentication challenges, allowing clients to login to bnet once with the server's keys.</summary>
    Public Class CklServer
        Public Const PACKET_ID As Byte = 1

        Public ReadOnly name As String
        Protected WithEvents accepter As New ConnectionAccepter()
        Protected ReadOnly logger As New Logger()
        Protected ReadOnly keys As New List(Of CklKey)
        Protected ReadOnly ref As ICallQueue = New ThreadPooledCallQueue
        Private keyIndex As Integer
        Private ReadOnly portHandle As PortPool.PortHandle
        Public Event KeyAdded(ByVal sender As CklServer, ByVal key As CklKey)
        Public Event KeyRemoved(ByVal sender As CklServer, ByVal key As CklKey)

        Public Sub New(ByVal name As String,
                       ByVal listenPort As PortPool.PortHandle)
            Me.new(name, listenPort.port)
            Me.portHandle = listenPort
        End Sub
        Public Sub New(ByVal name As String,
                       ByVal listenPort As UShort)
            Me.name = name
            Dim out = accepter.OpenPort(listenPort)
            If Not out.succeeded Then Throw New Exception(out.message)
        End Sub

        Public Function AddKey(ByVal name As String, ByVal rocKey As String, ByVal tftKey As String) As IFuture(Of Outcome)
            Return ref.QueueFunc(
                Function()
                    If (From k In keys Where k.name.ToLower() = name.ToLower()).Any Then  Return failure("A key with the name '{0}' already exists.".frmt(name))
                    Dim key = New CklKey(name, rocKey, tftKey)
                    keys.Add(key)
                    RaiseEvent KeyAdded(Me, key)
                    Return success("Key '{0}' added.".frmt(name))
                End Function
            )
        End Function
        Public Function RemoveKey(ByVal name As String) As IFuture(Of Outcome)
            Return ref.QueueFunc(
                Function()
                    Dim key = (From k In keys Where k.name.ToLower() = name.ToLower()).FirstOrDefault
                    If key Is Nothing Then  Return failure("No key found with the name '{0}'.".frmt(name))
                    keys.Remove(key)
                    RaiseEvent KeyRemoved(Me, key)
                    Return success("Key '{0}' removed.".frmt(name))
                End Function
            )
        End Function

        Private Sub c_AcceptedConnection(ByVal sender As ConnectionAccepter, ByVal accepted_client As System.Net.Sockets.TcpClient) Handles accepter.AcceptedConnection
            Dim socket = New BnetSocket(accepted_client, New TimeSpan(0, 0, 10), Me.logger)
            AddHandler socket.ReceivedPacket, AddressOf c_ReceivedPacket
            socket.SetReading(True)
            logger.log("Connection from " + socket.Name, LogMessageTypes.Positive)
        End Sub

        Public Overridable Sub [stop]()
            accepter.CloseAllPorts()
            portHandle.Dispose()
        End Sub

        Private Sub c_ReceivedPacket(ByVal sender As BnetSocket,
                                     ByVal flag As Byte,
                                     ByVal id As Byte,
                                     ByVal data As IViewableList(Of Byte))
            ref.QueueAction(
                Sub()
                    Dim response_data As Byte() = Nothing
                    Dim error_message As String = Nothing
                    If flag <> PACKET_ID Then
                        error_message = "Invalid header id."
                    Else
                        Select Case CType(id, CklPacketId)
                            Case CklPacketId.error
                                'ignore
                            Case CklPacketId.keys
                                If keys.Count <= 0 Then
                                    error_message = "No keys to lend."
                                ElseIf data.Length <> 8 Then
                                    error_message = "Invalid length. Require client token [4] + server token [4]."
                                Else
                                    If keyIndex >= keys.Count Then  keyIndex = 0
                                    response_data = keys(keyIndex).Pack(clientToken:=data.SubView(0, 4),
                                                                        serverToken:=data.SubView(4, 4))
                                    logger.log("Provided key '{0}' to {1}".frmt(keys(keyIndex).name, sender.Name), LogMessageTypes.Positive)
                                    keyIndex += 1
                                End If
                            Case Else
                                error_message = "Invalid packet id."
                        End Select
                    End If

                    If response_data IsNot Nothing Then
                        sender.Write(New Byte() {PACKET_ID, id}, response_data)
                    End If
                    If error_message IsNot Nothing Then
                        logger.log("Error parsing data from client: " + error_message, LogMessageTypes.Negative)
                        sender.Write(New Byte() {PACKET_ID, CklPacketId.error}, System.Text.UTF8Encoding.UTF8.GetBytes(error_message))
                    End If
                End Sub
            )
        End Sub
    End Class
End Namespace
