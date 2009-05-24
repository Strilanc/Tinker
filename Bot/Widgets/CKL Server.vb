Imports System.Net.Sockets

Public Class BotCKLServer
    Inherits CKL.CKLServer
    Implements IBotWidget

    Public Const WIDGET_TYPE_NAME As String = "CKL Server"

    Private ReadOnly commander As Commands.UICommandSet(Of BotCKLServer)

    Public Event add_state_string(ByVal state As String, ByVal insert_at_top As Boolean) Implements IBotWidget.add_state_string
    Public Event clear_state_strings() Implements IBotWidget.clear_state_strings
    Public Event remove_state_string(ByVal state As String) Implements IBotWidget.remove_state_string

    Public Sub New(ByVal commander As Commands.UICommandSet(Of BotCKLServer), ByVal name As String, ByVal listen_port As UShort, ByVal roc_key As String, ByVal tft_key As String)
        MyBase.New(name, listen_port, roc_key, tft_key)
        Me.commander = commander
    End Sub

    Private Function get_logger() As MultiLogger Implements IBotWidget.logger
        Return logger
    End Function
    Private Function get_name() As String Implements IBotWidget.name
        Return name
    End Function
    Private Function type_name() As String Implements IBotWidget.type_name
        Return WIDGET_TYPE_NAME
    End Function

    Private Sub command(ByVal text As String) Implements IBotWidget.command
        If commander Is Nothing Then
            logger.log("No commands available for CKL Server.", LogMessageTypes.NegativeEvent)
        Else
            commander.processLocalText(Me, text, logger)
        End If
    End Sub

    Private Sub hooked() Implements IBotWidget.hooked
        For Each port In accepter.EnumPorts
            RaiseEvent add_state_string("port " + port.ToString(), False)
        Next port
        logger.log("Started.", LogMessageTypes.NegativeEvent)
    End Sub

    Public Overrides Sub [stop]() Implements IBotWidget.stop
        MyBase.[stop]()
        RaiseEvent clear_state_strings()
        logger.log("Stopped.", LogMessageTypes.NegativeEvent)
    End Sub
End Class

Namespace CKL
    Public Enum CKLPackets As Byte
        [error] = 0
        keys = 1
    End Enum

    Public Class CKLBorrowedKeyVals
        Public ReadOnly roc_key As Dictionary(Of String, Object)
        Public ReadOnly tft_key As Dictionary(Of String, Object)
        Public Sub New(ByVal roc_key As Dictionary(Of String, Object), ByVal tft_key As Dictionary(Of String, Object))
            Me.roc_key = roc_key
            Me.tft_key = tft_key
        End Sub
    End Class

    '''<summary>Connects to a CKLServer and requests a response to a cd key authentication challenge from bnet.</summary>
    Public Class CKLClient
        Private ReadOnly client_cd_key_salt As Byte()
        Private ReadOnly server_cd_key_salt As Byte()
        Private ReadOnly client As New TcpClient
        Private ReadOnly f As New Future(Of Outcome(Of CKLBorrowedKeyVals))
        Private ReadOnly lock As New Object
        Private WithEvents timeout As New Timers.Timer()

        '''<summary>Begins connecting to the specified server to answer the given challenge.</summary>
        Public Sub New(ByVal remote_host As String, _
                       ByVal remote_port As UShort, _
                       ByVal client_cd_key_salt As Byte(), _
                       ByVal server_cd_key_salt As Byte())
            Me.client_cd_key_salt = client_cd_key_salt
            Me.server_cd_key_salt = server_cd_key_salt
            client.BeginConnect(remote_host, remote_port, AddressOf catch_connect, Nothing)
            timeout.Interval = 10000
            timeout.Start()
        End Sub

        Private Sub timeout_Elapsed(ByVal sender As Object, ByVal e As System.Timers.ElapsedEventArgs) Handles timeout.Elapsed
            SyncLock lock
                timeout.Stop()
                If f.isReady Then Return
                client.Close()
                f.setValue(failure("CKL request timed out."))
            End SyncLock
        End Sub

        '''<summary>Returns a future of the outcome of borrowing keys from the server.</summary>
        Public Function future() As IFuture(Of Outcome(Of CKLBorrowedKeyVals))
            Return f
        End Function

        '''<summary>Finishes connecting to the server and requests keys.</summary>
        Private Sub catch_connect(ByVal ar As IAsyncResult)
            Try
                'connect
                If f.isReady Then Return
                client.EndConnect(ar)
                Dim cstream = New PacketStream(client.GetStream, 2, 2, PacketStream.InterfaceModes.IncludeSizeBytes, Nothing, Nothing)

                'request
                If f.isReady Then Return
                Dim data = concat(New Byte() {CKLServer.PACKET_ID, CKLPackets.keys, 0, 0}, client_cd_key_salt, server_cd_key_salt)
                cstream.Write(data, 0, data.Length)

                'retrieve
                If f.isReady Then Return
                ReDim data(0 To 4098 - 1)
                Dim n = cstream.Read(data, 0, data.Length)
                If f.isReady Then Return
                If n = 0 Then Throw New Exception("No data.")
                Dim payload = subArray(data, 4, n - 4)
                client.Close()

                'parse
                SyncLock lock
                    If f.isReady Then Return
                    If data(0) <> CKLServer.PACKET_ID Then
                        f.setValue(failure("Incorrect header id in data returned from CKL server."))
                        Return
                    End If
                    Select Case CType(data(1), CKLPackets)
                        Case CKLPackets.error
                            'error
                            f.setValue(failure("CKL server returned an error: {0}.".frmt(toChrString(payload))))
                            Return
                        Case CKLPackets.keys
                            'success
                            Dim key_len = payload.Length \ 2
                            Dim roc_data = subArray(payload, 0, key_len)
                            Dim tft_data = subArray(payload, key_len, key_len)
                            Dim kv = New CKLBorrowedKeyVals(BNET.BnetPacket.CDKeyJar.packBorrowedCdKey(roc_data), BNET.BnetPacket.CDKeyJar.packBorrowedCdKey(tft_data))
                            f.setValue(successVal(kv, "Succesfully borrowed keys from CKL server."))
                            Return
                        Case Else
                            'unknown
                            f.setValue(failure("Incorrect packet id in data returned from CKL server."))
                            Return
                    End Select
                End SyncLock

            Catch e As Exception
                SyncLock lock
                    If f.isReady Then Return
                    client.Close()
                    f.setValue(failure("Error borrowing keys from CKL server: {0}.".frmt(e.Message)))
                End SyncLock
                Return
            End Try
        End Sub
    End Class

    '''<summary>Provides answers to bnet cd key authentication challenges, allowing clients to login to bnet once with the server's keys.</summary>
    Public Class CKLServer
        Public Const PACKET_ID As Byte = 1

        Public ReadOnly name As String
        Protected WithEvents accepter As New ConnectionAccepter()
        Public ReadOnly logger As New MultiLogger()
        Private ReadOnly roc_key As String
        Private ReadOnly tft_key As String

        Public Sub New(ByVal name As String, ByVal listen_port As UShort, ByVal roc_key As String, ByVal tft_key As String)
            Me.name = name
            Me.roc_key = roc_key
            Me.tft_key = tft_key
            Dim out = accepter.OpenPort(listen_port)
            If out.outcome <> Outcomes.succeeded Then Throw New Exception(out.message)
        End Sub

        Private Sub accepter_accepted_connection(ByVal sender As ConnectionAccepter, ByVal accepted_client As System.Net.Sockets.TcpClient) Handles accepter.accepted_connection
            Dim handler = New CKLServerSocket(accepted_client, roc_key, tft_key, logger)
        End Sub

        Public Overridable Sub [stop]()
            accepter.CloseAllPorts()
        End Sub

        Public Class CKLServerSocket
            Public ReadOnly logger As MultiLogger
            Private WithEvents socket As BnetSocket
            Private ReadOnly cd_key_jar As New BNET.BnetPacket.CDKeyJar("cdkey data")
            Private ReadOnly roc_key As String
            Private ReadOnly tft_key As String

            Public Sub New(ByVal client As TcpClient, ByVal roc_key As String, ByVal tft_key As String, ByVal logger As MultiLogger)
                Me.roc_key = roc_key
                Me.tft_key = tft_key
                Me.logger = logger
                socket = New BnetSocket(client, Me.logger)
                socket.set_reading(True)
                logger.log("Connection from " + socket.name, LogMessageTypes.PositiveEvent)
            End Sub

            Private Sub socket_receivedPacket(ByVal sender As BnetSocket, ByVal flag As Byte, ByVal id As Byte, ByVal data As ImmutableArrayView(Of Byte)) Handles socket.receivedPacket
                Dim response_data As Byte() = Nothing
                Dim error_message As String = Nothing
                If flag <> PACKET_ID Then
                    error_message = "Invalid header id."
                Else
                    Select Case CType(id, CKLPackets)
                        Case CKLPackets.error
                            'ignore
                        Case CKLPackets.keys
                            If data.length = 8 Then
                                Dim clientToken = data.SubView(0, 4)
                                Dim serverToken = data.SubView(4, 4)
                                Dim roc_data = cd_key_jar.pack(BNET.BnetPacket.CDKeyJar.packCDKey(roc_key, clientToken, serverToken)).getData.ToArray
                                Dim tft_data = cd_key_jar.pack(BNET.BnetPacket.CDKeyJar.packCDKey(tft_key, clientToken, serverToken)).getData.ToArray
                                response_data = concat(roc_data, tft_data)
                                logger.log("Lent keys to " + socket.name, LogMessageTypes.PositiveEvent)
                            Else
                                error_message = "Invalid length. Require client token [4] + server token [4]."
                            End If
                        Case Else
                            error_message = "Invalid packet id."
                    End Select
                End If

                If response_data IsNot Nothing Then
                    socket.Write(New Byte() {PACKET_ID, id}, response_data)
                End If
                If error_message IsNot Nothing Then
                    logger.log("Error parsing data from client: " + error_message, LogMessageTypes.NegativeEvent)
                    socket.Write(New Byte() {PACKET_ID, CKLPackets.error}, packString(error_message))
                End If
            End Sub
        End Class
    End Class
End Namespace
