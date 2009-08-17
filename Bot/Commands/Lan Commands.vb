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
        End Sub

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
