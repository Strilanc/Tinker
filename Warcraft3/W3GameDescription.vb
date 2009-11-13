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
        Implements ILocalGameDescription

        Private ReadOnly _name As String
        Private ReadOnly _gameStats As GameStats
        Private ReadOnly _hostPort As UShort
        Private ReadOnly _gameId As UInt32
        Private ReadOnly _entryKey As UInteger
        Private ReadOnly _creationTick As ModInt32
        Private _gameType As GameTypes
        Private _state As Bnet.Packet.GameStates
        Private ReadOnly _totalSlotCount As Integer
        Private ReadOnly _usedSlotCount As Integer

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_gameId > 0)
            Contract.Invariant(_totalSlotCount > 0)
            Contract.Invariant(_totalSlotCount <= 12)
            Contract.Invariant(_usedSlotCount > 0)
            Contract.Invariant(_usedSlotCount <= _totalSlotCount)
            Contract.Invariant(_gameStats IsNot Nothing)
            Contract.Invariant(_name IsNot Nothing)
        End Sub

        Public Shared Function FromArguments(ByVal name As String,
                                             ByVal map As Map,
                                             ByVal stats As GameStats) As GameDescription
            Contract.Requires(name IsNot Nothing)
            Contract.Requires(map IsNot Nothing)
            Contract.Requires(stats IsNot Nothing)
            Contract.Ensures(Contract.Result(Of GameDescription)() IsNot Nothing)
            Dim totalSlotCount = map.NumPlayerSlots
            If stats.observers = GameObserverOption.FullObservers OrElse stats.observers = GameObserverOption.Referees Then
                totalSlotCount = 12
            End If

            Return New GameDescription(name,
                                         stats,
                                         hostPort:=0,
                                         GameId:=1,
                                         EntryKey:=0,
                                         totalSlotCount:=totalSlotCount,
                                         GameType:=map.GameType,
                                         state:=0,
                                         UsedSlotCount:=0)
        End Function
        Public Sub New(ByVal name As String,
                       ByVal gameStats As GameStats,
                       ByVal hostPort As UShort,
                       ByVal gameId As UInt32,
                       ByVal entryKey As UInteger,
                       ByVal totalSlotCount As Integer,
                       ByVal gameType As GameTypes,
                       ByVal state As Bnet.Packet.GameStates,
                       ByVal usedSlotCount As Integer)
            Contract.Requires(gameId > 0)
            Contract.Requires(totalSlotCount > 0)
            Contract.Requires(totalSlotCount <= 12)
            Contract.Requires(usedSlotCount > 0)
            Contract.Requires(usedSlotCount <= totalSlotCount)
            Contract.Requires(name IsNot Nothing)
            Contract.Requires(gameStats IsNot Nothing)
            Me._name = name
            Me._gameStats = gameStats
            Me._hostPort = hostPort
            Me._gameType = gameType
            Me._gameId = gameId
            Me._entryKey = entryKey
            Me._creationTick = Environment.TickCount
            Me._totalSlotCount = totalSlotCount
            Me._usedSlotCount = usedSlotCount
        End Sub

        Public ReadOnly Property Name As String Implements IGameDescription.Name
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Return _name
            End Get
        End Property
        Public ReadOnly Property GameStats As GameStats Implements IGameDescription.GameStats
            Get
                Contract.Ensures(Contract.Result(Of GameStats)() IsNot Nothing)
                Return _gameStats
            End Get
        End Property
        Public ReadOnly Property GameType As GameTypes Implements IGameDescription.Type
            Get
                Return _gameType
            End Get
        End Property
        Public ReadOnly Property TotalSlotCount() As Integer Implements IGameDescription.TotalSlotCount
            Get
                Return _totalSlotCount
            End Get
        End Property
        Public ReadOnly Property AgeSeconds As UInteger Implements IGameDescription.AgeSeconds
            Get
                Return CUInt(Environment.TickCount - _creationTick) \ 1000UI
            End Get
        End Property
        Public ReadOnly Property GameId As UInteger Implements IGameDescription.GameId
            Get
                Return _gameId
            End Get
        End Property
        Public ReadOnly Property UsedSlotCount As Integer Implements IGameDescription.UsedSlotCount
            Get
                Return 0
            End Get
        End Property
        Public Overridable ReadOnly Property GameState As Bnet.Packet.GameStates Implements IGameDescription.State
            Get
                Return _state
            End Get
        End Property
        Public Sub Update(ByVal type As GameTypes, ByVal state As Bnet.Packet.GameStates)
            Me._state = state
            Me._gameType = type
        End Sub
        Public ReadOnly Property Port As UShort Implements ILocalGameDescription.port
            Get
                Return _hostPort
            End Get
        End Property
        Public ReadOnly Property EntryKey As UInteger Implements IGameDescription.EntryKey
            Get
                Return _entryKey
            End Get
        End Property
    End Class

    <ContractClass(GetType(IGameDescription.ContractClass))>
    Public Interface IGameDescription
        'Static
        ReadOnly Property Name As String
        ReadOnly Property EntryKey As UInt32
        ReadOnly Property GameId As UInt32
        ReadOnly Property GameStats As GameStats
        'Dynamic
        ReadOnly Property Type As GameTypes
        ReadOnly Property State As Bnet.Packet.GameStates
        ReadOnly Property TotalSlotCount As Integer
        ReadOnly Property UsedSlotCount As Integer
        ReadOnly Property AgeSeconds As UInteger

        <ContractClassFor(GetType(IGameDescription))>
        Class ContractClass
            Implements IGameDescription
            Public ReadOnly Property AgeSeconds As UInteger Implements IGameDescription.AgeSeconds
                Get
                    Throw New NotSupportedException
                End Get
            End Property
            Public ReadOnly Property Name As String Implements IGameDescription.Name
                Get
                    Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                    Throw New NotSupportedException
                End Get
            End Property
            Public ReadOnly Property TotalSlotCount As Integer Implements IGameDescription.TotalSlotCount
                Get
                    Contract.Ensures(Contract.Result(Of Integer)() >= 0)
                    Contract.Ensures(Contract.Result(Of Integer)() <= 12)
                    Throw New NotSupportedException
                End Get
            End Property
            Public ReadOnly Property UsedSlotCount As Integer Implements IGameDescription.UsedSlotCount
                Get
                    Contract.Ensures(Contract.Result(Of Integer)() >= 0)
                    Contract.Ensures(Contract.Result(Of Integer)() <= TotalSlotCount)
                    Throw New NotSupportedException
                End Get
            End Property
            Public ReadOnly Property GameId As UInteger Implements IGameDescription.GameId
                Get
                    Contract.Ensures(Contract.Result(Of UInteger)() > 0)
                    Throw New NotSupportedException
                End Get
            End Property
            Public ReadOnly Property GameStats As GameStats Implements IGameDescription.GameStats
                Get
                    Throw New NotSupportedException
                End Get
            End Property
            Public ReadOnly Property EntryKey As UInteger Implements IGameDescription.EntryKey
                Get
                    Throw New NotSupportedException
                End Get
            End Property
            Public ReadOnly Property State As Bnet.Packet.GameStates Implements IGameDescription.State
                Get
                    Throw New NotSupportedException
                End Get
            End Property
            Public ReadOnly Property Type As GameTypes Implements IGameDescription.Type
                Get
                    Throw New NotSupportedException
                End Get
            End Property
        End Class
    End Interface
    Public Interface ILocalGameDescription
        Inherits IGameDescription
        'Location
        ReadOnly Property port As UShort
    End Interface
    Public Interface IRemoteGameDescription
        Inherits ILocalGameDescription
        'Location
        ReadOnly Property Address As Net.IPAddress
    End Interface
End Namespace
