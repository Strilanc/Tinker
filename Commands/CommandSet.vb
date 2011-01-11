Namespace Commands
    ''' <summary>
    ''' A command which passes arguments to subcommands specified by arguments' first words.
    ''' </summary>
    Public Class CommandSet(Of T)
        Inherits PartialCommand(Of T)
        Private ReadOnly _commandMap As New Dictionary(Of InvariantString, ICommand(Of T))
        Private ReadOnly _help As New HelpCommand(Of T)
        Private ReadOnly lock As New Object()

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_commandMap IsNot Nothing)
            Contract.Invariant(_help IsNot Nothing)
        End Sub

        Public Sub New(Optional ByVal name As InvariantString? = Nothing)
            MyBase.New(name:=If(name Is Nothing, New InvariantString("CommandSet"), name.Value),
                       headType:="SubCommand",
                       Description:="Picks a sub-command using the first word in the argument and invokes it with the remaining argument.")
            IncludeCommand(Me._help)
        End Sub

        Public Overrides Function IsArgumentPrivate(ByVal argument As String) As Boolean
            Dim i = argument.IndexOf(" "c)
            If i = -1 Then i = argument.Length
            Dim head = argument.Substring(0, i)
            Dim rest = argument.Substring(Math.Min(i + 1, argument.Length))
            Dim command As ICommand(Of T) = Nothing
            SyncLock lock
                If Not _commandMap.TryGetValue(head, command) Then Return False
            End SyncLock
            Contract.Assume(command IsNot Nothing)
            Return command.IsArgumentPrivate(rest)
        End Function

        Public ReadOnly Property CommandMap As Dictionary(Of InvariantString, ICommand(Of T))
            Get
                Contract.Ensures(Contract.Result(Of Dictionary(Of InvariantString, ICommand(Of T)))() IsNot Nothing)
                Return _commandMap
            End Get
        End Property

        Public Function IncludeCommand(ByVal command As ICommand(Of T)) As IDisposable
            Contract.Requires(command IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            SyncLock lock
                If _commandMap.ContainsKey(command.Name) Then
                    Throw New InvalidOperationException("Command already registered to {0}.".Frmt(command.Name))
                End If
                _commandMap(command.Name) = command
                _help.AddCommand(command)
            End SyncLock
            Return New DelegatedDisposable(Sub() RemoveCommand(command))
        End Function

        Public Sub RemoveCommand(ByVal command As ICommand(Of T))
            Contract.Requires(command IsNot Nothing)
            SyncLock lock
                If Not _commandMap.ContainsKey(command.Name) Then Return
                If Not _commandMap(command.Name) Is command Then Return
                _commandMap.Remove(command.Name)
                _help.RemoveCommand(command)
            End SyncLock
        End Sub

        Protected NotOverridable Overrides Function PerformInvoke(ByVal target As T, ByVal user As BotUser, ByVal argumentHead As String, ByVal argumentRest As String) As Task(Of String)
            Dim command As ICommand(Of T) = Nothing
            SyncLock lock
                If Not _commandMap.TryGetValue(argumentHead, command) Then
                    Throw New ArgumentException("Unrecognized Command: {0}.".Frmt(argumentHead))
                End If
                Contract.Assume(command IsNot Nothing)
            End SyncLock
            Return command.Invoke(target, user, argumentRest)
        End Function
    End Class
End Namespace
