Imports Tinker.Pickling

Namespace WC3.Protocol
    Public NotInheritable Class Packet
        Public Const PacketPrefixValue As Byte = &HF7
        Public ReadOnly id As PacketId
        Private ReadOnly _payload As IPickle(Of Object)
        Private Shared ReadOnly packetJar As ManualSwitchJar = MakeW3PacketJar()

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_payload IsNot Nothing)
        End Sub

        Public ReadOnly Property Payload As IPickle(Of Object)
            Get
                Contract.Ensures(Contract.Result(Of IPickle(Of Object))() IsNot Nothing)
                Return _payload
            End Get
        End Property

        Public Sub New(ByVal id As PacketId, ByVal payload As IPickle(Of Object))
            Contract.Requires(payload IsNot Nothing)
            Me._payload = payload
            Me.id = id
        End Sub
        Public Sub New(ByVal id As PacketId, ByVal value As Object)
            Me.New(id, packetJar.Pack(id, value))
            Contract.Requires(value IsNot Nothing)
        End Sub

        Private Shared Sub reg(ByVal jar As ManualSwitchJar, ByVal id As PacketId, ByVal ParamArray subJars() As IJar(Of Object))
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(subJars IsNot Nothing)
            jar.AddPackerParser(id, New DefParser(id, subJars).Weaken)
        End Sub
        Private Shared Sub reg(ByVal jar As ManualSwitchJar, ByVal subjar As DefParser)
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(subjar IsNot Nothing)
            jar.AddPackerParser(subjar.id, subjar.Weaken)
        End Sub

        Private Shared Function MakeW3PacketJar() As ManualSwitchJar
            Dim jar = New ManualSwitchJar

            'Misc
            reg(jar, Jars.Ping)
            reg(jar, Jars.Pong)

            'Chat
            Dim chatJar = New InteriorSwitchJar(Of ChatType, Dictionary(Of InvariantString, Object))(
                        PacketId.Text.ToString,
                        Function(val) CType(val("type"), ChatType),
                        Function(data) CType(data(data(0) + 2), ChatType))
            chatJar.AddPackerParser(ChatType.Game, New TupleJar(PacketId.Text.ToString,
                    New ListJar(Of Byte)("receiving player indexes", New ByteJar("player index")).Weaken,
                    New ByteJar("sending player index").Weaken,
                    New EnumByteJar(Of ChatType)("type").Weaken,
                    New EnumUInt32Jar(Of ChatReceiverType)("receiver type").Weaken,
                    New StringJar("message").Weaken))
            chatJar.AddPackerParser(ChatType.Lobby, New TupleJar(PacketId.Text.ToString,
                    New ListJar(Of Byte)("receiving player indexes", New ByteJar("player index")).Weaken,
                    New ByteJar("sending player index").Weaken,
                    New EnumByteJar(Of ChatType)("type").Weaken,
                    New StringJar("message").Weaken))
            jar.AddPackerParser(PacketId.Text, chatJar.Weaken)
            jar.AddPackerParser(PacketId.NonGameAction, Jars.NonGameAction.Weaken)

            'Player Exit
            reg(jar, Jars.Leaving)
            reg(jar, Jars.OtherPlayerLeft)

            'Player Entry
            reg(jar, Jars.Knock)
            reg(jar, Jars.Greet)
            reg(jar, Jars.HostMapInfo)
            reg(jar, Jars.RejectEntry)
            reg(jar, Jars.OtherPlayerJoined)

            'Lobby
            reg(jar, Jars.OtherPlayerReady)
            reg(jar, Jars.StartLoading)
            reg(jar, Jars.StartCountdown)
            reg(jar, Jars.Ready)
            reg(jar, Jars.LobbyState)
            reg(jar, Jars.PeerConnectionInfo)

            'Gameplay
            reg(jar, Jars.ShowLagScreen)
            reg(jar, Jars.RemovePlayerFromLagScreen)
            reg(jar, Jars.RequestDropLaggers)
            reg(jar, Jars.Tick)
            reg(jar, Jars.Tock)
            reg(jar, Jars.GameAction)

            'Lan
            reg(jar, Jars.LanRequestGame)
            reg(jar, Jars.LanRefreshGame)
            reg(jar, Jars.LanCreateGame)
            reg(jar, Jars.LanDestroyGame)
            reg(jar, Jars.LanDescribeGame)

            'Peer
            reg(jar, Jars.PeerKnock)
            reg(jar, Jars.PeerPing)
            reg(jar, Jars.PeerPong)

            'Map Download
            reg(jar, Jars.ClientMapInfo)
            reg(jar, Jars.SetUploadTarget)
            reg(jar, Jars.SetDownloadSource)
            reg(jar, Jars.MapFileData)
            reg(jar, Jars.MapFileDataReceived)
            reg(jar, Jars.MapFileDataProblem)

            Return jar
        End Function

        Public Shared Function FromData(ByVal id As PacketId, ByVal data As IReadableList(Of Byte)) As Packet
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(id, packetJar.Parse(id, data))
        End Function
    End Class

    Public NotInheritable Class W3PacketHandler
        Inherits PacketHandler(Of PacketId)

        Public Sub New(ByVal logger As Logger)
            MyBase.New(logger)
        End Sub

        Public Overrides ReadOnly Property HeaderSize As Integer
            Get
                Contract.Ensures(Contract.Result(Of Integer)() = 4)
                Return 4
            End Get
        End Property

        'verification disabled due to stupid verifier
        <ContractVerification(False)>
        Protected Overrides Function ExtractKey(ByVal header As IReadableList(Of Byte)) As PacketId
            Contract.Assume(header.Count >= 4)
            If header(0) <> Packet.PacketPrefixValue Then Throw New IO.InvalidDataException("Invalid packet header.")
            Return CType(header(1), PacketId)
        End Function
    End Class
End Namespace
