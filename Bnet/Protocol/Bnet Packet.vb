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
        Private ReadOnly _payload As IPickle(Of Object)
        Private ReadOnly _id As PacketId
        Public ReadOnly Property Payload As IPickle(Of Object)
            Get
                Contract.Ensures(Contract.Result(Of IPickle(Of Object))() IsNot Nothing)
                Return _payload
            End Get
        End Property
        Public ReadOnly Property Id As PacketId
            Get
                Return _id
            End Get
        End Property

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_payload IsNot Nothing)
        End Sub

        Public Sub New(ByVal packer As SimplePacketDefinition, ByVal vals As Dictionary(Of InvariantString, Object))
            Contract.Requires(packer IsNot Nothing)
            Contract.Requires(vals IsNot Nothing)
            Contract.Ensures(Me.Id = packer.id)
            Me._id = packer.id
            Me._payload = packer.Pack(vals)
        End Sub
        Public Sub New(ByVal id As PacketId, ByVal payload As IPickle(Of Object))
            Contract.Requires(payload IsNot Nothing)
            Contract.Ensures(Me.Id = id)
            Contract.Ensures(Me.Payload Is payload)
            Me._id = id
            Me._payload = payload
        End Sub
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
            If header(0) <> ServerPackets.PacketPrefixValue Then Throw New IO.InvalidDataException("Invalid packet header.")
            Return CType(header(1), PacketId)
        End Function
    End Class
End Namespace
