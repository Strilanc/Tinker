Imports Tinker.Components

Namespace WC3
    Public Class GameManager
        Inherits FutureDisposable
        Implements IBotComponent

        Private ReadOnly _bot As Bot.MainBot
        Private ReadOnly _name As InvariantString
        Private ReadOnly _game As WC3.Game
        Private ReadOnly _control As Control
        Private ReadOnly _hooks As New List(Of IFuture(Of IDisposable))

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_bot IsNot Nothing)
            Contract.Invariant(_game IsNot Nothing)
            Contract.Invariant(_hooks IsNot Nothing)
            Contract.Invariant(_control IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As InvariantString,
                       ByVal bot As Bot.MainBot,
                       ByVal game As WC3.Game)
            Contract.Requires(bot IsNot Nothing)
            Contract.Requires(game IsNot Nothing)

            Me._bot = bot
            Me._name = name
            Me._game = game
            Me._control = New W3GameControl(Me)

            AddHandler game.PlayerTalked, Sub(sender, player, text) HandleText(player, text)
        End Sub
        Private Sub HandleText(ByVal player As WC3.Player, ByVal text As String)
            'Check
            If text.Substring(0, My.Settings.commandPrefix.AssumeNotNull.Length) <> My.Settings.commandPrefix _
                    AndAlso text <> Tinker.Bot.MainBot.TriggerCommandText Then
                Return 'not a command
            End If

            '?Trigger command
            If text = Tinker.Bot.MainBot.TriggerCommandText Then
                _game.QueueSendMessageTo("Command prefix: {0}".Frmt(My.Settings.commandPrefix), player)
                Return
            End If

            'Normal commands
            Dim commandText = text.Substring(My.Settings.commandPrefix.AssumeNotNull.Length)
            Dim commandResult = _game.QueueCommandProcessText(_bot, player, commandText)
            commandResult.CallOnValueSuccess(
                Sub(message) _game.QueueSendMessageTo(If(message, "Command Succeeded"), player)
            ).Catch(
                Sub(exception) _game.QueueSendMessageTo("Failed: {0}".Frmt(exception.Message), player)
            )

            'Delay notification
            Call New SystemClock().AsyncWait(2.Seconds).CallWhenReady(
                Sub()
                    If commandResult.State = FutureState.Unknown Then
                        _game.QueueSendMessageTo("Command '{0}' is running... You will be informed when it finishes.".Frmt(text), player)
                    End If
                End Sub)
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
        Public Function IsArgumentPrivate(ByVal argument As String) As Boolean Implements IBotComponent.IsArgumentPrivate
            Return False
        End Function
        Public ReadOnly Property Control As Control Implements IBotComponent.Control
            Get
                Return _control
            End Get
        End Property

        Public Function InvokeCommand(ByVal user As BotUser, ByVal argument As String) As IFuture(Of String) Implements IBotComponent.InvokeCommand
            Return Game.QueueCommandProcessText(Bot, Nothing, argument)
        End Function

        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As Strilbrary.Threading.IFuture
            For Each hook In _hooks
                Contract.Assume(hook IsNot Nothing)
                hook.CallOnValueSuccess(Sub(value) value.Dispose()).SetHandled()
            Next hook
            _game.Dispose()
            _control.AsyncInvokedAction(Sub() _control.Dispose())
            Return Nothing
        End Function
    End Class
End Namespace
