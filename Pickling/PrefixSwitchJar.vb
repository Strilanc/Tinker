Namespace Pickling
    Public NotInheritable Class PrefixPickle(Of TKey)
        Private ReadOnly _key As TKey
        Private ReadOnly _payload As ISimplePickle

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_key IsNot Nothing)
            Contract.Invariant(_payload IsNot Nothing)
        End Sub

        Public Sub New(ByVal key As TKey, ByVal payload As ISimplePickle)
            Contract.Requires(key IsNot Nothing)
            Contract.Requires(payload IsNot Nothing)
            Me._key = key
            Me._payload = payload
        End Sub
        Public ReadOnly Property Key As TKey
            Get
                Contract.Ensures(Contract.Result(Of TKey)() IsNot Nothing)
                Return _key
            End Get
        End Property
        Public ReadOnly Property Payload As ISimplePickle
            Get
                Contract.Ensures(Contract.Result(Of ISimplePickle)() IsNot Nothing)
                Return _payload
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return "{0}: {1}".Frmt(Key, Payload.Description.Value)
        End Function
    End Class
    Public NotInheritable Class PrefixSwitchJar(Of TKey)
        Implements IParseJar(Of PrefixPickle(Of TKey))
        Private ReadOnly parsers As New Dictionary(Of TKey, ISimpleParseJar)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(parsers IsNot Nothing)
        End Sub

        'verification disabled due to stupid verifier (1.2.30118.5)
        <ContractVerification(False)>
        Public Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of PrefixPickle(Of TKey)) Implements IParseJar(Of PrefixPickle(Of TKey)).Parse
            If data.Count < 1 Then Throw New PicklingNotEnoughDataException()
            Dim key = CType(CType(data(0), Object), TKey)
            If Not parsers.ContainsKey(key) Then Throw New PicklingException("No parser for key {0}".Frmt(key))

            Dim pickle = parsers(key).Parse(data.SubView(1))
            Dim datum = data.SubView(0, pickle.Data.Count + 1)
            Return New PrefixPickle(Of TKey)(key, pickle).Pickled(datum)
        End Function
        Private Function SimpleParse(ByVal data As IReadableList(Of Byte)) As ISimplePickle Implements ISimpleParseJar.Parse
            Return Parse(data)
        End Function

        Public Sub AddParser(ByVal key As TKey, ByVal parser As ISimpleParseJar)
            Contract.Requires(key IsNot Nothing)
            Contract.Requires(parser IsNot Nothing)
            If parsers.ContainsKey(key) Then Throw New InvalidOperationException("Parser already registered for key {0}.".Frmt(key))
            parsers.Add(key, parser)
        End Sub
    End Class
End Namespace
