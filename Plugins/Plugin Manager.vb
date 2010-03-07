Imports Tinker.Components

Namespace Plugins
    Friend Class PluginManager
        Inherits DisposableWithTask
        Implements IBotComponent

        Private Const TypeName As String = "Plugin"

        Private ReadOnly _socket As Plugins.Socket
        Private ReadOnly _hooks As New List(Of Task(Of IDisposable))

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_socket IsNot Nothing)
            Contract.Invariant(_hooks IsNot Nothing)
        End Sub

        Public Sub New(ByVal socket As Plugins.Socket)
            Contract.Requires(socket IsNot Nothing)
            Me._socket = socket
        End Sub

        Public ReadOnly Property Name As InvariantString Implements IBotComponent.Name
            Get
                Return _socket.Name
            End Get
        End Property
        Public ReadOnly Property Type As InvariantString Implements IBotComponent.Type
            Get
                Return TypeName
            End Get
        End Property
        Public ReadOnly Property Logger As Logger Implements IBotComponent.Logger
            Get
                Return _socket.Plugin.Logger
            End Get
        End Property
        Public ReadOnly Property HasControl As Boolean Implements IBotComponent.HasControl
            Get
                Contract.Ensures(Contract.Result(Of Boolean)() = _socket.Plugin.HasControl)
                Return _socket.Plugin.HasControl
            End Get
        End Property
        Public Function IsArgumentPrivate(ByVal argument As String) As Boolean Implements IBotComponent.IsArgumentPrivate
            Return _socket.Plugin.IsArgumentPrivate(argument)
        End Function
        Public Function InvokeCommand(ByVal user As BotUser, ByVal argument As String) As Task(Of String) Implements IBotComponent.InvokeCommand
            Return _socket.Plugin.InvokeCommand(user, argument)
        End Function

        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As Task
            For Each hook In _hooks
                Contract.Assume(hook IsNot Nothing)
                hook.ContinueWithAction(Sub(value) value.Dispose()).SetHandled()
            Next hook
            _socket.Dispose()
            Return Nothing
        End Function

        Public ReadOnly Property Control As Control Implements IBotComponent.Control
            'verification disabled due to stupid verifier (1.2.30118.5)
            <ContractVerification(False)>
            Get
                Return _socket.Plugin.Control
            End Get
        End Property
    End Class
End Namespace
