Namespace Components
    Public Module IBotComponentExtensions
        <Extension()>
        Public Sub UIInvokeCommand(ByVal component As IBotComponent, ByVal argument As String)
            Contract.Requires(component IsNot Nothing)
            Contract.Requires(argument IsNot Nothing)
            Try
                Dim i = argument.IndexOf(" "c)
                If i = -1 Then i = argument.Length
                Dim subcommand As InvariantString = argument.Substring(0, i)

                Dim argDesc = If(component.IsArgumentPrivate(subcommand), "{0} [arguments hidden]".Frmt(subcommand), argument)
                component.Logger.Log("Command: {0}".Frmt(argDesc), LogMessageType.Typical)

                component.Logger.FutureLog(
                    placeholder:="[running command {0}...]".Frmt(argDesc),
                    message:=component.InvokeCommand(Nothing, argument).EvalWhenValueReady(
                        Function(message, commandException)
                            If commandException IsNot Nothing Then
                                Return "Failed: {0}".Frmt(commandException.Message)
                            ElseIf message Is Nothing OrElse message = "" Then
                                Return "Command '{0}' succeeded.".Frmt(argDesc)
                            Else
                                Return message
                            End If
                        End Function))
            Catch e As Exception
                e.RaiseAsUnexpected("UIInvokeCommand for {0}:{1}".Frmt(component.Type, component.Name))
            End Try
        End Sub

    End Module
End Namespace
