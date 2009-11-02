Namespace Commands
    '''<summary>Specifies how an argument limit is checked.</summary>
    Public Enum ArgumentLimitType
        Free
        Exact
        Max
        Min
    End Enum

    '''<summary>Represents a simple command-line command.</summary>
    '''<typeparam name="T">The type of the target argument included when running the command.</typeparam>
    <ContractClass(GetType(ICommand(Of ).ContractClass))>
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

        <ContractClassFor(GetType(ICommand(Of )))>
        Class ContractClass
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
    End Interface

    Public Delegate Function CommandProcessFunc(Of T)(ByVal target As T, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)

    Public Module CommandExtensions
        Private NotInheritable Class DelegatedCommand(Of T)
            Inherits BaseCommand(Of T)
            Private ReadOnly processFunction As CommandProcessFunc(Of T)

            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(processFunction IsNot Nothing)
            End Sub

            Public Sub New(ByVal name As String,
                           ByVal help As String,
                           ByVal processFunction As CommandProcessFunc(Of T),
                           Optional ByVal requiredPermissions As String = Nothing,
                           Optional ByVal argumentLimit As Integer = 0,
                           Optional ByVal argumentLimitType As ArgumentLimitType = ArgumentLimitType.Min,
                           Optional ByVal extraHelp As String = Nothing,
                           Optional ByVal shouldHideArguments As Boolean = False)
                MyBase.New(name,
                           argumentLimit,
                           argumentLimitType,
                           help,
                           If(requiredPermissions, ""),
                           If(extraHelp, ""),
                           shouldHideArguments)
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
            Contract.Requires(commandSet IsNot Nothing)
            commandSet.AddCommand(New DelegatedCommand(Of T)(name,
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
                If Not this.CommandMap.ContainsKey(name) Then
                    logger.Log("Unrecognized Command: " + name, LogMessageType.Problem)
                    Return
                End If

                If this.CommandMap(name).ShouldHideArguments Then
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
End Namespace
