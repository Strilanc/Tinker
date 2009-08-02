Public Class ClientProfile
    Public name As String
    Public users As New BotUserSet()
    Public rocCdKey As String = ""
    Public tftCdKey As String = ""
    Public username As String = ""
    Public password As String = ""
    Public server As String = "useast.battle.net (Azeroth)"
    Public listenPort As UShort = 6113
    'Public lan_admin_port As UShort = 6114
    'Public lan_admin_host As String = " (None)"
    'Public lan_admin_password As String = ""
    Public lanHost As String = " (None)"
    Public initialChannel As String = My.Resources.ProgramName
    Public keyServerAddress As String = ""
    Private Const version As UInt16 = 1

    Public Sub New(Optional ByVal name As String = "Default")
        Contract.Requires(name IsNot Nothing)
        Me.name = name
    End Sub
    Public Sub New(ByVal r As IO.BinaryReader)
        Contract.Requires(r IsNot Nothing)
        load(r)
    End Sub

    Public Sub load(ByVal r As IO.BinaryReader)
        Contract.Requires(r IsNot Nothing)
        Dim version = r.ReadUInt16()
        name = r.ReadString()
        users.Load(r)
        rocCdKey = r.ReadString()
        tftCdKey = r.ReadString()
        username = r.ReadString()
        password = r.ReadString()
        server = r.ReadString()
        listenPort = r.ReadUInt16()
        initialChannel = r.ReadString()
        keyServerAddress = r.ReadString()
        If version >= 1 Then
            r.ReadString() 'lan_admin_host
            r.ReadUInt16() 'lan_admin_port
            lanHost = r.ReadString()
            r.ReadString() 'lan_admin_password
        End If
    End Sub

    Public Sub save(ByVal w As IO.BinaryWriter)
        Contract.Requires(w IsNot Nothing)
        w.Write(version)
        w.Write(name)
        users.Save(w)
        w.Write(rocCdKey)
        w.Write(tftCdKey)
        w.Write(username)
        w.Write(password)
        w.Write(server)
        w.Write(listenPort)
        w.Write(initialChannel)
        w.Write(keyServerAddress)
        If version >= 1 Then
            w.Write(" (None)") 'old default lan_admin_host
            w.Write(CUShort(6114)) 'old default lan_admin_port
            w.Write(lanHost)
            w.Write("") 'old default lan_admin_password
        End If
    End Sub

    Public Function clone(Optional ByVal name As String = Nothing) As ClientProfile
        Dim newProfile As New ClientProfile
        With newProfile
            .users = users.Clone()
            .rocCdKey = rocCdKey
            .tftCdKey = tftCdKey
            .username = username
            .password = password
            .server = server
            .name = If(name, Me.name)
            .listenPort = listenPort
            .initialChannel = initialChannel
            .keyServerAddress = keyServerAddress
        End With
        Return newProfile
    End Function
End Class
