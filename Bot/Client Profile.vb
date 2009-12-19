Namespace Bot
    Public NotInheritable Class ClientProfile
        Public name As InvariantString
        Public users As New BotUserSet()
        Public cdKeyROC As String = ""
        Public cdKeyTFT As String = ""
        Public userName As String = ""
        Public password As String = ""
        Public server As String = "useast.battle.net (Azeroth)"
        Private _lanHost As String = " (None)"
        Public initialChannel As String = "HostBot"
        Private _cklServerAddress As String = ""
        Private Const version As UInt16 = 1

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(users IsNot Nothing)
            Contract.Invariant(cdKeyROC IsNot Nothing)
            Contract.Invariant(cdKeyTFT IsNot Nothing)
            Contract.Invariant(userName IsNot Nothing)
            Contract.Invariant(password IsNot Nothing)
            Contract.Invariant(server IsNot Nothing)
            Contract.Invariant(_lanHost IsNot Nothing)
            Contract.Invariant(initialChannel IsNot Nothing)
            Contract.Invariant(_cklServerAddress IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As InvariantString)
            Me.name = name
        End Sub
        Public Sub New(ByVal reader As IO.BinaryReader)
            Contract.Requires(reader IsNot Nothing)
            Load(reader)
        End Sub

        Public Property LanHost As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Return Me._lanHost
            End Get
            Set(ByVal value As String)
                Contract.Requires(value IsNot Nothing)
                Me._lanHost = value
            End Set
        End Property
        Public Property CKLServerAddress As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Return Me._cklServerAddress
            End Get
            Set(ByVal value As String)
                Contract.Requires(value IsNot Nothing)
                Me._cklServerAddress = value
            End Set
        End Property

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
            reader.ReadUInt16() 'listen port
            initialChannel = reader.ReadString()
            _cklServerAddress = reader.ReadString()
            If version >= 1 Then
                reader.ReadString() 'lan_admin_host
                reader.ReadUInt16() 'lan_admin_port
                LanHost = reader.ReadString()
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
            bw.Write(6113US) 'listen port
            bw.Write(initialChannel)
            bw.Write(_cklServerAddress)
            If version >= 1 Then
                bw.Write(" (None)") 'old default lan_admin_host
                bw.Write(CUShort(6114)) 'old default lan_admin_port
                bw.Write(LanHost)
                bw.Write("") 'old default lan_admin_password
            End If
        End Sub

        Public Function Clone(Optional ByVal newName As InvariantString? = Nothing) As ClientProfile
            Dim newProfile = New ClientProfile("Default")
            With newProfile
                .users = users.Clone()
                .cdKeyROC = cdKeyROC
                .cdKeyTFT = cdKeyTFT
                .userName = userName
                .password = password
                .server = server
                .name = If(newName Is Nothing, Me.name, newName.Value)
                .initialChannel = initialChannel
                ._cklServerAddress = _cklServerAddress
            End With
            Return newProfile
        End Function
    End Class
End Namespace
