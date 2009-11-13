<ContractClass(GetType(PacketHandler(Of ).contractclass))>
Public MustInherit Class PacketHandler(Of TKey)
    Private ReadOnly handlers As New KeyedEvent(Of TKey, ViewableList(Of Byte))
    Private ReadOnly logger As Logger

    Public MustOverride ReadOnly Property HeaderSize As Integer
    Protected MustOverride Function ExtractKey(ByVal header As ViewableList(Of Byte)) As TKey

    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(handlers IsNot Nothing)
        Contract.Invariant(logger IsNot Nothing)
    End Sub

    Protected Sub New(Optional ByVal logger As Logger = Nothing)
        Me.logger = If(logger, New Logger)
    End Sub

    Public Function [AddHandler](Of TData)(ByVal key As TKey,
                                           ByVal jar As IParseJar(Of TData),
                                           ByVal handler As Func(Of IPickle(Of TData), IFuture)) As IDisposable
        Contract.Requires(jar IsNot Nothing)
        Contract.Requires(handler IsNot Nothing)
        Return handlers.AddHandler(key, Function(data As ViewableList(Of Byte))
                                            Dim value = jar.Parse(data)
                                            logger.Log(Function() "{0}".Frmt(value.Description.Value), LogMessageType.DataParsed)
                                            Return handler(value)
                                        End Function)
    End Function

    Public Function HandlePacket(ByVal packetData As ViewableList(Of Byte)) As IFuture
        Contract.Requires(packetData IsNot Nothing)
        Contract.Requires(packetData.Length = HeaderSize)
        Contract.Ensures(Contract.Result(Of ifuture)() IsNot Nothing)
        If packetData.Length < HeaderSize Then Throw New InvalidOperationException("Invalid packet header")

        Try
            Dim head = packetData.SubView(0, HeaderSize)
            Dim body = packetData.SubView(HeaderSize)
            Dim key = ExtractKey(head)
            logger.Log(Function() "Received {0}".Frmt(key), LogMessageType.DataEvent)

            Dim result = handlers.Raise(key, body)
            If result.Count = 0 Then
                Throw New IO.IOException("No handler for {0}".Frmt(key))
            End If
            Return result.Defuturized
        Catch e As Exception
            Return e.FuturizedFail
        End Try
    End Function

    <ContractClassFor(GetType(PacketHandler(Of )))>
    Class ContractClass
        Inherits PacketHandler(Of TKey)

        Protected Overrides Function ExtractKey(ByVal header As Strilbrary.ViewableList(Of Byte)) As TKey
            Contract.Requires(header IsNot Nothing)
            Contract.Requires(header.Length = HeaderSize)
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
