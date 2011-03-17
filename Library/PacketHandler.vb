Imports Tinker.Pickling

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
    Public Function TryIncludeLogger(key As TKey, jar As ISimpleJar) As IDisposable
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

                Return InstantTask()
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
        Dim ld = TryIncludeLogger(key, jar)
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
        Await handlerResults.AsAggregateTask
    End Function
End Class
