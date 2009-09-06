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
            Me.new(name, listenPort.Port)
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

        Private Sub c_AcceptedConnection(ByVal sender As ConnectionAccepter, ByVal acceptedClient As Net.Sockets.TcpClient) Handles accepter.AcceptedConnection
            Dim socket = New PacketSocket(acceptedClient, 10.Seconds, Me.logger)
            FutureIterate(AddressOf socket.FutureReadPacket, Function(result) ref.QueueFunc(
                Function()
                    If result.Exception IsNot Nothing Then  Return False
                    Dim flag = result.Value(0)
                    Dim id = result.Value(0)
                    Dim data = result.Value.SubView(4)
                    Dim responseData As Byte() = Nothing
                    Dim errorMessage As String = Nothing
                    If flag <> PACKET_ID Then
                        errorMessage = "Invalid header id."
                    Else
                        Select Case CType(id, CklPacketId)
                            Case CklPacketId.error
                                'ignore
                            Case CklPacketId.keys
                                If keys.Count <= 0 Then
                                    errorMessage = "No keys to lend."
                                ElseIf data.Length <> 8 Then
                                    errorMessage = "Invalid length. Require client token [4] + server token [4]."
                                Else
                                    If keyIndex >= keys.Count Then  keyIndex = 0
                                    responseData = keys(keyIndex).Pack(clientToken:=data.SubView(0, 4),
                                                                        serverToken:=data.SubView(4, 4))
                                    logger.log("Provided key '{0}' to {1}".frmt(keys(keyIndex).name, socket.Name), LogMessageType.Positive)
                                    keyIndex += 1
                                End If
                            Case Else
                                errorMessage = "Invalid packet id."
                        End Select
                    End If

                    If responseData IsNot Nothing Then
                        socket.WritePacket(Concat({PACKET_ID, id, 0, 0}, responseData))
                    End If
                    If errorMessage IsNot Nothing Then
                        logger.log("Error parsing data from client: " + errorMessage, LogMessageType.Negative)
                        socket.WritePacket(Concat({PACKET_ID, CklPacketId.error}, System.Text.UTF8Encoding.UTF8.GetBytes(errorMessage)))
                    End If
                    Return True
                End Function
            ))
            logger.log("Connection from " + socket.Name, LogMessageType.Positive)
        End Sub

        Public Overridable Sub [stop]()
            accepter.CloseAllPorts()
            portHandle.Dispose()
        End Sub
    End Class
End Namespace
