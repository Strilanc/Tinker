Imports Tinker.Pickling

Namespace WC3.Protocol
    Public NotInheritable Class Packet
        Public Const PacketPrefixValue As Byte = &HF7
        Public ReadOnly id As PacketId
        Private ReadOnly _payload As IPickle(Of Object)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_payload IsNot Nothing)
        End Sub

        Public ReadOnly Property Payload As IPickle(Of Object)
            Get
                Contract.Ensures(Contract.Result(Of IPickle(Of Object))() IsNot Nothing)
                Return _payload
            End Get
        End Property

        Private Sub New(ByVal id As PacketId, ByVal payload As IPickle(Of Object))
            Contract.Requires(payload IsNot Nothing)
            Me._payload = payload
            Me.id = id
        End Sub
        Public Sub New(ByVal definition As Packets.SimpleDefinition, ByVal vals As Dictionary(Of InvariantString, Object))
            Me.New(definition.id, definition.Pack(vals))
            Contract.Requires(definition IsNot Nothing)
            Contract.Requires(vals IsNot Nothing)
        End Sub

        Public Shared Function FromValue(Of T)(ByVal id As PacketId, ByVal jar As IPackJar(Of T), ByVal value As T) As Packet
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(value IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(id, jar.Weaken.Pack(CType(value, Object)))
        End Function
    End Class

    Public NotInheritable Class W3PacketHandler
        Inherits PacketHandler(Of PacketId)

        Public Sub New(ByVal sourceName As String,
                       Optional ByVal logger As Logger = Nothing)
            MyBase.New(sourceName, logger)
            Contract.Requires(sourceName IsNot Nothing)
        End Sub

        Public Overrides ReadOnly Property HeaderSize As Integer
            Get
                Contract.Ensures(Contract.Result(Of Integer)() = 4)
                Return 4
            End Get
        End Property

        'verification disabled due to stupid verifier (1.2.30118.5)
        <ContractVerification(False)>
        Protected Overrides Function ExtractKey(ByVal header As IReadableList(Of Byte)) As PacketId
            Contract.Assume(header.Count >= 4)
            If header(0) <> Packet.PacketPrefixValue Then Throw New IO.InvalidDataException("Invalid packet header.")
            Return CType(header(1), PacketId)
        End Function
    End Class
End Namespace
