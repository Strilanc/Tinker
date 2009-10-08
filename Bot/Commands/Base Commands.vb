Namespace Commands
#Region "Interfaces"
    '''<summary>Specifies how an argument limit is checked.</summary>
    Public Enum ArgumentLimitType
        Free
        Exact
        Max
        Min
    End Enum

    '''<summary>Represents a simple command-line command.</summary>
    '''<typeparam name="T">The type of the target argument included when running the command.</typeparam>
    <ContractClass(GetType(ContractClassForICommand(Of )))>
    Public Interface ICommand(Of T)
        '''<summary>The name of the command.</summary>
        ReadOnly Property Name() As String
        '''<summary>Help for using the command.</summary>
        ReadOnly Property Help() As String
        '''<summary>Determines if the command's arguments are appropriate for logging.</summary>
        ReadOnly Property ShouldHideArguments() As Boolean
        '''<summary>Used for pre-checks on number of arguments. Applied based on argument limit type.</summary>
        ReadOnly Property ArgumentLimit() As Integer
        '''<summary>Determines how the argument limit is applied.</summary>
        ReadOnly Property ArgumentLimitType() As ArgumentLimitType
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
    Public Class ContractClassForICommand(Of T)
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

        Public ReadOnly Property ArgumentLimitType As ArgumentLimitType Implements ICommand(Of T).ArgumentLimitType
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

        Public ReadOnly Property Name As String Implements ICommand(Of T).Name
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
        Private ReadOnly _name As String
        Private ReadOnly _help As String
        Private ReadOnly _argumentLimit As Integer
        Private ReadOnly _argumentLimitType As ArgumentLimitType
        Public ReadOnly requiredPermissions As Dictionary(Of String, UInteger)
        Private ReadOnly _extraHelp As Dictionary(Of String, String)
        Private ReadOnly _shouldHideArguments As Boolean = False

        Protected Sub New(ByVal name As String,
                          ByVal argumentLimit As Integer,
                          ByVal argumentLimitType As ArgumentLimitType,
                          ByVal help As String,
                          ByVal requiredPermissions As String,
                          ByVal extraHelp As String,
                          Optional ByVal shouldHideArguments As Boolean = False)
            Me.New(name,
                   argumentLimit,
                   argumentLimitType,
                   help,
                   BuildDictionaryFromString(requiredPermissions, Function(x) UInteger.Parse(x, CultureInfo.InvariantCulture)),
                   BuildDictionaryFromString(extraHelp, Function(x) x, vbNewLine),
                   shouldHideArguments)
        End Sub
        Protected Sub New(ByVal name As String,
                          ByVal argumentLimit As Integer,
                          ByVal argumentLimitType As ArgumentLimitType,
                          ByVal help As String,
                          Optional ByVal requiredPermissions As Dictionary(Of String, UInteger) = Nothing,
                          Optional ByVal extraHelp As Dictionary(Of String, String) = Nothing,
                          Optional ByVal shouldHideArguments As Boolean = False)
            Me._name = name
            Me._help = help
            Me._extraHelp = If(extraHelp, New Dictionary(Of String, String))
            Me._argumentLimit = argumentLimit
            Me._argumentLimitType = argumentLimitType
            Me._shouldHideArguments = shouldHideArguments
            Me.requiredPermissions = If(requiredPermissions, New Dictionary(Of String, UInteger))
            If Me.requiredPermissions.Count > 0 Then
                Me._help += " {"
                For Each permission As String In Me.requiredPermissions.Keys
                    Me._help += " {0}={1}".Frmt(permission, Me.requiredPermissions(permission))
                Next permission
                Me._help += " }"
            End If
        End Sub

#Region "Private Interface"
        Public ReadOnly Property Name() As String Implements ICommand(Of T).Name
            Get
                Return _name
            End Get
        End Property
        Public ReadOnly Property Help() As String Implements ICommand(Of T).Help
            Get
                Return _help
            End Get
        End Property
        Public ReadOnly Property ExtraHelp() As Dictionary(Of String, String) Implements ICommand(Of T).ExtraHelp
            Get
                Return _extraHelp
            End Get
        End Property
        Public ReadOnly Property ShouldHideArguments() As Boolean Implements ICommand(Of T).ShouldHideArguments
            Get
                Return _shouldHideArguments
            End Get
        End Property
        Public ReadOnly Property ArgumentLimit() As Integer Implements ICommand(Of T).ArgumentLimit
            Get
                Return _argumentLimit
            End Get
        End Property
        Public ReadOnly Property ArgumentLimitType() As ArgumentLimitType Implements ICommand(Of T).ArgumentLimitType
            Get
                Return _argumentLimitType
            End Get
        End Property
#End Region

        Public Function IsUserAllowed(ByVal user As BotUser) As Boolean Implements ICommand(Of T).IsUserAllowed
            If user Is Nothing Then Return True
            For Each key As String In requiredPermissions.Keys
                If user.Permission(key) < requiredPermissions(key) Then
                    Return False
                End If
            Next key
            Return True
        End Function

        '''<summary>Checks user permissions, number of arguments, and delegates processing to child implementation.</summary>
        <System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")>
        Public Function ProcessCommand(ByVal target As T, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String) Implements ICommand(Of T).Process
            Try
                'Check permissions
                If Not IsUserAllowed(user) Then
                    Throw New InvalidOperationException("You do not have sufficient permissions to use that command.")
                End If

                'Check arguments
                Select Case ArgumentLimitType
                    Case ArgumentLimitType.Exact
                        If arguments.Count > argumentLimit Then
                            Throw New IO.IOException("Too many arguments, expected at most {0}. Use quotes (""like this"") to surround individual arguments containing spaces.".Frmt(argumentLimit))
                        ElseIf arguments.Count < argumentLimit Then
                            Throw New IO.IOException("Not enough arguments, expected at least {0}.".Frmt(argumentLimit))
                        End If
                    Case ArgumentLimitType.Max
                        If arguments.Count > argumentLimit Then
                            Throw New IO.IOException("Too many arguments, expected at most {0}. Use quotes (""like this"") to surround individual arguments containing spaces.".Frmt(argumentLimit))
                        End If
                    Case ArgumentLimitType.Min
                        If arguments.Count < argumentLimit Then
                            Throw New IO.IOException("Not enough arguments, expected at least {0}.".Frmt(argumentLimit))
                        End If
                End Select

                'Run
                Return Process(target, user, arguments)
            Catch e As Exception
                e.RaiseAsUnexpected("Processing text for command.")
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
        Private helpMap As New Dictionary(Of String, String)
        Private commands As New List(Of ICommand(Of T))

        Public Sub New()
            MyBase.New(My.Resources.Command_General_Help,
                       2, ArgumentLimitType.Max,
                       My.Resources.Command_General_Help_Help)
        End Sub

        '''<summary>Adds a command to the list of commands and specific help.</summary>
        Public Sub AddCommand(ByVal command As ICommand(Of T))
            helpMap(command.Name.ToUpperInvariant) = command.Help
            commands.Add(command)
            Dim extraHelpNormalKeys = From pair In command.ExtraHelp Where pair.Value <> "[*]" Select pair.Key
            Dim extraHelpSummaryKeys = From pair In command.ExtraHelp Where pair.Value = "[*]" Select pair.Key
            For Each key In extraHelpNormalKeys
                helpMap("{0} {1}".Frmt(command.Name.ToUpperInvariant, key.ToUpperInvariant)) = command.ExtraHelp(key)
            Next key
            Dim helpKeysSummary = extraHelpNormalKeys.StringJoin(", ")
            For Each key In extraHelpSummaryKeys
                helpMap("{0} {1}".Frmt(command.Name.ToUpperInvariant, key.ToUpperInvariant)) = helpKeysSummary
            Next key
        End Sub

        Public Sub RemoveCommand(ByVal command As ICommand(Of T))
            If Not commands.Contains(command) Then Return
            commands.Remove(command)
            helpMap.Remove(command.Name.ToUpperInvariant)
            For Each key In command.ExtraHelp.Keys
                helpMap.Remove(command.Name.ToUpperInvariant + " " + key.ToUpperInvariant)
            Next key
        End Sub

        Public Overrides Function Process(ByVal target As T, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
            Dim arg As String = Nothing
            If arguments.Count > 0 Then arg = arguments(0)
            If arguments.Count = 2 Then arg += " " + arguments(1)
            If arg Is Nothing Then
                '[list of commands]
                Return (From command In commands
                        Where command.IsUserAllowed(user)
                        Select command.Name).StringJoin(" ").Futurized
            ElseIf helpMap.ContainsKey(arg.ToUpperInvariant) Then
                '[specific command help]
                Return helpMap(arg.ToUpperInvariant).Futurized
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
            MyBase.New(name, 1, ArgumentLimitType.Min, help)
            AddCommand(helpCommand)
        End Sub

        '''<summary>Returns a list of the names of all subcommands.</summary>
        Public ReadOnly Property CommandList() As List(Of String)
            Get
                Return commandMap.Keys.ToList
            End Get
        End Property

        '''<summary>Adds a potential subcommand to forward calls to. Automatically includes the new subcommand in the help subcommand.</summary>
        Public Sub AddCommand(ByVal command As ICommand(Of T))
            Contract.Requires(command IsNot Nothing)
            If commandMap.ContainsKey(command.Name.ToUpperInvariant) Then
                Throw New InvalidOperationException("Command already registered to {0}.".Frmt(command.Name))
            End If
            commandMap(command.Name.ToUpperInvariant) = command
            helpCommand.AddCommand(command)
        End Sub

        Public Sub RemoveCommand(ByVal command As ICommand(Of T))
            Contract.Requires(command IsNot Nothing)
            If Not commandMap.ContainsKey(command.Name.ToUpperInvariant) Then Return
            commandMap.Remove(command.Name.ToUpperInvariant)
            helpCommand.RemoveCommand(command)
        End Sub

        Public Overrides Function Process(ByVal target As T, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
            Return ThreadPooledFunc(
                Function()
                    'Get subcommand
                    Dim name = arguments(0).ToUpperInvariant
                    If Not commandMap.ContainsKey(name) Then
                        Throw New ArgumentException("Unrecognized Command: {0}.".Frmt(name))
                    End If
                    Dim subcommand As ICommand(Of T) = commandMap(name)

                    'Run subcommand
                    arguments.RemoveAt(0)
                    Return subcommand.Process(target, user, arguments)
                End Function).Defuturized()
        End Function
    End Class

    Public Delegate Function CommandProcessFunc(Of T)(ByVal target As T, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)

    Public Class Command(Of T)
        Inherits BaseCommand(Of T)
        Private ReadOnly processFunction As CommandProcessFunc(Of T)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(processFunction IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As String,
                       ByVal help As String,
                       ByVal processFunction As CommandProcessFunc(Of T),
                       Optional ByVal requiredPermissions As String = "",
                       Optional ByVal argumentLimit As Integer = 0,
                       Optional ByVal argumentLimitType As ArgumentLimitType = ArgumentLimitType.Min,
                       Optional ByVal extraHelp As String = Nothing,
                       Optional ByVal shouldHideArguments As Boolean = False)
            MyBase.New(name, argumentLimit, argumentLimitType, help, requiredPermissions, extraHelp, shouldHideArguments)
            Contract.Requires(name IsNot Nothing)
            Contract.Requires(help IsNot Nothing)
            Contract.Requires(processFunction IsNot Nothing)
            Me.processFunction = processFunction
        End Sub

        Public Overrides Function Process(ByVal target As T,
                                          ByVal user As BotUser,
                                          ByVal arguments As IList(Of String)) As IFuture(Of String)
            Dim result = processFunction(target, user, arguments)
            Contract.Assume(result IsNot Nothing)
            Return result
        End Function
    End Class

    Public Module CommandExtensions
        <Extension()>
        Public Sub Add(Of T)(ByVal commandSet As CommandSet(Of T),
                             ByVal name As String,
                             ByVal help As String,
                             ByVal func As CommandProcessFunc(Of T),
                             Optional ByVal requiredPermissions As String = "",
                             Optional ByVal argumentLimit As Integer = 0,
                             Optional ByVal argumentLimitType As ArgumentLimitType = ArgumentLimitType.Min,
                             Optional ByVal extraHelp As String = "",
                             Optional ByVal shouldHideArguments As Boolean = False)
            Contract.Requires(name IsNot Nothing)
            Contract.Requires(help IsNot Nothing)
            Contract.Requires(func IsNot Nothing)
            commandSet.AddCommand(New Command(Of T)(name,
                                                    help,
                                                    func,
                                                    requiredPermissions,
                                                    argumentLimit,
                                                    argumentLimitType,
                                                    extraHelp,
                                                    shouldHideArguments))
        End Sub

        <Extension()>
        Public Sub ProcessLocalText(Of T)(ByVal this As CommandSet(Of T), ByVal target As T, ByVal text As String, ByVal logger As Logger)
            Contract.Requires(this IsNot Nothing)
            Contract.Requires(target IsNot Nothing)
            Contract.Requires(text IsNot Nothing)
            Contract.Requires(logger IsNot Nothing)
            Try
                Dim words = BreakQuotedWords(text)
                Contract.Assume(words.Count > 0)
                Dim name = words(0).ToUpperInvariant
                If Not this.commandMap.ContainsKey(name) Then
                    logger.Log("Unrecognized Command: " + name, LogMessageType.Problem)
                    Return
                End If

                If this.commandMap(name).ShouldHideArguments Then
                    logger.Log("Command [arguments hidden]: {0}".Frmt(name), LogMessageType.Typical)
                Else
                    logger.Log("Command: {0}".Frmt(text), LogMessageType.Typical)
                End If

                logger.FutureLog("[running command '{0}'...]".Frmt(name), this.ProcessCommand(target, Nothing, words).EvalWhenValueReady(
                    Function(message, commandException)
                        If commandException IsNot Nothing Then
                            Return "Failed: {0}".Frmt(commandException.ToString)
                        ElseIf message Is Nothing OrElse message = "" Then
                            Return "Command succeeded."
                        Else
                            Return message
                        End If
                    End Function))
            Catch e As Exception
                e.RaiseAsUnexpected("Exception rose past {0}[{1}].ProcessLocalTest".Frmt(this.GetType.Name, this.Name))
            End Try
        End Sub
    End Module
#End Region
End Namespace
