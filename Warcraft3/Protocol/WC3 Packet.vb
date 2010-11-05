Imports Tinker.Pickling

Namespace WC3.Protocol
    Public NotInheritable Class Packet
        Private ReadOnly _id As PacketId
        Private ReadOnly _payload As ISimplePickle

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_payload IsNot Nothing)
        End Sub

        Private Sub New(ByVal id As PacketId, ByVal payload As ISimplePickle)
            Contract.Requires(payload IsNot Nothing)
            Me._id = id
            Me._payload = payload
        End Sub

        Public Shared Function FromValue(Of T)(ByVal packetDefinition As Packets.Definition(Of T),
                                               ByVal value As T) As Packet
            Contract.Requires(packetDefinition IsNot Nothing)
            Contract.Requires(value IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(packetDefinition.Id, packetDefinition.Jar.PackPickle(value))
        End Function

        Public ReadOnly Property Payload As ISimplePickle
            Get
                Contract.Ensures(Contract.Result(Of ISimplePickle)() IsNot Nothing)
                Return _payload
            End Get
        End Property
        Public ReadOnly Property Id As PacketId
            Get
                Return _id
            End Get
        End Property
    End Class

    Public Module W3PacketHandler
        Public Function MakeW3PacketHandlerLogger(ByVal sourceName As String, ByVal logger As Logger) As PacketHandlerLogger(Of PacketId)
            Contract.Requires(sourceName IsNot Nothing)
            Contract.Requires(logger IsNot Nothing)
            Dim handler = New PacketHandlerRaw(Of PacketId)(
                HeaderSize:=4,
                keyExtractor:=Function(header)
                                  If header(0) <> Packets.PacketPrefix Then Throw New IO.InvalidDataException("Invalid packet header.")
                                  Return CType(header(1), PacketId)
                              End Function)
            Return New PacketHandlerLogger(Of PacketId)(handler, sourceName, logger)
        End Function
    End Module
End Namespace
