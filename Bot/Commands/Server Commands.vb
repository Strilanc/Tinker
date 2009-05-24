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
        Inherits UICommandSet(Of IW3Server)

        Public Sub New()
            add_subcommand(New com_OpenInstance)
            add_subcommand(New com_StartListening)
            add_subcommand(New com_StopListening)
            add_subcommand(New com_CloseInstance)
            add_subcommand(New com_Bot)
        End Sub

        Private Class com_Bot
            Inherits BaseCommand(Of IW3Server)
            Public Sub New()
                MyBase.New("bot", _
                            0, ArgumentLimits.free, _
                            "[--bot command, --bot CreateUser Strilanc, --bot help] Forwards text commands to the main bot.")
            End Sub
            Public Overrides Function process(ByVal target As IW3Server, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return target.parent.bot_commands.processText(target.parent, user, mendQuotedWords(arguments))
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
            Inherits BaseCommand(Of IW3Server)
            Public Sub New()
                MyBase.New("StartListening", _
                            1, ArgumentLimits.exact, _
                            "[--StartListening port]", _
                            DictStrUInt("root=4"))
            End Sub
            Public Overrides Function process(ByVal target As IW3Server, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Dim port As UShort
                If Not UShort.TryParse(arguments(0), port) Then Return futurize(failure("Invalid port"))
                Return target.f_OpenPort(port)
            End Function
        End Class

        '''<summary>A command which tells the server to stop listening on a port or all ports.</summary>
        Private Class com_StopListening
            Inherits BaseCommand(Of IW3Server)
            Public Sub New()
                MyBase.New("StopListening", _
                            1, ArgumentLimits.max, _
                            "[--StopListening, --StopListening port] Tells the server to stop listening on a port or all ports.", _
                            DictStrUInt("root=4"))
            End Sub
            Public Overrides Function process(ByVal target As IW3Server, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                If arguments.Count = 0 Then
                    Return target.f_CloseAllPorts()
                Else
                    Dim port As UShort
                    If Not UShort.TryParse(arguments(0), port) Then
                        Return futurize(failure("Invalid port"))
                    End If
                    Return target.f_ClosePort(port)
                End If
            End Function
        End Class

        Private Class com_OpenInstance
            Inherits BaseCommand(Of IW3Server)
            Public Sub New()
                MyBase.New("Open", _
                            1, ArgumentLimits.max, _
                            "[--Open name=generated_name]", _
                            DictStrUInt("root=4;games=4"))
            End Sub
            Public Overrides Function process(ByVal target As IW3Server, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return stripFutureOutcome(target.f_CreateGame(arguments(0)))
            End Function
        End Class
        Private Class com_CloseInstance
            Inherits BaseCommand(Of IW3Server)
            Public Sub New()
                MyBase.New("Close", _
                            1, ArgumentLimits.exact, _
                            "[--Close name]", _
                            DictStrUInt("root=4;games=4"))
            End Sub
            Public Overrides Function process(ByVal target As IW3Server, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return target.f_RemoveGame(arguments(0))
            End Function
        End Class
    End Class
End Namespace
