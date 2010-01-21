Imports Tinker.Pickling

Namespace WC3.Protocol
    Public NotInheritable Class Packet
        Public Const PacketPrefixValue As Byte = &HF7
        Public ReadOnly id As PacketId
        Private ReadOnly _payload As IPickle(Of Object)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_payload IsNot Nothing)
        End Sub

        Public ReadOnly Property Payload As IPickle(Of Object)
            Get
                Contract.Ensures(Contract.Result(Of IPickle(Of Object))() IsNot Nothing)
                Return _payload
            End Get
        End Property

        Private Sub New(ByVal id As PacketId, ByVal payload As IPickle(Of Object))
            Contract.Requires(payload IsNot Nothing)
            Me._payload = payload
            Me.id = id
        End Sub
        Public Sub New(ByVal definition As Packets.SimpleDefinition, ByVal vals As Dictionary(Of InvariantString, Object))
            Me.New(definition.id, definition.Pack(vals))
            Contract.Requires(definition IsNot Nothing)
            Contract.Requires(vals IsNot Nothing)
        End Sub

        Public Shared Function FromValue(Of T)(ByVal id As PacketId, ByVal jar As IPackJar(Of T), ByVal value As T) As Packet
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(value IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(id, jar.Weaken.Pack(CType(value, Object)))
        End Function

        Private Shared Sub reg(ByVal jar As ManualSwitchJar, ByVal id As PacketId, ByVal ParamArray subJars() As IJar(Of Object))
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(subJars IsNot Nothing)
            jar.AddPackerParser(id, New Protocol.Packets.SimpleDefinition(id, subJars).Weaken)
        End Sub
        Private Shared Sub reg(ByVal jar As ManualSwitchJar, ByVal subjar As Protocol.Packets.SimpleDefinition)
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(subjar IsNot Nothing)
            jar.AddPackerParser(subjar.id, subjar.Weaken)
        End Sub

        Private Shared Function MakeW3PacketJar() As ManualSwitchJar
            Contract.Ensures(Contract.Result(Of ManualSwitchJar)() IsNot Nothing)
            Dim jar = New ManualSwitchJar

            'Misc
            reg(jar, Packets.Ping)
            reg(jar, Packets.Pong)

            'Player Exit
            reg(jar, Packets.Leaving)
            reg(jar, Packets.OtherPlayerLeft)

            'Player Entry
            reg(jar, Packets.Knock)
            reg(jar, Packets.Greet)
            reg(jar, Packets.HostMapInfo)
            reg(jar, Packets.RejectEntry)
            reg(jar, Packets.OtherPlayerJoined)

            'Lobby
            reg(jar, Packets.OtherPlayerReady)
            reg(jar, Packets.StartLoading)
            reg(jar, Packets.StartCountdown)
            reg(jar, Packets.Ready)
            reg(jar, Packets.LobbyState)
            reg(jar, Packets.PeerConnectionInfo)

            'Gameplay
            reg(jar, Packets.ShowLagScreen)
            reg(jar, Packets.RemovePlayerFromLagScreen)
            reg(jar, Packets.RequestDropLaggers)
            reg(jar, Packets.Tick)
            reg(jar, Packets.Tock)
            reg(jar, Packets.GameAction)

            'Lan
            reg(jar, Packets.LanRequestGame)
            reg(jar, Packets.LanRefreshGame)
            reg(jar, Packets.LanCreateGame)
            reg(jar, Packets.LanDestroyGame)
            reg(jar, Packets.LanGameDetails)

            'Peer
            reg(jar, Packets.PeerKnock)
            reg(jar, Packets.PeerPing)
            reg(jar, Packets.PeerPong)

            'Map Download
            reg(jar, Packets.ClientMapInfo)
            reg(jar, Packets.SetUploadTarget)
            reg(jar, Packets.SetDownloadSource)
            reg(jar, Packets.MapFileData)
            reg(jar, Packets.MapFileDataReceived)
            reg(jar, Packets.MapFileDataProblem)

            reg(jar, Packets.HostConfirmHostLeaving)
            reg(jar, Packets.ClientConfirmHostLeaving)

            Return jar
        End Function

        Public Shared Function FromData(ByVal id As PacketId, ByVal data As IReadableList(Of Byte)) As Packet
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(id, MakeW3PacketJar.Parse(id, data))
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

        'verification disabled due to stupid verifier (1.2.30118.5)
        <ContractVerification(False)>
        Protected Overrides Function ExtractKey(ByVal header As IReadableList(Of Byte)) As PacketId
            Contract.Assume(header.Count >= 4)
            If header(0) <> Packet.PacketPrefixValue Then Throw New IO.InvalidDataException("Invalid packet header.")
            Return CType(header(1), PacketId)
        End Function
    End Class
End Namespace
