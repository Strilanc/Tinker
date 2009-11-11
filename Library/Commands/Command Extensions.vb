Namespace Commands
    Public Module CommandExtensions
        <Extension()>
        Public Sub ProcessLocalText(Of T)(ByVal this As CommandSet(Of T), ByVal target As T, ByVal argument As String, ByVal logger As Logger)
            Contract.Requires(this IsNot Nothing)
            Contract.Requires(target IsNot Nothing)
            Contract.Requires(argument IsNot Nothing)
            Contract.Requires(logger IsNot Nothing)
            Try
                Dim i = argument.IndexOf(" "c)
                If i = -1 Then i = argument.Length
                Dim subcommand = argument.Substring(0, i).ToUpperInvariant

                If Not this.CommandMap.ContainsKey(subcommand) Then
                    logger.Log("Unrecognized Command: {0}".Frmt(subcommand), LogMessageType.Problem)
                    Return
                End If

                Dim argDesc = If(this.CommandMap(subcommand).HasPrivateArguments,
                                 "{0} [arguments hidden]".Frmt(subcommand),
                                 argument)
                logger.Log("Command: {0}".Frmt(argDesc), LogMessageType.Typical)

                logger.FutureLog("[running command {0}...]".Frmt(argDesc), this.Invoke(target, Nothing, argument).EvalWhenValueReady(
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
                e.RaiseAsUnexpected("Exception rose past {0}[{1}].ProcessLocalTest".Frmt(this.GetType.Name, this.Name))
            End Try
        End Sub
    End Module
End Namespace
