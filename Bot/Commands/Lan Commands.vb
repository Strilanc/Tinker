Imports HostBot.Commands
Imports HostBot.Bnet
Imports HostBot.Warcraft3
Imports HostBot.Links

Namespace Commands.Specializations
    Public NotInheritable Class LanCommands
        Inherits CommandSet(Of W3LanAdvertiser)

        Public Sub New()
            AddCommand(New CommandAdd)
            AddCommand(New CommandRemove)
            AddCommand(New CommandHost)
        End Sub

        Public NotInheritable Class CommandHost
            Inherits BaseCommand(Of W3LanAdvertiser)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_Host,
                           2, ArgumentLimitType.Min,
                           My.Resources.Command_Client_Host_Help,
                           My.Resources.Command_Client_Host_Access,
                           My.Resources.Command_Client_Host_ExtraHelp)
            End Sub
            Public Overrides Function Process(ByVal target As W3LanAdvertiser, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Contract.Assume(arguments.Count >= 2)
                Contract.Assume(arguments(0) IsNot Nothing)
                Contract.Assume(arguments(1) IsNot Nothing)
                Dim map = W3Map.FromArgument(arguments(1))

                'Server settings
                arguments = arguments.ToList
                For i = 0 To arguments.Count - 1
                    Select Case arguments(i).ToUpperInvariant
                        Case "-RESERVE", "-R"
                            arguments(i) = ""
                    End Select
                    If arguments(i).ToUpperInvariant Like "-PORT=*" Then
                        arguments(i) = ""
                    End If
                Next i
                Dim header = W3GameDescription.FromArguments(arguments(0),
                                                             arguments(1),
                                                             If(user Is Nothing, My.Resources.ProgramName, user.Name),
                                                             arguments)
                Dim settings = New ServerSettings(map, header, defaultListenPorts:={target.serverListenPort})
                Dim f_server = target.parent.QueueCreateServer(target.name, settings, "[Not Linked]", True)

                'Create the server, then advertise the game
                Return f_server.EvalOnSuccess(
                    Function()
                        target.AddGame(header)
                        Return "Server created."
                    End Function
                )
            End Function
        End Class

        '''<summary>Starts advertising a game.</summary>
        Private NotInheritable Class CommandAdd
            Inherits BaseCommand(Of W3LanAdvertiser)
            Public Sub New()
                MyBase.New("Add",
                            2, ArgumentLimitType.Exact,
                            "[--Add GameName Map] Adds a game to be advertised.")
            End Sub
            Public Overrides Function Process(ByVal target As W3LanAdvertiser, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Contract.Assume(arguments.Count = 2)
                Contract.Assume(arguments(0) IsNot Nothing)
                Contract.Assume(arguments(1) IsNot Nothing)
                Dim name = arguments(0)
                Dim map = W3Map.FromArgument(arguments(1))
                Dim id = target.AddGame(W3GameDescription.FromArguments(arguments(0),
                                                                        arguments(1),
                                                                        If(user Is Nothing, My.Resources.ProgramName, user.Name),
                                                                        arguments))
                Return "Started advertising game '{0}' for map '{1}'.".Frmt(name, map.RelativePath, id).Futurized
            End Function
        End Class

        '''<summary>Stops advertising a game.</summary>
        Private NotInheritable Class CommandRemove
            Inherits BaseCommand(Of W3LanAdvertiser)
            Public Sub New()
                MyBase.New("Remove",
                            1, ArgumentLimitType.Exact,
                            "[--Remove GameId] Removes a game being advertised.")
            End Sub
            Public Overrides Function Process(ByVal target As W3LanAdvertiser, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Contract.Assume(arguments.Count = 1)
                Contract.Assume(arguments(0) IsNot Nothing)
                Dim id As UInteger
                If Not UInteger.TryParse(arguments(0), id) Then
                    Throw New InvalidOperationException("Invalid game id.")
                End If
                If target.RemoveGame(id) Then
                    Return "Removed game with id {0}".Frmt(id).Futurized
                Else
                    Throw New InvalidOperationException("Invalid game id.")
                End If
            End Function
        End Class
    End Class
End Namespace
