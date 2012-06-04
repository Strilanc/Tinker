Namespace WC3
    Public Class GameDescription
        Implements IEquatable(Of GameDescription)

        Private ReadOnly _name As InvariantString
        Private ReadOnly _gameStats As GameStats
        Private ReadOnly _gameId As UInt32
        Private ReadOnly _entryKey As UInteger
        Private ReadOnly _ageClock As ClockTimer
        Private ReadOnly _gameType As Protocol.GameTypes
        Private ReadOnly _state As Bnet.Protocol.GameStates
        Private ReadOnly _totalSlotCount As Integer
        Private ReadOnly _usedSlotCount As Integer

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_gameId > 0)
            Contract.Invariant(_totalSlotCount > 0)
            Contract.Invariant(_totalSlotCount <= 12)
            Contract.Invariant(_usedSlotCount >= 0)
            Contract.Invariant(_usedSlotCount <= _totalSlotCount)
            Contract.Invariant(_gameStats IsNot Nothing)
            Contract.Invariant(_ageClock IsNot Nothing)
        End Sub

        Public Sub New(name As InvariantString,
                       gameStats As GameStats,
                       gameId As UInt32,
                       entryKey As UInteger,
                       totalSlotCount As Integer,
                       gameType As Protocol.GameTypes,
                       state As Bnet.Protocol.GameStates,
                       usedSlotCount As Integer,
                       ageClock As ClockTimer)
            Contract.Requires(gameId > 0)
            Contract.Requires(totalSlotCount > 0)
            Contract.Requires(totalSlotCount <= 12)
            Contract.Requires(usedSlotCount >= 0)
            Contract.Requires(usedSlotCount <= totalSlotCount)
            Contract.Requires(gameStats IsNot Nothing)
            Contract.Requires(ageClock IsNot Nothing)
            Me._name = name
            Me._gameStats = gameStats
            Me._gameType = gameType
            Me._gameId = gameId
            Me._entryKey = entryKey
            Me._ageClock = ageClock
            Me._totalSlotCount = totalSlotCount
            Me._usedSlotCount = usedSlotCount
            Me._state = state
        End Sub
        'Verification disabled because verifier doesn't seem to understand assumptions involving null coalescing
        <ContractVerification(False)>
        Public Function [With](Optional name As InvariantString? = Nothing,
                               Optional gameStats As GameStats = Nothing,
                               Optional gameId As UInt32? = Nothing,
                               Optional entryKey As UInt32? = Nothing,
                               Optional totalSlotCount As Integer? = Nothing,
                               Optional gameType As Protocol.GameTypes? = Nothing,
                               Optional state As Bnet.Protocol.GameStates? = Nothing,
                               Optional usedSlotCount As Integer? = Nothing,
                               Optional ageClock As ClockTimer = Nothing) As GameDescription
            Contract.Requires(gameId Is Nothing OrElse gameId.Value > 0)
            Contract.Requires(totalSlotCount Is Nothing OrElse totalSlotCount.Value > 0)
            Contract.Requires(totalSlotCount Is Nothing OrElse totalSlotCount.Value <= 12)
            Contract.Requires(usedSlotCount Is Nothing OrElse usedSlotCount.Value >= 0)
            Contract.Requires(If(usedSlotCount, Me.UsedSlotCount) < If(totalSlotCount, Me.TotalSlotCount))
            Contract.Ensures(Contract.Result(Of GameDescription)() IsNot Nothing)
            Return New GameDescription(If(name, _name),
                                       If(gameStats, _gameStats),
                                       If(gameId, _gameId),
                                       If(entryKey, _entryKey),
                                       If(totalSlotCount, _totalSlotCount),
                                       If(gameType, _gameType),
                                       If(state, _state),
                                       If(usedSlotCount, _usedSlotCount),
                                       If(ageClock, _ageClock))
        End Function

        Public Shared Function FromArguments(name As InvariantString,
                                             map As Map,
                                             stats As GameStats,
                                             ageClock As ClockTimer) As GameDescription
            Contract.Requires(map IsNot Nothing)
            Contract.Requires(stats IsNot Nothing)
            Contract.Requires(ageClock IsNot Nothing)
            Contract.Ensures(Contract.Result(Of GameDescription)() IsNot Nothing)
            Dim totalSlotCount = map.LobbySlots.Count
            If stats.Observers = GameObserverOption.FullObservers OrElse stats.Observers = GameObserverOption.Referees Then
                totalSlotCount = 12
            End If

            Return New GameDescription(name,
                                       stats,
                                       GameId:=1,
                                       EntryKey:=0,
                                       totalSlotCount:=totalSlotCount,
                                       GameType:=map.FilterGameType,
                                       state:=0,
                                       UsedSlotCount:=0,
                                       ageClock:=ageClock)
        End Function

        Public ReadOnly Property Name As InvariantString
            Get
                Return _name
            End Get
        End Property
        Public ReadOnly Property GameStats As GameStats
            Get
                Contract.Ensures(Contract.Result(Of GameStats)() IsNot Nothing)
                Return _gameStats
            End Get
        End Property
        Public ReadOnly Property GameType As Protocol.GameTypes
            Get
                Return _gameType
            End Get
        End Property
        Public ReadOnly Property TotalSlotCount() As Integer
            Get
                Contract.Ensures(Contract.Result(Of Integer)() > 0)
                Contract.Ensures(Contract.Result(Of Integer)() <= 12)
                Return _totalSlotCount
            End Get
        End Property
        Public ReadOnly Property AgeClock As ClockTimer
            Get
                Contract.Ensures(Contract.Result(Of ClockTimer)() IsNot Nothing)
                Return _ageClock
            End Get
        End Property
        Public ReadOnly Property GameId As UInteger
            Get
                Contract.Ensures(Contract.Result(Of UInteger)() > 0)
                Return _gameId
            End Get
        End Property
        Public ReadOnly Property UsedSlotCount As Integer
            Get
                Contract.Ensures(Contract.Result(Of Integer)() >= 0)
                Contract.Ensures(Contract.Result(Of Integer)() <= TotalSlotCount)
                Return _usedSlotCount
            End Get
        End Property
        Public ReadOnly Property GameState As Bnet.Protocol.GameStates
            Get
                Return _state
            End Get
        End Property
        Public ReadOnly Property EntryKey As UInteger
            Get
                Return _entryKey
            End Get
        End Property

        Public Overrides Function GetHashCode() As Integer
            Return GameId.GetHashCode Xor Name.GetHashCode
        End Function
        Public Overrides Function Equals(obj As Object) As Boolean
            Return Me.Equals(TryCast(obj, GameDescription))
        End Function
        Public Overloads Function Equals(other As GameDescription) As Boolean Implements IEquatable(Of GameDescription).Equals
            If other Is Nothing Then Return False
            If other Is Me Then Return True
            If Me.AgeClock.ElapsedTime <> other.AgeClock.ElapsedTime Then Return False
            If Me.EntryKey <> other.EntryKey Then Return False
            If Me.GameId <> other.GameId Then Return False
            If Me.GameState <> other.GameState Then Return False
            If Not Me.GameStats.Equals(other.GameStats) Then Return False
            If Me.GameType <> other.GameType Then Return False
            If Me.Name <> other.Name Then Return False
            If Me.TotalSlotCount <> other.TotalSlotCount Then Return False
            If Me.UsedSlotCount <> other.UsedSlotCount Then Return False
            Return True
        End Function
    End Class
    Public Class LocalGameDescription
        Inherits GameDescription
        Implements IEquatable(Of LocalGameDescription)

        Private ReadOnly _hostPort As UShort

        Public Sub New(name As InvariantString,
                       gameStats As GameStats,
                       hostPort As UShort,
                       gameId As UInt32,
                       entryKey As UInteger,
                       totalSlotCount As Integer,
                       gameType As Protocol.GameTypes,
                       state As Bnet.Protocol.GameStates,
                       usedSlotCount As Integer,
                       ageClock As ClockTimer)
            MyBase.New(name, gameStats, gameId, entryKey, totalSlotCount, gameType, state, usedSlotCount, ageClock)
            Contract.Requires(gameId > 0)
            Contract.Requires(totalSlotCount > 0)
            Contract.Requires(totalSlotCount <= 12)
            Contract.Requires(usedSlotCount >= 0)
            Contract.Requires(usedSlotCount <= totalSlotCount)
            Contract.Requires(gameStats IsNot Nothing)
            Contract.Requires(ageClock IsNot Nothing)
            Me._hostPort = hostPort
        End Sub
        'Verification disabled because verifier doesn't seem to understand assumptions involving null coalescing
        <ContractVerification(False)>
        Public Shadows Function [With](Optional name As InvariantString? = Nothing,
                                       Optional gameStats As GameStats = Nothing,
                                       Optional gameId As UInt32? = Nothing,
                                       Optional entryKey As UInt32? = Nothing,
                                       Optional totalSlotCount As Integer? = Nothing,
                                       Optional gameType As Protocol.GameTypes? = Nothing,
                                       Optional state As Bnet.Protocol.GameStates? = Nothing,
                                       Optional usedSlotCount As Integer? = Nothing,
                                       Optional ageClock As ClockTimer = Nothing,
                                       Optional hostPort As UShort? = Nothing) As LocalGameDescription
            Contract.Requires(gameId Is Nothing OrElse gameId.Value > 0)
            Contract.Requires(totalSlotCount Is Nothing OrElse totalSlotCount.Value > 0)
            Contract.Requires(totalSlotCount Is Nothing OrElse totalSlotCount.Value <= 12)
            Contract.Requires(usedSlotCount Is Nothing OrElse usedSlotCount.Value >= 0)
            Contract.Requires(If(usedSlotCount, Me.UsedSlotCount) < If(totalSlotCount, Me.TotalSlotCount))
            Contract.Ensures(Contract.Result(Of GameDescription)() IsNot Nothing)
            Return New LocalGameDescription(If(name, Me.Name),
                                            If(gameStats, Me.GameStats),
                                            If(hostPort, _hostPort),
                                            If(gameId, Me.GameId),
                                            If(entryKey, Me.EntryKey),
                                            If(totalSlotCount, Me.TotalSlotCount),
                                            If(gameType, Me.GameType),
                                            If(state, Me.GameState),
                                            If(usedSlotCount, Me.UsedSlotCount),
                                            If(ageClock, Me.AgeClock))
        End Function

        Public Shared Shadows Function FromArguments(name As InvariantString,
                                                     map As Map,
                                                     id As UInt32,
                                                     stats As GameStats,
                                                     ageClock As ClockTimer) As LocalGameDescription
            Contract.Requires(map IsNot Nothing)
            Contract.Requires(stats IsNot Nothing)
            Contract.Requires(ageClock IsNot Nothing)
            Contract.Requires(id > 0)
            Contract.Ensures(Contract.Result(Of LocalGameDescription)() IsNot Nothing)
            Dim totalSlotCount = map.LobbySlots.Count
            If stats.Observers = GameObserverOption.FullObservers OrElse stats.Observers = GameObserverOption.Referees Then
                totalSlotCount = 12
            End If

            Return New LocalGameDescription(name,
                                            stats,
                                            GameId:=id,
                                            EntryKey:=0,
                                            totalSlotCount:=totalSlotCount,
                                            GameType:=map.FilterGameType,
                                            state:=0,
                                            UsedSlotCount:=0,
                                            hostPort:=0,
                                            ageClock:=ageClock)
        End Function

        Public ReadOnly Property Port As UShort
            Get
                Return _hostPort
            End Get
        End Property

        Public Overrides Function GetHashCode() As Integer
            Return Port.GetHashCode Xor MyBase.GetHashCode
        End Function
        Public Overrides Function Equals(obj As Object) As Boolean
            Return Me.Equals(TryCast(obj, LocalGameDescription))
        End Function
        Public Overloads Function Equals(other As LocalGameDescription) As Boolean Implements IEquatable(Of LocalGameDescription).Equals
            If other Is Nothing Then Return False
            If Me.Port <> other.Port Then Return False
            Return MyBase.Equals(other)
        End Function
    End Class
    Public Class RemoteGameDescription
        Inherits LocalGameDescription
        Implements IEquatable(Of RemoteGameDescription)

        Private ReadOnly _address As Net.IPAddress

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_address IsNot Nothing)
        End Sub

        Public Sub New(name As InvariantString,
                       gameStats As GameStats,
                       location As Net.IPEndPoint,
                       gameId As UInt32,
                       entryKey As UInteger,
                       totalSlotCount As Integer,
                       gameType As Protocol.GameTypes,
                       state As Bnet.Protocol.GameStates,
                       usedSlotCount As Integer,
                       ageClock As ClockTimer)
            MyBase.New(name, gameStats, CUShort(location.Port), gameId, entryKey, totalSlotCount, gameType, state, usedSlotCount, ageClock)
            Contract.Requires(gameId > 0)
            Contract.Requires(totalSlotCount > 0)
            Contract.Requires(totalSlotCount <= 12)
            Contract.Requires(usedSlotCount >= 0)
            Contract.Requires(usedSlotCount <= totalSlotCount)
            Contract.Requires(gameStats IsNot Nothing)
            Contract.Requires(location IsNot Nothing)
            Contract.Requires(ageClock IsNot Nothing)
            Contract.Assume(location.Address IsNot Nothing)
            Me._address = location.Address
        End Sub
        'Verification disabled because verifier doesn't seem to understand assumptions involving null coalescing
        <ContractVerification(False)>
        Public Shadows Function [With](Optional name As InvariantString? = Nothing,
                                       Optional gameStats As GameStats = Nothing,
                                       Optional gameId As UInt32? = Nothing,
                                       Optional entryKey As UInt32? = Nothing,
                                       Optional totalSlotCount As Integer? = Nothing,
                                       Optional gameType As Protocol.GameTypes? = Nothing,
                                       Optional state As Bnet.Protocol.GameStates? = Nothing,
                                       Optional usedSlotCount As Integer? = Nothing,
                                       Optional ageClock As ClockTimer = Nothing,
                                       Optional location As Net.IPEndPoint = Nothing) As RemoteGameDescription
            Contract.Requires(gameId Is Nothing OrElse gameId.Value > 0)
            Contract.Requires(totalSlotCount Is Nothing OrElse totalSlotCount.Value > 0)
            Contract.Requires(totalSlotCount Is Nothing OrElse totalSlotCount.Value <= 12)
            Contract.Requires(usedSlotCount Is Nothing OrElse usedSlotCount.Value >= 0)
            Contract.Requires(If(usedSlotCount, Me.UsedSlotCount) < If(totalSlotCount, Me.TotalSlotCount))
            Contract.Ensures(Contract.Result(Of GameDescription)() IsNot Nothing)
            Return New RemoteGameDescription(If(name, Me.Name),
                                             If(gameStats, Me.GameStats),
                                             If(location, _address.WithPort(Me.Port)),
                                             If(gameId, Me.GameId),
                                             If(entryKey, Me.EntryKey),
                                             If(totalSlotCount, Me.TotalSlotCount),
                                             If(gameType, Me.GameType),
                                             If(state, Me.GameState),
                                             If(usedSlotCount, Me.UsedSlotCount),
                                             If(ageClock, Me.AgeClock))
        End Function

        Public ReadOnly Property Address As Net.IPAddress
            Get
                Contract.Ensures(Contract.Result(Of Net.IPAddress)() IsNot Nothing)
                Return _address
            End Get
        End Property

        Public Overrides Function GetHashCode() As Integer
            Return MyBase.GetHashCode
        End Function
        Public Overrides Function Equals(obj As Object) As Boolean
            Return Me.Equals(TryCast(obj, RemoteGameDescription))
        End Function
        Public Overloads Function Equals(other As RemoteGameDescription) As Boolean Implements IEquatable(Of RemoteGameDescription).Equals
            If other Is Nothing Then Return False
            If Not Me.Address.GetAddressBytes.SequenceEqual(other.Address.GetAddressBytes) Then Return False
            Return MyBase.Equals(other)
        End Function
    End Class
End Namespace
