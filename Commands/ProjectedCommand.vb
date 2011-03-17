Namespace Commands
    ''' <summary>
    ''' A command which delegates to a subcommand which takes a target derived from the given target.
    ''' </summary>
    Public NotInheritable Class ProjectedCommand(Of TInput, TProjected)
        Inherits BaseCommand(Of TInput)

        Private ReadOnly _projection As Func(Of TInput, TProjected)
        Private ReadOnly _command As ICommand(Of TProjected)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_command IsNot Nothing)
            Contract.Invariant(_projection IsNot Nothing)
        End Sub

        Public Sub New(command As ICommand(Of TProjected),
                       projection As Func(Of TInput, TProjected))
            MyBase.New(command.Name, command.Format, command.Description, command.Permissions)
            Contract.Requires(command IsNot Nothing)
            Contract.Requires(projection IsNot Nothing)
            Me._command = command
            Me._projection = projection
        End Sub

        Public Overrides Function IsArgumentPrivate(argument As String) As Boolean
            Return _command.IsArgumentPrivate(argument)
        End Function
        Public Overrides ReadOnly Property HelpTopics As IDictionary(Of InvariantString, String)
            Get
                Return _command.HelpTopics
            End Get
        End Property
        Protected Overrides Function PerformInvoke(target As TInput, user As BotUser, argument As String) As Task(Of String)
            Dim subTarget = _projection(target)
            Contract.Assume(subTarget IsNot Nothing)
            Return _command.Invoke(subTarget, user, argument)
        End Function
    End Class
    Public Module ProjectedCommandExtensions
        <Extension()> <Pure()>
        Public Function ProjectedFrom(Of TOuter, TInner)(command As ICommand(Of TInner), projection As Func(Of TOuter, TInner)) As ICommand(Of TOuter)
            Contract.Requires(command IsNot Nothing)
            Contract.Requires(projection IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ICommand(Of TOuter))() IsNot Nothing)
            Return New ProjectedCommand(Of TOuter, TInner)(command, projection)
        End Function
    End Module
End Namespace
