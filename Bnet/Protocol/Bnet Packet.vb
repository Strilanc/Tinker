'Tinker - Warcraft 3 game hosting bot
'Copyright (C) 2010 Craig Gidney
'
'This program is free software: you can redistribute it and/or modify
'it under the terms of the GNU General Public License as published by
'the Free Software Foundation, either version 3 of the License, or
'(at your option) any later version.
'
'This program is distributed in the hope that it will be useful,
'but WITHOUT ANY WARRANTY; without even the implied warranty of
'MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
'GNU General Public License for more details.
'You should have received a copy of the GNU General Public License
'along with this program.  If not, see http://www.gnu.org/licenses/

Imports Tinker.Pickling

Namespace Bnet.Protocol
    Public NotInheritable Class Packet
        Private ReadOnly _id As PacketId
        Private ReadOnly _payload As ISimplePickle

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_payload IsNot Nothing)
        End Sub

        Private Sub New(id As PacketId, payload As ISimplePickle)
            Contract.Requires(payload IsNot Nothing)
            Me._id = id
            Me._payload = payload
        End Sub

        Public Shared Function FromValue(Of T)(packetDefinition As Packets.Definition(Of T),
                                               value As T) As Packet
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

    Public Module BnetPacketHandler
        <Pure()>
        Public Function MakeBnetPacketHandlerLogger(logger As Logger) As PacketHandlerLogger(Of PacketId)
            Contract.Requires(logger IsNot Nothing)
            Contract.Ensures(Contract.Result(Of PacketHandlerLogger(Of PacketId))() IsNot Nothing)
            Dim handler = New PacketHandlerRaw(Of PacketId)(
                HeaderSize:=4,
                keyExtractor:=Function(header)
                                  If header(0) <> Packets.PacketPrefixValue Then Throw New IO.InvalidDataException("Invalid packet header.")
                                  Return DirectCast(header(1), PacketId)
                              End Function)
            Return New PacketHandlerLogger(Of PacketId)(handler, "BNET", logger)
        End Function
    End Module
End Namespace
