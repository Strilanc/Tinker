Public Class ClientProfile
    Public name As String
    Public users As New BotUserSet()
    Public roc_cd_key As String = ""
    Public tft_cd_key As String = ""
    Public username As String = ""
    Public password As String = ""
    Public server As String = "useast.battle.net (Azeroth)"
    Public listen_port As UShort = 6113
    'Public lan_admin_port As UShort = 6114
    'Public lan_admin_host As String = " (None)"
    'Public lan_admin_password As String = ""
    Public lanHost As String = " (None)"
    Public initial_channel As String = My.Resources.ProgramName
    Public CKL_server As String = ""
    Private Const version As UInt16 = 1

    Public Sub New(Optional ByVal name As String = "Default")
        Me.name = name
    End Sub
    Public Sub New(ByVal r As IO.BinaryReader)
        load(r)
    End Sub

    Public Sub load(ByVal r As IO.BinaryReader)
        Dim version = r.ReadUInt16()
        name = r.ReadString()
        users.Load(r)
        roc_cd_key = r.ReadString()
        tft_cd_key = r.ReadString()
        username = r.ReadString()
        password = r.ReadString()
        server = r.ReadString()
        listen_port = r.ReadUInt16()
        initial_channel = r.ReadString()
        CKL_server = r.ReadString()
        If version >= 1 Then
            r.ReadString() 'lan_admin_host
            r.ReadUInt16() 'lan_admin_port
            lanHost = r.ReadString()
            r.ReadString() 'lan_admin_password
        End If
    End Sub

    Public Sub save(ByVal w As IO.BinaryWriter)
        w.Write(version)
        w.Write(name)
        users.Save(w)
        w.Write(roc_cd_key)
        w.Write(tft_cd_key)
        w.Write(username)
        w.Write(password)
        w.Write(server)
        w.Write(listen_port)
        w.Write(initial_channel)
        w.Write(CKL_server)
        If version >= 1 Then
            w.Write(" (None)") 'old default lan_admin_host
            w.Write(CUShort(6114)) 'old default lan_admin_port
            w.Write(lanHost)
            w.Write("") 'old default lan_admin_password
        End If
    End Sub

    Public Function clone(Optional ByVal name As String = Nothing) As ClientProfile
        Dim new_profile As New ClientProfile
        If name Is Nothing Then name = Me.name
        With new_profile
            .users = users.Clone()
            .roc_cd_key = roc_cd_key
            .tft_cd_key = tft_cd_key
            .username = username
            .password = password
            .server = server
            .name = name
            .listen_port = listen_port
            .initial_channel = initial_channel
            .CKL_server = CKL_server
        End With
        Return new_profile
    End Function
End Class
