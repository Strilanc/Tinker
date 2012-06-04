Imports Tinker.Pickling

Public Interface IPacketHandler(Of T)
    Function TryHandle(arg As T) As Task
    Function IsDisposed() As Boolean
End Interface

Public NotInheritable Class DelegatedHandler(Of T)
    Implements IPacketHandler(Of T)
    Public ReadOnly handler As Func(Of T, Task)
    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(handler IsNot Nothing)
    End Sub

    Public Sub New(handler As Func(Of T, Task))
        Contract.Requires(handler IsNot Nothing)
        Me.handler = handler
    End Sub
    Public Function TryHandle(arg As T) As Task Implements IPacketHandler(Of T).TryHandle
        If IsDisposed() Then Return Nothing
        Return handler(arg)
    End Function
    Public Function IsDisposed() As Boolean Implements IPacketHandler(Of T).IsDisposed
        Return False
    End Function
End Class
Public NotInheritable Class PotentialHandler(Of T)
    Implements IPacketHandler(Of T)
    Public ReadOnly ct As CancellationToken
    Public ReadOnly handler As IPacketHandler(Of T)
    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(handler IsNot Nothing)
    End Sub
    Public Sub New(ct As CancellationToken, handler As IPacketHandler(Of T))
        Contract.Requires(handler IsNot Nothing)
        Me.ct = ct
        Me.handler = handler
    End Sub
    Public Function TryHandle(arg As T) As Task Implements IPacketHandler(Of T).TryHandle
        If IsDisposed() Then Return Nothing
        Return handler.TryHandle(arg)
    End Function
    Public Function IsDisposed() As Boolean Implements IPacketHandler(Of T).IsDisposed
        Return ct.IsCancellationRequested
    End Function
End Class

Public NotInheritable Class KeyedPacketHandler(Of TKey, TArg, THandler As IPacketHandler(Of TArg))
    Implements IPacketHandler(Of IKeyValue(Of TKey, TArg))
    Private ReadOnly _handlers As Dictionary(Of TKey, THandler)
    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(_handlers IsNot Nothing)
    End Sub
    Public Sub New(Optional handlers As Dictionary(Of TKey, THandler) = Nothing)
        Me._handlers = If(handlers, New Dictionary(Of TKey, THandler)())
    End Sub
    Public Function IsDisposed() As Boolean Implements IPacketHandler(Of IKeyValue(Of TKey, TArg)).IsDisposed
        Return False
    End Function

    Public Property Handler(key As TKey) As THandler
        Get
            Dim r As THandler = Nothing
            If Not _handlers.TryGetValue(key, r) Then Return Nothing
            Return r
        End Get
        Set(value As THandler)
            If value IsNot Nothing Then
                _handlers(key) = value
            Else
                _handlers.Remove(key)
            End If
        End Set
    End Property

    Public Function TryHandle(arg As IKeyValue(Of TKey, TArg)) As Task Implements IPacketHandler(Of IKeyValue(Of TKey, TArg)).TryHandle
        If arg Is Nothing Then Throw New ArgumentNullException("arg")
        If Not _handlers.ContainsKey(arg.Key) Then Return Nothing
        Return _handlers(arg.Key).TryHandle(arg.Value)
    End Function
End Class
Public MustInherit Class PacketHandlerGroup(Of T)
    Implements IPacketHandler(Of T)
    Private ReadOnly _handlers As New LinkedList(Of IPacketHandler(Of T))
    Public Sub IncludeHandler(handler As IPacketHandler(Of T))
        _handlers.AddLast(handler)
    End Sub
    Public Function IsDisposed() As Boolean Implements IPacketHandler(Of T).IsDisposed
        Return False
    End Function
    Public Function TryHandle(arg As T) As Task Implements IPacketHandler(Of T).TryHandle
        Dim e = _handlers.First
        While e IsNot Nothing
            Dim n = e.Next
            If e.Value.AssumeNotNull().IsDisposed Then _handlers.Remove(e)
            e = n
        End While
        If _handlers.None Then Return Nothing
        Return PerformTryHandle(arg, _handlers)
    End Function
    Protected MustOverride Function PerformTryHandle(arg As T, list As LinkedList(Of IPacketHandler(Of T))) As Task
End Class
Public NotInheritable Class PickAllHandler(Of T)
    Inherits PacketHandlerGroup(Of T)
    Protected Overrides Function PerformTryHandle(arg As T, list As LinkedList(Of IPacketHandler(Of T))) As Task
        Dim r = New List(Of Task)
        For Each e In list
            r.Add(e.AssumeNotNull().TryHandle(arg))
        Next
        r.RemoveAll(Function(e) e Is Nothing)
        If r.Count = 0 Then Return Nothing
        If r.Count = 1 Then Return r.Single
        Return Task.WhenAll(r)
    End Function
End Class
Public NotInheritable Class PickOneHandler(Of T)
    Inherits PacketHandlerGroup(Of T)
    Protected Overrides Function PerformTryHandle(arg As T, list As LinkedList(Of IPacketHandler(Of T))) As Task
        For Each e In list
            Dim r = e.AssumeNotNull().TryHandle(arg)
            If r IsNot Nothing Then Return r
        Next
        Return Nothing
    End Function
End Class
Public NotInheritable Class QueueOneHandler(Of T)
    Inherits PacketHandlerGroup(Of T)
    Protected Overrides Function PerformTryHandle(arg As T, list As LinkedList(Of IPacketHandler(Of T))) As Task
        Dim r = list.First.AssumeNotNull().Value.AssumeNotNull().TryHandle(arg)
        list.RemoveFirst()
        Return r
    End Function
End Class

Public NotInheritable Class ComboHandler(Of T)
    Implements IPacketHandler(Of T)

    Private ReadOnly pa As New PickAllHandler(Of T)
    Private ReadOnly po As New PickOneHandler(Of T)
    Private ReadOnly qo As New QueueOneHandler(Of T)
    Public Sub New()
        pa.IncludeHandler(qo)
        pa.IncludeHandler(po)
    End Sub
    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(pa IsNot Nothing)
        Contract.Invariant(po IsNot Nothing)
        Contract.Invariant(qo IsNot Nothing)
    End Sub

    Public Sub IncludeHandler(handler As IPacketHandler(Of T))
        Contract.Requires(handler IsNot Nothing)
        pa.IncludeHandler(handler)
    End Sub
    Public Sub IncludePickOneHandler(handler As IPacketHandler(Of T))
        Contract.Requires(handler IsNot Nothing)
        po.IncludeHandler(handler)
    End Sub
    Public Sub QueueOneTimeHandler(handler As IPacketHandler(Of T))
        Contract.Requires(handler IsNot Nothing)
        qo.IncludeHandler(handler)
    End Sub

    Public Function IsDisposed() As Boolean Implements IPacketHandler(Of T).IsDisposed
        Return False
    End Function
    Public Function TryHandle(arg As T) As Task Implements IPacketHandler(Of T).TryHandle
        Return pa.TryHandle(arg)
    End Function
End Class
Public NotInheritable Class PacketHandler(Of TKey, TArg)
    Implements IPacketHandler(Of IKeyValue(Of TKey, TArg))

    Private ReadOnly x As New KeyedPacketHandler(Of TKey, TArg, ComboHandler(Of TArg))()
    Public ReadOnly Name As String
    Public Sub New(name As String)
        Contract.Requires(name IsNot Nothing)
        Me.Name = name
    End Sub
    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(Name IsNot Nothing)
        Contract.Invariant(x IsNot Nothing)
    End Sub

    Private Function GetFor(key As TKey) As ComboHandler(Of TArg)
        If x.Handler(key) Is Nothing Then x.Handler(key) = New ComboHandler(Of TArg)()
        Return x.Handler(key)
    End Function
    Public Sub IncludeHandler(key As TKey, handler As IPacketHandler(Of TArg))
        Contract.Requires(key IsNot Nothing)
        Contract.Requires(handler IsNot Nothing)
        GetFor(key).IncludeHandler(handler)
    End Sub
    Public Sub IncludeHandler(key As TKey, handler As Func(Of TArg, Task), ct As CancellationToken)
        Contract.Requires(key IsNot Nothing)
        Contract.Requires(handler IsNot Nothing)
        IncludeHandler(key, New PotentialHandler(Of TArg)(ct, New DelegatedHandler(Of TArg)(handler)))
    End Sub
    Public Sub IncludePickOneHandler(key As TKey, handler As IPacketHandler(Of TArg))
        Contract.Requires(key IsNot Nothing)
        Contract.Requires(handler IsNot Nothing)
        GetFor(key).IncludePickOneHandler(handler)
    End Sub
    Public Sub IncludePickOneHandler(key As TKey, handler As Func(Of TArg, Task), ct As CancellationToken)
        Contract.Requires(key IsNot Nothing)
        Contract.Requires(handler IsNot Nothing)
        IncludePickOneHandler(key, New PotentialHandler(Of TArg)(ct, New DelegatedHandler(Of TArg)(handler)))
    End Sub
    Public Sub QueueOneTimeHandler(key As TKey, handler As IPacketHandler(Of TArg))
        Contract.Requires(key IsNot Nothing)
        Contract.Requires(handler IsNot Nothing)
        GetFor(key).QueueOneTimeHandler(handler)
    End Sub
    Public Sub QueueOneTimeHandler(key As TKey, handler As Func(Of TArg, Task), ct As CancellationToken)
        Contract.Requires(key IsNot Nothing)
        Contract.Requires(handler IsNot Nothing)
        QueueOneTimeHandler(key, New PotentialHandler(Of TArg)(ct, New DelegatedHandler(Of TArg)(handler)))
    End Sub

    Public Function IsDisposed() As Boolean Implements IPacketHandler(Of IKeyValue(Of TKey, TArg)).IsDisposed
        Return False
    End Function

    Public Function TryHandle(arg As IKeyValue(Of TKey, TArg)) As Task Implements IPacketHandler(Of IKeyValue(Of TKey, TArg)).TryHandle
        Return GetFor(arg.Key).TryHandle(arg.Value)
    End Function
End Class
Public Module PacketHandlerExtensions
    <Extension()>
    Public Sub IncludeLogger(Of K, T)(this As PacketHandler(Of K, IRist(Of Byte)), key As K, jar As IJar(Of T), logger As Logger, ct As CancellationToken)
        Contract.Requires(this IsNot Nothing)
        Contract.Requires(key IsNot Nothing)
        Contract.Requires(jar IsNot Nothing)
        Contract.Requires(logger IsNot Nothing)
        this.IncludePickOneHandler(key, Function(data)
                                            'Event
                                            logger.Log(Function() "Received {0} from {1}".Frmt(key, this.Name), LogMessageType.DataEvent)

                                            'Parsed
                                            Dim parsed = jar.Parse(data)
                                            If parsed.UsedDataCount < data.Count Then Throw New PicklingException("Data left over after parsing.")
                                            logger.Log(Function() "Received {0} from {1}: {2}".Frmt(key, this.Name, jar.Describe(parsed.Value)), LogMessageType.DataParsed)

                                            Return CompletedTask()
                                        End Function, ct)
    End Sub
    <Extension()>
    Public Sub QueueOneTimeHandlerWithLogger(Of K, T)(this As PacketHandler(Of K, IRist(Of Byte)), key As K, jar As IJar(Of T), handler As Func(Of IPickle(Of T), Task), logger As Logger, ct As CancellationToken)
        Contract.Requires(this IsNot Nothing)
        Contract.Requires(key IsNot Nothing)
        Contract.Requires(handler IsNot Nothing)
        Contract.Requires(jar IsNot Nothing)
        Contract.Requires(logger IsNot Nothing)
        Dim ct2 = New CancellationTokenSource()
        ct.Register(Sub() ct2.Cancel())
        this.IncludeLogger(key, jar, logger, ct2.Token)
        this.QueueOneTimeHandler(key, Function(data)
                                          ct2.Cancel()
                                          Return handler(jar.ParsePickle(data))
                                      End Function, ct)
    End Sub
    <Extension()>
    Public Sub IncludeHandlerWithLogger(Of K, T)(this As PacketHandler(Of K, IRist(Of Byte)), key As K, jar As IJar(Of T), handler As Func(Of IPickle(Of T), Task), logger As Logger, ct As CancellationToken)
        Contract.Requires(this IsNot Nothing)
        Contract.Requires(key IsNot Nothing)
        Contract.Requires(handler IsNot Nothing)
        Contract.Requires(jar IsNot Nothing)
        Contract.Requires(logger IsNot Nothing)
        this.IncludeLogger(key, jar, logger, ct)
        this.IncludeHandler(key, Function(data) handler(jar.ParsePickle(data)), ct)
    End Sub
End Module

Public NotInheritable Class PacketHandlerLogger(Of TKey)
    Private ReadOnly _logger As Logger
    Private ReadOnly _handler As PacketHandlerRaw(Of TKey)
    Private ReadOnly _sourceName As String

    Private ReadOnly _includedJars As New HashSet(Of TKey)
    Private ReadOnly lock As New Object()

    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(_logger IsNot Nothing)
        Contract.Invariant(_handler IsNot Nothing)
        Contract.Invariant(_sourceName IsNot Nothing)
        Contract.Invariant(_includedJars IsNot Nothing)
        Contract.Invariant(lock IsNot Nothing)
    End Sub

    <Pure()>
    Public Sub New(handler As PacketHandlerRaw(Of TKey), sourceName As String, logger As Logger)
        Contract.Requires(handler IsNot Nothing)
        Contract.Requires(sourceName IsNot Nothing)
        Contract.Requires(logger IsNot Nothing)
        Me._handler = handler
        Me._sourceName = sourceName
        Me._logger = logger
    End Sub

    ''' <summary>
    ''' Adds a logger to the underlying packet handler, if one has not already been added for the given key type.
    ''' Returns null on failure.
    ''' </summary>
    Public Function TryIncludeLogger(key As TKey, jar As IJar(Of Object)) As IDisposable
        Contract.Requires(key IsNot Nothing)
        Contract.Requires(jar IsNot Nothing)

        SyncLock lock
            If _includedJars.Contains(key) Then Return Nothing
            _includedJars.Add(key)
        End SyncLock

        Dim handlerRemover = _handler.IncludeHandler(key,
            Function(data)
                'Event
                _logger.Log(Function() "Received {0} from {1}".Frmt(key, _sourceName), LogMessageType.DataEvent)

                'Parsed
                Dim parsed = jar.Parse(data)
                If parsed.UsedDataCount < data.Count Then Throw New PicklingException("Data left over after parsing.")
                _logger.Log(Function() "Received {0} from {1}: {2}".Frmt(key, _sourceName, jar.Describe(parsed.Value)), LogMessageType.DataParsed)

                Return CompletedTask()
            End Function)
        Return New DelegatedDisposable(
            Sub()
                handlerRemover.Dispose()
                SyncLock lock
                    _includedJars.Remove(key)
                End SyncLock
            End Sub)
    End Function

    Public Function IncludeHandler(Of T)(key As TKey, jar As IJar(Of T), handler As Func(Of IPickle(Of T), Task)) As IDisposable
        Contract.Requires(key IsNot Nothing)
        Contract.Requires(jar IsNot Nothing)
        Contract.Requires(handler IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
        Dim ld = TryIncludeLogger(key, jar.Weaken())
        Dim hd = _handler.IncludeHandler(key, Function(data) handler(jar.ParsePickle(data)))
        Return New DelegatedDisposable(
            Sub()
                If ld IsNot Nothing Then ld.Dispose()
                hd.Dispose()
            End Sub)
    End Function

    Public Function HandlePacket(packetData As IRist(Of Byte)) As Task
        Contract.Requires(packetData IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
        If packetData.Count < _handler.HeaderSize Then Throw New ArgumentException("Not enough data.")
        Return _handler.HandlePacket(packetData).AssumeNotNull()
    End Function
End Class

Public NotInheritable Class PacketHandlerRaw(Of TKey)
    Private ReadOnly _headerSize As Integer
    Private ReadOnly _keyExtractor As Func(Of IRist(Of Byte), TKey)
    Private ReadOnly _handlers As New KeyedEvent(Of TKey, IRist(Of Byte))

    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(_headerSize >= 0)
        Contract.Invariant(_keyExtractor IsNot Nothing)
        Contract.Invariant(_handlers IsNot Nothing)
    End Sub

    Public Sub New(headerSize As Integer, keyExtractor As Func(Of IRist(Of Byte), TKey))
        Contract.Requires(headerSize >= 0)
        Contract.Requires(keyExtractor IsNot Nothing)
        Contract.Ensures(Me.HeaderSize = _headerSize)
        Me._headerSize = headerSize
        Me._keyExtractor = keyExtractor
    End Sub

    Public ReadOnly Property HeaderSize As Integer
        Get
            Contract.Ensures(Contract.Result(Of Integer)() >= 0)
            Return _headerSize
        End Get
    End Property

    Public Function IncludeHandler(key As TKey, handler As Func(Of IRist(Of Byte), Task)) As IDisposable
        Contract.Requires(key IsNot Nothing)
        Contract.Requires(handler IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
        Return _handlers.AddHandler(key, handler)
    End Function
    Public Async Function HandlePacket(packetData As IRist(Of Byte)) As Task
        Contract.Assume(packetData IsNot Nothing)
        Contract.Assume(packetData.Count >= HeaderSize)
        'Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)

        Dim head = packetData.TakeExact(HeaderSize)
        Dim body = packetData.SkipExact(HeaderSize)
        Dim key = _keyExtractor(head)

        Dim handlerResults = _handlers.Raise(key, body)
        If handlerResults.Count = 0 Then
            Throw New IO.IOException("No handler for {0}.".Frmt(key))
        End If
        Await Task.WhenAll(handlerResults)
    End Function
End Class
