Namespace CKL
    '''<summary>Connects to a CKLServer and requests a response to a cd key authentication challenge from bnet.</summary>
    Public Class CklClient
        Public Shared Function BeginBorrowKeys(ByVal remoteHost As String,
                                               ByVal remotePort As UShort,
                                               ByVal clientCdKeySalt As Byte(),
                                               ByVal serverCdKeySalt As Byte()) As IFuture(Of Outcome(Of CklEncodedKey))
            Contract.Requires(remoteHost IsNot Nothing)
            Contract.Requires(clientCdKeySalt IsNot Nothing)
            Contract.Requires(serverCdKeySalt IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome(Of CklEncodedKey)))() IsNot Nothing)

            Dim ref = New ThreadPooledCallQueue
            Dim future = New Future(Of Outcome(Of CklEncodedKey))
            Dim payload = Concat({CklServer.PACKET_ID, CklPacketId.keys, 0, 0}, clientCdKeySalt, serverCdKeySalt)
            Dim socket As PacketSocket = Nothing

            'prepare timeout
            FutureWait(10.Seconds).QueueCallWhenReady(ref,
                Sub()
                    If future.TrySetValue(failure("CKL request timed out.")) Then
                        If socket IsNot Nothing Then  socket.Disconnect("timeout")
                    End If
                End Sub
            )

            'prepare connect and receive
            FutureConnectTo(remoteHost, remotePort).QueueCallWhenValueReady(ref,
                Sub(result)
                    If future.IsReady Then
                        If result.Value IsNot Nothing Then  result.Value.Close()
                    ElseIf result.Exception IsNot Nothing Then
                        future.SetValue(Failure("Failed to connect: {0}".Frmt(result.Exception.Message)))
                    Else
                        socket = New PacketSocket(result.Value, timeout:=10.Seconds)
                        socket.WritePacket(payload)
                        socket.FutureReadPacket.QueueCallWhenValueReady(ref,
                            Sub(result2)
                                Try
                                    If result2.Exception IsNot Nothing Then  Throw result2.Exception
                                    Dim flag = result2.Value(0)
                                    Dim id = result2.Value(1)
                                    Dim data = result2.Value.SubView(4)
                                    If future.IsReady Then  Return

                                    socket.Disconnect("Received response")

                                    If flag <> CklServer.PACKET_ID Then
                                        future.SetValue(Failure("Incorrect header id in data returned from CKL server."))
                                        Return
                                    End If
                                    Select Case CType(id, CklPacketId)
                                        Case CklPacketId.error
                                            'error
                                            future.SetValue(Failure("CKL server returned an error: {0}.".Frmt(System.Text.UTF8Encoding.UTF8.GetString(data.ToArray))))
                                        Case CklPacketId.keys
                                            'success
                                            Dim keyLength = data.Length \ 2
                                            Dim rocKeyData = data.SubView(0, keyLength).ToArray
                                            Dim tftKeyData = data.SubView(keyLength, keyLength).ToArray
                                            Dim kv = New CklEncodedKey(Bnet.BnetPacket.CDKeyJar.packBorrowedCdKey(rocKeyData), Bnet.BnetPacket.CDKeyJar.packBorrowedCdKey(tftKeyData))
                                            future.SetValue(Success(kv, "Succesfully borrowed keys from CKL server."))
                                        Case Else
                                            'unknown
                                            future.SetValue(Failure("Incorrect packet id in data returned from CKL server."))
                                    End Select

                                Catch e As Exception
                                    future.SetValue(Failure("Error borrowing keys from CKL server: {0}.".Frmt(e.ToString)))
                                End Try
                            End Sub
                        )
                    End If
                End Sub
            )

            Return future
        End Function
    End Class
End Namespace