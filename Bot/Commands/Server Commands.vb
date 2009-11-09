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

Imports HostBot.Commands
Imports HostBot.Warcraft3

Namespace Commands.Specializations
    Public NotInheritable Class ServerCommands
        Inherits CommandSet(Of W3Server)

        Public Sub New()
            AddCommand(OpenInstance)
            AddCommand(StartListening)
            AddCommand(StopListening)
            AddCommand(CloseInstance)
            AddCommand(Bot)
        End Sub

        Private Shared ReadOnly Bot As New DelegatedCommand(Of W3Server)(
            Name:="Bot",
            Format:="...",
            Description:="Forwards commands to the main bot.",
            Permissions:="root=1",
            func:=Function(client, user, argument)
                      Return client.Parent.BotCommands.invoke(client.Parent, user, argument)
                  End Function)

        Private ReadOnly StartListening As New DelegatedTemplatedCommand(Of W3Server)(
            Name:="Listen",
            template:="port",
            Description:="Starts listening for connections on a port.",
            Permissions:="root=4",
            func:=Function(target, user, argument)
                      Dim port As UShort
                      If Not UShort.TryParse(argument.RawValue(0), port) Then Throw New ArgumentException("Invalid port")
                      Return target.QueueOpenPort(port).EvalOnSuccess(Function() "Port opened.")
                  End Function)

        Private ReadOnly StopListening As New DelegatedTemplatedCommand(Of W3Server)(
            Name:="StopListening",
            template:="-port=#",
            Description:="Stops listening on a port. If no port is given, stops listening on all ports.",
            Permissions:="root=4",
            func:=Function(target, user, argument)
                      If argument.TryGetOptionalNamedValue("port") Is Nothing Then
                          Return target.QueueCloseAllPorts().EvalOnSuccess(Function() "Ports closed.")
                      Else
                          Dim port As UShort
                          If Not UShort.TryParse(argument.TryGetOptionalNamedValue("port"), port) Then
                              Throw New InvalidOperationException("Invalid port")
                          End If
                          Return target.QueueClosePort(port).EvalOnSuccess(Function() "Port closed.")
                      End If
                  End Function)

        Private ReadOnly OpenInstance As New DelegatedTemplatedCommand(Of W3Server)(
            Name:="Open",
            template:="name",
            Description:="Opens a new game instance.",
            Permissions:="root=4;games=4",
            func:=Function(target, user, argument)
                      Return target.QueueCreateGame(argument.RawValue(0)).EvalOnSuccess(Function() "Created instance.")
                  End Function)

        Private ReadOnly CloseInstance As New DelegatedTemplatedCommand(Of W3Server)(
            Name:="Close",
            template:="name",
            Description:="Closes the named game instance.",
            Permissions:="root=4;games=4",
            func:=Function(target, user, argument)
                      Return target.QueueRemoveGame(argument.RawValue(0), ignorePermanent:=True).EvalOnSuccess(Function() "Closed instance.")
                  End Function)
    End Class
End Namespace
