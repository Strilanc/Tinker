Public NotInheritable Class BotUser
    Private ReadOnly _name As InvariantString
    Private _settingMap As New Dictionary(Of InvariantString, String)
    Private _permissionMap As New Dictionary(Of InvariantString, UInteger)
    Private Const MAX_PERMISSION_VALUE As UInteger = 10
    Private Const MIN_PERMISSION_VALUE As UInteger = 0
    Private Const SEPARATION_CHAR As Char = ","c
    Private ReadOnly Property PermissionMap As Dictionary(Of InvariantString, UInteger)
        Get
            Contract.Ensures(Contract.Result(Of Dictionary(Of InvariantString, UInteger))() IsNot Nothing)
            Return _permissionMap
        End Get
    End Property
    Private ReadOnly Property SettingMap As Dictionary(Of InvariantString, String)
        Get
            Contract.Ensures(Contract.Result(Of Dictionary(Of InvariantString, String))() IsNot Nothing)
            Return _settingMap
        End Get
    End Property

    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(_settingMap IsNot Nothing)
        Contract.Invariant(_permissionMap IsNot Nothing)
    End Sub

    Public ReadOnly Property Name As InvariantString
        Get
            Return _name
        End Get
    End Property

    Public Shared Function Pack(ByVal value As String) As String
        Contract.Requires(value IsNot Nothing)
        Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
        value = value.Replace("\", "\/")
        value = value.Replace(SEPARATION_CHAR, "\sep/")
        value = value.Replace(Environment.NewLine, "\n")
        value = value.Replace(Microsoft.VisualBasic.vbTab, "\t")
        value = value.Replace("=", "\eq/")
        value = value.Replace(":", "\colon/")
        Return value
    End Function
    Public Shared Function Unpack(ByVal value As String) As String
        Contract.Requires(value IsNot Nothing)
        Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
        value = value.Replace("\colon/", ":")
        value = value.Replace("\eq/", "=")
        value = value.Replace("\t", Microsoft.VisualBasic.vbTab)
        value = value.Replace("\n", Environment.NewLine)
        value = value.Replace("\sep/", SEPARATION_CHAR)
        value = value.Replace("\/", "\")
        Return value
    End Function

    Public Function PackPermissions() As String
        Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
        Return (From pair In PermissionMap
                Select "{0}:{1}".Frmt(Pack(pair.Key), pair.Value)
                ).StringJoin(SEPARATION_CHAR)
    End Function
    Public Function PackSettings() As String
        Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
        Return (From pair In SettingMap
                Select "{0}:{1}".Frmt(Pack(pair.Key), Pack(pair.Value))
                ).StringJoin(SEPARATION_CHAR)
    End Function

    Public Shared Function UnpackPermissions(ByVal packedPermissions As String) As Dictionary(Of InvariantString, UInteger)
        Contract.Requires(packedPermissions IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Dictionary(Of InvariantString, UInteger))() IsNot Nothing)
        Dim permissionMap = New Dictionary(Of InvariantString, UInteger)
        For Each key In packedPermissions.Split(SEPARATION_CHAR)
            Contract.Assume(key IsNot Nothing)
            Dim pair = key.Split("="c, ":"c)
            If pair.Length <> 2 Then Continue For
            Dim value As UInteger
            If Not UInteger.TryParse(pair(1), value) Then Continue For
            value = value.Between(MIN_PERMISSION_VALUE, MAX_PERMISSION_VALUE)
            permissionMap(Unpack(pair(0))) = value
        Next key
        Return permissionMap
    End Function
    Public Shared Function UnpackSettings(ByVal packedSettings As String) As Dictionary(Of InvariantString, String)
        Contract.Requires(packedSettings IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Dictionary(Of InvariantString, String))() IsNot Nothing)

        Dim settingMap As New Dictionary(Of InvariantString, String)
        For Each key In packedSettings.Split(SEPARATION_CHAR)
            Contract.Assume(key IsNot Nothing)
            Dim pair = key.Split("="c, ":"c)
            If pair.Length <> 2 Then Continue For
            Contract.Assume(pair(1) IsNot Nothing)
            settingMap(Unpack(pair(0))) = Unpack(pair(1))
        Next key
        Return settingMap
    End Function

    Public Sub New(ByVal name As InvariantString,
                   Optional ByVal packedPermissions As String = Nothing,
                   Optional ByVal packedSettings As String = Nothing)
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

    Public Property Permission(ByVal key As InvariantString) As UInteger
        Get
            If Not PermissionMap.ContainsKey(key) Then Return 0
            Return PermissionMap(key)
        End Get
        Set(ByVal value As UInteger)
            PermissionMap(key) = value
        End Set
    End Property
    Public Property Setting(ByVal key As InvariantString) As String
        Get
            If Not SettingMap.ContainsKey(key) Then Return Nothing
            Return SettingMap(key)
        End Get
        Set(ByVal value As String)
            SettingMap(key) = value
        End Set
    End Property

    Public Sub Save(ByVal writer As IO.BinaryWriter)
        Contract.Requires(writer IsNot Nothing)
        writer.Write(Name)
        writer.Write(CUShort(settingMap.Keys.Count))
        For Each pair In settingMap
            Contract.Assume(pair.Value IsNot Nothing)
            writer.Write(pair.Key)
            writer.Write(pair.Value)
        Next pair
        writer.Write(CUShort(permissionMap.Keys.Count))
        For Each pair In permissionMap
            writer.Write(pair.Key)
            writer.Write(pair.Value)
        Next pair
    End Sub
    <ContractVerification(False)>
    Public Sub New(ByVal reader As IO.BinaryReader)
        Contract.Requires(reader IsNot Nothing)
        Me._name = reader.ReadString()
        Dim settingCount = reader.ReadUInt16()
        For Each repeat In settingCount.Range
            SettingMap(reader.ReadString()) = reader.ReadString()
        Next repeat
        Dim permissionCount = reader.ReadUInt16()
        For Each repeat In permissionCount.Range
            PermissionMap(reader.ReadString()) = reader.ReadUInt32()
        Next repeat
    End Sub

    Public Function Clone(Optional ByVal newName As InvariantString? = Nothing) As BotUser
        Contract.Ensures(Contract.Result(Of BotUser)() IsNot Nothing)
        If newName Is Nothing Then newName = Name
        Dim newUser As New BotUser(newName.Value)
        For Each key In SettingMap.Keys
            newUser.SettingMap(key) = SettingMap(key)
        Next key
        For Each key In PermissionMap.Keys
            newUser.PermissionMap(key) = PermissionMap(key)
        Next key
        Return newUser
    End Function

    Public Shared Operator <(ByVal user1 As BotUser, ByVal user2 As BotUser) As Boolean
        Contract.Requires(user1 IsNot Nothing)
        Contract.Requires(user2 IsNot Nothing)
        If Not user1 <= user2 Then Return False

        For Each user In {user1, user2}
            For Each key In user.PermissionMap.Keys
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
            For Each key In user.permissionMap.Keys
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
        Return "{0}: {1}".Frmt(Name, (From pair In PermissionMap
                                      Where pair.Value <> 0
                                      Select "{0}:{1}".Frmt(pair.Key, pair.Value)
                                      ).StringJoin(", "))
    End Function
End Class

Public NotInheritable Class BotUserSet
    Private ReadOnly _userMap As New Dictionary(Of InvariantString, BotUser)
    Private ReadOnly tempUserMap As New Dictionary(Of InvariantString, BotUser)
    Public Shared ReadOnly UnknownUserKey As InvariantString = "*unknown"
    Public Shared ReadOnly NewUserKey As InvariantString = "*new"

    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(_userMap IsNot Nothing)
        Contract.Invariant(tempUserMap IsNot Nothing)
    End Sub

    Public ReadOnly Property Users() As IEnumerable(Of BotUser)
        Get
            Contract.Ensures(Contract.Result(Of IEnumerable(Of BotUser))() IsNot Nothing)
            Return _userMap.Values()
        End Get
    End Property

    Public Function CreateNewUser(ByVal name As InvariantString) As BotUser
        Contract.Ensures(Contract.Result(Of BotUser)() IsNot Nothing)
        If _userMap.ContainsKey(name) Then
            Throw New InvalidOperationException("User already exists")
        ElseIf _userMap.ContainsKey(NewUserKey) Then
            Dim user = _userMap(NewUserKey).AssumeNotNull.Clone(name)
            AddUser(user)
            Return user
        Else
            Dim user = New BotUser(name)
            AddUser(user)
            Return user
        End If
    End Function

    Public Sub RemoveUser(ByVal name As InvariantString)
        If Not ContainsUser(name) Then Throw New InvalidOperationException("That user doesn't exist")
        _userMap.Remove(name)
    End Sub
    <CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1043:UseIntegralOrStringArgumentForIndexers")>
    Default Public ReadOnly Property User(ByVal name As InvariantString) As BotUser
        Get
            If _userMap.ContainsKey(name) Then
                Return _userMap(name)
            ElseIf tempUserMap.ContainsKey(name) Then
                Return tempUserMap(name)
            ElseIf _userMap.ContainsKey(UnknownUserKey) Then
                tempUserMap(name) = _userMap(UnknownUserKey).AssumeNotNull.Clone(name)
                Return tempUserMap(name)
            Else
                Return Nothing
            End If
        End Get
    End Property

    <Pure()>
    Public Function ContainsUser(ByVal name As InvariantString) As Boolean
        Return _userMap.ContainsKey(name)
    End Function

    Public Sub RemoveAllExcept(ByVal userNames As IEnumerable(Of InvariantString))
        Contract.Requires(userNames IsNot Nothing)
        For Each name In _userMap.Keys.Except(userNames)
            RemoveUser(name)
        Next name
    End Sub
    Public Sub UpdateUser(ByVal user As BotUser)
        Contract.Requires(user IsNot Nothing)
        If ContainsUser(user.Name) Then
            Dim oldUser = Me(user.Name)
            Contract.Assume(oldUser IsNot Nothing)
            oldUser.UpdatePermissions(user.PackPermissions)
            oldUser.UpdateSettings(user.PackSettings)
        Else
            AddUser(user)
        End If
    End Sub

    Public Sub AddUser(ByVal user As BotUser)
        Contract.Requires(user IsNot Nothing)
        If ContainsUser(user.Name) Then Throw New InvalidOperationException("That user already exists")
        _userMap(user.Name) = user
    End Sub

    <ContractVerification(False)>
    Public Sub Load(ByVal reader As IO.BinaryReader)
        Contract.Requires(reader IsNot Nothing)
        Dim userCount = reader.ReadUInt16()
        For Each repeat In userCount.Range
            AddUser(New BotUser(reader))
        Next repeat
    End Sub
    Public Sub Save(ByVal bw As IO.BinaryWriter)
        Contract.Requires(bw IsNot Nothing)
        bw.Write(CUShort(_userMap.Keys.Count))
        For Each e In _userMap.Values
            Contract.Assume(e IsNot Nothing)
            e.Save(bw)
        Next e
    End Sub

    <Pure()>
    Public Function Clone() As BotUserSet
        Contract.Ensures(Contract.Result(Of BotUserSet)() IsNot Nothing)
        Dim newUserSet As New BotUserSet
        For Each user As BotUser In _userMap.Values
            Contract.Assume(user IsNot Nothing)
            newUserSet.AddUser(user.Clone())
        Next user
        Return newUserSet
    End Function
End Class
