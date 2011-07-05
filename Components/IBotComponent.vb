Namespace Components
    <ContractClass(GetType(ContractClassIBotComponent))>
    Public Interface IBotComponent
        Inherits IDisposableWithTask
        ReadOnly Property Name As InvariantString
        ReadOnly Property Type As InvariantString
        ReadOnly Property Logger As Logger
        ReadOnly Property HasControl As Boolean
        ReadOnly Property Control() As Control
        <Pure()>
        Function IsArgumentPrivate(argument As String) As Boolean
        Function InvokeCommand(user As BotUser, argument As String) As Task(Of String)
        '''<summary>Adds a command to the component and returns an IDisposable that removes the command upon disposal.</summary>
        Function IncludeCommand(command As Commands.ICommand(Of IBotComponent)) As Task(Of IDisposable)
    End Interface

    <ContractClassFor(GetType(IBotComponent))>
    Public MustInherit Class ContractClassIBotComponent
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
                Contract.Requires(DirectCast(Me, IBotComponent).HasControl)
                Contract.Ensures(Contract.Result(Of Control)() IsNot Nothing)
                Throw New NotSupportedException
            End Get
        End Property

        Public Function InvokeCommand(user As BotUser, argument As String) As Task(Of String) Implements IBotComponent.InvokeCommand
            Contract.Requires(argument IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of String))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        <Pure()>
        Public Function IsArgumentPrivate(argument As String) As Boolean Implements IBotComponent.IsArgumentPrivate
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

        Public ReadOnly Property DisposalTask As Task Implements IDisposableWithTask.DisposalTask
            Get
                Throw New NotSupportedException
            End Get
        End Property
        Public Sub Dispose() Implements IDisposable.Dispose
            Throw New NotSupportedException
        End Sub

        Public Function IncludeCommand(command As Commands.ICommand(Of IBotComponent)) As Task(Of IDisposable) Implements IBotComponent.IncludeCommand
            Contract.Requires(command IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of IDisposable))() IsNot Nothing)
            Throw New NotSupportedException
        End Function
    End Class
End Namespace
