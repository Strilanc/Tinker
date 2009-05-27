Imports HostBot.Warcraft3.Warden.WardenPacketId
Imports HostBot.Pickling
Imports HostBot.Pickling.Jars

Namespace Warcraft3.Warden
    Public Enum WardenPacketId As Byte
        LoadModule = 0
        DownloadModule = 1
        PerformCheck = 2
        RunModule = 5
    End Enum

    Public Class WardenPacket
#Region "Members"
        Public ReadOnly payload As IPickle
        Public ReadOnly id As WardenPacketId
        Private Shared ReadOnly packet_jar As ManualSwitchJar = MakeWardenPacketJar()
#End Region

#Region "New"
        Private Sub New(ByVal id As WardenPacketId, ByVal payload As IPickle)
            If Not (payload IsNot Nothing) Then Throw New ArgumentException()

            Me.payload = payload
            Me.id = id
        End Sub
        Private Sub New(ByVal id As WardenPacketId, ByVal val As Object)
            Me.New(id, packet_jar.pack(id, val))
        End Sub
#End Region

#Region "Jar"
        Private Shared Sub regPack(ByVal jar As ManualSwitchJar, ByVal id As WardenPacketId, ByVal ParamArray subjars() As IJar)
            jar.regPacker(id, New TupleJar(id.ToString(), subjars))
        End Sub
        Private Shared Sub regParse(ByVal jar As ManualSwitchJar, ByVal id As WardenPacketId, ByVal ParamArray subjars() As IJar)
            jar.regParser(id, New TupleJar(id.ToString(), subjars))
        End Sub
        Public Shared Function MakeWardenPacketJar() As ManualSwitchJar
            Dim g = New ManualSwitchJar()
            regParse(g, LoadModule,
                                New ArrayJar("module id", 16),
                                New ArrayJar("module rc4 seed", 16),
                                New ValueJar("dl size", 4))
            regParse(g, DownloadModule, New ArrayJar("dl data", , 2))
            regParse(g, PerformCheck, New ArrayJar("unknown0", , , True))
            regParse(g, RunModule, New ArrayJar("module input data", , , True))
            Return g
        End Function
#End Region

#Region "Parsing"
        Public Shared Function FromData(ByVal id As WardenPacketId, ByVal data As ImmutableArrayView(Of Byte)) As WardenPacket
            If Not (data IsNot Nothing) Then Throw New ArgumentException()

            Return New WardenPacket(id, packet_jar.parse(id, data))
        End Function
#End Region
    End Class
End Namespace