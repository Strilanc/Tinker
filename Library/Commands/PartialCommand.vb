Namespace Commands
    ''' <summary>
    ''' A command which processes arguments preceded by a keyword.
    ''' </summary>
    <ContractClass(GetType(PartialCommand(Of ).ContractClass))>
    Public MustInherit Class PartialCommand(Of TTarget)
        Inherits Command(Of TTarget)

        Protected Sub New(ByVal name As String,
                          ByVal headType As String,
                          ByVal description As String,
                          Optional ByVal permissions As String = Nothing,
                          Optional ByVal extraHelp As String = Nothing,
                          Optional ByVal hasPrivateArguments As Boolean = False)
            MyBase.New(name:=name,
                       Format:="{0} ...".Frmt(headType),
                       description:=description,
                       permissions:=permissions,
                       extraHelp:=extraHelp,
                       hasPrivateArguments:=hasPrivateArguments)
            Contract.Requires(name IsNot Nothing)
            Contract.Requires(headType IsNot Nothing)
            Contract.Requires(description IsNot Nothing)
        End Sub

        Protected NotOverridable Overrides Function PerformInvoke(ByVal target As TTarget, ByVal user As BotUser, ByVal argument As String) As Strilbrary.Threading.IFuture(Of String)
            Dim i = argument.IndexOf(" "c)
            If i = -1 Then i = argument.Length
            Dim head = argument.Substring(0, i)
            Dim rest = argument.Substring(Math.Min(i + 1, argument.Length))
            Return PerformInvoke(target, user, head, rest)
        End Function

        Protected MustOverride Overloads Function PerformInvoke(ByVal target As TTarget, ByVal user As BotUser, ByVal argumentHead As String, ByVal argumentRest As String) As IFuture(Of String)

        <ContractClassFor(GetType(PartialCommand(Of )))>
        MustInherit Shadows Class ContractClass
            Inherits PartialCommand(Of TTarget)
            Protected Sub New()
                MyBase.New("", "", "")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As TTarget, ByVal user As BotUser, ByVal argumentHead As String, ByVal argumentRest As String) As IFuture(Of String)
                Contract.Requires(target IsNot Nothing)
                Contract.Requires(argumentHead IsNot Nothing)
                Contract.Requires(argumentRest IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IFuture(Of String))() IsNot Nothing)
                Throw New NotSupportedException
            End Function
        End Class
    End Class
End Namespace
