Namespace Commands
    '''<summary>Implements a command which uses the first argument to match a subcommand, then runs that subcommand with the remaining arguments.</summary>
    Public Class CommandSet(Of T)
        Inherits BaseCommand(Of T)
        Private ReadOnly _commandMap As New Dictionary(Of String, ICommand(Of T))
        Private ReadOnly helpCommand As New HelpCommand(Of T)
        Public ReadOnly Property CommandMap As Dictionary(Of String, ICommand(Of T))
            Get
                Contract.Ensures(Contract.Result(Of Dictionary(Of String, ICommand(Of T)))() IsNot Nothing)
                Return _commandMap
            End Get
        End Property

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_commandMap IsNot Nothing)
            Contract.Invariant(helpCommand IsNot Nothing)
        End Sub

        Public Sub New(Optional ByVal name As String = Nothing,
                       Optional ByVal help As String = Nothing)
            MyBase.New(If(name, ""), 1, ArgumentLimitType.Min, If(help, ""))
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
End Namespace
