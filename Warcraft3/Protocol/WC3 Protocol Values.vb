Imports Tinker.Pickling

Namespace WC3.Protocol
    Public NotInheritable Class KnockData
        Implements IEquatable(Of KnockData)

        Private ReadOnly _gameId As UInt32
        Private ReadOnly _entryKey As UInt32
        Private ReadOnly _unknown As Byte
        Private ReadOnly _listenPort As UInt16
        Private ReadOnly _peerKey As UInt32
        Private ReadOnly _name As InvariantString
        Private ReadOnly _peerData As IRist(Of Byte)
        Private ReadOnly _internalEndPoint As Net.IPEndPoint

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_internalEndPoint IsNot Nothing)
            Contract.Invariant(_internalEndPoint.Address IsNot Nothing)
            Contract.Invariant(_peerData IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As InvariantString,
                       ByVal gameId As UInt32,
                       ByVal peerKey As UInt32,
                       ByVal peerData As IRist(Of Byte),
                       ByVal entryKey As UInt32,
                       ByVal listenPort As UInt16,
                       ByVal internalEndPoint As Net.IPEndPoint,
                       ByVal unknown As Byte)
            Contract.Requires(peerData IsNot Nothing)
            Contract.Requires(internalEndPoint IsNot Nothing)
            Contract.Assume(internalEndPoint.Address IsNot Nothing)
            Me._name = name
            Me._peerKey = peerKey
            Me._peerData = peerData
            Me._listenPort = listenPort
            Me._internalEndPoint = internalEndPoint
            Me._gameId = gameId
            Me._entryKey = entryKey
            Me._unknown = unknown
        End Sub

#Region "Properties"
        Public ReadOnly Property Name As InvariantString
            Get
                Return _name
            End Get
        End Property
        Public ReadOnly Property PeerKey As UInteger
            Get
                Return _peerKey
            End Get
        End Property
        Public ReadOnly Property PeerData As IRist(Of Byte)
            Get
                Contract.Ensures(Contract.Result(Of IRist(Of Byte))() IsNot Nothing)
                Return _peerData
            End Get
        End Property
        Public ReadOnly Property EntryKey As UInteger
            Get
                Return _entryKey
            End Get
        End Property
        Public ReadOnly Property GameId As UInteger
            Get
                Return _gameId
            End Get
        End Property
        Public ReadOnly Property ListenPort As UShort
            Get
                Return _listenPort
            End Get
        End Property
        Public ReadOnly Property InternalEndPoint As Net.IPEndPoint
            Get
                Contract.Ensures(Contract.Result(Of Net.IPEndPoint)() IsNot Nothing)
                Contract.Ensures(Contract.Result(Of Net.IPEndPoint)().Address IsNot Nothing)
                Return _internalEndPoint
            End Get
        End Property
        Public ReadOnly Property Unknown As Byte
            Get
                Return _unknown
            End Get
        End Property
#End Region

        Public Overloads Function Equals(ByVal other As KnockData) As Boolean Implements IEquatable(Of KnockData).Equals
            If other Is Nothing Then Return False
            Return Me.Name = other.Name AndAlso
                   Me.PeerData.SequenceEqual(other.PeerData) AndAlso
                   Me.PeerKey = other.PeerKey AndAlso
                   Me.EntryKey = other.EntryKey AndAlso
                   Me.GameId = other.GameId AndAlso
                   Me.ListenPort = other.ListenPort AndAlso
                   Me.InternalEndPoint.Equals(other.InternalEndPoint) AndAlso
                   Me.Unknown = other.Unknown
        End Function
        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return Me.Equals(TryCast(obj, KnockData))
        End Function
        Public Overrides Function GetHashCode() As Integer
            Return Name.GetHashCode
        End Function
        Public Overrides Function ToString() As String
            Return "{0} joining game {1}".Frmt(Me.Name, Me.GameId)
        End Function
        Public Shared Operator =(ByVal value1 As KnockData, ByVal value2 As KnockData) As Boolean
            If value1 Is Nothing Then Return value2 Is Nothing
            Return value1.Equals(value2)
        End Operator
        Public Shared Operator <>(ByVal value1 As KnockData, ByVal value2 As KnockData) As Boolean
            Return Not value1 = value2
        End Operator
    End Class
    Public NotInheritable Class KnockDataJar
        Inherits BaseConversionJar(Of KnockData, NamedValueMap)

        Private Shared ReadOnly DataJar As New TupleJar(
                New UInt32Jar().Named("game id"),
                New UInt32Jar(showhex:=True).Named("entry key"),
                New ByteJar().Named("unknown value"),
                New UInt16Jar().Named("listen port"),
                New UInt32Jar(showhex:=True).Named("peer key"),
                New UTF8Jar(maxCharCount:=Protocol.MaxPlayerNameLength).NullTerminated.Limited(maxDataCount:=Protocol.MaxSerializedPlayerNameLength).Named("name"),
                New DataJar().DataSizePrefixed(prefixSize:=1).Named("peer data"),
                New Bnet.Protocol.IPEndPointJar().Named("internal address"))

        Public Overrides Function SubJar() As Pickling.IJar(Of NamedValueMap)
            Return DataJar
        End Function
        Public Overrides Function PackRaw(ByVal value As KnockData) As NamedValueMap
            Contract.Assume(value IsNot Nothing)
            Return New Dictionary(Of InvariantString, Object) From {
                    {"game id", value.GameId},
                    {"entry key", value.EntryKey},
                    {"unknown value", value.Unknown},
                    {"listen port", value.ListenPort},
                    {"peer key", value.PeerKey},
                    {"name", value.Name.ToString},
                    {"peer data", value.PeerData},
                    {"internal address", value.InternalEndPoint}}
        End Function
        Public Overrides Function ParseRaw(ByVal value As NamedValueMap) As KnockData
            Contract.Assume(value IsNot Nothing)
            Return New KnockData(GameId:=value.ItemAs(Of UInt32)("game id"),
                                 EntryKey:=value.ItemAs(Of UInt32)("entry key"),
                                 Unknown:=value.ItemAs(Of Byte)("unknown value"),
                                 ListenPort:=value.ItemAs(Of UInt16)("listen port"),
                                 PeerKey:=value.ItemAs(Of UInt32)("peer key"),
                                 Name:=value.ItemAs(Of String)("name"),
                                 PeerData:=value.ItemAs(Of IRist(Of Byte))("peer data"),
                                 InternalEndPoint:=value.ItemAs(Of Net.IPEndPoint)("internal address"))
        End Function
    End Class
End Namespace