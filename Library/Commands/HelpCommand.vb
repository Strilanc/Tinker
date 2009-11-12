Namespace Commands
    ''' <summary>
    ''' A command which provides help using commands.
    ''' </summary>
    Public NotInheritable Class HelpCommand(Of T)
        Inherits Command(Of T)

        Private ReadOnly _commandMap As New Dictionary(Of String, Command(Of T))

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
            If _commandMap.ContainsKey(command.Name.ToUpperInvariant) Then
                Throw New InvalidOperationException("There is already a command named '{0}'.".Frmt(command.Name))
            End If
            _commandMap.Add(command.Name.ToUpperInvariant, command)
        End Sub
        Public Sub RemoveCommand(ByVal command As Command(Of T))
            Contract.Requires(command IsNot Nothing)
            If Not _commandMap.ContainsKey(command.Name.ToUpperInvariant) Then Return
            If Not _commandMap(command.Name.ToUpperInvariant) Is command Then Return
            _commandMap.Remove(command.Name.ToUpperInvariant)
        End Sub

        Protected Overrides Function PerformInvoke(ByVal target As T, ByVal user As BotUser, ByVal argument As String) As IFuture(Of String)
            Select Case argument
                Case "" 'intro
                    Return {"'Help ?' for how to use commands",
                            "'Help *' for available commands",
                            "'Help +' for all commands",
                            "'Help command' for help with specific commands."
                            }.StringJoin(", ").Futurized

                Case "?" 'how to use commands
                    Return {"Use 'Help command' for a command's description, arguments and permissions.",
                            "For example, 'Help host' could return 'Hosts a game. [name=<game name> -map=query -private'] {games:1}",
                            "The argument format is inside the [] brackets and the permissions are inside the {} brackets.",
                            "In this case, 'name' is a required named argument, 'map' is an optional named argument, and -private is an optional switch.",
                            "Given that format, you could host a public game of castle fight like this: 'Host name=<Castle Fight!!> -map=<castle*1.14b>'.",
                            "Arguments can be re-ordered but must be separated by spaces (use brackets for argument values with spaces)."
                            }.StringJoin(" ").Futurized

                Case "*" 'list available commands
                    Return (From command In _commandMap.Values
                            Order By command.Name
                            Where command.IsUserAllowed(user)
                            Select command.Name
                            ).StringJoin(" ").Futurized

                Case "+" 'list all commands
                    Return (From command In _commandMap.Values
                            Order By command.Name
                            Select command.Name
                            ).StringJoin(" ").Futurized

                Case Else 'help with specific commands
                    If Not argument.Contains(" "c) Then
                        'basic command help
                        Dim command As Command(Of T) = Nothing
                        If _commandMap.TryGetValue(key:=argument.ToUpperInvariant, value:=command) Then
                            Return "{0} [{1} {2}] {{{3}}}".Frmt(command.Description,
                                                                command.Name,
                                                                command.Format,
                                                                command.Permissions).Futurized
                        End If
                    Else
                        'command subtopics
                        Dim p = argument.IndexOf(" "c)
                        Dim firstWord = argument.Substring(0, p)
                        Dim rest = argument.Substring(p + 1)
                        Dim command As Command(Of T) = Nothing
                        If _commandMap.TryGetValue(key:=firstWord.ToUpperInvariant, value:=command) Then
                            Dim result As String = Nothing
                            If rest = "*" Then
                                Return (From key In command.HelpTopics.Keys Order By key).StringJoin(" ").Futurized
                            ElseIf command.HelpTopics.TryGetValue(key:=rest.ToUpperInvariant, value:=result) Then
                                Return result.Futurized
                            End If
                        End If
                    End If

                    Throw New ArgumentException("No help found for '{0}'.".Frmt(argument))
            End Select
        End Function
    End Class
End Namespace
