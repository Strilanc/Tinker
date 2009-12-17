Namespace Commands
    ''' <summary>
    ''' A command which uses a delegate to process arguments.
    ''' </summary>
    Public NotInheritable Class DelegatedCommand(Of T)
        Inherits Command(Of T)
        Private ReadOnly processFunction As Func(Of T, BotUser, String, IFuture(Of String))

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(processFunction IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As InvariantString,
                       ByVal format As InvariantString,
                       ByVal description As String,
                       ByVal func As Func(Of T, BotUser, String, IFuture(Of String)),
                       Optional ByVal permissions As String = Nothing,
                       Optional ByVal extraHelp As String = Nothing,
                       Optional ByVal hasPrivateArguments As Boolean = False)
            MyBase.New(name, format, description, permissions, extraHelp, hasPrivateArguments)
            Contract.Requires(description IsNot Nothing)
            Contract.Requires(func IsNot Nothing)
            Me.processFunction = func
        End Sub

        Protected Overrides Function PerformInvoke(ByVal target As T, ByVal user As BotUser, ByVal argument As String) As IFuture(Of String)
            Dim result = processFunction(target, user, argument)
            Contract.Assume(result IsNot Nothing)
            Return result
        End Function
    End Class

    ''' <summary>
    ''' A command which uses a delegate to process arguments preceded by a keyword.
    ''' </summary>
    Public NotInheritable Class DelegatedPartialCommand(Of T)
        Inherits PartialCommand(Of T)
        Private ReadOnly processFunction As Func(Of T, BotUser, String, String, IFuture(Of String))

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(processFunction IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As InvariantString,
                       ByVal headType As String,
                       ByVal description As String,
                       ByVal func As Func(Of T, BotUser, String, String, IFuture(Of String)),
                       Optional ByVal permissions As String = Nothing,
                       Optional ByVal extraHelp As String = Nothing,
                       Optional ByVal hasPrivateArguments As Boolean = False)
            MyBase.New(name, headType, description, permissions, extraHelp, hasPrivateArguments)
            Contract.Requires(headType IsNot Nothing)
            Contract.Requires(description IsNot Nothing)
            Contract.Requires(func IsNot Nothing)
            Me.processFunction = func
        End Sub

        Protected Overrides Function PerformInvoke(ByVal target As T, ByVal user As BotUser, ByVal head As String, ByVal rest As String) As IFuture(Of String)
            Dim result = processFunction(target, user, head, rest)
            Contract.Assume(result IsNot Nothing)
            Return result
        End Function
    End Class

    ''' <summary>
    ''' A command which uses a delegate to process arguments matching a template.
    ''' </summary>
    Public NotInheritable Class DelegatedTemplatedCommand(Of T)
        Inherits TemplatedCommand(Of T)
        Private ReadOnly processFunction As Func(Of T, BotUser, CommandArgument, IFuture(Of String))

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(processFunction IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As InvariantString,
                       ByVal template As InvariantString,
                       ByVal description As String,
                       ByVal func As Func(Of T, BotUser, CommandArgument, IFuture(Of String)),
                       Optional ByVal permissions As String = Nothing,
                       Optional ByVal extraHelp As String = Nothing,
                       Optional ByVal hasPrivateArguments As Boolean = False)
            MyBase.New(name, template, description, permissions, extraHelp, hasPrivateArguments)
            Contract.Requires(description IsNot Nothing)
            Contract.Requires(func IsNot Nothing)
            Me.processFunction = func
        End Sub

        Protected Overrides Function PerformInvoke(ByVal target As T, ByVal user As BotUser, ByVal argument As CommandArgument) As IFuture(Of String)
            Dim result = processFunction(target, user, argument)
            Contract.Assume(result IsNot Nothing)
            Return result
        End Function
    End Class
End Namespace