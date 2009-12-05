Namespace Components
    Public Class LanAdvertiserManager
        Inherits FutureDisposable
        Implements IBotComponent

        Public Shared ReadOnly LanCommands As New Commands.Specializations.LanCommands()

        Private ReadOnly _name As InvariantString
        Private ReadOnly _bot As MainBot
        Private ReadOnly _lanAdvertiser As WC3.LanAdvertiser
        Private ReadOnly _hooks As New List(Of IFuture(Of IDisposable))
        Private ReadOnly _control As Control

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_lanAdvertiser IsNot Nothing)
            Contract.Invariant(_hooks IsNot Nothing)
            Contract.Invariant(_bot IsNot Nothing)
            Contract.Invariant(_control IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As InvariantString,
                       ByVal bot As MainBot,
                       ByVal lanAdvertiser As WC3.LanAdvertiser)
            Contract.Requires(lanAdvertiser IsNot Nothing)
            Contract.Requires(bot IsNot Nothing)

            Me._bot = bot
            Me._name = name
            Me._lanAdvertiser = lanAdvertiser
            Me._control = New LanAdvertiserControl(Me)
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
        Public ReadOnly Property LanAdvertiser As WC3.LanAdvertiser
            Get
                Contract.Ensures(Contract.Result(Of WC3.LanAdvertiser)() IsNot Nothing)
                Return _lanAdvertiser
            End Get
        End Property
        Public ReadOnly Property Bot As MainBot
            Get
                Contract.Ensures(Contract.Result(Of MainBot)() IsNot Nothing)
                Return _bot
            End Get
        End Property

        Public Function InvokeCommand(ByVal user As BotUser, ByVal argument As String) As IFuture(Of String) Implements IBotComponent.InvokeCommand
            Return LanCommands.Invoke(Me, user, argument)
        End Function

        Public Function IsArgumentPrivate(ByVal argument As String) As Boolean Implements IBotComponent.IsArgumentPrivate
            Return LanCommands.IsArgumentPrivate(argument)
        End Function

        Public ReadOnly Property Logger As Logger Implements IBotComponent.Logger
            Get
                Return _lanAdvertiser.Logger
            End Get
        End Property

        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As Strilbrary.Threading.IFuture
            For Each hook In _hooks
                hook.CallOnValueSuccess(Sub(value) value.Dispose()).SetHandled()
            Next hook
            _lanAdvertiser.Dispose()
            _control.Dispose()
            Return Nothing
        End Function
    End Class
End Namespace
