Public NotInheritable Class BotUser
    Private ReadOnly _name As String
    Private _settingMap As New Dictionary(Of String, String)
    Private _permissionMap As New Dictionary(Of String, UInteger)
    Private Const MAX_PERMISSION_VALUE As UInteger = 10
    Private Const MIN_PERMISSION_VALUE As UInteger = 0
    Private Const SEPARATION_CHAR As Char = ";"c
    Private ReadOnly Property PermissionMap As Dictionary(Of String, UInteger)
        Get
            Contract.Ensures(Contract.Result(Of Dictionary(Of String, UInteger))() IsNot Nothing)
            Return _permissionMap
        End Get
    End Property
    Private ReadOnly Property SettingMap As Dictionary(Of String, String)
        Get
            Contract.Ensures(Contract.Result(Of Dictionary(Of String, String))() IsNot Nothing)
            Return _settingMap
        End Get
    End Property

    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(_name IsNot Nothing)
        Contract.Invariant(_settingMap IsNot Nothing)
        Contract.Invariant(_permissionMap IsNot Nothing)
    End Sub

    Public ReadOnly Property Name As String
        Get
            Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
            Return _name
        End Get
    End Property

    Public Shared Function Pack(ByVal value As String) As String
        Contract.Requires(value IsNot Nothing)
        Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
        value = value.Replace("\", "\/")
        value = value.Replace(SEPARATION_CHAR, "\sep/")
        value = value.Replace(Environment.NewLine, "\n")
        value = value.Replace(vbTab, "\t")
        value = value.Replace("=", "\eq/")
        Return value
    End Function
    Public Shared Function Unpack(ByVal value As String) As String
        Contract.Requires(value IsNot Nothing)
        Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
        value = value.Replace("\eq/", "=")
        value = value.Replace("\t", vbTab)
        value = value.Replace("\n", Environment.NewLine)
        value = value.Replace("\sep/", SEPARATION_CHAR)
        value = value.Replace("\/", "\")
        Return value
    End Function

    Public Function PackPermissions() As String
        Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
        Return (From pair In permissionMap
                Select "{0}={1}".Frmt(Pack(pair.Key), pair.Value)
                ).StringJoin(SEPARATION_CHAR)
    End Function
    Public Function PackSettings() As String
        Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
        Return (From pair In settingMap
                Select "{0}={1}".Frmt(Pack(pair.Key), Pack(pair.Value))
                ).StringJoin(SEPARATION_CHAR)
    End Function

    Public Shared Function UnpackPermissions(ByVal packedPermissions As String) As Dictionary(Of String, UInteger)
        Contract.Requires(packedPermissions IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Dictionary(Of String, UInteger))() IsNot Nothing)

        Dim permissionMap = New Dictionary(Of String, UInteger)
        For Each key In packedPermissions.Split(";"c)
            Contract.Assume(key IsNot Nothing)
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
        Contract.Requires(packedSettings IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Dictionary(Of String, String))() IsNot Nothing)

        Dim settingMap As New Dictionary(Of String, String)
        For Each key In packedSettings.Split(";"c)
            Contract.Assume(key IsNot Nothing)
            Dim pair = key.Split("="c)
            If pair.Length <> 2 Then Continue For
            Contract.Assume(pair(1) IsNot Nothing)
            settingMap(Unpack(pair(0))) = Unpack(pair(1))
        Next key
        Return settingMap
    End Function

    Public Sub New(ByVal name As String,
                   Optional ByVal packedPermissions As String = Nothing,
                   Optional ByVal packedSettings As String = Nothing)
        Contract.Requires(name IsNot Nothing)
        Me._name = name
        If packedPermissions IsNot Nothing Then
            UpdatePermissions(packedPermissions)
        End If
        If packedSettings IsNot Nothing Then
            UpdateSettings(packedSettings)
        End If
    End Sub

    Public Sub UpdatePermissions(ByVal packedPermissions As String)
        Contract.Requires(packedPermissions IsNot Nothing)
        _permissionMap = UnpackPermissions(packedPermissions)
    End Sub
    Public Sub UpdateSettings(ByVal packedSettings As String)
        Contract.Requires(packedSettings IsNot Nothing)
        _settingMap = UnpackSettings(packedSettings)
    End Sub

    Public Property Permission(ByVal key As String) As UInteger
        Get
            Contract.Requires(key IsNot Nothing)
            If Not PermissionMap.ContainsKey(key.ToUpperInvariant) Then Return 0
            Return PermissionMap(key.ToUpperInvariant)
        End Get
        Set(ByVal value As UInteger)
            Contract.Requires(key IsNot Nothing)
            PermissionMap(key.ToUpperInvariant) = value
        End Set
    End Property
    Public Property Setting(ByVal key As String) As String
        Get
            Contract.Requires(key IsNot Nothing)
            If Not SettingMap.ContainsKey(key.ToUpperInvariant) Then Return Nothing
            Return SettingMap(key.ToUpperInvariant)
        End Get
        Set(ByVal value As String)
            Contract.Requires(key IsNot Nothing)
            SettingMap(key.ToUpperInvariant) = value
        End Set
    End Property

    Public Sub Save(ByVal writer As IO.BinaryWriter)
        Contract.Requires(writer IsNot Nothing)
        writer.Write(Name)
        writer.Write(CUShort(settingMap.Keys.Count))
        For Each pair In settingMap
            Contract.Assume(pair.Key IsNot Nothing)
            Contract.Assume(pair.Value IsNot Nothing)
            writer.Write(pair.Key)
            writer.Write(pair.Value)
        Next pair
        writer.Write(CUShort(permissionMap.Keys.Count))
        For Each pair In permissionMap
            Contract.Assume(pair.Key IsNot Nothing)
            writer.Write(pair.Key)
            writer.Write(pair.Value)
        Next pair
    End Sub
    Public Sub New(ByVal reader As IO.BinaryReader)
        Contract.Requires(reader IsNot Nothing)
        Me._name = reader.ReadString()
        For i = 1 To reader.ReadUInt16()
            SettingMap(reader.ReadString().ToUpperInvariant) = reader.ReadString()
        Next i
        For i = 1 To reader.ReadUInt16()
            PermissionMap(reader.ReadString().ToUpperInvariant) = reader.ReadUInt32()
        Next i
    End Sub

    Public Function Clone(Optional ByVal newName As String = Nothing) As BotUser
        Contract.Ensures(Contract.Result(Of BotUser)() IsNot Nothing)
        If newName Is Nothing Then newName = Name
        Dim newUser As New BotUser(newName)
        For Each key In SettingMap.Keys
            Contract.Assume(key IsNot Nothing)
            newUser.SettingMap(key.ToUpperInvariant) = SettingMap(key)
        Next key
        For Each key In PermissionMap.Keys
            newUser.PermissionMap(key.ToUpperInvariant) = PermissionMap(key)
        Next key
        Return newUser
    End Function

    Public Shared Operator <(ByVal user1 As BotUser, ByVal user2 As BotUser) As Boolean
        Contract.Requires(user1 IsNot Nothing)
        Contract.Requires(user2 IsNot Nothing)
        If Not user1 <= user2 Then Return False

        For Each user As BotUser In New BotUser() {user1, user2}
            Contract.Assume(user IsNot Nothing)
            For Each key In user.permissionMap.Keys
                Contract.Assume(key IsNot Nothing)
                If user1.Permission(key) < user2.Permission(key) Then Return True
            Next key
        Next user

        Return False
    End Operator
    Public Shared Operator >(ByVal user1 As BotUser, ByVal user2 As BotUser) As Boolean
        Contract.Requires(user1 IsNot Nothing)
        Contract.Requires(user2 IsNot Nothing)
        Return user2 < user1
    End Operator
    Public Shared Operator <=(ByVal user1 As BotUser, ByVal user2 As BotUser) As Boolean
        Contract.Requires(user1 IsNot Nothing)
        Contract.Requires(user2 IsNot Nothing)
        For Each user In {user1, user2}
            Contract.Assume(user IsNot Nothing)
            For Each key In user.permissionMap.Keys
                Contract.Assume(key IsNot Nothing)
                If user1.Permission(key) > user2.Permission(key) Then Return False
            Next key
        Next user

        Return True
    End Operator
    Public Shared Operator >=(ByVal user1 As BotUser, ByVal user2 As BotUser) As Boolean
        Contract.Requires(user1 IsNot Nothing)
        Contract.Requires(user2 IsNot Nothing)
        Return user2 <= user1
    End Operator

    Public Overrides Function ToString() As String
        Return "{0}: {1}".Frmt(Name, (From pair In permissionMap
                                      Where pair.Value <> 0
                                      Select "{0}={1}".Frmt(pair.Key, pair.Value)
                                      ).StringJoin(", "))
    End Function
End Class

Public NotInheritable Class BotUserSet
    Private ReadOnly userMap As New Dictionary(Of String, BotUser)
    Private ReadOnly tempUserMap As New Dictionary(Of String, BotUser)
    Public Const UnknownUserKey As String = "*unknown"
    Public Const NewUserKey As String = "*new"

    Public Function Users() As IEnumerable(Of BotUser)
        Return userMap.Values()
    End Function

    Public Function CreateNewUser(ByVal name As String) As BotUser
        Contract.Requires(name IsNot Nothing)
        Contract.Ensures(Contract.Result(Of BotUser)() IsNot Nothing)
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
            Contract.Assume(name IsNot Nothing)
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
            Contract.Assume(user IsNot Nothing)
            newUserSet.AddUser(user.Clone())
        Next user
        Return newUserSet
    End Function
End Class