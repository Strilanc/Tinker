Namespace Commands
    ''' <summary>
    ''' A command which provides help using commands.
    ''' </summary>
    Public NotInheritable Class HelpCommand(Of T)
        Inherits Command(Of T)

        Private ReadOnly _commandMap As New Dictionary(Of InvariantString, ICommand(Of T))
        Private ReadOnly lock As New Object

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_commandMap IsNot Nothing)
        End Sub

        Public Sub New()
            MyBase.New(Name:="Help",
                       Format:="?topic",
                       Description:="Provides help for using commands.")
        End Sub

        Public Sub AddCommand(ByVal command As ICommand(Of T))
            Contract.Requires(command IsNot Nothing)
            SyncLock lock
                If _commandMap.ContainsKey(command.Name) Then
                    Throw New InvalidOperationException("There is already help for a command named '{0}'.".Frmt(command.Name))
                End If
                _commandMap.Add(command.Name, command)
            End SyncLock
        End Sub
        Public Sub RemoveCommand(ByVal command As ICommand(Of T))
            Contract.Requires(command IsNot Nothing)
            SyncLock lock
                If Not _commandMap.ContainsKey(command.Name) Then Return
                If Not _commandMap(command.Name) Is command Then Return
                _commandMap.Remove(command.Name)
            End SyncLock
        End Sub

        'verification disabled due to stupid verifier (1.2.3.0118.5)
        <ContractVerification(False)>
        Protected Overrides Function PerformInvoke(ByVal target As T, ByVal user As BotUser, ByVal argument As String) As Task(Of String)
            Return GetHelp(user, argument).AsTask
        End Function

        Private Function GetHelp(ByVal user As BotUser, ByVal argument As String) As String
            Contract.Requires(argument IsNot Nothing)
            Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)

            Select Case argument
                Case "" 'intro
                    Return {"[Tinker] 'Help ?' for how to use commands",
                            "'Help *' for available commands",
                            "'Help +' for all commands",
                            "'Help command' for help with specific commands."
                            }.StringJoin(", ")

                Case "?" 'how to use commands
                    Return {"Commands take four types of arguments: named arguments, optional named arguments, optional switches, and raw arguments.",
                            "TIP: You can use any command by copying the format shown in its help (modify the arguments to be what you want).",
                            "Raw arguments are just plain normal arguments, what you should expect. You can skip them if their format starts with a ?.",
                            "Named arguments, such as name=value, are arguments preceded by their name=.",
                            "Optional switches, such as -useFancyPants, are argument which can either be skipped or included.",
                            "Optional named arguments, such as -optional=value, can be skipped but are otherwise named arguments prefixed with a -.",
                            "There are also tail arguments, which aren't separated by spaces, like MapQuery in 'FindMaps MapQuery...'."
                            }.StringJoin(Environment.NewLine)

                Case "*" 'list available commands
                    SyncLock lock
                        Return (From command In _commandMap.Values
                                Order By command.Name
                                Where command.IsUserAllowed(user)
                                Select command.Name
                                ).StringJoin(" ")
                    End SyncLock

                Case "+" 'list all commands
                    SyncLock lock
                        Return (From command In _commandMap.Values
                                Order By command.Name
                                Select command.Name
                                ).StringJoin(" ")
                    End SyncLock

                Case Else
                    Return GetSpecificHelp(argument)
            End Select
        End Function

        Private Function GetSpecificHelp(ByVal argument As String) As String
            Contract.Requires(argument IsNot Nothing)
            Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)

            Dim p = argument.IndexOf(" "c)
            Dim commandName = If(p = -1, argument, argument.Substring(0, p))
            Dim subtopic = If(p = -1, Nothing, argument.Substring(p + 1))

            Dim command As ICommand(Of T) = Nothing
            SyncLock lock
                If Not _commandMap.TryGetValue(key:=commandName, value:=command) Then
                    Throw New ArgumentException("No command named '{0}'.".Frmt(argument))
                End If
            End SyncLock
            Contract.Assume(command IsNot Nothing)

            If subtopic Is Nothing Then
                'Basic command help
                Return "{0} [{1} {2}] {{{3}}}".Frmt(command.Description,
                                                    command.Name,
                                                    command.Format,
                                                    command.Permissions)
            End If

            If subtopic = "*" Then
                'List subtopics
                Return (From key In command.HelpTopics.Keys
                        Order By key
                        ).StringJoin(" ")
            End If

            Dim subtopicHelp As String = Nothing
            If command.HelpTopics.TryGetValue(key:=subtopic, value:=subtopicHelp) Then
                Contract.Assume(subtopicHelp IsNot Nothing)
                Return subtopicHelp
            End If

            Throw New ArgumentException("No help found for '{0}'.".Frmt(argument))
        End Function
    End Class
End Namespace
