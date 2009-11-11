Namespace Commands
    ''' <summary>
    ''' A command which passes arguments to subcommands specified by arguments' first words.
    ''' </summary>
    Public Class CommandSet(Of T)
        Inherits PartialCommand(Of T)
        Private ReadOnly _commandMap As New Dictionary(Of String, Command(Of T))
        Private ReadOnly _help As New HelpCommand(Of T)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_commandMap IsNot Nothing)
            Contract.Invariant(_help IsNot Nothing)
        End Sub

        Public Sub New(Optional ByVal name As String = Nothing)
            MyBase.New(name:=If(name, "CommandSet"),
                       headType:="SubCommand",
                       Description:="Picks a sub-command using the first word in the argument and inokves it with the remaining argument.")
            AddCommand(Me._help)
        End Sub

        Public ReadOnly Property CommandMap As Dictionary(Of String, Command(Of T))
            Get
                Contract.Ensures(Contract.Result(Of Dictionary(Of String, Command(Of T)))() IsNot Nothing)
                Return _commandMap
            End Get
        End Property
        Public ReadOnly Property CommandList() As List(Of String)
            Get
                Return CommandMap.Keys.ToList
            End Get
        End Property

        Public Sub AddCommand(ByVal command As Command(Of T))
            Contract.Requires(command IsNot Nothing)
            If _commandMap.ContainsKey(command.Name.ToUpperInvariant) Then
                Throw New InvalidOperationException("Command already registered to {0}.".Frmt(command.Name))
            End If
            _commandMap(command.Name.ToUpperInvariant) = command
            _help.AddCommand(command)
        End Sub

        Public Sub RemoveCommand(ByVal command As Command(Of T))
            Contract.Requires(command IsNot Nothing)
            If Not _commandMap.ContainsKey(command.Name.ToUpperInvariant) Then Return
            If Not _commandMap(command.Name.ToUpperInvariant) Is command Then Return
            _commandMap.Remove(command.Name.ToUpperInvariant)
            _help.RemoveCommand(command)
        End Sub

        Protected NotOverridable Overrides Function PerformInvoke(ByVal target As T, ByVal user As BotUser, ByVal argumentHead As String, ByVal argumentRest As String) As Strilbrary.Threading.IFuture(Of String)
            Return TaskedFunc(
                Function()
                    If Not _commandMap.ContainsKey(argumentHead.ToUpperInvariant) Then
                        Throw New ArgumentException("Unrecognized Command: {0}.".Frmt(argumentHead))
                    End If
                    Return _commandMap(argumentHead.ToUpperInvariant).Invoke(target, user, argumentRest)
                End Function
            ).Defuturized
        End Function
    End Class
End Namespace
