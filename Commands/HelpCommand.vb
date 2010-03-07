Namespace Commands
    ''' <summary>
    ''' A command which provides help using commands.
    ''' </summary>
    Public NotInheritable Class HelpCommand(Of T)
        Inherits Command(Of T)

        Private ReadOnly _commandMap As New Dictionary(Of InvariantString, Command(Of T))

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_commandMap IsNot Nothing)
        End Sub

        Public Sub New()
            MyBase.New(Name:="Help",
                       Format:="?topic",
                       Description:="Provides help for using commands.")
        End Sub

        Public Sub AddCommand(ByVal command As Command(Of T))
            Contract.Requires(command IsNot Nothing)
            If _commandMap.ContainsKey(command.Name) Then
                Throw New InvalidOperationException("There is already help for a command named '{0}'.".Frmt(command.Name))
            End If
            _commandMap.Add(command.Name, command)
        End Sub
        Public Sub RemoveCommand(ByVal command As Command(Of T))
            Contract.Requires(command IsNot Nothing)
            If Not _commandMap.ContainsKey(command.Name) Then Return
            If Not _commandMap(command.Name) Is command Then Return
            _commandMap.Remove(command.Name)
        End Sub

        'verification disabled due to stupid verifier (1.2.3.0118.5)
        <ContractVerification(False)>
        Protected Overrides Function PerformInvoke(ByVal target As T, ByVal user As BotUser, ByVal argument As String) As Task(Of String)
            Select Case argument
                Case "" 'intro
                    Return {"[Tinker] 'Help ?' for how to use commands",
                            "'Help *' for available commands",
                            "'Help +' for all commands",
                            "'Help command' for help with specific commands."
                            }.StringJoin(", ").AsTask

                Case "?" 'how to use commands
                    Return {"Commands take four types of arguments: named arguments, optional named arguments, optional switches, and raw arguments.",
                            "TIP: You can use any command by copying the format shown in its help (modify the arguments to be what you want).",
                            "Raw arguments are just plain normal arguments, what you should expect. You can skip them if their format starts with a ?.",
                            "Named arguments, such as name=value, are arguments preceded by their name=.",
                            "Optional switches, such as -useFancyPants, are argument which can either be skipped or included.",
                            "Optional named arguments, such as -optional=value, can be skipped but are otherwise named arguments prefixed with a -.",
                            "There are also tail arguments, which aren't separated by spaces, like MapQuery in 'FindMaps MapQuery...'."
                            }.StringJoin(Environment.NewLine).AsTask

                Case "*" 'list available commands
                    Return (From command In _commandMap.Values
                            Order By command.Name
                            Where command.IsUserAllowed(user)
                            Select command.Name
                            ).StringJoin(" ").AsTask

                Case "+" 'list all commands
                    Return (From command In _commandMap.Values
                            Order By command.Name
                            Select command.Name
                            ).StringJoin(" ").AsTask

                Case Else 'help with specific commands
                    Dim p = argument.IndexOf(" "c)
                    If p = -1 Then
                        'basic command help
                        Dim command As Command(Of T) = Nothing
                        If _commandMap.TryGetValue(key:=argument, value:=command) Then
                            Contract.Assume(command IsNot Nothing)
                            Return "{0} [{1} {2}] {{{3}}}".Frmt(command.Description,
                                                                command.Name,
                                                                command.Format,
                                                                command.Permissions).AsTask
                        End If
                    Else
                        'command subtopics
                        Dim firstWord = argument.Substring(0, p)
                        Dim rest = argument.Substring(p + 1)
                        Dim command As Command(Of T) = Nothing
                        If _commandMap.TryGetValue(key:=firstWord, value:=command) Then
                            Contract.Assume(command IsNot Nothing)
                            Dim result As String = Nothing
                            If rest = "*" Then 'list subtopics
                                Return (From key In command.HelpTopics.Keys
                                        Order By key
                                        ).StringJoin(" ").AsTask
                            ElseIf command.HelpTopics.TryGetValue(key:=rest, value:=result) Then
                                Contract.Assume(result IsNot Nothing)
                                Return result.AsTask
                            End If
                        End If
                    End If

                    Throw New ArgumentException("No help found for '{0}'.".Frmt(argument))
            End Select
        End Function
    End Class
End Namespace
