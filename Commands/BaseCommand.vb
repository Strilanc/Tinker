Namespace Commands
    ''' <summary>
    ''' Performs actions specified by text arguments.
    ''' </summary>
    <ContractClass(GetType(ContractClassBaseCommand(Of )))>
    Public MustInherit Class BaseCommand(Of T)
        Implements ICommand(Of T)
        Private ReadOnly _name As InvariantString
        Private ReadOnly _format As InvariantString
        Private ReadOnly _description As String
        Private ReadOnly _permissions As IDictionary(Of InvariantString, UInteger)
        Private ReadOnly _extraHelp As IDictionary(Of InvariantString, String)
        Private ReadOnly _hasPrivateArguments As Boolean

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_description IsNot Nothing)
            Contract.Invariant(_permissions IsNot Nothing)
            Contract.Invariant(_extraHelp IsNot Nothing)
        End Sub

        Protected Sub New(name As InvariantString,
                          format As InvariantString,
                          description As String,
                          Optional permissions As String = Nothing,
                          Optional extraHelp As String = Nothing,
                          Optional hasPrivateArguments As Boolean = False)
            Contract.Requires(description IsNot Nothing)
            If name.Value.Contains(" "c) Then Throw New ArgumentException("Command names can't contain spaces.")

            Me._name = name
            Me._format = format
            Me._description = description
            Me._permissions = BuildDictionaryFromString(If(permissions, ""),
                                                        parser:=Function(x) UInteger.Parse(x, CultureInfo.InvariantCulture),
                                                        pairDivider:=",",
                                                        valueDivider:=":")
            Me._extraHelp = BuildDictionaryFromString(If(extraHelp, ""),
                                                      parser:=Function(x) x,
                                                      pairDivider:=Environment.NewLine,
                                                      valueDivider:="=")
            Me._hasPrivateArguments = hasPrivateArguments
        End Sub

        Public ReadOnly Property Name As InvariantString Implements ICommand(Of T).Name
            Get
                Return _name
            End Get
        End Property
        Public ReadOnly Property Description As String Implements ICommand(Of T).Description
            Get
                Return _description
            End Get
        End Property
        Public ReadOnly Property Format As InvariantString Implements ICommand(Of T).Format
            Get
                Return _format
            End Get
        End Property
        Public Overridable ReadOnly Property HelpTopics As IDictionary(Of InvariantString, String) Implements ICommand(Of T).HelpTopics
            Get
                Return _extraHelp
            End Get
        End Property
        Public ReadOnly Property Permissions As String Implements ICommand(Of T).Permissions
            Get
                Return (From pair In Me._permissions Select "{0}:{1}".Frmt(pair.Key, pair.Value)).StringJoin(",")
            End Get
        End Property

        <Pure()>
        Public Overridable Function IsArgumentPrivate(argument As String) As Boolean Implements ICommand(Of T).IsArgumentPrivate
            Return _hasPrivateArguments
        End Function
        <Pure()>
        Public Function IsUserAllowed(user As BotUser) As Boolean Implements ICommand(Of T).IsUserAllowed
            If user Is Nothing Then Return True
            Return (From pair In _permissions Where user.Permission(pair.Key) < pair.Value).None
        End Function

        <SuppressMessage("Microsoft.Contracts", "Ensures-40-81")>
        Public Async Function Invoke(target As T, user As BotUser, argument As String) As Task(Of String) Implements ICommand(Of T).Invoke
            If Not IsUserAllowed(user) Then Throw New InvalidOperationException("Insufficient permissions. Need {0}.".Frmt(Me.Permissions))

            Try
                Return Await PerformInvoke(target, user, argument)
            Catch ex As Exception
                ex.RaiseAsUnexpected("Error invoking command")
                Throw
            End Try
        End Function

        Protected MustOverride Function PerformInvoke(target As T, user As BotUser, argument As String) As Task(Of String)
    End Class
    <ContractClassFor(GetType(BaseCommand(Of )))>
    MustInherit Class ContractClassBaseCommand(Of T)
        Inherits BaseCommand(Of T)
        Protected Sub New()
            MyBase.New("", "", "")
        End Sub
        Protected Overrides Function PerformInvoke(target As T, user As BotUser, argument As String) As Task(Of String)
            Contract.Requires(target IsNot Nothing)
            Contract.Requires(argument IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of String))() IsNot Nothing)
            Throw New NotSupportedException
        End Function
    End Class
End Namespace
