Namespace Commands
    ''' <summary>
    ''' A command which processes arguments matching a template.
    ''' </summary>
    <ContractClass(GetType(ContractClassTemplatedCommand(Of )))>
    Public MustInherit Class TemplatedCommand(Of TTarget)
        Inherits BaseCommand(Of TTarget)

        Private ReadOnly _template As CommandTemplate

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_template IsNot Nothing)
        End Sub

        Protected Sub New(name As InvariantString,
                          template As InvariantString,
                          description As String,
                          Optional permissions As String = Nothing,
                          Optional extraHelp As String = Nothing,
                          Optional hasPrivateArguments As Boolean = False)
            MyBase.New(name, template, description, permissions, extraHelp, hasPrivateArguments)
            Contract.Requires(description IsNot Nothing)
            Me._template = New CommandTemplate(template)
        End Sub

        Protected NotOverridable Overloads Overrides Function PerformInvoke(target As TTarget, user As BotUser, argument As String) As Task(Of String)
            Dim arg = New CommandArgument(argument)
            Dim argException = _template.TryFindMismatch(arg)
            If argException IsNot Nothing Then Throw argException
            Return PerformInvoke(target, user, arg)
        End Function

        '''<summary>Uses a parsed argument to processes the command.</summary>
        Protected MustOverride Overloads Function PerformInvoke(target As TTarget, user As BotUser, argument As CommandArgument) As Task(Of String)
    End Class
    <ContractClassFor(GetType(TemplatedCommand(Of )))>
    Public MustInherit Class ContractClassTemplatedCommand(Of TTarget)
        Inherits TemplatedCommand(Of TTarget)
        Protected Sub New()
            MyBase.New("", "", "")
        End Sub
        Protected Overrides Function PerformInvoke(target As TTarget, user As BotUser, argument As CommandArgument) As Task(Of String)
            Contract.Requires(target IsNot Nothing)
            Contract.Requires(argument IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of String))() IsNot Nothing)
            Throw New NotSupportedException
        End Function
    End Class
End Namespace
