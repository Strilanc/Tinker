Imports Tinker.Commands
Imports Tinker.Components
Imports Tinker.Bot

Namespace Lan
    ''' <summary>
    ''' Exposes a <see cref="Lan.UDPAdvertiser" /> as an <see cref="IBotComponent" />.
    ''' </summary>
    Public Class UDPAdvertiserComponent
        Inherits DisposableWithTask
        Implements IBotComponent

        Private ReadOnly inQueue As CallQueue = MakeTaskedCallQueue
        Private ReadOnly _commands As New CommandSet(Of UDPAdvertiserComponent)
        Private ReadOnly _name As InvariantString
        Private ReadOnly _bot As Bot.MainBot
        Private ReadOnly _advertiser As Lan.UDPAdvertiser
        Private ReadOnly _control As Control

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(inQueue IsNot Nothing)
            Contract.Invariant(_advertiser IsNot Nothing)
            Contract.Invariant(_bot IsNot Nothing)
            Contract.Invariant(_control IsNot Nothing)
            Contract.Invariant(_commands IsNot Nothing)
        End Sub

        Public Sub New(name As InvariantString,
                       bot As Bot.MainBot,
                       advertiser As Lan.UDPAdvertiser)
            Contract.Requires(advertiser IsNot Nothing)
            Contract.Requires(bot IsNot Nothing)

            Me._bot = bot
            Me._name = name
            Me._advertiser = advertiser
            Me._control = New UDPAdvertiserControl(Me)
        End Sub

        Public ReadOnly Property Name As InvariantString Implements IBotComponent.Name
            Get
                Return _name
            End Get
        End Property
        Public ReadOnly Property Type As InvariantString Implements IBotComponent.Type
            Get
                Return "Lan"
            End Get
        End Property
        Public ReadOnly Property HasControl As Boolean Implements IBotComponent.HasControl
            Get
                Contract.Ensures(Contract.Result(Of Boolean)())
                Return True
            End Get
        End Property
        Public ReadOnly Property Control As Control Implements IBotComponent.Control
            Get
                Return _control
            End Get
        End Property
        Public ReadOnly Property Advertiser As Lan.UDPAdvertiser
            Get
                Contract.Ensures(Contract.Result(Of Lan.UDPAdvertiser)() IsNot Nothing)
                Return _advertiser
            End Get
        End Property
        Public ReadOnly Property Bot As Bot.MainBot
            Get
                Contract.Ensures(Contract.Result(Of Bot.MainBot)() IsNot Nothing)
                Return _bot
            End Get
        End Property

        Public Function InvokeCommand(user As BotUser, argument As String) As Task(Of String) Implements IBotComponent.InvokeCommand
            Return _commands.Invoke(Me, user, argument)
        End Function

        Public Function IsArgumentPrivate(argument As String) As Boolean Implements IBotComponent.IsArgumentPrivate
            Return _commands.IsArgumentPrivate(argument)
        End Function

        Public ReadOnly Property Logger As Logger Implements IBotComponent.Logger
            Get
                Return _advertiser.Logger
            End Get
        End Property

        Private _autoHook As Task(Of IDisposable)
        Private Sub SetAutomatic(slaved As Boolean)
            If slaved = (_autoHook IsNot Nothing) Then Return
            If slaved Then
                _autoHook = _bot.ObserveGameSets(
                        adder:=Sub(server, gameSet) _advertiser.QueueAddGame(gameSet.GameSettings.GameDescription).ConsiderExceptionsHandled(),
                        remover:=Sub(server, gameSet) _advertiser.QueueRemoveGame(gameSet.GameSettings.GameDescription.GameId).ConsiderExceptionsHandled())
            Else
                Contract.Assume(_autoHook IsNot Nothing)
                _autoHook.DisposeAsync()
                _autoHook = Nothing
            End If
        End Sub
        Public Function QueueSetAutomatic(slaved As Boolean) As Task
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() SetAutomatic(slaved))
        End Function

        Private Function IncludeCommandImpl(command As ICommand(Of IBotComponent)) As Task(Of IDisposable) Implements IBotComponent.IncludeCommand
            Return IncludeCommand(command)
        End Function
        Public Function IncludeCommand(command As ICommand(Of UDPAdvertiserComponent)) As Task(Of IDisposable)
            Contract.Requires(command IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of IDisposable))() IsNot Nothing)
            Return _commands.IncludeCommand(command).AsTask()
        End Function

        Protected Overrides Function PerformDispose(finalizing As Boolean) As Task
            If finalizing Then Return Nothing
            _advertiser.Dispose()
            Return TaskEx.WhenAll({
                _control.DisposeControlAsync(),
                QueueSetAutomatic(False)
            })
        End Function
    End Class
End Namespace
