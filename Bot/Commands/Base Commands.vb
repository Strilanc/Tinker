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
    <ContractClass(GetType(ContractClassICommand(Of )))>
    Public Interface ICommand(Of T)
        '''<summary>The name of the command.</summary>
        ReadOnly Property name() As String
        '''<summary>Help for using the command.</summary>
        ReadOnly Property Help() As String
        '''<summary>Determines if the command's arguments are appropriate for logging.</summary>
        ReadOnly Property ShouldHideArguments() As Boolean
        '''<summary>Used for pre-checks on number of arguments. Applied based on argument limit type.</summary>
        ReadOnly Property ArgumentLimit() As Integer
        '''<summary>Determines how the argument limit is applied.</summary>
        ReadOnly Property ArgumentLimitType() As ArgumentLimits
        '''<summary>Runs the command.</summary>
        '''<param name="target">The object the command is being run from.</param>
        '''<param name="user">The user running the command. Local user is nothing.</param>
        '''<param name="arguments">The arguments passed to the command</param>
        Function Process(ByVal target As T, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
        '''<summary>Determines if a user is allowed to use the command.</summary>
        Function IsUserAllowed(ByVal user As BotUser) As Boolean
        ReadOnly Property ExtraHelp() As Dictionary(Of String, String)
    End Interface
    <ContractClassFor(GetType(ICommand(Of )))>
    Public Class ContractClassICommand(Of T)
        Implements ICommand(Of T)

        Public Function Process(ByVal target As T, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String) Implements ICommand(Of T).Process
            Contract.Requires(target IsNot Nothing)
            Contract.Requires(arguments IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of String))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function IsUserAllowed(ByVal user As BotUser) As Boolean Implements ICommand(Of T).IsUserAllowed
            Throw New NotSupportedException
        End Function

        Public ReadOnly Property ArgumentLimitType As ArgumentLimits Implements ICommand(Of T).ArgumentLimitType
            Get
                Throw New NotSupportedException
            End Get
        End Property

        Public ReadOnly Property ArgumentLimit As Integer Implements ICommand(Of T).ArgumentLimit
            Get
                Throw New NotSupportedException
            End Get
        End Property

        Public ReadOnly Property ExtraHelp As Dictionary(Of String, String) Implements ICommand(Of T).ExtraHelp
            Get
                Contract.Ensures(Contract.Result(Of Dictionary(Of String, String))() IsNot Nothing)
                Throw New NotSupportedException
            End Get
        End Property

        Public ReadOnly Property Help As String Implements ICommand(Of T).Help
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Throw New NotSupportedException
            End Get
        End Property

        Public ReadOnly Property ShouldHideArguments As Boolean Implements ICommand(Of T).ShouldHideArguments
            Get
                Throw New NotSupportedException
            End Get
        End Property

        Public ReadOnly Property Name As String Implements ICommand(Of T).name
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Throw New NotSupportedException
            End Get
        End Property
    End Class
#End Region

#Region "Base Classes"
    '''<summary>A simple base implementation of the itfCommand(Of T) interface.</summary>
    Public MustInherit Class BaseCommand(Of T)
        Implements ICommand(Of T)
        Public ReadOnly name As String
        Public ReadOnly help As String
        Public ReadOnly argumentLimit As Integer
        Public ReadOnly argumentLimitType As ArgumentLimits
        Public ReadOnly requiredPermissions As Dictionary(Of String, UInteger)
        Public ReadOnly extraHelp As Dictionary(Of String, String)
        Public ReadOnly shouldHideArguments As Boolean = False

        Public Sub New(ByVal name As String,
                       ByVal argumentLimit As Integer,
                       ByVal argumentLimitType As ArgumentLimits,
                       ByVal help As String,
                       ByVal requiredPermissions As String,
                       ByVal extraHelp As String,
                       Optional ByVal shouldHideArguments As Boolean = False)
            Me.New(name,
                   argumentLimit,
                   argumentLimitType,
                   help,
                   DictStrT(requiredPermissions, Function(x) UInteger.Parse(x)),
                   DictStrT(extraHelp, Function(x) x, vbNewLine),
                   shouldHideArguments)
        End Sub
        Public Sub New(ByVal name As String,
                       ByVal argumentLimit As Integer,
                       ByVal argumentLimitType As ArgumentLimits,
                       ByVal help As String,
                       Optional ByVal requiredPermissions As Dictionary(Of String, UInteger) = Nothing,
                       Optional ByVal extraHelp As Dictionary(Of String, String) = Nothing,
                       Optional ByVal shouldHideArguments As Boolean = False)
            Me.name = name
            Me.help = help
            Me.extraHelp = If(extraHelp, New Dictionary(Of String, String))
            Me.argumentLimit = argumentLimit
            Me.argumentLimitType = argumentLimitType
            Me.shouldHideArguments = shouldHideArguments
            Me.requiredPermissions = If(requiredPermissions, New Dictionary(Of String, UInteger))
            If Me.requiredPermissions.Count > 0 Then
                Me.help += " {"
                For Each permission As String In Me.requiredPermissions.Keys
                    Me.help += " {0}={1}".frmt(permission, Me.requiredPermissions(permission))
                Next permission
                Me.help += " }"
            End If
        End Sub

#Region "Private Interface"
        Protected ReadOnly Property _Name() As String Implements ICommand(Of T).name
            Get
                Return name
            End Get
        End Property
        Protected ReadOnly Property _Help() As String Implements ICommand(Of T).Help
            Get
                Return help
            End Get
        End Property
        Protected ReadOnly Property _ExtraHelp() As Dictionary(Of String, String) Implements ICommand(Of T).ExtraHelp
            Get
                Return extraHelp
            End Get
        End Property
        Protected ReadOnly Property _ShouldHideArguments() As Boolean Implements ICommand(Of T).ShouldHideArguments
            Get
                Return shouldHideArguments
            End Get
        End Property
        Protected ReadOnly Property _ArgumentLimit() As Integer Implements ICommand(Of T).ArgumentLimit
            Get
                Return argumentLimit
            End Get
        End Property
        Protected ReadOnly Property _ArgumentLimitType() As ArgumentLimits Implements ICommand(Of T).ArgumentLimitType
            Get
                Return argumentLimitType
            End Get
        End Property
#End Region

        Public Function IsUserAllowed(ByVal user As BotUser) As Boolean Implements ICommand(Of T).IsUserAllowed
            If user Is Nothing Then Return True
            For Each key As String In requiredPermissions.Keys
                If user.permission(key) < requiredPermissions(key) Then
                    Return False
                End If
            Next key
            Return True
        End Function

        '''<summary>Checks user permissions, number of arguments, and delegates processing to child implementation.</summary>
        Public Function ProcessCommand(ByVal target As T, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String) Implements ICommand(Of T).Process
            Try
                'Check permissions
                If Not IsUserAllowed(user) Then
                    Throw New InvalidOperationException("You do not have sufficient permissions to use that command.")
                End If

                'Check arguments
                Select Case argumentLimitType
                    Case ArgumentLimits.exact
                        If arguments.Count > argumentLimit Then
                            Throw New IO.IOException("Too many arguments, expected at most {0}. Use quotes (""like this"") to surround individual arguments containing spaces.".Frmt(argumentLimit))
                        ElseIf arguments.Count < argumentLimit Then
                            Throw New IO.IOException("Not enough arguments, expected at least {0}.".Frmt(argumentLimit))
                        End If
                    Case ArgumentLimits.max
                        If arguments.Count > argumentLimit Then
                            Throw New IO.IOException("Too many arguments, expected at most {0}. Use quotes (""like this"") to surround individual arguments containing spaces.".Frmt(argumentLimit))
                        End If
                    Case ArgumentLimits.min
                        If arguments.Count < argumentLimit Then
                            Throw New IO.IOException("Not enough arguments, expected at least {0}.".Frmt(argumentLimit))
                        End If
                End Select

                'Run
                Return Process(target, user, arguments)
            Catch e As Exception
                LogUnexpectedException("Processing text for command.", e)
                Dim result = New FutureFunction(Of String)
                result.SetFailed(e)
                Return result
            End Try
        End Function

        Public Overridable Function Process(ByVal target As T, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
            Contract.Requires(target IsNot Nothing)
            Contract.Requires(arguments IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of String))() IsNot Nothing)
            Throw New NotSupportedException()
        End Function
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
        Public Sub AddCommand(ByVal command As ICommand(Of T))
            help_map(command.name.ToLower()) = command.Help
            commands.Add(command)
            Dim all_extra = ""
            Dim all_extra_vals As New List(Of String)
            For Each pair In command.ExtraHelp
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

        Public Sub RemoveCommand(ByVal command As ICommand(Of T))
            If Not commands.Contains(command) Then Return
            commands.Remove(command)
            help_map.Remove(command.name.ToLower())
            For Each key In command.ExtraHelp.Keys
                help_map.Remove(command.name.ToLower + " " + key.ToLower)
            Next key
        End Sub

        Public Overrides Function Process(ByVal target As T, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
            Dim arg As String = Nothing
            If arguments.Count > 0 Then arg = arguments(0)
            If arguments.Count = 2 Then arg += " " + arguments(1)
            If arg Is Nothing Then
                '[list of commands]
                Dim command_list As String = ""
                For Each c In commands
                    If c.IsUserAllowed(user) Then
                        If command_list <> "" Then command_list += " "
                        command_list += c.name
                    End If
                Next c
                Return command_list.Futurized
            ElseIf help_map.ContainsKey(arg.ToLower()) Then
                '[specific command help]
                Return help_map(arg.ToLower()).Futurized
            Else
                '[no matching command]
                Throw New ArgumentException("{0} is not a recognized help topic.".Frmt(arg))
            End If
        End Function
    End Class

    '''<summary>Implements a command which uses the first argument to match a subcommand, then runs that subcommand with the remaining arguments.</summary>
    Public Class CommandSet(Of T)
        Inherits BaseCommand(Of T)
        Public ReadOnly commandMap As New Dictionary(Of String, ICommand(Of T))
        Private helpCommand As New CommandHelp(Of T)

        Public Sub New(Optional ByVal name As String = "", Optional ByVal help As String = "")
            MyBase.New(name, 1, ArgumentLimits.min, help)
            AddCommand(helpCommand)
        End Sub

        '''<summary>Returns a list of the names of all subcommands.</summary>
        Public Function GetCommandList() As List(Of String)
            Return commandMap.Keys.ToList
        End Function

        '''<summary>Adds a potential subcommand to forward calls to. Automatically includes the new subcommand in the help subcommand.</summary>
        Public Sub AddCommand(ByVal command As ICommand(Of T))
            Contract.Requires(command IsNot Nothing)
            If commandMap.ContainsKey(command.name.ToLower()) Then Throw New InvalidOperationException("Command already registered to " + command.name)
            commandMap(command.name.ToLower()) = command
            helpCommand.AddCommand(command)
        End Sub

        Public Sub RemoveCommand(ByVal command As ICommand(Of T))
            Contract.Requires(command IsNot Nothing)
            If Not commandMap.ContainsKey(command.name.ToLower()) Then Return
            commandMap.Remove(command.name.ToLower())
            helpCommand.RemoveCommand(command)
        End Sub

        Public Overrides Function Process(ByVal target As T, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
            'Get subcommand
            Dim name As String = arguments(0).ToLower()
            If Not commandMap.ContainsKey(name) Then
                Throw New ArgumentException("Unrecognized Command: " + name)
            End If
            Dim subcommand As ICommand(Of T) = commandMap(name)

            'Run subcommand
            arguments.RemoveAt(0)
            Return subcommand.Process(target, user, arguments)
        End Function
    End Class

    Public Class ThreadedCommandSet(Of T)
        Inherits CommandSet(Of T)

        Public Overrides Function Process(ByVal target As T, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
            Return ThreadPooledFunc(Function() MyBase.Process(target, user, arguments)).Defuturized
        End Function
    End Class

    Public Class UICommandSet(Of T)
        Inherits ThreadedCommandSet(Of T)

        Public Overridable Sub ProcessLocalText(ByVal target As T, ByVal text As String, ByVal logger As Logger)
            Contract.Requires(target IsNot Nothing)
            Contract.Requires(text IsNot Nothing)
            Contract.Requires(logger IsNot Nothing)
            Try
                Dim words = breakQuotedWords(text)
                Contract.Assume(words.Count > 0)
                Dim name = words(0).ToLower()
                If Not commandMap.ContainsKey(name) Then
                    logger.log("Unrecognized Command: " + name, LogMessageType.Problem)
                    Return
                End If
                Dim hide_args = commandMap(name).ShouldHideArguments
                If hide_args Then
                    logger.log("Command [arguments hidden]: {0}".frmt(name), LogMessageType.Typical)
                Else
                    logger.log("Command: {0}".frmt(text), LogMessageType.Typical)
                End If

                logger.FutureLog("[running command '{0}'...]".Frmt(name), ProcessCommand(target, Nothing, words).EvalWhenValueReady(
                    Function(message, commandException)
                        If commandException IsNot Nothing Then
                            Return "Failed: {0}".Frmt(commandException.Message)
                        ElseIf message Is Nothing OrElse message = "" Then
                            Return "Command succeeded."
                        Else
                            Return message
                        End If
                    End Function))
            Catch e As Exception
                LogUnexpectedException("Exception rose past " + Me.GetType.Name + "[" + Me.name + "].processLocalText", e)
            End Try
        End Sub
    End Class
#End Region
End Namespace
