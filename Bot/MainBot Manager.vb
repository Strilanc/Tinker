Imports Tinker.Components

Namespace Bot
    Public Class MainBotManager
        Inherits DisposableWithTask
        Implements IBotComponent

        Public Shared ReadOnly BotCommands As New Bot.BotCommands()

        Private ReadOnly _bot As MainBot
        Private ReadOnly _control As Control

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_bot IsNot Nothing)
            Contract.Invariant(_control IsNot Nothing)
        End Sub

        Public Sub New(ByVal bot As MainBot)
            Contract.Requires(bot IsNot Nothing)
            Me._bot = bot
            Me._control = New GenericBotComponentControl(Me)
        End Sub

        Public ReadOnly Property Bot As MainBot
            Get
                Contract.Ensures(Contract.Result(Of MainBot)() IsNot Nothing)
                Return _bot
            End Get
        End Property
        Public ReadOnly Property Name As InvariantString Implements IBotComponent.Name
            Get
                Return "Main"
            End Get
        End Property
        Public ReadOnly Property Type As InvariantString Implements IBotComponent.Type
            Get
                Return "Bot"
            End Get
        End Property
        Public ReadOnly Property Logger As Logger Implements IBotComponent.Logger
            Get
                Return _bot.Logger
            End Get
        End Property
        Public ReadOnly Property HasControl As Boolean Implements IBotComponent.HasControl
            Get
                Contract.Ensures(Contract.Result(Of Boolean)())
                Return True
            End Get
        End Property
        Public Function IsArgumentPrivate(ByVal argument As String) As Boolean Implements IBotComponent.IsArgumentPrivate
            Return BotCommands.IsArgumentPrivate(argument)
        End Function
        Public ReadOnly Property Control As Control Implements IBotComponent.Control
            Get
                Return _control
            End Get
        End Property

        Public Function InvokeCommand(ByVal user As BotUser, ByVal argument As String) As Task(Of String) Implements IBotComponent.InvokeCommand
            Return BotCommands.Invoke(Bot, user, argument)
        End Function
        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As Task
            _bot.Dispose()
            _control.AsyncInvokedAction(Sub() _control.Dispose()).IgnoreExceptions()
            Return Nothing
        End Function
    End Class
End Namespace
