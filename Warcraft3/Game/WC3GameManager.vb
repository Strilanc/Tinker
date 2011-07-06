Imports Tinker.Commands
Imports Tinker.Components

Namespace WC3
    Public Class GameManager
        Inherits DisposableWithTask
        Implements IBotComponent

        Private ReadOnly _bot As Bot.MainBot
        Private ReadOnly _name As InvariantString
        Private ReadOnly _game As WC3.Game
        Private ReadOnly _control As Control
        Private ReadOnly _hooks As New List(Of Task(Of IDisposable))

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_bot IsNot Nothing)
            Contract.Invariant(_game IsNot Nothing)
            Contract.Invariant(_hooks IsNot Nothing)
            Contract.Invariant(_control IsNot Nothing)
        End Sub

        Public Sub New(name As InvariantString,
                       bot As Bot.MainBot,
                       game As WC3.Game)
            Contract.Requires(bot IsNot Nothing)
            Contract.Requires(game IsNot Nothing)

            game.HackManager = Me
            Me._bot = bot
            Me._name = name
            Me._game = game
            Me._control = New W3GameControl(Me)

            AddHandler game.PlayerTalked, Sub(sender, player, text, receivers) HandleText(player, text)

            game.ChainEventualDisposalTo(Me)
        End Sub
        Private Async Sub HandleText(player As WC3.Player, text As String)
            Contract.Assume(player IsNot Nothing)
            Contract.Assume(text IsNot Nothing)

            Dim commandPrefix = My.Settings.commandPrefix.AssumeNotNull
            If text = Tinker.Bot.MainBot.TriggerCommandText Then '?Trigger command
                _game.QueueSendMessageTo("Command prefix: {0}".Frmt(My.Settings.commandPrefix), player)
                Return
            ElseIf Not text.StartsWith(commandPrefix, StringComparison.OrdinalIgnoreCase) Then 'not a command
                Return
            End If

            'Start command
            Dim commandText = text.Substring(commandPrefix.Length)
            Dim commandResult = _game.QueueCommandProcessText(Me, player, commandText)
            Call Async Sub()
                     Await _game.Clock.AsyncWait(2.Seconds)
                     If commandResult.Status <> TaskStatus.RanToCompletion AndAlso commandResult.Status <> TaskStatus.Faulted Then
                         _game.QueueSendMessageTo("Command '{0}' is running... You will be informed when it finishes.".Frmt(text), player)
                     End If
                 End Sub

            'Finish command
            Try
                Dim message = Await commandResult
                _game.QueueSendMessageTo(If(message, "Command Succeeded"), player)
            Catch ex As Exception
                _game.QueueSendMessageTo("Failed: {0}".Frmt(ex.Summarize), player)
            End Try
        End Sub

        Public ReadOnly Property Game As WC3.Game
            Get
                Contract.Ensures(Contract.Result(Of WC3.Game)() IsNot Nothing)
                Return _game
            End Get
        End Property
        Public ReadOnly Property Bot As Bot.MainBot
            Get
                Contract.Ensures(Contract.Result(Of Bot.MainBot)() IsNot Nothing)
                Return _bot
            End Get
        End Property
        Public ReadOnly Property Name As InvariantString Implements IBotComponent.Name
            Get
                Return _name
            End Get
        End Property
        Public ReadOnly Property Type As InvariantString Implements IBotComponent.Type
            Get
                Return "Game"
            End Get
        End Property
        Public ReadOnly Property Logger As Logger Implements IBotComponent.Logger
            Get
                Return _game.Logger
            End Get
        End Property
        Public ReadOnly Property HasControl As Boolean Implements IBotComponent.HasControl
            Get
                Contract.Ensures(Contract.Result(Of Boolean)())
                Return True
            End Get
        End Property
        Public Function IsArgumentPrivate(argument As String) As Boolean Implements IBotComponent.IsArgumentPrivate
            Return False
        End Function
        Public ReadOnly Property Control As Control Implements IBotComponent.Control
            Get
                Return _control
            End Get
        End Property

        Public Function InvokeCommand(user As BotUser, argument As String) As Task(Of String) Implements IBotComponent.InvokeCommand
            Return Game.QueueCommandProcessText(Me, Nothing, argument)
        End Function

        Protected Overrides Function PerformDispose(finalizing As Boolean) As Task
            _game.Dispose()
            _control.DisposeControlAsync()
            Return _hooks.DisposeAllAsync()
        End Function

        Private Function IncludeCommandImpl(command As ICommand(Of IBotComponent)) As Task(Of IDisposable) Implements IBotComponent.IncludeCommand
            Throw New NotImplementedException("Game commands are admin/lobby/loading/etc specific.")
        End Function
    End Class
End Namespace
