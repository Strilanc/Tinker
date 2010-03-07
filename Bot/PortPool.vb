Public NotInheritable Class PortPool
    Private ReadOnly _inPorts As New HashSet(Of UShort)
    Private ReadOnly _outPorts As New HashSet(Of UShort)
    Private ReadOnly _portPool As New HashSet(Of UShort)
    Private ReadOnly lock As New Object()

    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(_inPorts IsNot Nothing)
        Contract.Invariant(_outPorts IsNot Nothing)
        Contract.Invariant(_portPool IsNot Nothing)
        Contract.Invariant(lock IsNot Nothing)
    End Sub

    Private ReadOnly Property PortPool As HashSet(Of UShort)
        Get
            Contract.Ensures(Contract.Result(Of HashSet(Of UShort))() IsNot Nothing)
            Return _portPool
        End Get
    End Property
    Private ReadOnly Property InPorts As HashSet(Of UShort)
        Get
            Contract.Ensures(Contract.Result(Of HashSet(Of UShort))() IsNot Nothing)
            Return _inPorts
        End Get
    End Property
    Private ReadOnly Property OutPorts As HashSet(Of UShort)
        Get
            Contract.Ensures(Contract.Result(Of HashSet(Of UShort))() IsNot Nothing)
            Return _outPorts
        End Get
    End Property

    Public Function EnumPorts() As IEnumerable(Of UShort)
        Contract.Ensures(Contract.Result(Of IEnumerable(Of UShort))() IsNot Nothing)
        SyncLock lock
            Return PortPool.Cache()
        End SyncLock
    End Function
    Public Function EnumUsedPorts() As IEnumerable(Of UShort)
        Contract.Ensures(Contract.Result(Of IEnumerable(Of UShort))() IsNot Nothing)
        SyncLock lock
            Return OutPorts.Cache()
        End SyncLock
    End Function
    Public Function EnumAvailablePorts() As IEnumerable(Of UShort)
        Contract.Ensures(Contract.Result(Of IEnumerable(Of UShort))() IsNot Nothing)
        SyncLock lock
            Return InPorts.Cache()
        End SyncLock
    End Function

    Public Enum TryAddPortOutcome
        Added
        WasAlreadyInPool
        '''<summary>The port was still in use when removed from the pool, and is still in use now after being re-added.</summary>
        ReturnedWhileInUse
    End Enum
    Public Function TryAddPort(ByVal port As UShort) As TryAddPortOutcome
        SyncLock lock
            If PortPool.Contains(port) Then Return TryAddPortOutcome.WasAlreadyInPool
            PortPool.Add(port)
            If OutPorts.Contains(port) Then Return TryAddPortOutcome.ReturnedWhileInUse
            InPorts.Add(port)
            Return TryAddPortOutcome.Added
        End SyncLock
    End Function

    Public Enum TryRemovePortOutcome
        Removed
        WasAlreadyNotInPool
        '''<summary>The port was in use, but it is 'removed' in that it will not return to the pool when it is released.</summary>
        RemovedButStillInUse
    End Enum
    Public Function TryRemovePort(ByVal port As UShort) As TryRemovePortOutcome
        SyncLock lock
            If Not PortPool.Contains(port) Then Return TryRemovePortOutcome.WasAlreadyNotInPool
            PortPool.Remove(port)
            If OutPorts.Contains(port) Then Return TryRemovePortOutcome.RemovedButStillInUse
            InPorts.Remove(port)
            Return TryRemovePortOutcome.Removed
        End SyncLock
    End Function

    Public Function TryAcquireAnyPort() As PortHandle
        SyncLock lock
            If InPorts.Count = 0 Then Return Nothing
            Dim port = New PortHandle(Me, InPorts.First)
            InPorts.Remove(port.Port)
            OutPorts.Add(port.Port)
            Return port
        End SyncLock
    End Function

    <DebuggerDisplay("{ToString}")>
    Public NotInheritable Class PortHandle
        Inherits DisposableWithTask
        Private ReadOnly _pool As PortPool
        Private ReadOnly _port As UShort

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_pool IsNot Nothing)
        End Sub

        Public Sub New(ByVal pool As PortPool, ByVal port As UShort)
            Contract.Requires(pool IsNot Nothing)
            Me._pool = pool
            Me._port = port
        End Sub

        Public ReadOnly Property Port() As UShort
            Get
                If Me.IsDisposed Then Throw New ObjectDisposedException(Me.GetType.Name)
                Return _port
            End Get
        End Property

        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As Task
            SyncLock _pool.lock
                Contract.Assume(_pool.OutPorts.Contains(_port))
                Contract.Assume(Not _pool.InPorts.Contains(_port))
                _pool.OutPorts.Remove(_port)
                If _pool.PortPool.Contains(_port) Then
                    _pool.InPorts.Add(_port)
                End If
            End SyncLock
            Return Nothing
        End Function

        Public Overrides Function ToString() As String
            Return _port.ToString(CultureInfo.InvariantCulture) + If(Me.IsDisposed, " (disposed)", "")
        End Function
    End Class
End Class
