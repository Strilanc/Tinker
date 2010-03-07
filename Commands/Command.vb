Namespace Commands
    ''' <summary>
    ''' Performs actions specified by text arguments.
    ''' </summary>
    <ContractClass(GetType(Command(Of ).ContractClass))>
    Public MustInherit Class Command(Of TTarget)
        Private ReadOnly _name As InvariantString
        Private ReadOnly _format As InvariantString
        Private ReadOnly _description As String
        Private ReadOnly _permissions As Dictionary(Of InvariantString, UInteger)
        Private ReadOnly _extraHelp As Dictionary(Of InvariantString, String)
        Private ReadOnly _hasPrivateArguments As Boolean

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_description IsNot Nothing)
            Contract.Invariant(_permissions IsNot Nothing)
            Contract.Invariant(_extraHelp IsNot Nothing)
        End Sub

        Protected Sub New(ByVal name As InvariantString,
                          ByVal format As InvariantString,
                          ByVal description As String,
                          Optional ByVal permissions As String = Nothing,
                          Optional ByVal extraHelp As String = Nothing,
                          Optional ByVal hasPrivateArguments As Boolean = False)
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

        Public ReadOnly Property Name As InvariantString
            Get
                Return _name
            End Get
        End Property
        Public ReadOnly Property Description As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Return _description
            End Get
        End Property
        Public ReadOnly Property Format As InvariantString
            Get
                Return _format
            End Get
        End Property
        Public Overridable ReadOnly Property HelpTopics As Dictionary(Of InvariantString, String)
            Get
                Contract.Ensures(Contract.Result(Of Dictionary(Of InvariantString, String))() IsNot Nothing)
                Return _extraHelp
            End Get
        End Property
        Public ReadOnly Property Permissions As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Return (From pair In Me._permissions Select "{0}:{1}".Frmt(pair.Key, pair.Value)).StringJoin(",")
            End Get
        End Property

        <Pure()>
        Public Overridable Function IsArgumentPrivate(ByVal argument As String) As Boolean
            Contract.Requires(argument IsNot Nothing)
            Return _hasPrivateArguments
        End Function
        <Pure()>
        Public Function IsUserAllowed(ByVal user As BotUser) As Boolean
            If user Is Nothing Then Return True
            Return (From pair In _permissions Where user.Permission(pair.Key) < pair.Value).None
        End Function

        Public Function Invoke(ByVal target As TTarget, ByVal user As BotUser, ByVal argument As String) As Task(Of String)
            Contract.Requires(target IsNot Nothing)
            Contract.Requires(argument IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of String))() IsNot Nothing)
            If IsUserAllowed(user) Then
                Dim result = New TaskCompletionSource(Of Task(Of String))()
                result.SetByEvaluating(Function() PerformInvoke(target, user, argument))
                result.Task.Unwrap.Catch(Sub(ex) ex.RaiseAsUnexpected("Error invoking command"))
                Return result.Task.Unwrap
            Else
                Dim result = New TaskCompletionSource(Of String)
                result.SetException(New InvalidOperationException("Insufficient permissions. Need {0}.".Frmt(Me.Permissions)))
                Return result.Task
            End If
        End Function

        Protected MustOverride Function PerformInvoke(ByVal target As TTarget, ByVal user As BotUser, ByVal argument As String) As Task(Of String)

        <ContractClassFor(GetType(Command(Of )))>
        MustInherit Class ContractClass
            Inherits Command(Of TTarget)
            Protected Sub New()
                MyBase.New("", "", "")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As TTarget, ByVal user As BotUser, ByVal argument As String) As Task(Of String)
                Contract.Requires(target IsNot Nothing)
                Contract.Requires(argument IsNot Nothing)
                Contract.Ensures(Contract.Result(Of Task(Of String))() IsNot Nothing)
                Throw New NotSupportedException
            End Function
        End Class
    End Class
End Namespace
