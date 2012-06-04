'Imports Tinker.Pickling

'Public Delegate Sub IHandler2(Of K, TCur, TMore)(value As TCur, inline As ISource(Of K, TMore))
'Public Delegate Sub IHandler(Of K, T)(value As T, inline As ISource(Of K, T))
'Public Interface ISource(Of K, T)
'    Event Unhandled(key As K, value As T)
'    ReadOnly Property Name As String
'    Sub Observe(key As K, handler As IHandler(Of K, T), ct As CancellationToken)
'End Interface

'Public Class QueueSource(Of K, T)
'    Implements ISource(Of K, T)

'    Private NotInheritable Class Node
'        Public ReadOnly Key As K
'        Public ReadOnly Value As T
'        Public [Next] As Node
'        Public ReadOnly Parent As QueueSource(Of K, T)
'        <ContractInvariantMethod()> Private Sub ObjectInvariant()
'            Contract.Invariant(Parent IsNot Nothing)
'        End Sub

'        Public Sub New(key As K, value As T, parent As QueueSource(Of K, T))
'            Contract.Requires(parent IsNot Nothing)
'            Me.Value = value
'            Me.Key = key
'            Me.Parent = parent
'        End Sub
'        Public Sub SetHandled()
'            GC.SuppressFinalize(Me)
'        End Sub

'        Protected Overrides Sub Finalize()
'            Parent.RaiseUnhandled(Key, Value)
'        End Sub
'    End Class

'    <ContractInvariantMethod()> Private Sub ObjectInvariant()
'        Contract.Invariant(Tail IsNot Nothing)
'        Contract.Invariant(inQueue IsNot Nothing)
'        Contract.Invariant(Handlers IsNot Nothing)
'    End Sub

'    Public ReadOnly Property Name As String Implements ISource(Of K, T).Name
'        Get
'            Return _name
'        End Get
'    End Property

'    Private ReadOnly _name As String
'    Private Tail As Node
'    Private ReadOnly Handlers As New Dictionary(Of K, List(Of IHandler(Of K, T)))()
'    Private ReadOnly inQueue As CallQueue

'    Public Event Unhandled(key As K, value As T) Implements ISource(Of K, T).Unhandled
'    Private Sub RaiseUnhandled(key As K, value As T)
'        RaiseEvent Unhandled(key, value)
'    End Sub

'    Public Sub New(name As String, inQueue As CallQueue)
'        Contract.Requires(name IsNot Nothing)
'        Contract.Requires(inQueue IsNot Nothing)
'        Me._name = name
'        Me.inQueue = inQueue
'        Me.Tail = New Node(Nothing, Nothing, Me)
'        Me.Tail.SetHandled()
'    End Sub

'    Public Async Sub EnqueueHandle(key As K, value As T)
'        Await inQueue.AwaitableEntrance()

'        Tail.Next = New Node(key, value, Me)
'        Tail = Tail.Next

'        Dim position = New Position(Me, Tail)
'        If Handlers.ContainsKey(key) Then
'            For Each h In Handlers(key)
'                h(value, position)
'                Tail.SetHandled()
'            Next
'        End If
'    End Sub

'    Private NotInheritable Class Position
'        Implements ISource(Of K, T)
'        Public ReadOnly Parent As QueueSource(Of K, T)
'        Public ReadOnly PreviousNode As Node
'        <ContractInvariantMethod()> Private Sub ObjectInvariant()
'            Contract.Invariant(Parent IsNot Nothing)
'            Contract.Invariant(PreviousNode IsNot Nothing)
'        End Sub

'        Public Sub New(parent As QueueSource(Of K, T), previousNode As Node)
'            Contract.Requires(parent IsNot Nothing)
'            Contract.Requires(previousNode IsNot Nothing)
'            Me.Parent = parent
'            Me.PreviousNode = previousNode
'        End Sub
'        Public Sub Observe(key As K, handler As IHandler(Of K, T), ct As CancellationToken) Implements ISource(Of K, T).Observe
'            Parent.Observe(key, PreviousNode, handler, ct)
'        End Sub
'    End Class

'    Public Async Sub Observe(key As K, handler As IHandler(Of K, T), ct As CancellationToken) Implements ISource(Of K, T).Observe
'        Await inQueue.AwaitableEntrance()
'        If Not Handlers.ContainsKey(key) Then Handlers(key) = New List(Of IHandler(Of K, T))()
'        Handlers(key).Add(handler)
'        ct.Register(Async Sub()
'                        Await inQueue.AwaitableEntrance()
'                        Handlers(key).Remove(handler)
'                        If Handlers(key).Count = 0 Then Handlers.Remove(key)
'                    End Sub)
'    End Sub
'    Private Async Sub Observe(key As K, node As Node, handler As IHandler(Of K, T), ct As CancellationToken)
'        Await inQueue.AwaitableEntrance()
'        Dim n = node
'        Do
'            n = n.Next
'            If n Is Nothing Then Exit Do
'            n.SetHandled()
'            handler(n.Value, New Position(Me, n))
'        Loop
'        Observe(key, handler, ct)
'    End Sub
'End Class





'Public Module SExtensions
'    <Extension()>
'    Public Sub IncludeHandler(Of K, T)(this As ISource(Of K, IRist(Of Byte)), key As K, jar As IJar(Of T), handler As IHandler2(Of K, IPickle(Of T), IRist(Of Byte)), ct As CancellationToken)
'        Contract.Requires(this IsNot Nothing)
'        Contract.Requires(key IsNot Nothing)
'        Contract.Requires(handler IsNot Nothing)
'        Contract.Requires(jar IsNot Nothing)
'        this.Observe(key, Sub(data, inline) handler(jar.ParsePickle(data), inline), ct)
'    End Sub
'    <Extension()>
'    Public Sub IncludeOneTimeHandler(Of K, T)(this As ISource(Of K, IRist(Of Byte)), key As K, jar As IJar(Of T), handler As IHandler2(Of K, IPickle(Of T), IRist(Of Byte)), ct As CancellationToken)
'        Contract.Requires(this IsNot Nothing)
'        Contract.Requires(key IsNot Nothing)
'        Contract.Requires(handler IsNot Nothing)
'        Contract.Requires(jar IsNot Nothing)
'        Dim ct2 = New CancellationTokenSource()
'        ct.Register(Sub() ct2.Cancel())
'        IncludeHandler(this, key, jar, Sub(x1, x2)
'                                           If ct2.Token.IsCancellationRequested Then Return
'                                           ct2.Cancel()
'                                           handler(x1, x2)
'                                       End Sub, ct2.Token)
'    End Sub
'    <Extension()>
'    Public Sub IncludeLogger(Of K, T)(this As ISource(Of K, IRist(Of Byte)), key As K, jar As IJar(Of T), logger As Logger, ct As CancellationToken)
'        Contract.Requires(this IsNot Nothing)
'        Contract.Requires(key IsNot Nothing)
'        Contract.Requires(jar IsNot Nothing)
'        Contract.Requires(logger IsNot Nothing)
'        this.Observe(key, Function(data, inline)
'                              'Event
'                              logger.Log(Function() "Received {0} from {1}".Frmt(key, name), LogMessageType.DataEvent)

'                              'Parsed
'                              Dim parsed = jar.Parse(data)
'                              If parsed.UsedDataCount < data.Count Then Throw New PicklingException("Data left over after parsing.")
'                              logger.Log(Function() "Received {0} from {1}: {2}".Frmt(key, name, jar.Describe(parsed.Value)), LogMessageType.DataParsed)

'                              Return CompletedTask()
'                          End Function, ct)
'    End Sub
'    <Extension()>
'    Public Sub IncludeHandlerWithLogger(Of K, T)(this As ISource(Of K, IRist(Of Byte)), key As K, jar As IJar(Of T), handler As IHandler2(Of K, IPickle(Of T), IRist(Of Byte)), logger As Logger, ct As CancellationToken)
'        Contract.Requires(this IsNot Nothing)
'        Contract.Requires(key IsNot Nothing)
'        Contract.Requires(handler IsNot Nothing)
'        Contract.Requires(jar IsNot Nothing)
'        Contract.Requires(logger IsNot Nothing)
'        this.IncludeLogger(key, jar, logger, ct)
'        this.IncludeHandler(key, jar, handler, ct)
'    End Sub
'    <Extension()>
'    Public Sub IncludeOneTimeHandlerWithLogger(Of K, T)(this As ISource(Of K, IRist(Of Byte)), key As K, jar As IJar(Of T), handler As IHandler2(Of K, IPickle(Of T), IRist(Of Byte)), logger As Logger, ct As CancellationToken)
'        Contract.Requires(this IsNot Nothing)
'        Contract.Requires(key IsNot Nothing)
'        Contract.Requires(handler IsNot Nothing)
'        Contract.Requires(jar IsNot Nothing)
'        Contract.Requires(logger IsNot Nothing)
'        Dim ct2 = New CancellationTokenSource()
'        ct.Register(Sub() ct2.Cancel())
'        IncludeHandler(this, key, jar, Sub(x1, x2)
'                                           If ct2.Token.IsCancellationRequested Then Return
'                                           ct2.Cancel()
'                                           handler(x1, x2)
'                                       End Sub, ct2.Token)
'        this.IncludeLogger(key, jar, logger, ct2.Token)
'    End Sub

'    <Extension()>
'    Public Function AwaitReceive(Of K, T)(this As ISource(Of K, IRist(Of Byte)), key As K, jar As IJar(Of T), logger As Logger, ct As CancellationToken) As Task(Of Receival(Of K, T, IRist(Of Byte)))
'        Dim receivedResult = New TaskCompletionSource(Of Receival(Of K, T, IRist(Of Byte)))()
'        ct.Register(Sub() receivedResult.TrySetCanceled())
'        this.IncludeOneTimeHandlerWithLogger(key, jar, Sub(data, inline2) receivedResult.TrySetResult(New Receival(Of K, T, IRist(Of Byte))(data.Value, inline2)), logger, ct)
'        Return receivedResult.Task
'    End Function
'End Module
'Public Structure Receival(Of K, TCur, TMore)
'    Public ReadOnly Value As TCur
'    Public ReadOnly Inline As ISource(Of K, TMore)
'    Public Sub New(value As TCur, inline As ISource(Of K, TMore))
'        Me.Value = value
'        Me.Inline = inline
'    End Sub
'End Structure