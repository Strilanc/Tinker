Imports Tinker.Pickling

<ContractClass(GetType(PacketHandler(Of ).ContractClass))>
Public MustInherit Class PacketHandler(Of TKey)
    Private ReadOnly handlers As New KeyedEvent(Of TKey, IReadableList(Of Byte))
    Private ReadOnly logger As Logger
    Private ReadOnly sourceName As String

    Public MustOverride ReadOnly Property HeaderSize As Integer
    Protected MustOverride Function ExtractKey(ByVal header As IReadableList(Of Byte)) As TKey

    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(handlers IsNot Nothing)
        Contract.Invariant(logger IsNot Nothing)
        Contract.Invariant(sourceName IsNot Nothing)
    End Sub

    Protected Sub New(ByVal sourceName As String,
                      Optional ByVal logger As Logger = Nothing)
        Contract.Requires(sourceName IsNot Nothing)
        Me.logger = If(logger, New Logger)
        Me.sourceName = sourceName
    End Sub

    Public Sub AddLogger(ByVal key As TKey,
                         ByVal jar As IParseJar(Of Object))
        Contract.Requires(key IsNot Nothing)
        Contract.Requires(jar IsNot Nothing)
        Call [AddHandler](key, Function(data)
                                   Dim pickle = jar.Parse(data)
                                   If pickle.Data.Count < data.Count Then Throw New PicklingException("Data left over after parsing.")
                                   If pickle.Data.Count > data.Count Then Throw New PicklingException("Pickle contains more data than was parsed.")
                                   logger.Log(Function() "Received {0} from {1}: {2}".Frmt(key, sourceName, pickle.Description.Value), LogMessageType.DataParsed)
                                   Dim result = New TaskCompletionSource(Of Boolean)()
                                   result.SetResult(True)
                                   Return result.Task
                               End Function)
    End Sub

    Public Function [AddHandler](ByVal key As TKey,
                                 ByVal handler As Func(Of IReadableList(Of Byte), Task)) As IDisposable
        Contract.Requires(key IsNot Nothing)
        Contract.Requires(handler IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
        Return handlers.AddHandler(key, handler)
    End Function

    Public Function HandlePacket(ByVal packetData As IReadableList(Of Byte)) As Task
        Contract.Requires(packetData IsNot Nothing)
        Contract.Requires(packetData.Count >= HeaderSize)
        Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)

        Dim result = New TaskCompletionSource(Of Task)()
        result.SetByEvaluating(Function()
                                   Dim head = packetData.SubView(0, HeaderSize)
                                   Dim body = packetData.SubView(HeaderSize)
                                   Dim key = ExtractKey(head)
                                   logger.Log(Function() "Received {0} from {1}".Frmt(key, sourceName), LogMessageType.DataEvent)

                                   Dim handlerResults = handlers.Raise(key, body)
                                   If handlerResults.Count = 0 Then
                                       Throw New IO.IOException("No handler for {0}".Frmt(key))
                                   End If
                                   Return handlerResults.AsAggregateTask
                               End Function)
        Return result.Task.Unwrap.AssumeNotNull
    End Function

    <ContractClassFor(GetType(PacketHandler(Of )))>
    MustInherit Class ContractClass
        Inherits PacketHandler(Of TKey)

        Protected Sub New()
            MyBase.New(Nothing, Nothing)
        End Sub

        Protected Overrides Function ExtractKey(ByVal header As IReadableList(Of Byte)) As TKey
            Contract.Requires(header IsNot Nothing)
            Contract.Requires(header.Count = HeaderSize)
            Contract.Ensures(Contract.Result(Of TKey)() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Overrides ReadOnly Property HeaderSize As Integer
            Get
                Contract.Ensures(Contract.Result(Of Integer)() > 0)
                Throw New NotSupportedException
            End Get
        End Property
    End Class
End Class
