''HostBot - Warcraft 3 game hosting bot
''Copyright (C) 2008 Craig Gidney
''
''This program is free software: you can redistribute it and/or modify
''it under the terms of the GNU General Public License as published by
''the Free Software Foundation, either version 3 of the License, or
''(at your option) any later version.
''
''This program is distributed in the hope that it will be useful,
''but WITHOUT ANY WARRANTY; without even the implied warranty of
''MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
''GNU General Public License for more details.
''You should have received a copy of the GNU General Public License
''along with this program.  If not, see http://www.gnu.org/licenses/

Imports Tinker.Commands

Namespace WC3.ServerCommands
    Public NotInheritable Class CommandAddGame
        Inherits TemplatedCommand(Of WC3.GameServerManager)
        Public Sub New()
            MyBase.New(Name:="Add",
                       template:=Concat({"name=<game name>", "map=<search query>"},
                                        WC3.GameSettings.PartialArgumentTemplates,
                                        WC3.GameStats.PartialArgumentTemplates).StringJoin(" "),
                       Description:="Adds a game set to the server.",
                       extraHelp:=Concat(WC3.GameSettings.PartialArgumentHelp,
                                         WC3.GameStats.PartialArgumentHelp).StringJoin(Environment.NewLine))
        End Sub
        <SuppressMessage("Microsoft.Contracts", "Ensures-40-81")>
        Protected Overloads Overrides Async Function PerformInvoke(ByVal target As WC3.GameServerManager, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
            Await target.QueueAddGameFromArguments(argument, user)
            Return "Game added."
        End Function
    End Class
End Namespace
