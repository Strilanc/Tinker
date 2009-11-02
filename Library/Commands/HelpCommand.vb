Namespace Commands
    '''<summary>Implements a command for access to a list of commands and help with specific commands.</summary>
    Public NotInheritable Class HelpCommand(Of T)
        Inherits BaseCommand(Of T)
        Private ReadOnly helpMap As New Dictionary(Of String, String)
        Private ReadOnly commands As New List(Of ICommand(Of T))

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(commands IsNot Nothing)
            Contract.Invariant(helpMap IsNot Nothing)
        End Sub

        Public Sub New()
            MyBase.New(My.Resources.Command_General_Help,
                       2, ArgumentLimitType.Max,
                       My.Resources.Command_General_Help_Help)
        End Sub

        '''<summary>Adds a command to the list of commands and specific help.</summary>
        Public Sub AddCommand(ByVal command As ICommand(Of T))
            Contract.Requires(command IsNot Nothing)
            helpMap(command.Name.ToUpperInvariant) = command.Help
            commands.Add(command)
            Dim extraHelpNormalKeys = From pair In command.ExtraHelp Where pair.Value <> "[*]" Select pair.Key
            Dim extraHelpSummaryKeys = From pair In command.ExtraHelp Where pair.Value = "[*]" Select pair.Key
            For Each key In extraHelpNormalKeys
                Contract.Assume(key IsNot Nothing)
                helpMap("{0} {1}".Frmt(command.Name, key).ToUpperInvariant) = command.ExtraHelp(key)
            Next key
            Dim helpKeysSummary = extraHelpNormalKeys.StringJoin(", ")
            For Each key In extraHelpSummaryKeys
                Contract.Assume(key IsNot Nothing)
                helpMap("{0} {1}".Frmt(command.Name, key).ToUpperInvariant) = helpKeysSummary
            Next key
        End Sub

        Public Sub RemoveCommand(ByVal command As ICommand(Of T))
            Contract.Requires(command IsNot Nothing)
            If Not commands.Contains(command) Then Return
            commands.Remove(command)
            helpMap.Remove(command.Name.ToUpperInvariant)
            For Each key In command.ExtraHelp.Keys
                Contract.Assume(key IsNot Nothing)
                helpMap.Remove("{0} {1}".Frmt(command.Name, key).ToUpperInvariant)
            Next key
        End Sub

        Public Overrides Function Process(ByVal target As T, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
            Dim arg As String = Nothing
            If arguments.Count > 0 Then arg = arguments(0)
            If arguments.Count = 2 Then arg += " " + arguments(1)
            If arg Is Nothing Then
                '[list of commands]
                Return (From command In commands
                        Where command.IsUserAllowed(user)
                        Select command.Name).StringJoin(" ").Futurized
            ElseIf helpMap.ContainsKey(arg.ToUpperInvariant) Then
                '[specific command help]
                Return helpMap(arg.ToUpperInvariant).Futurized
            Else
                '[no matching command]
                Throw New ArgumentException("{0} is not a recognized help topic.".Frmt(arg))
            End If
        End Function
    End Class
End Namespace
