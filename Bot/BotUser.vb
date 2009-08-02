Public Class BotUser
    Public ReadOnly name As String
    Private setting_map As New Dictionary(Of String, String)
    Private permission_map As New Dictionary(Of String, UInteger)
    Private Const MAX_PERMISSION_VALUE As UInteger = 10
    Private Const MIN_PERMISSION_VALUE As UInteger = 0
    Private Const SEPARATION_CHAR As Char = ";"c

    Public Shared Function pack2(ByVal s As String) As String
        s = s.Replace("\", "\/")
        s = s.Replace(SEPARATION_CHAR, "\sep/")
        s = s.Replace(Environment.NewLine, "\n")
        s = s.Replace(vbTab, "\t")
        s = s.Replace("=", "\eq/")
        Return s
    End Function
    Public Shared Function unpack2(ByVal s As String) As String
        s = s.Replace("\eq/", "=")
        s = s.Replace("\t", vbTab)
        s = s.Replace("\n", Environment.NewLine)
        s = s.Replace("\sep/", SEPARATION_CHAR)
        s = s.Replace("\/", "\")
        Return s
    End Function

    Public Function packPermissions() As String
        Dim packed_vals As String = ""
        For Each permission As String In permission_map.Keys
            If packed_vals <> "" Then packed_vals += SEPARATION_CHAR
            packed_vals += pack2(permission) + "=" + permission_map(permission).ToString()
        Next permission
        Return packed_vals
    End Function
    Public Function packSettings() As String
        Dim packed_vals As String = ""
        For Each setting As String In setting_map.Keys
            If packed_vals <> "" Then packed_vals += SEPARATION_CHAR
            packed_vals += pack2(setting) + "=" + pack2(setting_map(setting))
        Next setting
        Return packed_vals
    End Function

    Public Shared Function unpackPermissions(ByVal packed_permissions As String) As Dictionary(Of String, UInteger)
        Dim permission_map As New Dictionary(Of String, UInteger)
        For Each permission As String In packed_permissions.Split(";"c)
            Dim pair() As String = permission.Split("="c)
            If pair.Length <> 2 Then Continue For
            Dim v As UInteger
            If Not UInteger.TryParse(pair(1), v) Then Continue For
            v = v.Between(MIN_PERMISSION_VALUE, MAX_PERMISSION_VALUE)
            permission_map(unpack2(pair(0))) = v
        Next permission
        Return permission_map
    End Function
    Public Shared Function unpackSettings(ByVal packed_settings As String) As Dictionary(Of String, String)
        Dim setting_map As New Dictionary(Of String, String)
        For Each setting As String In packed_settings.Split(";"c)
            Dim pair() As String = setting.Split("="c)
            If pair.Length <> 2 Then Continue For
            setting_map(unpack2(pair(0))) = unpack2(pair(1))
        Next setting
        Return setting_map
    End Function

    Public Sub New(ByVal name As String, Optional ByVal packed_permissions As String = Nothing, Optional ByVal packed_settings As String = Nothing)
        Me.name = name
        If packed_permissions IsNot Nothing Then
            updatePermissions(packed_permissions)
        End If
        If packed_settings IsNot Nothing Then
            updateSettings(packed_settings)
        End If
    End Sub

    Public Sub updatePermissions(ByVal packed_permissions As String)
        permission_map = unpackPermissions(packed_permissions)
    End Sub
    Public Sub updateSettings(ByVal packed_settings As String)
        setting_map = unpackSettings(packed_settings)
    End Sub

    Public Property permission(ByVal key As String) As UInteger
        Get
            If Not permission_map.ContainsKey(key) Then Return 0
            Return permission_map(key)
        End Get
        Set(ByVal value As UInteger)
            permission_map(key) = value
        End Set
    End Property
    Public Property setting(ByVal key As String) As String
        Get
            If Not setting_map.ContainsKey(key) Then Return Nothing
            Return setting_map(key)
        End Get
        Set(ByVal value As String)
            setting_map(key) = value
        End Set
    End Property
    Public Function getSettings() As IEnumerable(Of String)
        Return setting_map.Keys
    End Function
    Public Function getPermissions() As IEnumerable(Of String)
        Return permission_map.Keys
    End Function

    Public Sub save(ByVal w As IO.BinaryWriter)
        w.Write(name)
        w.Write(CUShort(setting_map.Keys.Count))
        For Each key As String In setting_map.Keys
            w.Write(key)
            w.Write(setting_map(key))
        Next key
        w.Write(CUShort(permission_map.Keys.Count))
        For Each key As String In permission_map.Keys
            w.Write(key)
            w.Write(permission_map(key))
        Next key
    End Sub
    Public Sub New(ByVal r As IO.BinaryReader)
        name = r.ReadString()
        For i As Integer = 1 To r.ReadUInt16()
            Dim key As String = r.ReadString()
            setting_map(key) = r.ReadString()
        Next i
        For i As Integer = 1 To r.ReadUInt16()
            Dim key As String = r.ReadString()
            permission_map(key) = r.ReadUInt32()
        Next i
    End Sub

    Public Function clone(Optional ByVal new_name As String = Nothing) As BotUser
        If new_name Is Nothing Then new_name = name
        Dim new_user As New BotUser(new_name)
        For Each key As String In setting_map.Keys
            new_user.setting_map(key) = setting_map(key)
        Next key
        For Each key As String In permission_map.Keys
            new_user.permission_map(key) = permission_map(key)
        Next key
        Return new_user
    End Function

    Public Shared Operator <(ByVal user1 As BotUser, ByVal user2 As BotUser) As Boolean
        If Not user1 <= user2 Then Return False

        For Each user As BotUser In New BotUser() {user1, user2}
            For Each key As String In user.permission_map.Keys
                If user1.permission(key) < user2.permission(key) Then Return True
            Next key
        Next user

        Return False
    End Operator
    Public Shared Operator >(ByVal user1 As BotUser, ByVal user2 As BotUser) As Boolean
        Return user2 < user1
    End Operator
    Public Shared Operator <=(ByVal user1 As BotUser, ByVal user2 As BotUser) As Boolean
        For Each user As BotUser In New BotUser() {user1, user2}
            For Each key As String In user.permission_map.Keys
                If user1.permission(key) > user2.permission(key) Then Return False
            Next key
        Next user

        Return True
    End Operator
    Public Shared Operator >=(ByVal user1 As BotUser, ByVal user2 As BotUser) As Boolean
        Return user2 <= user1
    End Operator

    Public Overrides Function ToString() As String
        ToString = name + ":"
        For Each key As String In permission_map.Keys
            If permission_map(key) = 0 Then Continue For
            ToString += " " + key + "=" + permission_map(key).ToString()
        Next key
    End Function
End Class

Public Class BotUserSet
    Private ReadOnly userMap As New Dictionary(Of String, BotUser)
    Private ReadOnly tempUserMap As New Dictionary(Of String, BotUser)
    Public Const NAME_UNKNOWN_USER As String = "*unknown"
    Public Const NAME_NEW_USER As String = "*new"

    Public Function users() As IEnumerable(Of BotUser)
        Return userMap.Values()
    End Function

    Public Function CreateNewUser(ByVal name As String) As BotUser
        Contract.Requires(name IsNot Nothing)
        Dim key = name.ToLower
        If userMap.ContainsKey(key) Then
            Throw New InvalidOperationException("User already exists")
        ElseIf userMap.ContainsKey(NAME_NEW_USER) Then
            Dim user = userMap(NAME_NEW_USER).clone(name)
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
        userMap.Remove(name.ToLower)
    End Sub
    Default Public ReadOnly Property user(ByVal name As String) As BotUser
        Get
            Contract.Requires(name IsNot Nothing)
            Dim key As String = name.ToLower
            If userMap.ContainsKey(key) Then
                Return userMap(key)
            ElseIf tempUserMap.ContainsKey(key) Then
                Return tempUserMap(key)
            ElseIf userMap.ContainsKey(NAME_UNKNOWN_USER) Then
                tempUserMap(name) = userMap(NAME_UNKNOWN_USER).clone(name)
                Return tempUserMap(name)
            Else
                Return Nothing
            End If
        End Get
    End Property

    Public Function ContainsUser(ByVal name As String) As Boolean
        Contract.Requires(name IsNot Nothing)
        Return userMap.ContainsKey(name.ToLower())
    End Function

    Public Sub RemoveOtherUsers(ByVal usernames As IList(Of String))
        Contract.Requires(usernames IsNot Nothing)
        'Find users not in the usernames list
        Dim removed_users = New List(Of String)
        For Each existing_name In userMap.Keys
            Dim keep = False
            For Each kept_name In usernames
                If kept_name.ToLower() = existing_name.ToLower() Then
                    keep = True
                    Exit For
                End If
            Next kept_name
            If Not keep Then removed_users.Add(existing_name)
        Next existing_name

        'Remove those users
        For Each name In removed_users
            RemoveUser(name)
        Next name
    End Sub
    Public Sub UpdateUser(ByVal user As BotUser)
        Contract.Requires(user IsNot Nothing)
        If ContainsUser(user.name) Then
            Dim oldUser = Me(user.name)
            oldUser.updatePermissions(user.packPermissions)
            oldUser.updateSettings(user.packSettings)
        Else
            AddUser(user)
        End If
    End Sub

    Public Sub AddUser(ByVal user As BotUser)
        Contract.Requires(user IsNot Nothing)
        If ContainsUser(user.name) Then Throw New InvalidOperationException("That user already exists")
        userMap(user.name.ToLower) = user
    End Sub

    Public Sub Load(ByVal r As IO.BinaryReader)
        Contract.Requires(r IsNot Nothing)
        For i As Integer = 1 To r.ReadUInt16()
            Dim new_user As New BotUser(r)
            AddUser(new_user)
        Next i
    End Sub
    Public Sub Save(ByVal w As IO.BinaryWriter)
        Contract.Requires(w IsNot Nothing)
        w.Write(CUShort(userMap.Keys.Count))
        For Each user As BotUser In userMap.Values
            user.save(w)
        Next user
    End Sub

    Public Function Clone() As BotUserSet
        Dim newUserSet As New BotUserSet
        For Each user As BotUser In userMap.Values
            newUserSet.AddUser(user.clone())
        Next user
        Return newUserSet
    End Function
End Class