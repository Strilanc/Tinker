Imports Tinker.Commands
Imports Tinker.Components

Namespace CKL
    Public NotInheritable Class ServerManager
        Inherits DisposableWithTask
        Implements IBotComponent

        Public Shared ReadOnly WidgetTypeName As InvariantString = "CKL"
        Private ReadOnly _commands As New CommandSet(Of CKL.ServerManager)
        Private ReadOnly _server As CKL.Server
        Private ReadOnly _control As GenericBotComponentControl

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_control IsNot Nothing)
            Contract.Invariant(_server IsNot Nothing)
            Contract.Invariant(_commands IsNot Nothing)
        End Sub

        Public Sub New(server As CKL.Server)
            Contract.Requires(server IsNot Nothing)

            Me._server = server
            Me._control = New GenericBotComponentControl(Me)
        End Sub

        Protected Overrides Function PerformDispose(finalizing As Boolean) As Task
            _server.Stop()
            Call Async Sub() Await _control.DisposeControlAsync()
            Return Nothing
        End Function

        Public ReadOnly Property Control As System.Windows.Forms.Control Implements IBotComponent.Control
            Get
                Return _control
            End Get
        End Property
        Public ReadOnly Property HasControl As Boolean Implements IBotComponent.HasControl
            Get
                Contract.Ensures(Contract.Result(Of Boolean)())
                Return True
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
                Return _server.Logger
            End Get
        End Property
        Public ReadOnly Property Name As InvariantString Implements IBotComponent.Name
            Get
                Return _server.name
            End Get
        End Property
        Public ReadOnly Property Server As CKL.Server
            Get
                Contract.Ensures(Contract.Result(Of CKL.Server)() IsNot Nothing)
                Return _server
            End Get
        End Property
        Public ReadOnly Property Type As InvariantString Implements IBotComponent.Type
            Get
                Return WidgetTypeName
            End Get
        End Property

        Private Function IncludeCommandImpl(command As ICommand(Of IBotComponent)) As Task(Of IDisposable) Implements IBotComponent.IncludeCommand
            Return IncludeCommand(command)
        End Function
        Public Function IncludeCommand(command As ICommand(Of CKL.ServerManager)) As Task(Of IDisposable)
            Contract.Requires(command IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of IDisposable))() IsNot Nothing)
            Return _commands.IncludeCommand(command).AsTask()
        End Function
    End Class
End Namespace
