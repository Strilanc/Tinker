Namespace Commands
    ''' <summary>
    ''' A command which processes arguments preceded by a keyword.
    ''' </summary>
    <ContractClass(GetType(ContractClassPartialCommand(Of )))>
    Public MustInherit Class PartialCommand(Of TTarget)
        Inherits BaseCommand(Of TTarget)

        Protected Sub New(name As InvariantString,
                          headType As String,
                          description As String,
                          Optional permissions As String = Nothing,
                          Optional extraHelp As String = Nothing,
                          Optional hasPrivateArguments As Boolean = False)
            MyBase.New(name:=name,
                       Format:="{0} ...".Frmt(headType),
                       description:=description,
                       permissions:=permissions,
                       extraHelp:=extraHelp,
                       hasPrivateArguments:=hasPrivateArguments)
            Contract.Requires(headType IsNot Nothing)
            Contract.Requires(description IsNot Nothing)
        End Sub

        Protected NotOverridable Overrides Function PerformInvoke(target As TTarget, user As BotUser, argument As String) As Task(Of String)
            Dim i = argument.IndexOf(" "c)
            If i = -1 Then i = argument.Length
            Dim head = argument.Substring(0, i)
            Dim rest = argument.Substring(Math.Min(i + 1, argument.Length))
            Return PerformInvoke(target, user, head, rest)
        End Function

        Protected MustOverride Overloads Function PerformInvoke(target As TTarget, user As BotUser, argumentHead As String, argumentRest As String) As Task(Of String)
    End Class
    <ContractClassFor(GetType(PartialCommand(Of )))>
    Public MustInherit Class ContractClassPartialCommand(Of TTarget)
        Inherits PartialCommand(Of TTarget)
        Protected Sub New()
            MyBase.New("", "", "")
        End Sub
        Protected Overrides Function PerformInvoke(target As TTarget, user As BotUser, argumentHead As String, argumentRest As String) As Task(Of String)
            Contract.Requires(target IsNot Nothing)
            Contract.Requires(argumentHead IsNot Nothing)
            Contract.Requires(argumentRest IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of String))() IsNot Nothing)
            Throw New NotSupportedException
        End Function
    End Class
End Namespace
