Imports Tinker.Pickling

<ContractClass(GetType(PacketHandler(Of ).ContractClass))>
Public MustInherit Class PacketHandler(Of TKey)
    Private ReadOnly handlers As New KeyedEvent(Of TKey, IReadableList(Of Byte))
    Private ReadOnly logger As Logger

    Public MustOverride ReadOnly Property HeaderSize As Integer
    Protected MustOverride Function ExtractKey(ByVal header As IReadableList(Of Byte)) As TKey

    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(handlers IsNot Nothing)
        Contract.Invariant(logger IsNot Nothing)
    End Sub

    Protected Sub New(Optional ByVal logger As Logger = Nothing)
        Me.logger = If(logger, New Logger)
    End Sub

    Public Sub AddLogger(ByVal key As TKey,
                         ByVal jar As IParseJar(Of Object))
        Contract.Requires(key IsNot Nothing)
        Contract.Requires(jar IsNot Nothing)
        Call [AddHandler](key, Function(data)
                                   Dim pickle = jar.Parse(data)
                                   If pickle.Data.Count < data.Count Then Throw New PicklingException("Data left over after parsing.")
                                   If pickle.Data.Count > data.Count Then Throw New PicklingException("Pickle contains more data than was parsed.")
                                   logger.Log(Function() pickle.Description.Value, LogMessageType.DataParsed)
                                   Dim result = New FutureAction()
                                   result.SetSucceeded()
                                   Return result
                               End Function)
    End Sub

    Public Function [AddHandler](ByVal key As TKey,
                                 ByVal handler As Func(Of IReadableList(Of Byte), IFuture)) As IDisposable
        Contract.Requires(key IsNot Nothing)
        Contract.Requires(handler IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
        Return handlers.AddHandler(key, handler)
    End Function

    Public Function HandlePacket(ByVal packetData As IReadableList(Of Byte), ByVal source As String) As IFuture
        Contract.Requires(packetData IsNot Nothing)
        Contract.Requires(packetData.Count >= HeaderSize)
        Contract.Requires(source IsNot Nothing)
        Contract.Ensures(Contract.Result(Of ifuture)() IsNot Nothing)

        Try
            Dim head = packetData.SubView(0, HeaderSize)
            Dim body = packetData.SubView(HeaderSize)
            Dim key = ExtractKey(head)
            logger.Log(Function() "Received {0} from {1}".Frmt(key, source), LogMessageType.DataEvent)

            Dim result = handlers.Raise(key, body)
            If result.Count = 0 Then
                Throw New IO.IOException("No handler for {0}".Frmt(key))
            End If
            Return result.Defuturized
        Catch e As Exception
            Dim result = New FutureAction()
            result.SetFailed(e)
            Return result
        End Try
    End Function

    <ContractClassFor(GetType(PacketHandler(Of )))>
    MustInherit Class ContractClass
        Inherits PacketHandler(Of TKey)

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
