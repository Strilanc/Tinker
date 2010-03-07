Namespace Components
    <ContractClass(GetType(IBotComponent.ContractClass))>
    Public Interface IBotComponent
        Inherits IDisposableWithTask
        ReadOnly Property Name As InvariantString
        ReadOnly Property Type As InvariantString
        ReadOnly Property Logger As Logger
        ReadOnly Property HasControl As Boolean
        ReadOnly Property Control() As Control
        <Pure()>
        Function IsArgumentPrivate(ByVal argument As String) As Boolean
        Function InvokeCommand(ByVal user As BotUser, ByVal argument As String) As Task(Of String)

        <ContractClassFor(GetType(IBotComponent))>
        NotInheritable Shadows Class ContractClass
            Implements IBotComponent

            Public ReadOnly Property Logger As Logger Implements IBotComponent.Logger
                Get
                    Contract.Ensures(Contract.Result(Of Logger)() IsNot Nothing)
                    Throw New NotSupportedException
                End Get
            End Property

            Public ReadOnly Property HasControl As Boolean Implements IBotComponent.HasControl
                Get
                    Throw New NotSupportedException
                End Get
            End Property
            Public ReadOnly Property Control() As Control Implements IBotComponent.Control
                Get
                    Contract.Requires(Me.HasControl)
                    Contract.Ensures(Contract.Result(Of Control)() IsNot Nothing)
                    Throw New NotSupportedException
                End Get
            End Property

            Public Function InvokeCommand(ByVal user As BotUser, ByVal argument As String) As Task(Of String) Implements IBotComponent.InvokeCommand
                Contract.Requires(argument IsNot Nothing)
                Contract.Ensures(Contract.Result(Of Task(Of String))() IsNot Nothing)
                Throw New NotSupportedException
            End Function

            <Pure()>
            Public Function IsArgumentPrivate(ByVal argument As String) As Boolean Implements IBotComponent.IsArgumentPrivate
                Contract.Requires(argument IsNot Nothing)
                Throw New NotSupportedException
            End Function

            Public ReadOnly Property Name As InvariantString Implements IBotComponent.Name
                Get
                    Throw New NotSupportedException
                End Get
            End Property
            Public ReadOnly Property Type As InvariantString Implements IBotComponent.Type
                Get
                    Throw New NotSupportedException
                End Get
            End Property
            Public Sub Dispose() Implements IDisposable.Dispose
            End Sub
            Public ReadOnly Property DisposalTask As Task Implements IDisposableWithTask.DisposalTask
                Get
                    Throw New NotSupportedException
                End Get
            End Property
        End Class
    End Interface
End Namespace
