Public Class ClientProfile
    Public name As String
    Public users As New BotUserSet()
    Public cdKeyROC As String = ""
    Public cdKeyTFT As String = ""
    Public userName As String = ""
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
    Public Sub New(ByVal reader As IO.BinaryReader)
        Contract.Requires(reader IsNot Nothing)
        Load(reader)
    End Sub

    Public Sub Load(ByVal reader As IO.BinaryReader)
        Contract.Requires(reader IsNot Nothing)
        Dim version = reader.ReadUInt16()
        name = reader.ReadString()
        users.Load(reader)
        cdKeyROC = reader.ReadString()
        cdKeyTFT = reader.ReadString()
        userName = reader.ReadString()
        password = reader.ReadString()
        server = reader.ReadString()
        listenPort = reader.ReadUInt16()
        initialChannel = reader.ReadString()
        keyServerAddress = reader.ReadString()
        If version >= 1 Then
            reader.ReadString() 'lan_admin_host
            reader.ReadUInt16() 'lan_admin_port
            lanHost = reader.ReadString()
            reader.ReadString() 'lan_admin_password
        End If
    End Sub

    Public Sub Save(ByVal bw As IO.BinaryWriter)
        Contract.Requires(bw IsNot Nothing)
        bw.Write(version)
        bw.Write(name)
        users.Save(bw)
        bw.Write(cdKeyROC)
        bw.Write(cdKeyTFT)
        bw.Write(userName)
        bw.Write(password)
        bw.Write(server)
        bw.Write(listenPort)
        bw.Write(initialChannel)
        bw.Write(keyServerAddress)
        If version >= 1 Then
            bw.Write(" (None)") 'old default lan_admin_host
            bw.Write(CUShort(6114)) 'old default lan_admin_port
            bw.Write(lanHost)
            bw.Write("") 'old default lan_admin_password
        End If
    End Sub

    Public Function Clone(Optional ByVal newName As String = Nothing) As ClientProfile
        Dim newProfile = New ClientProfile
        With newProfile
            .users = users.Clone()
            .cdKeyROC = cdKeyROC
            .cdKeyTFT = cdKeyTFT
            .userName = userName
            .password = password
            .server = server
            .name = If(newName, Me.name)
            .listenPort = listenPort
            .initialChannel = initialChannel
            .keyServerAddress = keyServerAddress
        End With
        Return newProfile
    End Function
End Class
