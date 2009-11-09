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

                If this.CommandMap(subcommand).HasPrivateArguments Then
                    logger.Log("Command [arguments hidden]: {0}".Frmt(subcommand), LogMessageType.Typical)
                Else
                    logger.Log("Command: {0}".Frmt(subcommand), LogMessageType.Typical)
                End If

                logger.FutureLog("[running command '{0}'...]".Frmt(subcommand), this.Invoke(target, Nothing, argument).EvalWhenValueReady(
                    Function(message, commandException)
                        If commandException IsNot Nothing Then
                            Return "Failed: {0}".Frmt(commandException.ToString)
                        ElseIf message Is Nothing OrElse message = "" Then
                            Return "Command succeeded."
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
