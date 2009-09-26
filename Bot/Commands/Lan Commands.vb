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
            Public Overrides Function Process(ByVal target As W3LanAdvertiser, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Dim map = W3Map.FromArgument(arguments(1))

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
                Return f_server.EvalOnSuccess(
                    Function()
                        target.AddGame(header)
                        Return "Server created."
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
            Public Overrides Function Process(ByVal target As W3LanAdvertiser, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Dim name = arguments(0)
                Dim map = W3Map.FromArgument(arguments(1))
                Dim id = target.AddGame(New W3GameHeader(arguments(0),
                                                         My.Resources.ProgramName,
                                                         New W3MapSettings(arguments, map),
                                                         0, 0, 0, arguments, map.NumPlayerSlots))
                Return "Started advertising game '{0}' for map '{1}'.".Frmt(name, map.RelativePath, id).Futurized
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
            Public Overrides Function Process(ByVal target As W3LanAdvertiser, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
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
