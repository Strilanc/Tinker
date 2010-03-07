Imports Tinker.Commands
Imports Tinker.Bot

Namespace Lan
    Public NotInheritable Class AdvertiserCommands
        Inherits CommandSet(Of Lan.AdvertiserManager)

        Public Sub New()
            AddCommand(New CommandAdd)
            AddCommand(New CommandAuto)
            AddCommand(New CommandHost)
            AddCommand(New CommandRemove)
        End Sub

        Public Overloads Function AddCommand(ByVal command As Command(Of Lan.Advertiser)) As IDisposable
            Contract.Requires(command IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            Return AddCommand(New ProjectedCommand(Of Lan.AdvertiserManager, Lan.Advertiser)(
                    command:=command,
                    projection:=Function(manager) manager.Advertiser))
        End Function

        Private Class CommandAuto
            Inherits TemplatedCommand(Of AdvertiserManager)
            Public Sub New()
                MyBase.New(Name:="Auto",
                           template:="On|Off",
                           Description:="Causes the advertiser to automatically advertise all games on any server when 'On'.")
            End Sub
            Protected Overloads Overrides Function PerformInvoke(ByVal target As AdvertiserManager, ByVal user As BotUser, ByVal argument As Commands.CommandArgument) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                Select Case New InvariantString(argument.RawValue(0))
                    Case "On"
                        Return target.QueueSetAutomatic(True).ContinueWithFunc(Function() "Now automatically advertising games.")
                    Case "Off"
                        Return target.QueueSetAutomatic(False).ContinueWithFunc(Function() "Now not automatically advertising games.")
                    Case Else
                        Throw New ArgumentException("Must specify 'On' or 'Off' as an argument.")
                End Select
            End Function
        End Class

        Private Class CommandAdd
            Inherits TemplatedCommand(Of Advertiser)
            Public Sub New()
                MyBase.New(Name:="Add",
                           template:="id=# name=<game name> map=<search query>",
                           Description:="Adds a game to be advertised, but doesn't create a new server to go with it.")
            End Sub
            Protected Overloads Overrides Function PerformInvoke(ByVal target As Advertiser, ByVal user As BotUser, ByVal argument As Commands.CommandArgument) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                Dim name = argument.NamedValue("name")
                Dim map = WC3.Map.FromArgument(argument.NamedValue("map"))
                Dim gameStats = New WC3.GameStats(map, If(user Is Nothing, Application.ProductName.AssumeNotNull, user.Name.Value), argument)
                Dim gameDescription = WC3.LocalGameDescription.FromArguments(name, map, gameStats, clock:=New SystemClock())

                Return target.QueueAddGame(gameDescription).ContinueWithFunc(Function() "Started advertising game '{0}' for map '{1}'.".Frmt(name, gameStats.AdvertisedPath))
            End Function
        End Class

        Private Class CommandRemove
            Inherits TemplatedCommand(Of Advertiser)
            Public Sub New()
                MyBase.New(Name:="Remove",
                           template:="id",
                           Description:="Removes a game being advertised.")
            End Sub
            Protected Overloads Overrides Function PerformInvoke(ByVal target As Advertiser, ByVal user As BotUser, ByVal argument As Commands.CommandArgument) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                Dim id As UInteger
                If Not UInteger.TryParse(argument.RawValue(0), id) Then
                    Throw New InvalidOperationException("Invalid game id.")
                End If
                Return target.QueueRemoveGame(id).Select(
                    Function(val)
                        If val Then
                            Return "Removed game with id {0}".Frmt(id)
                        Else
                            Throw New InvalidOperationException("Invalid game id.")
                        End If
                    End Function)
            End Function
        End Class

        Private Class CommandHost
            Inherits TemplatedCommand(Of AdvertiserManager)
            Public Sub New()
                MyBase.New(Name:="Host",
                           template:=Concat({"name=<game name>", "map=<search query>"},
                                            WC3.GameSettings.PartialArgumentTemplates,
                                            WC3.GameStats.PartialArgumentTemplates).StringJoin(" "),
                           Description:="Creates a server of a game and advertises it on lan. More help topics under 'Help Host *'.",
                           Permissions:="games:1",
                           extraHelp:=Concat(WC3.GameSettings.PartialArgumentHelp, WC3.GameStats.PartialArgumentHelp).StringJoin(Environment.NewLine))
            End Sub
            Protected Overloads Overrides Function PerformInvoke(ByVal target As AdvertiserManager, ByVal user As BotUser, ByVal argument As Commands.CommandArgument) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                Dim futureServer = target.Bot.QueueGetOrConstructGameServer()
                Dim futureGameSet = (From server In futureServer
                                     Select server.QueueAddGameFromArguments(argument, user)
                                    ).Unwrap.AssumeNotNull
                Dim futureAdvertised = (From gameSet In futureGameSet
                                        Select target.Advertiser.QueueAddGame(gameSet.GameSettings.GameDescription)
                                       ).Unwrap.AssumeNotNull
                futureAdvertised.Catch(Sub() If futureGameSet.Status = TaskStatus.RanToCompletion Then futureGameSet.Result.Dispose())
                Dim futureDesc = futureAdvertised.ContinueWithFunc(Function() futureGameSet.Result.GameSettings.GameDescription)
                Return futureDesc.select(Function(desc) "Hosted game '{0}' for map '{1}'".Frmt(desc.name, desc.GameStats.AdvertisedPath))
            End Function
        End Class
    End Class
End Namespace
