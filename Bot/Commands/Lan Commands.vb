Imports HostBot.Commands
Imports HostBot.Links

Namespace Commands.Specializations
    Public NotInheritable Class LanCommands
        Inherits CommandSet(Of WC3.LanAdvertiser)

        Public Sub New()
            AddCommand(Add)
            AddCommand(Remove)
            AddCommand(Host)
        End Sub

        Private Shared ReadOnly Add As New DelegatedTemplatedCommand(Of WC3.LanAdvertiser)(
            Name:="Add",
            template:="name=<game name> map=<search query>",
            Description:="Adds a game to be advertised, but doesn't create a new server to go with it.",
            func:=Function(target, user, argument)
                      Dim name = argument.NamedValue("name")
                      Dim map = WC3.Map.FromArgument(argument.NamedValue("map"))
                      Dim desc = WC3.GameDescription.FromArguments(name,
                                                                 map,
                                                                 New WC3.GameStats(map, If(user Is Nothing, My.Resources.ProgramName, user.Name), argument))
                      Dim id = target.AddGame(desc)
                      Return "Started advertising game '{0}' for map '{1}'.".Frmt(name, desc.GameStats.relativePath, id).Futurized
                  End Function)

        Private Shared ReadOnly Remove As New DelegatedTemplatedCommand(Of WC3.LanAdvertiser)(
            Name:="Remove",
            template:="id",
            Description:="Removes a game being advertised.",
            func:=Function(target, user, argument)
                      Dim id As UInteger
                      If Not UInteger.TryParse(argument.RawValue(0), id) Then
                          Throw New InvalidOperationException("Invalid game id.")
                      End If
                      If target.RemoveGame(id) Then
                          Return "Removed game with id {0}".Frmt(id).Futurized
                      Else
                          Throw New InvalidOperationException("Invalid game id.")
                      End If
                  End Function)

        Private Shared ReadOnly Host As New DelegatedTemplatedCommand(Of WC3.LanAdvertiser)(
            Name:="Host",
            template:=Concat({"name=<game name>", "map=<search query>"},
                             WC3.ServerSettings.PartialArgumentTemplates,
                             WC3.GameStats.PartialArgumentTemplates).StringJoin(" "),
            Description:="Creates a server of a game and advertises it on lan. More help topics under 'Help Host *'.",
            Permissions:="games=1",
            extraHelp:=Concat(New String() {},
                              WC3.ServerSettings.PartialArgumentHelp,
                              WC3.GameStats.PartialArgumentHelp).StringJoin(Environment.NewLine),
            func:=Function(target, user, argument)
                      Dim map = WC3.Map.FromArgument(argument.NamedValue("map"))

                      If argument.TryGetOptionalNamedValue("Port") IsNot Nothing AndAlso user IsNot Nothing AndAlso user.Permission("root") < 5 Then
                          Throw New InvalidOperationException("You need root:5 to use -port.")
                      End If
                      Dim header = WC3.GameDescription.FromArguments(argument.NamedValue("name"),
                                                                   map,
                                                                   New WC3.GameStats(map, If(user Is Nothing, My.Resources.ProgramName, user.Name), argument))
                      Dim settings = New WC3.ServerSettings(map, header, argument, defaultListenPorts:={target.serverListenPort})
                      Dim f_server = target.parent.QueueCreateServer(target.name, settings, "[Not Linked]", True)

                      'Create the server, then advertise the game
                      Return f_server.EvalOnSuccess(
                          Function()
                              target.AddGame(header)
                              Return "Server created."
                          End Function
                      )
                          End Function)
    End Class
End Namespace
