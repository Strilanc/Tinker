Imports HostBot.Warcraft3.Warden.WardenPacketId

Namespace Warcraft3.Warden
    Public Enum WardenPacketId As Byte
        LoadModule = 0
        DownloadModule = 1
        PerformCheck = 2
        RunModule = 5
    End Enum

    Public Class WardenPacket
        Public ReadOnly payload As IPickle(Of Object)
        Public ReadOnly id As WardenPacketId
        Private Shared ReadOnly packetJar As ManualSwitchJar = MakeWardenPacketJar()

        Private Sub New(ByVal id As WardenPacketId, ByVal payload As IPickle(Of Object))
            Contract.Requires(payload IsNot Nothing)
            Me.payload = payload
            Me.id = id
        End Sub
        Private Sub New(ByVal id As WardenPacketId, ByVal val As Object)
            Me.New(id, packetJar.pack(id, val))
        End Sub

#Region "Jar"
        Private Shared Sub regPack(ByVal jar As ManualSwitchJar, ByVal id As WardenPacketId, ByVal ParamArray subjars() As IPackJar(Of Object))
            jar.regPacker(id, New TuplePackJar(id.ToString(), subjars).Weaken)
        End Sub
        Private Shared Sub regParse(ByVal jar As ManualSwitchJar, ByVal id As WardenPacketId, ByVal ParamArray subjars() As IParseJar(Of Object))
            jar.regParser(id, New TupleParseJar(id.ToString(), subjars))
        End Sub
        Public Shared Function MakeWardenPacketJar() As ManualSwitchJar
            Contract.Ensures(Contract.Result(Of ManualSwitchJar)() IsNot Nothing)
            Dim g = New ManualSwitchJar()
            regParse(g, LoadModule,
                                New ArrayJar("module id", expectedSize:=16).Weaken,
                                New ArrayJar("module rc4 seed", expectedSize:=16).Weaken,
                                New UInt32Jar("dl size").Weaken)
            regParse(g, DownloadModule, New ArrayJar("dl data", sizePrefixSize:=2).Weaken)
            regParse(g, PerformCheck, New ArrayJar("unknown0", takeRest:=True).Weaken)
            regParse(g, RunModule, New ArrayJar("module input data", takeRest:=True).Weaken)
            Return g
        End Function
#End Region

        Public Shared Function FromData(ByVal id As WardenPacketId, ByVal data As ViewableList(Of Byte)) As WardenPacket
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of WardenPacket)() IsNot Nothing)
            Return New WardenPacket(id, packetJar.parse(id, data))
        End Function
    End Class
End Namespace