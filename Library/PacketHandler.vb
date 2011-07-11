Imports Tinker.Pickling

Public NotInheritable Class PacketHandler(Of TKey, TArg)
    Private NotInheritable Class PotentialHandler
        Public ReadOnly ct As CancellationToken
        Public ReadOnly handler As Func(Of TArg, Task)
        Public ReadOnly type As UseType
        Public Enum UseType
            Queue
            All
            Any
        End Enum
        Public Sub New(ct As CancellationToken, type As UseType, handler As Func(Of TArg, Task))
            Contract.Requires(handler IsNot Nothing)
            Me.ct = ct
            Me.type = type
            Me.handler = handler
        End Sub
    End Class

    Public ReadOnly Name As String
    Private ReadOnly _handlers As New Dictionary(Of TKey, LinkedList(Of PotentialHandler))
    Public Sub New(name As String)
        Me.Name = name
    End Sub

    Private Function GetPotentialHandlers(key As TKey) As LinkedList(Of PotentialHandler)
        Contract.Requires(key IsNot Nothing)
        Contract.Ensures(Contract.Result(Of LinkedList(Of PotentialHandler))() IsNot Nothing)
        If Not _handlers.ContainsKey(key) Then _handlers(key) = New LinkedList(Of PotentialHandler)
        Return _handlers(key)
    End Function

    Public Sub IncludeHandler(key As TKey, handler As Func(Of TArg, Task), ct As CancellationToken)
        Contract.Requires(key IsNot Nothing)
        Contract.Requires(handler IsNot Nothing)
        GetPotentialHandlers(key).AddLast(New PotentialHandler(ct, PotentialHandler.UseType.All, handler))
    End Sub
    Public Sub IncludePickOneHandler(key As TKey, handler As Func(Of TArg, Task), ct As CancellationToken)
        Contract.Requires(key IsNot Nothing)
        Contract.Requires(handler IsNot Nothing)
        GetPotentialHandlers(key).AddLast(New PotentialHandler(ct, PotentialHandler.UseType.Any, handler))
    End Sub
    Public Sub QueueOneTimeHandler(key As TKey, handler As Func(Of TArg, Task), ct As CancellationToken)
        Contract.Requires(key IsNot Nothing)
        Contract.Requires(handler IsNot Nothing)
        GetPotentialHandlers(key).AddLast(New PotentialHandler(ct, PotentialHandler.UseType.Queue, handler))
    End Sub

    Public Function TryHandle(key As TKey, arg As TArg) As Task
        Contract.Requires(key IsNot Nothing)

        Dim results = New List(Of Task)()
        Dim usedQueue = False
        Dim usedAny = False
        Dim keptHandlers = New LinkedList(Of PotentialHandler)
        For Each handler In GetPotentialHandlers(key).Where(Function(e) Not e.ct.IsCancellationRequested)
            Dim keep = True
            Dim use = False
            Select Case handler.type
                Case PotentialHandler.UseType.All
                    use = True
                Case PotentialHandler.UseType.Any
                    use = usedAny
                    usedAny = True
                Case PotentialHandler.UseType.Queue
                    use = usedQueue
                    usedQueue = True
                    keep = Not use
                Case Else
                    Throw handler.type.MakeImpossibleValueException()
            End Select
            results.Add(handler.handler(arg))
            If keep Then keptHandlers.AddLast(handler)
        Next
        _handlers(key) = keptHandlers
        If results.None() Then Return Nothing
        Return TaskEx.WhenAll(results)
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
        this.QueueOneTimeHandler(key, Function(data)
                                          ct2.Cancel()
                                          Return handler(jar.ParsePickle(data))
                                      End Function, ct)
        this.IncludeLogger(key, jar, logger, ct2.Token)
    End Sub
    <Extension()>
    Public Sub IncludeHandlerWithLogger(Of K, T)(this As PacketHandler(Of K, IRist(Of Byte)), key As K, jar As IJar(Of T), handler As Func(Of IPickle(Of T), Task), logger As Logger, ct As CancellationToken)
        Contract.Requires(this IsNot Nothing)
        Contract.Requires(key IsNot Nothing)
        Contract.Requires(handler IsNot Nothing)
        Contract.Requires(jar IsNot Nothing)
        Contract.Requires(logger IsNot Nothing)
        this.IncludeHandler(key, Function(data) handler(jar.ParsePickle(data)), ct)
        this.IncludeLogger(key, jar, logger, ct)
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
        Await TaskEx.WhenAll(handlerResults)
    End Function
End Class
