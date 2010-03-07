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
                Dim subcommand As InvariantString = argument.Substring(0, i)

                Dim argDesc = If(component.IsArgumentPrivate(subcommand), "{0} [arguments hidden]".Frmt(subcommand), argument)
                component.Logger.Log("Command: {0}".Frmt(argDesc), LogMessageType.Typical)

                component.Logger.FutureLog(placeholder:="[running command {0}...]".Frmt(argDesc),
                                           message:=component.InvokeCommand(Nothing, argument).ContinueWith(
                                               Function(task)
                                                   If task.Status = TaskStatus.Faulted Then
                                                       Return "Failed: {0}".Frmt(task.Exception.Summarize)
                                                   ElseIf task.Result Is Nothing OrElse task.Result = "" Then
                                                       Return "Command '{0}' succeeded.".Frmt(argDesc)
                                                   Else
                                                       Return task.Result
                                                   End If
                                               End Function).AssumeNotNull)
            Catch e As Exception
                e.RaiseAsUnexpected("UIInvokeCommand for {0}:{1}".Frmt(component.Type, component.Name))
            End Try
        End Sub
    End Module
End Namespace
