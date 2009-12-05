Namespace Components
    Public Class WC3GameManager
        Inherits FutureDisposable
        Implements IBotComponent

        Private ReadOnly _bot As MainBot
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
                       ByVal bot As MainBot,
                       ByVal game As WC3.Game)
            Contract.Requires(bot IsNot Nothing)
            Contract.Requires(game IsNot Nothing)

            Me._bot = bot
            Me._name = name
            Me._game = game
            Me._control = New W3GameControl(Me)
        End Sub

        Public ReadOnly Property Game As WC3.Game
            Get
                Contract.Ensures(Contract.Result(Of WC3.Game)() IsNot Nothing)
                Return _game
            End Get
        End Property
        Public ReadOnly Property Bot As MainBot
            Get
                Contract.Ensures(Contract.Result(Of MainBot)() IsNot Nothing)
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
                hook.CallOnValueSuccess(Sub(value) value.Dispose()).SetHandled()
            Next hook
            _game.Dispose()
            _control.Dispose()
            Return Nothing
        End Function
    End Class
End Namespace
