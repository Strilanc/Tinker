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

        Private Sub New(ByVal id As PacketId, ByVal payload As ISimplePickle)
            Contract.Requires(payload IsNot Nothing)
            Me._id = id
            Me._payload = payload
        End Sub

        Public Shared Function FromEmpty(ByVal packetDefinition As Packets.Definition) As Packet
            Contract.Requires(packetDefinition IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            If Not TypeOf packetDefinition.Jar Is EmptyJar Then Throw New ArgumentException("Packet definition isn't empty")
            Return New Packet(packetDefinition.Id, packetDefinition.Jar.Pack(New Object))
        End Function
        Public Shared Function FromValue(Of T)(ByVal packetDefinition As Packets.Definition(Of T),
                                               ByVal value As T) As Packet
            Contract.Requires(packetDefinition IsNot Nothing)
            Contract.Requires(value IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(packetDefinition.Id, packetDefinition.Jar.Pack(value))
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

    Public NotInheritable Class BnetPacketHandler
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
        'verification disabled due to stupid verifier (1.2.3.0118.5)
        <ContractVerification(False)>
        Protected Overrides Function ExtractKey(ByVal header As IReadableList(Of Byte)) As PacketId
            If header(0) <> Packets.PacketPrefixValue Then Throw New IO.InvalidDataException("Invalid packet header.")
            Return CType(header(1), PacketId)
        End Function
    End Class
End Namespace
