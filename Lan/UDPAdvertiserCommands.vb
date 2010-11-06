Imports Tinker.Commands
Imports Tinker.Bot

Namespace Lan.Commands
    Public Class CommandAuto
        Inherits TemplatedCommand(Of UDPAdvertiserComponent)
        Public Sub New()
            MyBase.New(Name:="Auto",
                       template:="On|Off",
                       Description:="Causes the advertiser to automatically advertise all games on any server when 'On'.")
        End Sub
        <ContractVerification(False)>
        Protected Overloads Overrides Async Function PerformInvoke(ByVal target As UDPAdvertiserComponent, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
            Select Case New InvariantString(argument.RawValue(0))
                Case "On"
                    Await target.QueueSetAutomatic(True)
                    Return "Now automatically advertising games."
                Case "Off"
                    Await target.QueueSetAutomatic(False)
                    Return "Now not automatically advertising games."
                Case Else
                    Throw New ArgumentException("Must specify 'On' or 'Off' as an argument.")
            End Select
        End Function
    End Class

    Public Class CommandAdd
        Inherits TemplatedCommand(Of UDPAdvertiser)
        Public Sub New()
            MyBase.New(Name:="Add",
                       template:="id=# name=<game name> map=<search query>",
                       Description:="Adds a game to be advertised, but doesn't create a new server to go with it.")
        End Sub
        <ContractVerification(False)>
        Protected Overloads Overrides Async Function PerformInvoke(ByVal target As UDPAdvertiser, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
            Dim id = UInt32.Parse(argument.NamedValue("id"))
            Dim name = argument.NamedValue("name")
            Dim map = WC3.Map.FromArgument(argument.NamedValue("map"))
            If id = 0 Then Throw New ArgumentException("Non-positive id.")
            Dim gameStats = WC3.GameStats.FromMapAndArgument(map, If(user Is Nothing, Application.ProductName.AssumeNotNull, user.Name.Value), argument)
            Dim gameDescription = WC3.LocalGameDescription.FromArguments(name, map, id, gameStats, clock:=New SystemClock())

            Await target.QueueAddGame(gameDescription)
            Return "Started advertising game '{0}' for map '{1}'.".Frmt(name, gameStats.AdvertisedPath)
        End Function
    End Class

    Public Class CommandRemove
        Inherits TemplatedCommand(Of UDPAdvertiser)
        Public Sub New()
            MyBase.New(Name:="Remove",
                       template:="id",
                       Description:="Removes a game being advertised.")
        End Sub
        <ContractVerification(False)>
        Protected Overloads Overrides Async Function PerformInvoke(ByVal target As UDPAdvertiser, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
            Dim id As UInteger
            If Not UInteger.TryParse(argument.RawValue(0), id) Then Throw New InvalidOperationException("Invalid game id.")

            Dim removed = Await target.QueueRemoveGame(id)
            If Not removed Then Throw New InvalidOperationException("Invalid game id.")

            Return "Removed game with id {0}".Frmt(id)
        End Function
    End Class

    Public Class CommandHost
        Inherits TemplatedCommand(Of UDPAdvertiserComponent)
        Public Sub New()
            MyBase.New(Name:="Host",
                       template:=Concat({"name=<game name>", "map=<search query>"},
                                        WC3.GameSettings.PartialArgumentTemplates,
                                        WC3.GameStats.PartialArgumentTemplates).StringJoin(" "),
                       Description:="Creates a server of a game and advertises it on lan. More help topics under 'Help Host *'.",
                       Permissions:="games:1",
                       extraHelp:=Concat(WC3.GameSettings.PartialArgumentHelp, WC3.GameStats.PartialArgumentHelp).StringJoin(Environment.NewLine))
        End Sub
        <ContractVerification(False)>
        Protected Overloads Overrides Async Function PerformInvoke(ByVal target As UDPAdvertiserComponent, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
            Dim server = Await target.Bot.QueueGetOrConstructGameServer()
            Dim gameSet = Await server.QueueAddGameFromArguments(argument, user)
            Try
                Await target.Advertiser.QueueAddGame(gameSet.GameSettings.GameDescription)
            Catch ex As Exception
                gameSet.Dispose()
                Throw
            End Try
            Dim desc = gameSet.GameSettings.GameDescription
            Return "Hosted game '{0}' for map '{1}'".Frmt(desc.Name, desc.GameStats.AdvertisedPath)
        End Function
    End Class
End Namespace
