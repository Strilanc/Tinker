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
    Public Class ServerCommands
        Inherits UICommandSet(Of W3Server)

        Public Sub New()
            AddCommand(New com_OpenInstance)
            AddCommand(New com_StartListening)
            AddCommand(New com_StopListening)
            AddCommand(New com_CloseInstance)
            AddCommand(New com_Bot)
        End Sub

        Private Class com_Bot
            Inherits BaseCommand(Of W3Server)
            Public Sub New()
                MyBase.New("bot",
                            0, ArgumentLimits.free,
                            "[--bot command, --bot CreateUser Strilanc, --bot help] Forwards text commands to the main bot.")
            End Sub
            Public Overrides Function Process(ByVal target As W3Server, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.parent.BotCommands.ProcessCommand(target.parent, user, arguments)
            End Function
        End Class

        'Private Class com_ParentCommand
        '    Inherits BaseCommand(Of W3GameServer)
        '    Private parent_command As BaseCommand(Of MainBot)
        '    Public Sub New(ByVal parent_command As BaseCommand(Of MainBot))
        '        MyBase.New(parent_command.name, parent_command.argument_limit_value, parent_command.argument_limit_type, parent_command.help, parent_command.required_permissions)
        '        Me.parent_command = parent_command
        '    End Sub
        '    Public Overrides Function process(ByVal target As W3GameServer, ByVal user As BotUser, ByVal arguments As IList(Of String)) As itfFuture(Of operationoutcome)
        '        Return parent_command.processText(target.parent, user, mendQuotedWords(arguments))
        '    End Function
        'End Class

        '''<summary>A command which tells the server to stop listening on a port.</summary>
        Private Class com_StartListening
            Inherits BaseCommand(Of W3Server)
            Public Sub New()
                MyBase.New("Listen",
                            1, ArgumentLimits.exact,
                            "[--Listen port]",
                            "root=4", "")
            End Sub
            Public Overrides Function Process(ByVal target As W3Server, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Dim port As UShort
                If Not UShort.TryParse(arguments(0), port) Then Throw New ArgumentException("Invalid port")
                Return target.QueueOpenPort(port).EvalOnSuccess(Function() "Port opened.")
            End Function
        End Class

        '''<summary>A command which tells the server to stop listening on a port or all ports.</summary>
        Private Class com_StopListening
            Inherits BaseCommand(Of W3Server)
            Public Sub New()
                MyBase.New("StopListening",
                            1, ArgumentLimits.max,
                            "[--StopListening, --StopListening port] Tells the server to stop listening on a port or all ports.",
                            "root=4", "")
            End Sub
            Public Overrides Function Process(ByVal target As W3Server, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                If arguments.Count = 0 Then
                    Return target.QueueCloseAllPorts().EvalOnSuccess(Function() "Ports closed.")
                Else
                    Dim port As UShort
                    If Not UShort.TryParse(arguments(0), port) Then
                        Throw New InvalidOperationException("Invalid port")
                    End If
                    Return target.QueueClosePort(port).EvalOnSuccess(Function() "Port closed.")
                End If
            End Function
        End Class

        Private Class com_OpenInstance
            Inherits BaseCommand(Of W3Server)
            Public Sub New()
                MyBase.New("Open",
                            1, ArgumentLimits.max,
                            "[--Open name=generated_name]",
                            "root=4;games=4", "")
            End Sub
            Public Overrides Function Process(ByVal target As W3Server, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.QueueCreateGame(arguments(0)).EvalOnSuccess(Function() "Created instance.")
            End Function
        End Class
        Private Class com_CloseInstance
            Inherits BaseCommand(Of W3Server)
            Public Sub New()
                MyBase.New("Close",
                            1, ArgumentLimits.exact,
                            "[--Close name]",
                            "root=4;games=4", "")
            End Sub
            Public Overrides Function Process(ByVal target As W3Server, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.QueueRemoveGame(arguments(0), ignorePermanent:=True).EvalOnSuccess(Function() "Closed instance.")
            End Function
        End Class
    End Class
End Namespace
