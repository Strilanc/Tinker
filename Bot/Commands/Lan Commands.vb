Imports HostBot.Commands
Imports HostBot.Bnet
Imports HostBot.Warcraft3
Imports HostBot.Links

Namespace Commands.Specializations
    Public Class LanCommands
        Inherits UICommandSet(Of W3LanAdvertiser)

        Public Sub New()
            add_subcommand(New com_Add)
            add_subcommand(New com_Remove)
        End Sub

        '''<summary>Starts advertising a game.</summary>
        Private Class com_Add
            Inherits BaseCommand(Of W3LanAdvertiser)
            Public Sub New()
                MyBase.New("Add", _
                            2, ArgumentLimits.exact, _
                            "[--Add GameName Map] Adds a game to be advertised.")
            End Sub
            Public Overrides Function process(ByVal target As W3LanAdvertiser, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Dim map_out = W3Map.FromArgument(arguments(1))
                If map_out.outcome <> Outcomes.succeeded Then Return futurize(Of Outcome)(map_out)
                Dim map = map_out.val
                Dim name = arguments(0)
                Dim id = target.add_game(name, map, New W3Map.MapSettings(arguments))
                Return futurize(success("Started advertising game '{0}' for map '{1}'.".frmt(name, map.relative_path, id)))
            End Function
        End Class

        '''<summary>Stops advertising a game.</summary>
        Private Class com_Remove
            Inherits BaseCommand(Of W3LanAdvertiser)
            Public Sub New()
                MyBase.New("Remove", _
                            1, ArgumentLimits.exact, _
                            "[--Remove GameId] Removes a game being advertised.")
            End Sub
            Public Overrides Function process(ByVal target As W3LanAdvertiser, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Dim id As UInteger
                If Not UInteger.TryParse(arguments(0), id) Then
                    Return futurize(failure("Invalid game id."))
                End If
                If target.remove_game(id) Then
                    Return futurize(success("Removed game with id " + id.ToString))
                Else
                    Return futurize(failure("Invalid game id."))
                End If
            End Function
        End Class
    End Class
End Namespace
