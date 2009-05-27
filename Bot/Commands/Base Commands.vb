Namespace Commands
#Region "Interfaces"
    '''<summary>Specifies how an argument limit is checked.</summary>
    Public Enum ArgumentLimits
        free
        exact
        max
        min
    End Enum

    '''<summary>Represents a simple command-line command.</summary>
    '''<typeparam name="T">The type of the target argument included when running the command.</typeparam>
    Public Interface ICommand(Of T)
        '''<summary>The name of the command.</summary>
        Function name() As String
        '''<summary>Help for using the command.</summary>
        Function help() As String
        '''<summary>Determines if the command's arguments are appropriate for logging.</summary>
        Function hide_arguments() As Boolean
        '''<summary>Used for pre-checks on number of arguments. Applied based on argument limit type.</summary>
        Function argument_limit_value() As Integer
        '''<summary>Determines how the argument limit is applied.</summary>
        Function argument_limit_type() As ArgumentLimits
        '''<summary>Runs the command.</summary>
        '''<param name="target">The object the command is being run from.</param>
        '''<param name="user">The user running the command. Local user is nothing.</param>
        '''<param name="argument">The argument text passed to the command</param>
        Function process(ByVal target As T, ByVal user As BotUser, ByVal argument As String) As IFuture(Of Outcome)
        '''<summary>Determines if a user is allowed to use the command.</summary>
        Function user_allowed(ByVal user As BotUser) As Boolean
        Function extra_help() As Dictionary(Of String, String)
    End Interface
#End Region

#Region "Base Classes"
    '''<summary>A simple base implementation of the itfCommand(Of T) interface.</summary>
    Public MustInherit Class BaseCommand(Of T)
        Implements ICommand(Of T)
        Public ReadOnly name As String
        Public ReadOnly help As String
        Public ReadOnly argument_limit_value As Integer
        Public ReadOnly argument_limit_type As ArgumentLimits
        Public ReadOnly required_permissions As Dictionary(Of String, UInteger)
        Public ReadOnly extra_help As Dictionary(Of String, String)
        Public ReadOnly hide_arguments As Boolean = False

        Public Sub New( _
                    ByVal name As String,
                    ByVal argument_limit_value As Integer,
                    ByVal argument_limit_type As ArgumentLimits,
                    ByVal help As String,
                    ByVal required_permissions As String,
                    ByVal extra_help As String,
                    Optional ByVal hide_arguments As Boolean = False _
                    )
            Me.New(name, argument_limit_value, argument_limit_type, help, DictStrUInt(required_permissions), DictStrStr(extra_help, vbNewLine), hide_arguments)
        End Sub
        Public Sub New( _
                    ByVal name As String,
                    ByVal argument_limit_value As Integer,
                    ByVal argument_limit_type As ArgumentLimits,
                    ByVal help As String,
                    Optional ByVal required_permissions As Dictionary(Of String, UInteger) = Nothing,
                    Optional ByVal extra_help As Dictionary(Of String, String) = Nothing,
                    Optional ByVal hide_arguments As Boolean = False _
                    )
            If extra_help Is Nothing Then extra_help = New Dictionary(Of String, String)
            If required_permissions Is Nothing Then required_permissions = New Dictionary(Of String, UInteger)

            Me.name = name
            Me.help = help
            If required_permissions.Count > 0 Then
                Me.help += " {"
                For Each permission As String In required_permissions.Keys
                    Me.help += " " + permission + "=" + required_permissions(permission).ToString()
                Next permission
                Me.help += " }"
            End If
            Me.extra_help = extra_help
            Me.argument_limit_value = argument_limit_value
            Me.argument_limit_type = argument_limit_type
            Me.hide_arguments = hide_arguments
            Me.required_permissions = required_permissions
        End Sub

        Private Function get_name() As String Implements ICommand(Of T).name
            Return name
        End Function
        Private Function get_help() As String Implements ICommand(Of T).help
            Return help
        End Function
        Private Function get_extra_help() As Dictionary(Of String, String) Implements ICommand(Of T).extra_help
            Return extra_help
        End Function
        Private Function get_hide_arguments() As Boolean Implements ICommand(Of T).hide_arguments
            Return hide_arguments
        End Function
        Private Function get_argument_limit_type() As ArgumentLimits Implements ICommand(Of T).argument_limit_type
            Return argument_limit_type
        End Function
        Private Function get_argument_limit_value() As Integer Implements ICommand(Of T).argument_limit_value
            Return argument_limit_value
        End Function
        Public Function user_allowed(ByVal user As BotUser) As Boolean Implements ICommand(Of T).user_allowed
            If user Is Nothing Then Return True
            For Each key As String In required_permissions.Keys
                If user.permission(key) < required_permissions(key) Then
                    Return False
                End If
            Next key
            Return True
        End Function

        '''<summary>Checks user permissions, number of arguments, and delegates processing to child implementation.</summary>
        Public Function processText(ByVal target As T, ByVal user As BotUser, ByVal argument As String) As IFuture(Of Outcome) Implements ICommand(Of T).process
            'Check permissions
            If Not user_allowed(user) Then
                Return futurize(failure("You do not have sufficient permissions to use that command."))
            End If

            'Check arguments
            Dim arguments As IList(Of String) = breakQuotedWords(argument)
            Select Case argument_limit_type
                Case ArgumentLimits.exact
                    If arguments.Count > argument_limit_value Then
                        Return futurize(failure("Too many arguments, expected at most {0}. Use quotes (""like this"") to surround individual arguments containing spaces.".frmt(argument_limit_value)))
                    ElseIf arguments.Count < argument_limit_value Then
                        Return futurize(failure("Not enough arguments, expected at least {0}.".frmt(argument_limit_value)))
                    End If
                Case ArgumentLimits.max
                    If arguments.Count > argument_limit_value Then
                        Return futurize(failure("Too many arguments, expected at most {0}. Use quotes (""like this"") to surround individual arguments containing spaces.".frmt(argument_limit_value)))
                    End If
                Case ArgumentLimits.min
                    If arguments.Count < argument_limit_value Then
                        Return futurize(failure("Not enough arguments, expected at least {0}.".frmt(argument_limit_value)))
                    End If
            End Select

            'Run
            Try
                Return Process(target, user, arguments)
            Catch e As Exception
                Logging.LogUnexpectedException("Processing text for command.", e)
                Return futurize(failure("Unexpected exception encountered ({0}).".frmt(e.Message)))
            End Try
        End Function

        Public MustOverride Function Process(ByVal target As T, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
    End Class
#End Region

#Region "Derived Classes"
    '''<summary>Implements a command for access to a list of commands and help with specific commands.</summary>
    Public Class CommandHelp(Of T)
        Inherits BaseCommand(Of T)
        Private help_map As New Dictionary(Of String, String)
        Private commands As New List(Of ICommand(Of T))

        Public Sub New()
            MyBase.New(My.Resources.Command_General_Help,
                       2, ArgumentLimits.max,
                       My.Resources.Command_General_Help_Help)
        End Sub

        '''<summary>Adds a command to the list of commands and specific help.</summary>
        Public Sub add_command(ByVal command As ICommand(Of T))
            help_map(command.name.ToLower()) = command.help
            commands.Add(command)
            Dim all_extra = ""
            Dim all_extra_vals As New List(Of String)
            For Each pair In command.extra_help
                If pair.Value = "[*]" Then
                    all_extra_vals.Add(pair.Key)
                Else
                    help_map(command.name.ToLower + " " + pair.Key.ToLower) = pair.Value
                    all_extra += pair.Key + ", "
                End If
            Next pair
            For Each s In all_extra_vals
                help_map(command.name.ToLower + " " + s.ToLower) = all_extra
            Next s
        End Sub

        Public Sub remove_command(ByVal command As ICommand(Of T))
            If Not commands.Contains(command) Then Return
            commands.Remove(command)
            help_map.Remove(command.name.ToLower())
            For Each key In command.extra_help.Keys
                help_map.Remove(command.name.ToLower + " " + key.ToLower)
            Next key
        End Sub

        Public Overrides Function Process(ByVal target As T, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
            Dim arg As String = Nothing
            If arguments.Count > 0 Then arg = arguments(0)
            If arguments.Count = 2 Then arg += " " + arguments(1)
            If arg Is Nothing Then
                '[list of commands]
                Dim command_list As String = ""
                For Each c In commands
                    If c.user_allowed(user) Then
                        If command_list <> "" Then command_list += " "
                        command_list += c.name
                    End If
                Next c
                Return futurize(success(command_list))
            ElseIf help_map.ContainsKey(arg.ToLower()) Then
                '[specific command help]
                Return futurize(success(help_map(arg.ToLower())))
            Else
                '[no matching command]
                Return futurize(failure("{0} is not a recognized help topic.".frmt(arg)))
            End If
        End Function
    End Class

    '''<summary>Implements a command which uses the first argument to match a subcommand, then runs that subcommand with the remaining arguments.</summary>
    Public Class CommandSet(Of T)
        Inherits BaseCommand(Of T)
        Public ReadOnly subcommand_map As New Dictionary(Of String, ICommand(Of T))
        Private help_command As New CommandHelp(Of T)

        Public Sub New(Optional ByVal name As String = "", Optional ByVal help As String = "")
            MyBase.New(name, 1, ArgumentLimits.min, help)
            add_subcommand(help_command)
        End Sub

        '''<summary>Returns a list of the names of all subcommands.</summary>
        Public Function getSubCommandList() As List(Of String)
            Dim L As New List(Of String)
            For Each key As String In subcommand_map.Keys
                L.Add(key)
            Next key
            Return L
        End Function

        '''<summary>Adds a potential subcommand to forward calls to. Automatically includes the new subcommand in the help subcommand.</summary>
        Public Sub add_subcommand(ByVal subcommand As ICommand(Of T))
            If Not (subcommand IsNot Nothing) Then Throw New ArgumentException()
            If subcommand_map.ContainsKey(subcommand.name.ToLower()) Then Throw New InvalidOperationException("Command already registered to " + subcommand.name)
            subcommand_map(subcommand.name.ToLower()) = subcommand
            help_command.add_command(subcommand)
        End Sub

        Public Sub remove_subcommand(ByVal subcommand As ICommand(Of T))
            If subcommand Is Nothing Then Return
            If Not subcommand_map.ContainsKey(subcommand.name.ToLower()) Then Return
            subcommand_map.Remove(subcommand.name.ToLower())
            help_command.remove_command(subcommand)
        End Sub

        Public Overrides Function Process(ByVal target As T, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
            'Get subcommand
            Dim name As String = arguments(0).ToLower()
            If Not subcommand_map.ContainsKey(name) Then
                Return futurize(failure("Unrecognized Command: " + name))
            End If
            Dim subcommand As ICommand(Of T) = subcommand_map(name)

            'Run subcommand
            arguments.RemoveAt(0)
            Return subcommand.process(target, user, mendQuotedWords(arguments))
        End Function
    End Class

    Public Class ThreadedCommandSet(Of T)
        Inherits CommandSet(Of T)

        Private Function process_helper(ByVal f As Future(Of Outcome), ByVal target As T, ByVal user As BotUser, ByVal arguments As IList(Of String)) As Boolean
            Try
                FutureSub.frun(MyBase.Process(target, user, arguments), AddressOf f.setValue)
                Return True
            Catch e As Exception
                f.setValue(failure("Error processing command: " + e.Message))
                Return False
            End Try
        End Function
        Public Overrides Function Process(ByVal target As T, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
            Dim f As New Future(Of Outcome)
            ThreadedAction(Function() process_helper(f, target, user, arguments), Me.GetType.Name)
            Return f
        End Function
    End Class

    Public Class UICommandSet(Of T)
        Inherits ThreadedCommandSet(Of T)

        Public Overridable Sub processLocalText(ByVal target As T, ByVal text As String, ByVal logger As Logger)
            Try
                Dim name = breakQuotedWords(text)(0).ToLower()
                If Not subcommand_map.ContainsKey(name) Then
                    logger.log("Unrecognized Command: " + name, LogMessageTypes.Problem)
                    Return
                End If
                Dim hide_args = subcommand_map(name).hide_arguments
                If hide_args Then
                    logger.log("Command [arguments hidden]: " + name, LogMessageTypes.Typical)
                Else
                    logger.log("Command: " + text, LogMessageTypes.Typical)
                End If

                logger.futurelog("[running command '{0}'...]".frmt(name),
                                 FutureFunc.frun(processText(target, Nothing, text),
                                                             AddressOf output_of_command))
            Catch e As Exception
                Logging.logUnexpectedException("Exception rose past " + Me.GetType.Name + "[" + Me.name + "].processLocalText", e)
            End Try
        End Sub

        Private Function output_of_command(ByVal out As Outcome) As Outcome
            Dim message = out.message
            If message Is Nothing Or message = "" Then
                message = "Command {0}.".frmt(If(out.succeeded, "Succeeded", "Failed"))
            End If
            Return New Outcome(out.succeeded, "({0}) {1}".frmt(If(out.succeeded, "Succeeded", "Failed"), message))
        End Function
    End Class
#End Region
End Namespace
