Imports HostBot.Commands
Imports HostBot.Bnet
Imports HostBot.Warcraft3
Imports HostBot.Links

Namespace Commands.Specializations
    Public Class LanCommands
        Inherits UICommandSet(Of W3LanAdvertiser)

        Public Sub New()
            AddCommand(New com_Add)
            AddCommand(New com_Remove)
            AddCommand(New com_Host)
        End Sub

        Public Class com_Host
            Inherits BaseCommand(Of W3LanAdvertiser)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_Host,
                           2, ArgumentLimits.min,
                           My.Resources.Command_Client_Host_Help,
                           My.Resources.Command_Client_Host_Access,
                           My.Resources.Command_Client_Host_ExtraHelp)
            End Sub
            Public Overrides Function Process(ByVal target As W3LanAdvertiser, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                'Map
                Dim map_out = W3Map.FromArgument(arguments(1))
                If Not map_out.succeeded Then Return map_out.Outcome.Futurize
                Dim map = map_out.Value

                'Server settings
                arguments = arguments.ToList
                For i = 0 To arguments.Count - 1
                    Select Case arguments(i).ToLower()
                        Case "-reserve", "-r"
                            arguments(i) = ""
                    End Select
                    If arguments(i).ToLower Like "-port=*" Then
                        arguments(i) = ""
                    End If
                Next i
                Dim header = New W3GameHeader(arguments(0),
                                              If(user Is Nothing, My.Resources.ProgramName, user.name),
                                              New W3MapSettings(arguments, map),
                                              target.serverListenPort, 0, 0, arguments, map.NumPlayerSlots)
                Dim settings = New ServerSettings(map, header, default_listen_ports:={target.serverListenPort})
                Dim f_server = target.parent.QueueCreateServer(target.name, settings, "[Not Linked]", True)

                'Create the server, then advertise the game
                Return f_server.EvalWhenValueReady(
                    Function(created_server)
                        If Not created_server.succeeded Then
                            Return created_server.Outcome
                        End If

                        'Start advertising
                        target.AddGame(header)
                        Return Success("Server created.")
                    End Function
                )
            End Function
        End Class

        '''<summary>Starts advertising a game.</summary>
        Private Class com_Add
            Inherits BaseCommand(Of W3LanAdvertiser)
            Public Sub New()
                MyBase.New("Add",
                            2, ArgumentLimits.exact,
                            "[--Add GameName Map] Adds a game to be advertised.")
            End Sub
            Public Overrides Function Process(ByVal target As W3LanAdvertiser, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Dim map_out = W3Map.FromArgument(arguments(1))
                If Not map_out.succeeded Then Return map_out.Outcome.Futurize
                Dim map = map_out.Value
                Dim name = arguments(0)
                Dim id = target.AddGame(New W3GameHeader(arguments(0),
                                                         My.Resources.ProgramName,
                                                         New W3MapSettings(arguments, map),
                                                         0, 0, 0, arguments, map.numPlayerSlots))
                Return success("Started advertising game '{0}' for map '{1}'.".frmt(name, map.RelativePath, id)).Futurize
            End Function
        End Class

        '''<summary>Stops advertising a game.</summary>
        Private Class com_Remove
            Inherits BaseCommand(Of W3LanAdvertiser)
            Public Sub New()
                MyBase.New("Remove",
                            1, ArgumentLimits.exact,
                            "[--Remove GameId] Removes a game being advertised.")
            End Sub
            Public Overrides Function Process(ByVal target As W3LanAdvertiser, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Dim id As UInteger
                If Not UInteger.TryParse(arguments(0), id) Then
                    Return failure("Invalid game id.").Futurize
                End If
                If target.RemoveGame(id) Then
                    Return success("Removed game with id " + id.ToString).Futurize
                Else
                    Return failure("Invalid game id.").Futurize
                End If
            End Function
        End Class
    End Class
End Namespace
