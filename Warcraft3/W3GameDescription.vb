Namespace WC3
    <Flags()>
    Public Enum GameTypes As UInteger
        CreateGameUnknown0 = 1 << 0 'this bit always seems to be set by wc3

        '''<summary>Setting this bit causes wc3 to check the map and disc if it is not signed by Blizzard</summary>
        AuthenticatedMakerBlizzard = 1 << 3

        PrivateGame = 1 << 11

        MakerUser = 1 << 13
        MakerBlizzard = 1 << 14
        TypeMelee = 1 << 15
        TypeScenario = 1 << 16
        SizeSmall = 1 << 17
        SizeMedium = 1 << 18
        SizeLarge = 1 << 19
        ObsFull = 1 << 20
        ObsOnDeath = 1 << 21
        ObsNone = 1 << 22

        MaskObs = ObsFull Or ObsOnDeath Or ObsNone
        MaskMaker = MakerBlizzard Or MakerUser
        MaskType = TypeMelee Or TypeScenario
        MaskSize = SizeLarge Or SizeMedium Or SizeSmall

        MaskFilterable = MaskObs Or MaskMaker Or MaskType Or MaskSize
    End Enum

    Public Class GameDescription
        Private ReadOnly _name As InvariantString
        Private ReadOnly _gameStats As GameStats
        Private ReadOnly _gameId As UInt32
        Private ReadOnly _entryKey As UInteger
        Private ReadOnly _ageTimer As ITimer
        Private ReadOnly _baseAge As TimeSpan
        Private ReadOnly _gameType As GameTypes
        Private ReadOnly _state As Bnet.Packet.GameStates
        Private ReadOnly _totalSlotCount As Integer
        Private ReadOnly _usedSlotCount As Integer

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_gameId > 0)
            Contract.Invariant(_totalSlotCount > 0)
            Contract.Invariant(_totalSlotCount <= 12)
            Contract.Invariant(_usedSlotCount >= 0)
            Contract.Invariant(_usedSlotCount <= _totalSlotCount)
            Contract.Invariant(_gameStats IsNot Nothing)
        End Sub

        Public Shared Function FromArguments(ByVal name As InvariantString,
                                             ByVal map As Map,
                                             ByVal stats As GameStats) As GameDescription
            Contract.Requires(map IsNot Nothing)
            Contract.Requires(stats IsNot Nothing)
            Contract.Ensures(Contract.Result(Of GameDescription)() IsNot Nothing)
            Dim totalSlotCount = map.NumPlayerSlots
            If stats.observers = GameObserverOption.FullObservers OrElse stats.observers = GameObserverOption.Referees Then
                totalSlotCount = 12
            End If

            Return New GameDescription(name,
                                       stats,
                                       GameId:=1,
                                       EntryKey:=0,
                                       totalSlotCount:=totalSlotCount,
                                       GameType:=map.GameType,
                                       state:=0,
                                       UsedSlotCount:=0)
        End Function
        Public Sub New(ByVal name As InvariantString,
                       ByVal gameStats As GameStats,
                       ByVal gameId As UInt32,
                       ByVal entryKey As UInteger,
                       ByVal totalSlotCount As Integer,
                       ByVal gameType As GameTypes,
                       ByVal state As Bnet.Packet.GameStates,
                       ByVal usedSlotCount As Integer,
                       Optional ByVal baseAge As TimeSpan = Nothing)
            Contract.Requires(gameId > 0)
            Contract.Requires(totalSlotCount > 0)
            Contract.Requires(totalSlotCount <= 12)
            Contract.Requires(usedSlotCount >= 0)
            Contract.Requires(usedSlotCount <= totalSlotCount)
            Contract.Requires(gameStats IsNot Nothing)
            Contract.Requires(baseAge.Ticks >= 0)
            Me._name = name
            Me._gameStats = gameStats
            Me._gameType = gameType
            Me._gameId = gameId
            Me._entryKey = entryKey
            Me._ageTimer = New SystemClock().StartTimer
            Me._baseAge = baseAge
            Me._totalSlotCount = totalSlotCount
            Me._usedSlotCount = usedSlotCount
        End Sub

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
        Public ReadOnly Property GameType As GameTypes
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
        Public ReadOnly Property Age As TimeSpan
            Get
                Return _ageTimer.ElapsedTime + _baseAge
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
        Public Overridable ReadOnly Property GameState As Bnet.Packet.GameStates
            Get
                Return _state
            End Get
        End Property
        Public ReadOnly Property EntryKey As UInteger
            Get
                Return _entryKey
            End Get
        End Property
    End Class
    Public Class LocalGameDescription
        Inherits GameDescription
        Private ReadOnly _hostPort As UShort

        Public Shared Shadows Function FromArguments(ByVal name As InvariantString,
                                                     ByVal map As Map,
                                                     ByVal stats As GameStats) As LocalGameDescription
            Contract.Requires(map IsNot Nothing)
            Contract.Requires(stats IsNot Nothing)
            Contract.Ensures(Contract.Result(Of LocalGameDescription)() IsNot Nothing)
            Dim totalSlotCount = map.NumPlayerSlots
            If stats.observers = GameObserverOption.FullObservers OrElse stats.observers = GameObserverOption.Referees Then
                totalSlotCount = 12
            End If

            Return New LocalGameDescription(name,
                                            stats,
                                            GameId:=1,
                                            EntryKey:=0,
                                            totalSlotCount:=totalSlotCount,
                                            GameType:=map.GameType,
                                            state:=0,
                                            UsedSlotCount:=0,
                                            hostPort:=0)
        End Function

        Public Sub New(ByVal gameDescription As GameDescription,
                       ByVal port As UShort)
            Me.New(gameDescription.Name,
                   gameDescription.GameStats,
                   port,
                   gameDescription.GameId,
                   gameDescription.EntryKey,
                   gameDescription.TotalSlotCount,
                   gameDescription.GameType,
                   gameDescription.GameState,
                   gameDescription.UsedSlotCount,
                   gameDescription.Age)
            Contract.Requires(gameDescription IsNot Nothing)
        End Sub
        Public Sub New(ByVal name As InvariantString,
                       ByVal gameStats As GameStats,
                       ByVal hostPort As UShort,
                       ByVal gameId As UInt32,
                       ByVal entryKey As UInteger,
                       ByVal totalSlotCount As Integer,
                       ByVal gameType As GameTypes,
                       ByVal state As Bnet.Packet.GameStates,
                       ByVal usedSlotCount As Integer,
                       Optional ByVal baseAge As TimeSpan = Nothing)
            MyBase.new(name, gameStats, gameId, entryKey, totalSlotCount, gameType, state, usedSlotCount, baseAge)
            Contract.Requires(gameId > 0)
            Contract.Requires(totalSlotCount > 0)
            Contract.Requires(totalSlotCount <= 12)
            Contract.Requires(usedSlotCount >= 0)
            Contract.Requires(usedSlotCount <= totalSlotCount)
            Contract.Requires(gameStats IsNot Nothing)
            Contract.Requires(baseAge.Ticks >= 0)
            Me._hostPort = hostPort
        End Sub

        Public ReadOnly Property Port As UShort
            Get
                Return _hostPort
            End Get
        End Property
    End Class
    Public Class RemoteGameDescription
        Inherits LocalGameDescription
        Private ReadOnly _address As Net.IPAddress

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_address IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As InvariantString,
                       ByVal gameStats As GameStats,
                       ByVal location As Net.IPEndPoint,
                       ByVal gameId As UInt32,
                       ByVal entryKey As UInteger,
                       ByVal totalSlotCount As Integer,
                       ByVal gameType As GameTypes,
                       ByVal state As Bnet.Packet.GameStates,
                       ByVal usedSlotCount As Integer,
                       Optional ByVal baseAge As TimeSpan = Nothing)
            MyBase.new(name, gameStats, CUShort(location.Port), gameId, entryKey, totalSlotCount, gameType, state, usedSlotCount, baseAge)
            Contract.Requires(gameId > 0)
            Contract.Requires(totalSlotCount > 0)
            Contract.Requires(totalSlotCount <= 12)
            Contract.Requires(usedSlotCount >= 0)
            Contract.Requires(usedSlotCount <= totalSlotCount)
            Contract.Requires(gameStats IsNot Nothing)
            Contract.Requires(location IsNot Nothing)
            Contract.Requires(location.Address IsNot Nothing)
            Contract.Requires(baseAge.Ticks >= 0)
            Me._address = location.Address
        End Sub

        Public ReadOnly Property Address As Net.IPAddress
            Get
                Contract.Ensures(Contract.Result(Of Net.IPAddress)() IsNot Nothing)
                Return _address
            End Get
        End Property
    End Class
End Namespace
