Namespace Commands
    ''' <summary>
    ''' Performs actions specified by text arguments.
    ''' </summary>
    <ContractClass(GetType(Command(Of ).ContractClass))>
    Public MustInherit Class Command(Of TTarget)
        Private ReadOnly _name As String
        Private ReadOnly _format As String
        Private ReadOnly _description As String
        Private ReadOnly _permissions As Dictionary(Of String, UInteger)
        Private ReadOnly _extraHelp As Dictionary(Of String, String)
        Private ReadOnly _hasPrivateArguments As Boolean
        Public ReadOnly Property Name As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Return _name
            End Get
        End Property
        Public ReadOnly Property Description As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Return _description
            End Get
        End Property
        Public ReadOnly Property Format As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Return _format
            End Get
        End Property
        Public ReadOnly Property HasPrivateArguments As Boolean
            Get
                Return _hasPrivateArguments
            End Get
        End Property
        Public ReadOnly Property HelpTopics As Dictionary(Of String, String)
            Get
                Contract.Ensures(Contract.Result(Of Dictionary(Of String, String))() IsNot Nothing)
                Return _extraHelp
            End Get
        End Property
        Public ReadOnly Property Permissions As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Return (From pair In Me._permissions Select "{0}:{1}".Frmt(pair.Key, pair.Value)).StringJoin(",")
            End Get
        End Property

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_name IsNot Nothing)
            Contract.Invariant(_format IsNot Nothing)
            Contract.Invariant(_description IsNot Nothing)
            Contract.Invariant(_permissions IsNot Nothing)
            Contract.Invariant(_extraHelp IsNot Nothing)
        End Sub

        Protected Sub New(ByVal name As String,
                          ByVal format As String,
                          ByVal description As String,
                          Optional ByVal permissions As String = Nothing,
                          Optional ByVal extraHelp As String = Nothing,
                          Optional ByVal hasPrivateArguments As Boolean = False)
            Contract.Requires(name IsNot Nothing)
            Contract.Requires(format IsNot Nothing)
            Contract.Requires(description IsNot Nothing)
            Contract.Requires(Not name.Contains(" "c))
            Me._name = name
            Me._format = format
            Me._description = description
            Me._permissions = BuildDictionaryFromString(If(permissions, ""),
                                                        parser:=Function(x) UInteger.Parse(x, CultureInfo.InvariantCulture),
                                                        pairDivider:=";",
                                                        valueDivider:="=",
                                                        useUpperInvariantKeys:=True)
            Me._extraHelp = BuildDictionaryFromString(If(extraHelp, ""),
                                                      parser:=Function(x) x,
                                                      pairDivider:=Environment.NewLine,
                                                      valueDivider:="=",
                                                      useUpperInvariantKeys:=True)
            Me._hasPrivateArguments = hasPrivateArguments
        End Sub

        <Pure()>
        Public Function IsUserAllowed(ByVal user As BotUser) As Boolean
            If user Is Nothing Then Return True
            Return (From pair In _permissions Where user.Permission(pair.Key) < pair.Value).None
        End Function

        Public Function Invoke(ByVal target As TTarget, ByVal user As BotUser, ByVal argument As String) As IFuture(Of String)
            Dim result = New FutureFunction(Of IFuture(Of String))
            If IsUserAllowed(user) Then
                result.SetByEvaluating(Function() PerformInvoke(target, user, argument))
            Else
                result.SetFailed(New InvalidOperationException("Unsufficient permissions. Need {0}.".Frmt(Me.Permissions)))
            End If
            Return result.Defuturized
        End Function

        Protected MustOverride Function PerformInvoke(ByVal target As TTarget, ByVal user As BotUser, ByVal argument As String) As IFuture(Of String)

        <ContractClassFor(GetType(Command(Of )))>
        MustInherit Class ContractClass
            Inherits Command(Of TTarget)
            Protected Sub New()
                MyBase.New("", "", "")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As TTarget, ByVal user As BotUser, ByVal argument As String) As IFuture(Of String)
                Contract.Requires(target IsNot Nothing)
                Contract.Requires(argument IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IFuture(Of String))() IsNot Nothing)
                Throw New NotSupportedException
            End Function
        End Class
    End Class
End Namespace
