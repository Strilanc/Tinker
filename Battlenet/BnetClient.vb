''HostBot - Warcraft 3 game hosting bot
''Copyright (C) 2008 Craig Gidney
''
''This program is free software: you can redistribute it and/or modify
''it under the terms of the GNU General Public License as published by
''the Free Software Foundation, either version 3 of the License, or
''(at your option) any later version.
''
''This program is distributed in the hope that it will be useful,
''but WITHOUT ANY WARRANTY; without even the implied warranty of
''MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
''GNU General Public License for more details.
''You should have received a copy of the GNU General Public License
''along with this program.  If not, see http://www.gnu.org/licenses/

Imports HostBot.Warcraft3
Imports HostBot.Links

Namespace Bnet
    Public NotInheritable Class BnetClient
        Implements IDependencyLinkMaster
        Implements IAdvertisingLinkMember
#Region "Inner"
        Public Enum States
            disconnected
            connecting
            enter_username
            logon
            channel
            creating_game
            game
        End Enum

        Public Class GameSettings
            Public ReadOnly name As String
            Public ReadOnly map As W3Map
            Public [private] As Boolean
            Public ReadOnly creation_time As Date
            Public ReadOnly map_settings As W3Map.MapSettings
            Public Sub New(ByVal name As String,
                           ByVal map As W3Map,
                           ByVal arguments As IList(Of String))
                Me.name = name
                Me.map = map
                Me.private = [private]
                Me.creation_time = Now()
                Me.map_settings = New W3Map.MapSettings(arguments)
                For Each arg In arguments
                    Select Case arg.ToLower.Trim()
                        Case "-p", "-private"
                            Me.private = True
                    End Select
                Next arg
            End Sub
        End Class
#End Region

#Region "Variables"
        Public ReadOnly profile As ClientProfile
        Public ReadOnly parent As MainBot
        Public ReadOnly name As String = "unnamed_client"
        Public ReadOnly logger As Logger
        Public Const BNET_PORT As Integer = 6112
        Private WithEvents socket_P As BnetSocket = Nothing

        'refs
        Private ReadOnly eventRef As ICallQueue
        Private ReadOnly ref As ICallQueue
        Private ReadOnly warden_ref As ICallQueue

        'packets
        Private ReadOnly LOCAL_handlers(0 To 255) As Action(Of Dictionary(Of String, Object))

        'game
        Private game_settings As GameSettings
        Private flag_create_game_success As Future(Of Outcome)
        Private host_count As Integer = 0
        Private Const REFRESH_PERIOD As Integer = 20000
        Private WithEvents game_refresh_timer As New Timers.Timer(REFRESH_PERIOD)

        'crypto
        Private ReadOnly client_private_key As Byte()
        Private ReadOnly client_public_key As Byte()
        Private client_password_proof As Byte() = Nothing
        Private server_password_proof As Byte() = Nothing

        'futures
        Private future_connect As Future(Of Outcome) = Nothing
        Private future_logon As Future(Of Outcome) = Nothing

        'events
        Public Event state_changed(ByVal sender As BnetClient, ByVal old_state As States, ByVal new_state As States)
        Public Event chat_event(ByVal sender As BnetClient, ByVal id As BnetPacket.CHAT_EVENT_ID, ByVal user As String, ByVal text As String)
        Private WithEvents warden As Warden.WardenPacketHandler
        Private warden_seed As UInteger

        'state
        Private listen_port As UShort
        Private pool_port As PortPool.PortHandle
        Private last_channel As String = ""
        Private username As String
        Private password_P As String
        Private hostname_P As String
#End Region

#Region "Properties"
        Private property_state As States
        Public Property state_P() As States
            Get
                Return property_state
            End Get
            Private Set(ByVal value As States)
                Dim old_value As States = property_state
                property_state = value
                e_ThrowStateChanged(old_value, value)
            End Set
        End Property
#End Region

#Region "New"
        Public Sub New(ByVal parent As MainBot,
                       ByVal profile As ClientProfile,
                       ByVal name As String,
                       ByVal warden_ref As ICallQueue,
                       Optional ByVal logger As Logger = Nothing)
            'Pass values
            Me.warden_ref = ContractNotNull(warden_ref, "warden_ref")
            Me.name = name
            Me.parent = parent
            Me.profile = profile
            Me.listen_port = profile.listen_port
            Me.logger = If(logger, New Logger)
            Me.eventRef = New ThreadPooledCallQueue
            Me.ref = New ThreadPooledCallQueue

            'Init crypto
            With Bnet.Crypt.generatePublicPrivateKeyPair(New System.Random())
                client_public_key = .v1
                client_private_key = .v2
            End With

            'Start packet machinery
            LOCAL_handlers(Bnet.BnetPacketID.AUTHENTICATION_BEGIN) = AddressOf receivePacket_AUTHENTICATION_BEGIN_L
            LOCAL_handlers(Bnet.BnetPacketID.AUTHENTICATION_FINISH) = AddressOf receivePacket_AUTHENTICATION_FINISH_L
            LOCAL_handlers(Bnet.BnetPacketID.ACCOUNT_LOGON_BEGIN) = AddressOf receivePacket_ACCOUNT_LOGON_BEGIN_L
            LOCAL_handlers(Bnet.BnetPacketID.ACCOUNT_LOGON_FINISH) = AddressOf receivePacket_ACCOUNT_LOGON_FINISH_L
            LOCAL_handlers(Bnet.BnetPacketID.CHAT_EVENT) = AddressOf receivePacket_CHAT_EVENT_L
            LOCAL_handlers(Bnet.BnetPacketID.ENTER_CHAT) = AddressOf receivePacket_ENTER_CHAT_L
            LOCAL_handlers(Bnet.BnetPacketID.NULL) = AddressOf receivePacket_NULL_L
            LOCAL_handlers(Bnet.BnetPacketID.PING) = AddressOf receivePacket_PING_L
            LOCAL_handlers(Bnet.BnetPacketID.MESSAGE_BOX) = AddressOf receivePacket_MESSAGE_BOX_L
            LOCAL_handlers(Bnet.BnetPacketID.CREATE_GAME_3) = AddressOf receivePacket_CREATE_GAME_3_L
            LOCAL_handlers(Bnet.BnetPacketID.WARDEN) = AddressOf receivePacket_WARDEN_L
        End Sub
#End Region

#Region "Access"
        Public Function connected_P() As Boolean
            Dim s As BnetSocket = socket_P
            Return s IsNot Nothing AndAlso s.connected
        End Function

        Public Function send_text_R(ByVal text As String) As IFuture(Of outcome)
            Return ref.QueueFunc(Function() send_text_L(text))
        End Function
        Private Function send_text_L(ByVal text As String) As outcome
            If text Is Nothing Then Return failure("Invalid text")
            sendPacket_CHAT_COMMAND_L(text)
            Return success("Text sent")
        End Function

        Public Function sendWhisper_R(ByVal username As String, ByVal text As String) As IFuture(Of Outcome)
            Return ref.QueueFunc(Function() sendWhisper_L(username, text))
        End Function
        Private Function sendWhisper_L(ByVal username As String, ByVal text As String) As Outcome
            If text Is Nothing Then Return failure("Invalid text")
            sendPacket_CHAT_COMMAND_L("/w " + username + " " + text)
            Return success("Whisper sent")
        End Function

        Private Function set_listen_port_L(ByVal new_port As UShort) As Outcome
            If new_port = listen_port_P Then Return success("Already using that listen port.")
            Select Case state_P
                Case States.creating_game, States.game
                    Return failure("Can't change listen port while in-game")
                Case States.connecting, States.enter_username, States.logon
                    Return failure("Can't change listen port while connecting or logging in.")
                Case States.channel, States.disconnected
                    If pool_port IsNot Nothing Then
                        pool_port.Dispose()
                        pool_port = Nothing
                        logger.log("Returned port {0} to pool.".frmt(Me.listen_port), LogMessageTypes.Positive)
                    End If
                    listen_port = new_port
                    logger.log("Changed listen port to " + new_port.ToString(), LogMessageTypes.Typical)
                    If state_P <> States.disconnected Then
                        sendPacket_NET_GAME_PORT_L()
                    End If
                    Return success("Changed listen port to {0}.".frmt(new_port))
                Case Else
                    Return failure("Unrecognized state for changing listen port.")
            End Select
        End Function
#End Region

#Region "Events"
        Private Sub e_ThrowChatEvent(ByVal id As BnetPacket.CHAT_EVENT_ID, ByVal user As String, ByVal text As String)
            eventRef.QueueAction(
                Sub()
                    RaiseEvent chat_event(Me, id, user, text)
                End Sub
            )
        End Sub
        Private Sub e_ThrowStateChanged(ByVal old_state As States, ByVal new_state As States)
            eventRef.QueueAction(
                Sub()
                    RaiseEvent state_changed(Me, old_state, new_state)
                End Sub
            )
        End Sub

        Private Sub c_SocketDisconnected() Handles socket_P.disconnected
            ref.QueueAction(Sub() disconnect_L())
        End Sub
        Private Sub c_SocketReceivedPacket(ByVal sender As BnetSocket, ByVal flag As Byte, ByVal id As Byte, ByVal data As ImmutableArrayView(Of Byte)) Handles socket_P.receivedPacket
            ref.QueueAction(Sub() receivePacket_L(flag, CType(id, Bnet.BnetPacketID), data))
        End Sub

        Private Sub c_RefreshTimerTick() Handles game_refresh_timer.Elapsed
            ref.QueueAction(Sub() sendPacket_CREATE_GAME_3_L(False, True))
        End Sub
#End Region

#Region "State"
        Private Function connect_L(ByVal remoteHost As String) As IFuture(Of Outcome)
            Try
                If socket_P IsNot Nothing Then
                    Return futurize(failure("Client is already connected."))
                End If
                host_count = 0
                hostname_P = remoteHost

                'Allocate port
                If Me.listen_port = 0 Then
                    Dim out = parent.port_pool.TryTakePortFromPool()
                    If Not out.succeeded Then
                        Return futurize(failure("No listen port specified, and no ports available in the pool."))
                    End If
                    Me.pool_port = out.val
                    Me.listen_port = Me.pool_port.port
                    logger.log("Took port {0} from pool.".frmt(Me.listen_port), LogMessageTypes.Positive)
                End If

                'Establish connection
                logger.log("Connecting to " + remoteHost + "...", LogMessageTypes.Typical)
                Dim port = BNET_PORT
                If remoteHost Like "*:*" Then
                    port = UShort.Parse(remoteHost.Split(":"c)(1))
                    remoteHost = remoteHost.Split(":"c)(0)
                End If
                Dim c As New Net.Sockets.TcpClient()
                c.Connect(remoteHost, port)
                socket_P = New BnetSocket(c, logger, Function(stream) New ThrottledWriteStream(stream,
                                                                                                  initialSlack:=1000,
                                                                                                  costPerWrite:=100,
                                                                                                  costPerCharacter:=1,
                                                                                                  costLimit:=480,
                                                                                                  costRecoveredPerSecond:=48))
                socket_P.name = "BNET"
                state_P = States.connecting

                'Start log-on process
                socket_P.WriteMode(New Byte() {&H1}, PacketStream.InterfaceModes.RawBytes)
                Me.future_connect = New Future(Of Outcome)()
                sendPacket_AUTHENTICATION_BEGIN_L()
                socket_P.set_reading(True)
                Return Me.future_connect
            Catch e As Exception
                disconnect_L()
                Return futurize(failure("Error connecting: " + e.Message))
            End Try
        End Function

        Private Function logon_L(ByVal username As String, ByVal password As String) As IFuture(Of Outcome)
            If state_P <> States.enter_username Then
                Return futurize(failure("Incorrect state for logon."))
            End If

            future_logon = New Future(Of Outcome)
            Me.username = username
            Me.password_P = password
            Me.state_P = States.logon
            sendPacket_ACCOUNT_LOGON_BEGIN_L()
            logger.log("Initiating logon with username " + username, LogMessageTypes.Typical)
            Return future_logon
        End Function

        Private Function disconnect_L() As Outcome
            If socket_P IsNot Nothing Then
                If socket_P.connected Then
                    socket_P.disconnect()
                End If
                socket_P = Nothing
            ElseIf state_P = States.disconnected Then
                Return success("Client is already disconnected")
            End If
            If future_connect IsNot Nothing AndAlso Not future_connect.isReady Then
                future_connect.SetValue(failure("Disconnected before connection completed."))
            End If
            If future_logon IsNot Nothing AndAlso Not future_logon.isReady Then
                future_logon.SetValue(failure("Disconnected before logon completed."))
            End If
            If flag_create_game_success IsNot Nothing AndAlso Not flag_create_game_success.isReady Then
                flag_create_game_success.SetValue(failure("Disconnected before game creation completed."))
            End If

            state_P = States.disconnected
            logger.log("Disconnected", LogMessageTypes.Negative)
            warden = Nothing

            If pool_port IsNot Nothing Then
                pool_port.Dispose()
                pool_port = Nothing
                logger.log("Returned port {0} to pool.".frmt(Me.listen_port), LogMessageTypes.Positive)
                Me.listen_port = 0
            End If

            RaiseEvent closed()
            Return success("Disconnected.")
        End Function

        Private Function start_advertising_game_L(ByVal server As IW3Server,
                                                    ByVal game_name As String,
                                                    ByVal map As W3Map,
                                                    ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
            Select Case state_P
                Case States.disconnected, States.connecting, States.enter_username, States.logon
                    Return futurize(failure("Can't advertise a game until connected."))
                Case States.creating_game
                    Return futurize(failure("Already creating a game."))
                Case States.game
                    Return futurize(failure("Already advertising a game."))
                Case States.channel
                    game_settings = New GameSettings(game_name, map, arguments)
                    logger.log("Creating Game...", LogMessageTypes.Positive)
                    flag_create_game_success = New Future(Of Outcome)
                    state_P = States.creating_game
                    host_count += 1
                    Dim out = sendPacket_CREATE_GAME_3_L(False, False)
                    If Not out.succeeded Then
                        flag_create_game_success.SetValue(failure("Failed to send data."))
                        state_P = States.channel
                        Return futurize(out)
                    End If

                    RaiseEvent started_advertising(Me, server, game_name, map, arguments)
                    If server IsNot Nothing Then
                        server.f_AddAvertiser(Me)
                        DependencyLink.link(Me.advertising_dep, server.advertising_dep)
                        Dim listened = server.f_OpenPort(Me.listen_port)
                        FutureSub.Call(listened, AddressOf r_ServerListened)
                    End If
                    Return flag_create_game_success
                Case Else
                    Throw New InvalidOperationException("Unrecognized client state for advertising a game.")
            End Select
        End Function

        Public Function advertising_dep() As IDependencyLinkServant
            Return New AdvertisingDependency(Me)
        End Function
        Private Class AdvertisingDependency
            Implements IDependencyLinkServant
            Private WithEvents client As BnetClient
            Private ReadOnly lock As New Object()
            Public Event closed() Implements Links.IDependencyLinkMaster.Closed

            Public Sub New(ByVal client As BnetClient)
                If client Is Nothing Then Throw New ArgumentNullException("client")
                Me.client = client
            End Sub

            Public Sub close() Implements Links.IDependencyLinkServant.close
                SyncLock lock
                    If client Is Nothing Then Return
                    client = Nothing
                End SyncLock
                RaiseEvent closed()
            End Sub
            Private Sub client_stopped_advertising(ByVal sender As Links.IAdvertisingLinkMember, ByVal reason As String) Handles client.stopped_advertising
                close()
            End Sub
        End Class
        Private Sub r_ServerListened(ByVal listened As Outcome)
            ref.QueueAction(
                Sub()
                    If Not listened.succeeded Then
                        If Not flag_create_game_success.isReady Then
                            flag_create_game_success.SetValue(listened)
                        End If
                        stop_advertising_game_L(listened.message)
                    End If
                End Sub
            )
        End Sub

        Private Function stop_advertising_game_L(ByVal reason As String) As Outcome
            Select Case state_P
                Case States.creating_game, States.game
                    sendPacket_CLOSE_GAME_3_L()
                    sendPacket_JOIN_CHANNEL_L(last_channel)
                    state_P = States.channel
                    If Not flag_create_game_success.isReady Then
                        flag_create_game_success.SetValue(failure("Advertising cancelled."))
                    End If
                    RaiseEvent stopped_advertising(Me, reason)
                    Return success("Stopped advertising game.")

                Case Else
                    Return success("Already wasn't advertising.")
            End Select
        End Function
#End Region

#Region "Link"
        Public Event closed() Implements IDependencyLinkMaster.Closed
        Private ReadOnly user_link_map As New Dictionary(Of BotUser, ClientServerUserLink)

        Private Function get_user_server_L(ByVal user As BotUser) As IW3Server
            If user Is Nothing Then Return Nothing
            If Not user_link_map.ContainsKey(user) Then Return Nothing
            Return user_link_map(user).server
        End Function

        Private Sub set_user_server_L(ByVal user As BotUser, ByVal server As IW3Server)
            If user Is Nothing Then Return
            If user_link_map.ContainsKey(user) Then
                user_link_map(user).close()
                user_link_map.Remove(user)
            End If
            If server Is Nothing Then Return
            user_link_map(user) = New ClientServerUserLink(Me, server, user)
        End Sub

        Private Class ClientServerUserLink
            Implements IDependencyLinkServant
            Public ReadOnly client As BnetClient
            Public ReadOnly server As IW3Server
            Public ReadOnly user As BotUser

            Public Sub New(ByVal client As BnetClient, ByVal server As IW3Server, ByVal user As BotUser)
                Me.client = client
                Me.server = server
                Me.user = user
                DependencyLink.link(client, Me)
                DependencyLink.link(server, Me)
            End Sub

            Public Event closed() Implements IDependencyLinkMaster.Closed
            Public Sub close() Implements IDependencyLinkServant.close
                client.set_user_server_R(user, Nothing)
                RaiseEvent closed()
            End Sub
        End Class

        Private Event advlink_break(ByVal sender As IAdvertisingLinkMember, ByVal partner As IAdvertisingLinkMember) Implements IAdvertisingLinkMember.break
        Public Event started_advertising(ByVal sender As IAdvertisingLinkMember, ByVal server As IW3Server, ByVal name As String, ByVal map As W3Map, ByVal options As IList(Of String)) Implements IAdvertisingLinkMember.started_advertising
        Public Event stopped_advertising(ByVal sender As IAdvertisingLinkMember, ByVal reason As String) Implements IAdvertisingLinkMember.stopped_advertising
        Private Sub advlink_start_advertising(ByVal server As IW3Server, ByVal name As String, ByVal map As Warcraft3.W3Map, ByVal options As IList(Of String)) Implements Links.IAdvertisingLinkMember.start_advertising
            ref.QueueAction(Sub() start_advertising_game_L(server, name, map, options))
        End Sub
        Private Sub advlink_stop_advertising(ByVal reason As String) Implements Links.IAdvertisingLinkMember.stop_advertising
            ref.QueueAction(Sub() stop_advertising_game_L(reason))
        End Sub
        Private Sub advlink_set_advertising_options_R(ByVal [private] As Boolean) Implements Links.IAdvertisingLinkMember.set_advertising_options
            ref.QueueAction(Function() set_advertised_private_L([private]))
        End Sub
        Public Sub clear_advertising_partner(ByVal other As IAdvertisingLinkMember)
            RaiseEvent advlink_break(Me, other)
        End Sub
        Private Function set_advertised_private_L(ByVal [private] As Boolean) As Outcome
            If state_P <> States.game And state_P <> States.creating_game Then
                Return failure("Not advertising any games.")
            End If

            game_settings.private = [private]
            Me.c_RefreshTimerTick()
            If [private] Then
                Me.game_refresh_timer.Stop()
                Return success("Game will now be advertised as private.")
            Else
                Me.game_refresh_timer.Start()
                Return success("Game will now be advertised as public.")
            End If
        End Function
#End Region

#Region "Networking"
#Region "General"
        Private Function send_packet_L(ByVal cp As BnetPacket) As Outcome
            'if not (cp IsNot Nothing)

            logger.log(Function() "Sending {0} to BNET".frmt(cp.id), LogMessageTypes.DataEvent)
            logger.log(Function() cp.payload.toString, LogMessageTypes.DataParsed)

            If socket_P Is Nothing OrElse Not socket_P.connected Then
                Return failure("Couldn't send data: socket isn't open.")
            End If
            socket_P.Write(New Byte() {BnetPacket.PACKET_PREFIX, cp.id}, cp.payload.getData.ToArray)
            Return success("Sent data.")
        End Function

        Private Sub receivePacket_L(ByVal flag As Byte, ByVal id As Bnet.BnetPacketID, ByVal data As ImmutableArrayView(Of Byte))
            Try
                'Validate
                If flag <> BnetPacket.PACKET_PREFIX Then Throw New Pickling.PicklingException("Invalid packet prefix")

                'Log Event
                logger.log(Function() "Received {0} from BNET".frmt(id), LogMessageTypes.DataEvent)

                'Parse
                Dim p = BnetPacket.FromData(id, data).payload
                If p.getData.length <> data.length Then
                    Throw New Pickling.PicklingException("Data left over after parsing.")
                End If

                'Log Parsed Data
                logger.log(Function() p.toString, LogMessageTypes.DataParsed)

                'Handle
                If LOCAL_handlers(id) Is Nothing Then Throw New Pickling.PicklingException("No handler for parsed " + id.ToString())
                Call LOCAL_handlers(id)(CType(p.getVal(), Dictionary(Of String, Object)))

            Catch e As Pickling.PicklingException
                'Ignore
                logger.log("Error Parsing {0}: {1} (ignored)".frmt(id, e.Message), LogMessageTypes.Negative)

            Catch e As Exception
                'Fail
                logger.log("Error receiving data from bnet server: {0}".frmt(e.Message), LogMessageTypes.Problem)
                disconnect_L()
            End Try
        End Sub
#End Region

#Region "Connect"
        Private Sub sendPacket_AUTHENTICATION_BEGIN_L()
            send_packet_L(BnetPacket.MakePacket_AUTHENTICATION_BEGIN( _
                           MainBot.Wc3MajorVersion,
                           GetIpAddressBytes(external:=False)))
        End Sub
        Private Sub receivePacket_AUTHENTICATION_BEGIN_L(ByVal vals As Dictionary(Of String, Object))
            Const LOGON_TYPE_WC3 As UInteger = 2

            If state_P <> States.connecting Then
                Throw New Exception("Invalid state for receiving AUTHENTICATION_BEGIN")
            End If

            'validate
            If CType(vals("logon type"), UInteger) <> LOGON_TYPE_WC3 Then
                future_connect.SetValue(failure("Failed to connect: Unrecognized logon type from server."))
                Throw New IO.InvalidDataException("Unrecognized logon type")
            End If
            'respond
            Dim server_cd_key_salt = CType(vals("server cd key salt"), Byte())
            Dim mpq_number_string = CStr(vals("mpq number string"))
            Dim mpq_hash_challenge = CStr(vals("mpq hash challenge"))
            sendPacket_AUTHENTICATION_FINISH_L( _
                    server_cd_key_salt,
                    mpq_number_string,
                    mpq_hash_challenge)
        End Sub

        Private Sub sendPacket_AUTHENTICATION_FINISH_L(ByVal server_cd_key_salt As Byte(),
                                                       ByVal mpq_number_string As String,
                                                       ByVal mpq_hash_challenge As String)
            If Not (server_cd_key_salt IsNot Nothing) Then Throw New ArgumentException()
            If Not (mpq_number_string IsNot Nothing) Then Throw New ArgumentException()
            If Not (mpq_hash_challenge IsNot Nothing) Then Throw New ArgumentException()
            Dim war_3_path = My.Settings.war3path
            Dim cd_key_owner = My.Settings.cdKeyOwner
            Dim exe_info = My.Settings.exeInformation
            If profile.CKL_server Like "*:#*" Then
                Dim pair = profile.CKL_server.Split(":"c)
                Dim port = UShort.Parse(pair(1))
                FutureSub.Call(BnetPacket.CKL_MakePacket_AUTHENTICATION_FINISH(MainBot.Wc3Version,
                                                                               war_3_path,
                                                                               mpq_number_string,
                                                                               mpq_hash_challenge,
                                                                               server_cd_key_salt,
                                                                               cd_key_owner,
                                                                               exe_info,
                                                                               pair(0),
                                                                               port),
                               AddressOf finish_ckl)
            Else
                Dim p = BnetPacket.MakePacket_AUTHENTICATION_FINISH( _
                            MainBot.Wc3Version,
                            war_3_path,
                            mpq_number_string,
                            mpq_hash_challenge,
                            server_cd_key_salt,
                            cd_key_owner,
                            exe_info,
                            profile.roc_cd_key,
                            profile.tft_cd_key)
                Dim roc_key = CType(CType(p.payload.getVal(), Dictionary(Of String, Object))("ROC cd key"), Dictionary(Of String, Object))
                Dim roc_hash = CType(roc_key("hash"), Byte())
                Me.warden_seed = subArray(roc_hash, 0, 4).ToUInteger(ByteOrder.LittleEndian)
                send_packet_L(p)
            End If
        End Sub
        Private Sub finish_ckl(ByVal out As Outcome(Of BnetPacket))
            If out.succeeded Then
                Dim roc_key = CType(CType(out.val.payload.getVal(), Dictionary(Of String, Object))("ROC cd key"), Dictionary(Of String, Object))
                Dim roc_hash = CType(roc_key("hash"), Byte())
                Me.warden_seed = subArray(roc_hash, 0, 4).ToUInteger(ByteOrder.LittleEndian)
                logger.log(out.message, LogMessageTypes.Positive)
                send_packet_L(out.val)
            Else
                logger.log(out.message, LogMessageTypes.Negative)
                future_connect.SetValue(failure("Failed to borrow keys: '{0}'.".frmt(out.message)))
                ref.QueueFunc(AddressOf disconnect_L)
            End If
        End Sub

        Private Sub receivePacket_AUTHENTICATION_FINISH_L(ByVal vals As Dictionary(Of String, Object))
            Const RESULT_PASSED As UInteger = &H0
            Const RESULT_INVALID_CODE_MIN As UInteger = &H1
            Const RESULT_INVALID_CODE_MAX As UInteger = &HFF
            Const RESULT_OLD_VERSION As UInteger = &H100
            Const RESULT_INVALID_VERSION As UInteger = &H101
            Const RESULT_FUTURE_VERSION As UInteger = &H102
            Const RESULT_INVALID_CD_KEY As UInteger = &H200
            Const RESULT_USED_CD_KEY As UInteger = &H201
            Const RESULT_BANNED_CD_KEY As UInteger = &H202
            Const RESULT_WRONG_PRODUCT As UInteger = &H203

            If state_P <> States.connecting Then
                Throw New Exception("Invalid state for receiving AUTHENTICATION_FINISHED")
            End If

            Dim result As UInteger = CUInt(vals("result"))
            If result <> RESULT_PASSED Then
                Dim info As String = CStr(vals("info"))
                Dim errmsg As String
                Select Case result
                    Case RESULT_OLD_VERSION
                        errmsg = "Out of date version. " + info
                    Case RESULT_INVALID_VERSION
                        errmsg = "Invalid version. " + info
                    Case RESULT_FUTURE_VERSION
                        errmsg = "Future version (need to downgrade apparently). " + info
                    Case RESULT_INVALID_CD_KEY
                        errmsg = "Invalid CD key. " + info
                    Case RESULT_USED_CD_KEY
                        errmsg = "CD key in use by: " + info
                    Case RESULT_BANNED_CD_KEY
                        errmsg = "CD key banned! " + info
                    Case RESULT_WRONG_PRODUCT
                        errmsg = "Wrong product. " + info
                    Case RESULT_INVALID_CODE_MIN To RESULT_INVALID_CODE_MAX
                        errmsg = "Invalid version code. " + info
                    Case Else
                        errmsg = "Unknown authentication failure id: " + result.ToString() + ". " + info
                End Select

                future_connect.SetValue(failure("Failed to connect: " + errmsg))
                Throw New Exception(errmsg)
            End If

            state_P = States.enter_username
            future_connect.SetValue(success("Succesfully connected to battle.net server at {0}.".frmt(hostname_P)))
        End Sub
#End Region

#Region "Warden"
        Private Sub receivePacket_WARDEN_L(ByVal vals As Dictionary(Of String, Object))
            Try
                warden = If(warden, New Warden.WardenPacketHandler(warden_seed, warden_ref, logger))
                Dim data = CType(vals("encrypted data"), Byte())
                warden.ReceiveData(data)
            Catch e As Exception
                warden_Fail(e)
            End Try
        End Sub
        Private Sub warden_Send(ByVal data() As Byte) Handles warden.Send
            If data Is Nothing Then Throw New ArgumentException()
            send_packet_R(BnetPacket.MakePacket_Warden(data))
        End Sub
        Private Sub warden_Fail(ByVal e As Exception) Handles warden.Fail
            Logging.LogUnexpectedException("Warden", e)
            logger.log("Error dealing with Warden packet. Disconnecting.", LogMessageTypes.Problem)
            disconnect_R()
        End Sub
#End Region

#Region "Logon"
        Private Sub sendPacket_ACCOUNT_LOGON_BEGIN_L()
            send_packet_L(BnetPacket.MakePacket_ACCOUNT_LOGON_BEGIN( _
                        username,
                        client_public_key))
        End Sub
        Private Sub receivePacket_ACCOUNT_LOGON_BEGIN_L(ByVal vals As Dictionary(Of String, Object))
            Const RESULT_PASSED As UInteger = &H0
            Const RESULT_BAD_USERNAME As UInteger = &H1
            Const RESULT_UPGRADE_ACCOUNT As UInteger = &H5

            If state_P <> States.logon Then
                Throw New Exception("Invalid state for receiving ACCOUNT_LOGON_BEGIN")
            End If

            Dim result As UInteger = CUInt(vals("result"))
            If result <> RESULT_PASSED Then
                Dim errmsg As String
                Select Case result
                    Case RESULT_BAD_USERNAME
                        errmsg = "Username doesn't exist."
                    Case RESULT_UPGRADE_ACCOUNT
                        errmsg = "Account requires upgrade."
                    Case Else
                        errmsg = "Unrecognized logon problem: " + result.ToString()
                End Select
                future_logon.SetValue(failure("Failed to logon: " + errmsg))
                Throw New Exception(errmsg)
            End If

            'generate password proofs
            Dim account_salt = CType(vals("account password salt"), Byte())
            Dim server_key = CType(vals("server public key"), Byte())
            With Bnet.Crypt.generateClientServerPasswordProofs( _
                        username,
                        password_P,
                        account_salt,
                        server_key,
                        client_private_key,
                        client_public_key)
                client_password_proof = .v1
                server_password_proof = .v2
            End With

            'respond
            sendPacket_ACCOUNT_LOGON_FINISH_L()
        End Sub

        Private Sub sendPacket_ACCOUNT_LOGON_FINISH_L()
            send_packet_L(BnetPacket.MakePacket_ACCOUNT_LOGON_FINISH(client_password_proof))
        End Sub
        Private Sub receivePacket_ACCOUNT_LOGON_FINISH_L(ByVal vals As Dictionary(Of String, Object))
            Const RESULT_PASSED As UInteger = &H0
            Const RESULT_BAD_PASSWORD As UInteger = &H2
            Const RESULT_NO_EMAIL As UInteger = &HE
            Const RESULT_CUSTOM As UInteger = &HF

            If state_P <> States.logon Then
                Throw New Exception("Invalid state for receiving ACCOUNT_LOGON_FINISH")
            End If

            Dim result As UInteger = CUInt(vals("result"))

            If result <> RESULT_PASSED Then
                Dim errmsg As String
                Select Case result
                    Case RESULT_BAD_PASSWORD
                        errmsg = "Incorrect password."
                    Case RESULT_NO_EMAIL
                        errmsg = "No email address associated with account"
                    Case RESULT_CUSTOM
                        errmsg = "Logon error: " + CType(vals("custom error info"), String)
                    Case Else
                        errmsg = "Unrecognized logon error: " + result.ToString()
                End Select
                future_logon.SetValue(failure("Failed to logon: " + errmsg))
                Throw New Exception(errmsg)
            End If

            'validate
            Dim password_proof_incorrect As Boolean = False
            Dim claimed_server_password_proof As Byte() = CType(vals("server password proof"), Byte())
            If claimed_server_password_proof.Length <> server_password_proof.Length Then
                password_proof_incorrect = True
            Else
                For i As Integer = 0 To server_password_proof.Length - 1
                    If server_password_proof(i) <> claimed_server_password_proof(i) Then
                        password_proof_incorrect = True
                        Exit For
                    End If
                Next i
            End If
            If password_proof_incorrect Then
                future_logon.SetValue(failure("Failed to logon: Server didn't give correct password proof"))
                Throw New IO.InvalidDataException("Server didn't give correct password proof.")
            End If
            Dim lan_host = profile.lan_host.Split(" "c)(0)
            If lan_host <> "" Then
                Try
                    Dim lan = New W3LanAdvertiser(parent, name, listen_port, lan_host)
                    parent.add_widget_R(lan)
                    DependencyLink.link(Me, lan)
                    Dim link2 = New AdvertisingLink(Me, lan.make_advertising_link_member)
                Catch e As Exception
                    logger.log("Error creating lan advertiser: {0}".frmt(e.Message), LogMessageTypes.Problem)
                End Try
            End If
            'log
            logger.log("Logged on with username {0}.".frmt(username), LogMessageTypes.Typical)
            future_logon.SetValue(success("Succesfully logged on with username {0}.".frmt(username)))
            'respond
            sendPacket_NET_GAME_PORT_L()
            sendPacket_ENTER_CHAT_L()
        End Sub

        Private Sub sendPacket_ENTER_CHAT_L()
            send_packet_L(BnetPacket.MakePacket_ENTER_CHAT())
        End Sub
        Private Sub receivePacket_ENTER_CHAT_L(ByVal vals As Dictionary(Of String, Object))
            logger.log("Entered chat", LogMessageTypes.Typical)
            sendPacket_JOIN_CHANNEL_L(profile.initial_channel)
            state_P = States.channel
        End Sub
#End Region

#Region "Advertising"
        Private Sub sendPacket_NET_GAME_PORT_L()
            send_packet_L(BnetPacket.MakePacket_NET_GAME_PORT(listen_port))
        End Sub

        Private Function sendPacket_CREATE_GAME_3_L(Optional ByVal useFull As Boolean = False,
                                                    Optional ByVal refreshing As Boolean = False) As Outcome
            If refreshing Then
                If state_P <> States.game Then
                    Return failure("Must have already created game before refreshing")
                End If
                state_P = States.game '[throws event]
            End If

            Try
                Return send_packet_L(BnetPacket.MakePacket_CREATE_GAME_3( _
                                username,
                                game_settings.name,
                                game_settings.map,
                                game_settings.map_settings,
                                game_settings.creation_time,
                                game_settings.private,
                                host_count,
                                useFull))
            Catch e As ArgumentException
                Return failure("Error sending packet: {0}.".frmt(e.Message))
            End Try
        End Function
        Private Sub receivePacket_CREATE_GAME_3_L(ByVal vals As Dictionary(Of String, Object))
            Dim result = CUInt(vals("result"))

            If result = 0 Then
                If state_P = States.creating_game Then
                    logger.log("Finished creating game.", LogMessageTypes.Positive)
                    state_P = States.game
                    If Not game_settings.private Then game_refresh_timer.Start()
                    If Not flag_create_game_success.isReady Then
                        flag_create_game_success.SetValue(success("Succesfully created game {0} for map {1}.".frmt(game_settings.name, game_settings.map.relative_path)))
                    End If
                Else
                    logger.log("Refreshed game", LogMessageTypes.Positive)
                End If
            Else
                If state_P = States.creating_game Then
                    If Not flag_create_game_success.isReady Then
                        flag_create_game_success.SetValue(failure("BNET didn't allow game creation. Most likely cause is game name in use."))
                    End If
                End If
                game_refresh_timer.Stop()
                state_P = States.channel
                sendPacket_JOIN_CHANNEL_L(last_channel)
                RaiseEvent stopped_advertising(Me, "Client " + Me.name + " failed to advertise the game. Most likely cause is game name in use.")
            End If
        End Sub
        Private Sub sendPacket_CLOSE_GAME_3_L()
            send_packet_L(BnetPacket.MakePacket_CLOSE_GAME_3())
            game_refresh_timer.Stop()
        End Sub
#End Region

#Region "Channel"
        Private Sub sendPacket_JOIN_CHANNEL_L(ByVal channel As String)
            'Const FLAG_NO_CREATE As UInteger = 0
            'Const FLAG_FIRST_JOIN As UInteger = 1
            Const FLAG_FORCED_JOIN As UInteger = 2
            'Const FLAG_DIABLO2_JOIN As UInteger = 3
            If flag_create_game_success IsNot Nothing AndAlso Not flag_create_game_success.isReady Then
                flag_create_game_success.SetValue(failure("Re-entered channel."))
            End If
            send_packet_L(BnetPacket.MakePacket_JOIN_CHANNEL(FLAG_FORCED_JOIN, channel))
        End Sub

        Private Sub receivePacket_CHAT_EVENT_L(ByVal vals As Dictionary(Of String, Object))
            Dim eventId As BnetPacket.CHAT_EVENT_ID = CType(vals("event id"), BnetPacket.CHAT_EVENT_ID)
            Dim user As String = CType(vals("username"), String)
            Dim text As String = CType(vals("text"), String)
            If eventId = BnetPacket.CHAT_EVENT_ID.CHANNEL Then last_channel = text
            e_ThrowChatEvent(eventId, user, text)
        End Sub

        Private Sub sendPacket_CHAT_COMMAND_L(ByVal text As String)
            If Not (text IsNot Nothing) Then Throw New ArgumentException()
            send_packet_L(BnetPacket.MakePacket_CHAT_COMMAND(text))
        End Sub
#End Region

#Region "Misc"
        Private Sub receivePacket_PING_L(ByVal vals As Dictionary(Of String, Object))
            Dim salt = CType(vals("salt"), Byte())
            send_packet_L(BnetPacket.MakePacket_PING(salt))
        End Sub

        Private Sub receivePacket_NULL_L(ByVal vals As Dictionary(Of String, Object))
            '[ignore]
        End Sub

        Private Sub receivePacket_MESSAGE_BOX_L(ByVal vals As Dictionary(Of String, Object))
            Dim msg = "MESSAGE BOX FROM BNET: " + CType(vals("caption"), String) + ": " + CType(vals("text"), String)
            logger.log(msg, LogMessageTypes.Problem)
        End Sub
#End Region
#End Region

#Region "Remote Calls"
        Public Function send_packet_R(ByVal cp As BnetPacket) As IFuture(Of Outcome)
            Return ref.QueueFunc(Function() send_packet_L(cp))
        End Function
        Public Function set_listen_port_R(ByVal new_port As UShort) As IFuture(Of Outcome)
            Return ref.QueueFunc(Function() set_listen_port_L(new_port))
        End Function
        Public Function stop_advertising_game_R(ByVal reason As String) As IFuture(Of Outcome)
            Return ref.QueueFunc(Function() stop_advertising_game_L(reason))
        End Function
        Public Function start_advertising_game_R(ByVal server As IW3Server, ByVal game_name As String, ByVal map As W3Map, ByVal options As IList(Of String)) As IFuture(Of Outcome)
            Return futurefuture(ref.QueueFunc(Function() start_advertising_game_L(server, game_name, map, options)))
        End Function
        Public Function disconnect_R() As IFuture(Of Outcome)
            Return ref.QueueFunc(Function() disconnect_L())
        End Function
        Public Function f_Connect(ByVal remoteHost As String) As IFuture(Of Outcome)
            Return futurefuture(ref.QueueFunc(Function() connect_L(remoteHost)))
        End Function
        Public Function f_Login(ByVal username As String, ByVal password As String) As IFuture(Of Outcome)
            Return futurefuture(ref.QueueFunc(Function() logon_L(username, password)))
        End Function
        Public Function f_GetUserServer(ByVal user As BotUser) As IFuture(Of IW3Server)
            Return ref.QueueFunc(Function() get_user_server_L(user))
        End Function
        Public Function set_user_server_R(ByVal user As BotUser, ByVal server As IW3Server) As IFuture
            Return ref.QueueAction(Sub() set_user_server_L(user, server))
        End Function
        Public Shadows ReadOnly Property listen_port_P() As UShort
            Get
                Return listen_port
            End Get
        End Property
        Public Shadows ReadOnly Property username_P() As String
            Get
                Return username
            End Get
        End Property
#End Region
    End Class
End Namespace
