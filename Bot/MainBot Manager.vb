Imports Tinker.Components
Imports Tinker.Commands

Namespace Bot
    Public Class MainBotManager
        Inherits DisposableWithTask
        Implements IBotComponent

        Private ReadOnly _commands As New Bot.BotCommands()
        Private ReadOnly _bot As MainBot
        Private ReadOnly _control As Control

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_bot IsNot Nothing)
            Contract.Invariant(_control IsNot Nothing)
            Contract.Invariant(_commands IsNot Nothing)
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
            Return _commands.IsArgumentPrivate(argument)
        End Function
        Public ReadOnly Property Control As Control Implements IBotComponent.Control
            Get
                Return _control
            End Get
        End Property

        Public Function InvokeCommand(ByVal user As BotUser, ByVal argument As String) As Task(Of String) Implements IBotComponent.InvokeCommand
            Return _commands.Invoke(Bot, user, argument)
        End Function
        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As Task
            _bot.Dispose()
            _control.AsyncInvokedAction(Sub() _control.Dispose()).IgnoreExceptions()
            Return Nothing
        End Function

        Private Function IncludeCommandImpl(ByVal command As ICommand(Of IBotComponent)) As Task(Of IDisposable) Implements IBotComponent.IncludeCommand
            Dim converter = Function(bot As MainBot)
                                If bot IsNot Me.Bot Then
                                    Throw New NotSupportedException("Command mapped from manager to bot used on different bot.")
                                End If
                                Return Me
                            End Function
            Return IncludeCommand(command.ProjectedFrom(converter))
        End Function
        Public Function IncludeCommand(ByVal command As ICommand(Of MainBot)) As Task(Of IDisposable)
            Contract.Requires(command IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of IDisposable))() IsNot Nothing)
            _commands.AddCommand(command)
            Return DirectCast(New DelegatedDisposable(Sub() _commands.RemoveCommand(command)), IDisposable).AsTask()
        End Function
    End Class
End Namespace
