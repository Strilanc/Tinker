Namespace Components
    Public Module IBotComponentExtensions
        <Extension()>
        <CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")>
        Public Sub UIInvokeCommand(ByVal component As IBotComponent, ByVal argument As String)
            Contract.Requires(component IsNot Nothing)
            Contract.Requires(argument IsNot Nothing)
            Try
                Dim i = argument.IndexOf(" "c)
                If i = -1 Then i = argument.Length
                Dim subcommand = argument.Substring(0, i).ToInvariant

                Dim argDesc = If(component.IsArgumentPrivate(subcommand), "{0} [arguments hidden]".Frmt(subcommand), argument)
                component.Logger.Log("Command: {0}".Frmt(argDesc), LogMessageType.Typical)

                component.Logger.FutureLog(placeholder:="[running command {0}...]".Frmt(argDesc),
                                           message:=SafeInvokeCommand(component, argument))
            Catch e As Exception
                e.RaiseAsUnexpected("UIInvokeCommand for {0}:{1}".Frmt(component.Type, component.Name))
            End Try
        End Sub
        Private Async Function SafeInvokeCommand(ByVal component As IBotComponent, ByVal argument As String) As Task(Of String)
            Contract.Assume(component IsNot Nothing)
            Contract.Assume(argument IsNot Nothing)
            'Contract.Ensures(Contract.Result(Of Task(Of String))() IsNot Nothing)
            Try
                Dim result = Await Await TaskedFunc(Function() component.InvokeCommand(Nothing, argument))
                If String.IsNullOrEmpty(result) Then Return "Succeeded with no message"
                Return result
            Catch ex As Exception
                Return "Failed: {0}".Frmt(ex.Summarize)
            End Try
        End Function

        <Extension()>
        Public Async Function IncludeAllCommands(ByVal component As IBotComponent, ByVal commands As IEnumerable(Of Commands.ICommand(Of IBotComponent))) As Task(Of IDisposable)
            Contract.Assume(component IsNot Nothing)
            Contract.Assume(commands IsNot Nothing)
            'Contract.Ensures(Contract.Result(Of Task(Of IDisposable))() IsNot Nothing)

            Dim disposables = New List(Of Task(Of IDisposable))
            Try
                For Each command In commands
                    disposables.Add(component.IncludeCommand(command))
                Next command
                Await TaskEx.WhenAll(disposables)
                Return New DelegatedDisposable(Sub() disposables.DisposeAllAsync())
            Catch ex As Exception
                disposables.DisposeAllAsync()
                Throw
            End Try
        End Function
    End Module
End Namespace
