Public Class BotUser
    Public ReadOnly name As String
    Private settingMap As New Dictionary(Of String, String)
    Private permissionMap As New Dictionary(Of String, UInteger)
    Private Const MAX_PERMISSION_VALUE As UInteger = 10
    Private Const MIN_PERMISSION_VALUE As UInteger = 0
    Private Const SEPARATION_CHAR As Char = ";"c

    Public Shared Function Pack(ByVal value As String) As String
        value = value.Replace("\", "\/")
        value = value.Replace(SEPARATION_CHAR, "\sep/")
        value = value.Replace(Environment.NewLine, "\n")
        value = value.Replace(vbTab, "\t")
        value = value.Replace("=", "\eq/")
        Return value
    End Function
    Public Shared Function Unpack(ByVal value As String) As String
        value = value.Replace("\eq/", "=")
        value = value.Replace("\t", vbTab)
        value = value.Replace("\n", Environment.NewLine)
        value = value.Replace("\sep/", SEPARATION_CHAR)
        value = value.Replace("\/", "\")
        Return value
    End Function

    Public Function PackPermissions() As String
        Dim packedVals = ""
        For Each key In permissionMap.Keys
            If packedVals <> "" Then packedVals += SEPARATION_CHAR
            packedVals += "{0}={1}".Frmt(Pack(key), permissionMap(key))
        Next key
        Return packedVals
    End Function
    Public Function PackSettings() As String
        Dim packedVals As String = ""
        For Each key In settingMap.Keys
            If packedVals <> "" Then packedVals += SEPARATION_CHAR
            packedVals += Pack(key) + "=" + Pack(settingMap(key))
        Next key
        Return packedVals
    End Function

    Public Shared Function UnpackPermissions(ByVal packedPermissions As String) As Dictionary(Of String, UInteger)
        Dim permissionMap = New Dictionary(Of String, UInteger)
        For Each key In packedPermissions.Split(";"c)
            Dim pair = key.Split("="c)
            If pair.Length <> 2 Then Continue For
            Dim value As UInteger
            If Not UInteger.TryParse(pair(1), value) Then Continue For
            value = value.Between(MIN_PERMISSION_VALUE, MAX_PERMISSION_VALUE)
            permissionMap(Unpack(pair(0))) = value
        Next key
        Return permissionMap
    End Function
    Public Shared Function UnpackSettings(ByVal packedSettings As String) As Dictionary(Of String, String)
        Dim settingMap As New Dictionary(Of String, String)
        For Each key In packedSettings.Split(";"c)
            Dim pair = key.Split("="c)
            If pair.Length <> 2 Then Continue For
            settingMap(Unpack(pair(0))) = Unpack(pair(1))
        Next key
        Return settingMap
    End Function

    Public Sub New(ByVal name As String,
                   Optional ByVal packedPermissions As String = Nothing,
                   Optional ByVal packedSettings As String = Nothing)
        Me.name = name
        If packedPermissions IsNot Nothing Then
            UpdatePermissions(packedPermissions)
        End If
        If packedSettings IsNot Nothing Then
            UpdateSettings(packedSettings)
        End If
    End Sub

    Public Sub UpdatePermissions(ByVal packedPermissions As String)
        permissionMap = UnpackPermissions(packedPermissions)
    End Sub
    Public Sub UpdateSettings(ByVal packedSettings As String)
        settingMap = UnpackSettings(packedSettings)
    End Sub

    Public Property Permission(ByVal key As String) As UInteger
        Get
            If Not permissionMap.ContainsKey(key) Then Return 0
            Return permissionMap(key)
        End Get
        Set(ByVal value As UInteger)
            permissionMap(key) = value
        End Set
    End Property
    Public Property Setting(ByVal key As String) As String
        Get
            If Not settingMap.ContainsKey(key) Then Return Nothing
            Return settingMap(key)
        End Get
        Set(ByVal value As String)
            settingMap(key) = value
        End Set
    End Property

    Public Sub Save(ByVal bw As IO.BinaryWriter)
        bw.Write(name)
        bw.Write(CUShort(settingMap.Keys.Count))
        For Each key As String In settingMap.Keys
            bw.Write(key)
            bw.Write(settingMap(key))
        Next key
        bw.Write(CUShort(permissionMap.Keys.Count))
        For Each key As String In permissionMap.Keys
            bw.Write(key)
            bw.Write(permissionMap(key))
        Next key
    End Sub
    Public Sub New(ByVal reader As IO.BinaryReader)
        name = reader.ReadString()
        For i = 1 To reader.ReadUInt16()
            settingMap(reader.ReadString()) = reader.ReadString()
        Next i
        For i = 1 To reader.ReadUInt16()
            permissionMap(reader.ReadString()) = reader.ReadUInt32()
        Next i
    End Sub

    Public Function Clone(Optional ByVal newName As String = Nothing) As BotUser
        If newName Is Nothing Then newName = name
        Dim newUser As New BotUser(newName)
        For Each key As String In settingMap.Keys
            newUser.settingMap(key) = settingMap(key)
        Next key
        For Each key As String In permissionMap.Keys
            newUser.permissionMap(key) = permissionMap(key)
        Next key
        Return newUser
    End Function

    Public Shared Operator <(ByVal user1 As BotUser, ByVal user2 As BotUser) As Boolean
        If Not user1 <= user2 Then Return False

        For Each user As BotUser In New BotUser() {user1, user2}
            For Each key As String In user.permissionMap.Keys
                If user1.Permission(key) < user2.Permission(key) Then Return True
            Next key
        Next user

        Return False
    End Operator
    Public Shared Operator >(ByVal user1 As BotUser, ByVal user2 As BotUser) As Boolean
        Return user2 < user1
    End Operator
    Public Shared Operator <=(ByVal user1 As BotUser, ByVal user2 As BotUser) As Boolean
        For Each user As BotUser In New BotUser() {user1, user2}
            For Each key As String In user.permissionMap.Keys
                If user1.Permission(key) > user2.Permission(key) Then Return False
            Next key
        Next user

        Return True
    End Operator
    Public Shared Operator >=(ByVal user1 As BotUser, ByVal user2 As BotUser) As Boolean
        Return user2 <= user1
    End Operator

    Public Overrides Function ToString() As String
        ToString = name + ":"
        For Each key As String In permissionMap.Keys
            If permissionMap(key) = 0 Then Continue For
            ToString += " {0}={1}".Frmt(key, permissionMap(key))
        Next key
    End Function
End Class

Public Class BotUserSet
    Private ReadOnly userMap As New Dictionary(Of String, BotUser)
    Private ReadOnly tempUserMap As New Dictionary(Of String, BotUser)
    Public Const UnknownUserKey As String = "*unknown"
    Public Const NewUserKey As String = "*new"

    Public Function Users() As IEnumerable(Of BotUser)
        Return userMap.Values()
    End Function

    Public Function CreateNewUser(ByVal name As String) As BotUser
        Contract.Requires(name IsNot Nothing)
        Dim key = name.ToUpperInvariant
        If userMap.ContainsKey(key) Then
            Throw New InvalidOperationException("User already exists")
        ElseIf userMap.ContainsKey(NewUserKey) Then
            Dim user = userMap(NewUserKey).Clone(name)
            AddUser(user)
            Return user
        Else
            Dim user = New BotUser(name)
            AddUser(user)
            Return user
        End If
    End Function

    Public Sub RemoveUser(ByVal name As String)
        Contract.Requires(name IsNot Nothing)
        If Not ContainsUser(name) Then Throw New InvalidOperationException("That user doesn't exist")
        userMap.Remove(name.ToUpperInvariant)
    End Sub
    Default Public ReadOnly Property User(ByVal name As String) As BotUser
        Get
            Contract.Requires(name IsNot Nothing)
            Dim key = name.ToUpperInvariant
            If userMap.ContainsKey(key) Then
                Return userMap(key)
            ElseIf tempUserMap.ContainsKey(key) Then
                Return tempUserMap(key)
            ElseIf userMap.ContainsKey(UnknownUserKey) Then
                tempUserMap(name) = userMap(UnknownUserKey).Clone(name)
                Return tempUserMap(name)
            Else
                Return Nothing
            End If
        End Get
    End Property

    Public Function ContainsUser(ByVal name As String) As Boolean
        Contract.Requires(name IsNot Nothing)
        Return userMap.ContainsKey(name.ToUpperInvariant)
    End Function

    Public Sub RemoveOtherUsers(ByVal userNames As IList(Of String))
        Contract.Requires(userNames IsNot Nothing)
        'Find users not in the usernames list
        Dim removedUsers = New List(Of String)
        For Each key In userMap.Keys
            Dim keep = False
            For Each keptName In userNames
                If keptName.ToUpperInvariant = key.ToUpperInvariant Then
                    keep = True
                    Exit For
                End If
            Next keptName
            If Not keep Then removedUsers.Add(key)
        Next key

        'Remove those users
        For Each name In removedUsers
            RemoveUser(name)
        Next name
    End Sub
    Public Sub UpdateUser(ByVal user As BotUser)
        Contract.Requires(user IsNot Nothing)
        If ContainsUser(user.name) Then
            Dim oldUser = Me(user.name)
            oldUser.UpdatePermissions(user.PackPermissions)
            oldUser.UpdateSettings(user.PackSettings)
        Else
            AddUser(user)
        End If
    End Sub

    Public Sub AddUser(ByVal user As BotUser)
        Contract.Requires(user IsNot Nothing)
        If ContainsUser(user.name) Then Throw New InvalidOperationException("That user already exists")
        userMap(user.name.ToUpperInvariant) = user
    End Sub

    Public Sub Load(ByVal reader As IO.BinaryReader)
        Contract.Requires(reader IsNot Nothing)
        For i = 1 To reader.ReadUInt16()
            AddUser(New BotUser(reader))
        Next i
    End Sub
    Public Sub Save(ByVal bw As IO.BinaryWriter)
        Contract.Requires(bw IsNot Nothing)
        bw.Write(CUShort(userMap.Keys.Count))
        For Each e In userMap.Values
            e.Save(bw)
        Next e
    End Sub

    Public Function Clone() As BotUserSet
        Dim newUserSet As New BotUserSet
        For Each user As BotUser In userMap.Values
            newUserSet.AddUser(user.Clone())
        Next user
        Return newUserSet
    End Function
End Class