Namespace Commands
    ''' <summary>
    ''' A command which provides help using commands.
    ''' </summary>
    Public NotInheritable Class HelpCommand(Of T)
        Inherits Command(Of T)

        Private ReadOnly helpMap As New Dictionary(Of String, String)
        Private ReadOnly commands As New List(Of Command(Of T))

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(helpMap IsNot Nothing)
            Contract.Invariant(commands IsNot Nothing)
        End Sub

        Public Sub New()
            MyBase.New(Name:="Help",
                       Format:="* or ? or + or Command or Command or *",
                       Description:="Provides help for using commands.")

            helpMap("") = {"'Help ?' for how to use commands",
                           "'Help *' for available commands",
                           "'Help +' for all commands",
                           "'Help command' for help with specific commands."
                           }.StringJoin(", ")
            helpMap("?") = {"Use 'Help command' to for a command's description, arguments and required permissions.",
                            "For example, 'Help host' could return 'Hosts a game. [name=<game name> -map=query -private'] {games:1}",
                            "The argument format is inside the [] brackets and the permissions are inside the {} brackets.",
                            "In this case, 'name' is a required named argument, 'map' is an optional named argument, and -private is an optional switch.",
                            "Given that format, you could host a public game of castle fight like this: 'Host name=<Castle Fight!!> -map=<castle*1.14b>'.",
                            "Arguments can be re-ordered but must be separated by spaces (use brackets for argument values with spaces; eg. '<game name>')."
                            }.StringJoin(" ")
        End Sub

        Public Sub AddCommand(ByVal command As Command(Of T))
            Contract.Requires(command IsNot Nothing)
            commands.Add(command)
            'Basic help
            helpMap(command.Name.ToUpperInvariant) = "{0} [{1} {2}] {{{3}}}".Frmt(command.Description,
                                                                                  command.Name,
                                                                                  command.Format,
                                                                                  command.Permissions)
            'Extra help
            If command.HelpTopics.Count > 0 Then
                helpMap("{0} *".Frmt(command.Name.ToUpperInvariant)) = command.HelpTopics.Keys.StringJoin(" ")
                For Each pair In command.HelpTopics
                    helpMap("{0} {1}".Frmt(command.Name.ToUpperInvariant, pair.Key.ToUpperInvariant)) = pair.Value
                Next pair
            End If
        End Sub
        Public Sub RemoveCommand(ByVal command As Command(Of T))
            Contract.Requires(command IsNot Nothing)
            If Not commands.Contains(command) Then Return
            'Basic help
            helpMap.Remove(command.Name.ToUpperInvariant)
            'Extra help
            If command.HelpTopics.Count > 0 Then
                helpMap.Remove("{0} *".Frmt(command.Name.ToUpperInvariant))
                For Each pair In command.HelpTopics
                    helpMap.Remove("{0} {1}".Frmt(command.Name.ToUpperInvariant, pair.Key.ToUpperInvariant))
                Next pair
            End If
        End Sub

        Protected Overrides Function PerformInvoke(ByVal target As T, ByVal user As BotUser, ByVal argument As String) As IFuture(Of String)
            Select Case argument
                Case "*"
                    Return (From command In commands
                            Where command.IsUserAllowed(user)
                            Select command.Name).StringJoin(" ").Futurized
                Case "+"
                    Return (From command In commands
                            Select command.Name).StringJoin(" ").Futurized
            End Select

            If helpMap.ContainsKey(argument.ToUpperInvariant) Then
                Return helpMap(argument.ToUpperInvariant).Futurized
            End If

            Throw New ArgumentException("No help found for '{0}'.".Frmt(argument))
        End Function
    End Class
End Namespace
