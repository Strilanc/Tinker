Imports HostBot.Commands
Imports HostBot.Bnet
Imports HostBot.Warcraft3
Imports HostBot.Links

Namespace Commands.Specializations
    Public NotInheritable Class LanCommands
        Inherits CommandSet(Of W3LanAdvertiser)

        Public Sub New()
            AddCommand(Add)
            AddCommand(Remove)
            AddCommand(Host)
        End Sub

        Private Shared ReadOnly Add As New DelegatedTemplatedCommand(Of W3LanAdvertiser)(
            Name:="Add",
            template:="name=<game name> map=<search query>",
            Description:="Adds a game to be advertised, but doesn't create a new server to go with it.",
            func:=Function(target, user, argument)
                      Dim name = argument.NamedValue("name")
                      Dim map = argument.NamedValue("map")
                      If True Then Throw New NotImplementedException()
                      Dim desc = W3GameDescription.FromArguments(name,
                                                                 map,
                                                                 If(user Is Nothing, My.Resources.ProgramName, user.Name),
                                                                 New String() {})
                      Dim id = target.AddGame(desc)
                      Return "Started advertising game '{0}' for map '{1}'.".Frmt(name, desc.GameStats.relativePath, id).Futurized
                  End Function)

        Private Shared ReadOnly Remove As New DelegatedTemplatedCommand(Of W3LanAdvertiser)(
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

        Private Shared ReadOnly Host As New DelegatedTemplatedCommand(Of W3LanAdvertiser)(
            Name:="Host",
            template:={"name=<game name>", "map=<search query>", "-admin=user -admin -a=user -a", "-autoStart -as",
                     "-instances=# -i=#", "-fullSharedControl", "-grab", "-loadInGame", "-lig", "-multiObs -mo",
                     "-noUL", "-noDL", "-obs -o", "-obsOnDefeat -od", "-permanent -perm", "-private -p", "-randomHero -rh",
                     "-randomRace -rr", "-referees -ref", "-reserve -reserve=<name1 name2 ...> -r -r=<name1 name2 ...>",
                     "-speed={medium,slow}", "-teams=#v#... -t=#v#...", "-teamsApart", "-vis={all,explored,none}", "-port=#"
                     }.StringJoin(" "),
            Description:="Hosts a game in the custom games list. More help topics under 'Help Host *'.",
            Permissions:="games=1",
            extraHelp:={"Admin=admin, a: Sets the auto-elevated username. Use no argument to match your name.",
                        "Autostart=autostart, as: Instances will start automatically when they fill up.",
                        "Instances=instances, i: Sets the initial number of instances. Use 0 for unlimited instances.",
                        "FullShare=fullShare: Turns on wc3's 'full shared control' option.",
                        "Grab=grab: Downloads the map file from joining players. Meant for use when hosting a map by meta-data.",
                        "LoadInGame=loadInGame, lig: Players wait for loaders in the game instead of at the load screen.",
                        "MultiObs=multiObs, mo: Turns on observers, and creates a special slot which can accept large amounts of players. The map must have two available obs slots for this to work.",
                        "NoUL=noUL: Turns off uploads from the bot, but still allows players to download from each other.",
                        "NoDL=noDL: Boots players who don't already have the map.",
                        "Obs=obs, -o: Turns on full observers.",
                        "ObsOnDefeat=obsOnDefeat, -od: Turns on observers on defeat.",
                        "Permanent=permanent, perm: Automatically recreate closed instances and automatically sets the game to private/public as new instances are available.",
                        "Private=private, p: Creates a private game instead of a public game.",
                        "RandomHero=randomHero, rh: Turns on the wc3 'random hero' option.",
                        "RandomRace=randomRace, rr: Turns on the wc3 'random race' option.",
                        "Referees=referees, ref: Turns on observer referees.",
                        "Reserve=reserve, r: Reserves the slots for players or yourself.",
                        "Speed=speed: Sets wc3's game speed option to medium or slow.",
                        "Teams=Teams, t: Sets the initial number of open slots for each team.",
                        "TeamsApart=teamsApart: Turns off wc3's 'teams together' option.",
                        "UnlockTeams=unlockTeams: Turns off wc3's 'lock teams' option.",
                        "Visibility=visibility, vis: Sets wc3's visibility option to all, explored, or none.",
                        "Port=port: Sets the port the client will advertise on and the created server will listen on. Requires root:5."
                        }.StringJoin(Environment.NewLine),
            func:=Function(target, user, argument)
                      Dim map = W3Map.FromArgument(argument.RawValue(1))

                      ''Server settings
                      'arguments = arguments.ToList
                      'For i = 0 To arguments.Count - 1
                      'Select Case arguments(i).ToUpperInvariant
                      'Case "-RESERVE", "-R"
                      'arguments(i) = ""
                      'End Select
                      'If arguments(i).ToUpperInvariant Like "-PORT=*" Then
                      'arguments(i) = ""
                      'End If
                      'Next i
                      If True Then Throw New NotImplementedException()
                      Dim header = W3GameDescription.FromArguments(argument.RawValue(0),
                                                                   argument.RawValue(1),
                                                                   If(user Is Nothing, My.Resources.ProgramName, user.Name),
                                                                   New String() {})
                      Dim settings = New ServerSettings(map, header, defaultListenPorts:={target.serverListenPort})
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
