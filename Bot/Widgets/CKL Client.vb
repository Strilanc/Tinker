Namespace CKL
    '''<summary>Connects to a CKLServer and requests a response to a cd key authentication challenge from bnet.</summary>
    Public Class CklClient
        Inherits NotifyingDisposable

        Private socket As BnetSocket
        Private ReadOnly future As Future(Of Outcome(Of CklEncodedKey))
        Private ReadOnly ref As ICallQueue
        Private ReadOnly payload As Byte()
        Private ReadOnly timeoutTimer As Timers.Timer

        <ContractInvariantMethod()> Protected Sub Invariant()
            Contract.Invariant(future IsNot Nothing)
            Contract.Invariant(ref IsNot Nothing)
            Contract.Invariant(payload IsNot Nothing)
            Contract.Invariant(timeoutTimer IsNot Nothing)
        End Sub

        Public Shared Function BeginBorrowKeys(ByVal remoteHost As String,
                                               ByVal remotePort As UShort,
                                               ByVal clientCdKeySalt As Byte(),
                                               ByVal serverCdKeySalt As Byte()) As IFuture(Of Outcome(Of CklEncodedKey))
            Contract.Requires(remoteHost IsNot Nothing)
            Contract.Requires(clientCdKeySalt IsNot Nothing)
            Contract.Requires(serverCdKeySalt IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome(Of CklEncodedKey)))() IsNot Nothing)
            Return New CklClient(remoteHost, remotePort, clientCdKeySalt, serverCdKeySalt).future
        End Function

        Private Sub New(ByVal remoteHost As String,
                        ByVal remotePort As UShort,
                        ByVal clientCdKeySalt As Byte(),
                        ByVal serverCdKeySalt As Byte())
            Contract.Requires(remoteHost IsNot Nothing)
            Contract.Requires(clientCdKeySalt IsNot Nothing)
            Contract.Requires(serverCdKeySalt IsNot Nothing)

            Me.ref = New ThreadPooledCallQueue
            Me.future = New Future(Of Outcome(Of CklEncodedKey))

            payload = Concat({New Byte() {CklServer.PACKET_ID, CklPacketId.keys, 0, 0}, clientCdKeySalt, serverCdKeySalt})
            Me.timeoutTimer = New Timers.Timer()
            timeoutTimer.Interval = 10000
            timeoutTimer.AutoReset = False
            AddHandler timeoutTimer.Elapsed, AddressOf c_timeout
            timeoutTimer.Start()

            FutureConnectTo(remoteHost, remotePort).CallWhenValueReady(
                Sub(socketOutcome)
                                                                           ref.QueueAction(
                                                                               Sub()
                                                                                   If future.IsReady Then
                                                                                       If socketOutcome.succeeded Then  socketOutcome.val.Close()
                                                                                       Dispose()
                                                                                       Return
                                                                                   End If

                                                                                   If Not socketOutcome.succeeded Then
                                                                                       future.SetValue(CType(socketOutcome, Outcome))
                                                                                       Dispose()
                                                                                       Return
                                                                                   End If

                                                                                   Me.socket = New BnetSocket(socketOutcome.val, New TimeSpan(0, 0, 10))
                                                                                   AddHandler socket.ReceivedPacket, AddressOf c_ReceivedPacket
                                                                                   Me.socket.SetReading(True)
                                                                                   Me.socket.WriteWithMode(payload, PacketStream.InterfaceModes.IncludeSizeBytes)
                                                                               End Sub
                                                                           )
                                                                       End Sub
            )
        End Sub

        Protected Overrides Sub PerformDispose()
            ref.QueueAction(
                Sub()
                    timeoutTimer.Dispose()
                    If socket IsNot Nothing Then  socket.Disconnect("Disposed")
                    future.TrySetValue(failure("CKL Client Disposed"))
                End Sub
            )
        End Sub

        Private Sub c_timeout(ByVal sender As Object, ByVal e As Timers.ElapsedEventArgs)
            ref.QueueAction(
                Sub()
                    timeoutTimer.Stop()

                    If future.IsReady Then  Return
                    If socket IsNot Nothing Then  socket.Disconnect("timeout")
                    future.SetValue(failure("CKL request timed out."))
                    Dispose()
                End Sub
            )
        End Sub

        '''<summary>Finishes connecting to the server and requests keys.</summary>
        Private Sub c_ReceivedPacket(ByVal sender As BnetSocket,
                                     ByVal flag As Byte,
                                     ByVal id As Byte,
                                     ByVal data As IViewableList(Of Byte))
            ref.QueueAction(
                Sub()
                    Try
                        If future.IsReady Then  Return

                        socket.Disconnect("Received response")

                        If flag <> CklServer.PACKET_ID Then
                            future.SetValue(failure("Incorrect header id in data returned from CKL server."))
                            Return
                        End If
                        Select Case CType(id, CklPacketId)
                            Case CklPacketId.error
                                'error
                                future.SetValue(failure("CKL server returned an error: {0}.".frmt(System.Text.UTF8Encoding.UTF8.GetString(data.ToArray))))
                            Case CklPacketId.keys
                                'success
                                Dim key_len = data.Length \ 2
                                Dim roc_data = data.SubView(0, key_len).ToArray
                                Dim tft_data = data.SubView(key_len, key_len).ToArray
                                Dim kv = New CklEncodedKey(Bnet.BnetPacket.CDKeyJar.packBorrowedCdKey(roc_data), Bnet.BnetPacket.CDKeyJar.packBorrowedCdKey(tft_data))
                                future.SetValue(successVal(kv, "Succesfully borrowed keys from CKL server."))
                            Case Else
                                'unknown
                                future.SetValue(failure("Incorrect packet id in data returned from CKL server."))
                        End Select

                    Catch e As Exception
                        future.SetValue(failure("Error borrowing keys from CKL server: {0}.".frmt(e.Message)))
                    End Try

                    Dispose()
                End Sub
            )
        End Sub
    End Class
End Namespace