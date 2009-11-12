Imports HostBot.Bnet.BnetPacket

Namespace Warcraft3
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

    Public Class W3GameDescription
        Implements ILocalGameDescription

        Private ReadOnly _name As String
        Private ReadOnly _gameStats As W3GameStats
        Private ReadOnly _hostPort As UShort
        Private ReadOnly _gameId As UInt32
        Private ReadOnly _entryKey As UInteger
        Private ReadOnly _creationTick As ModInt32
        Private _gameType As GameTypes
        Private _state As Bnet.BnetPacket.GameStates
        Private ReadOnly _totalSlotCount As Integer

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_gameId > 0)
            Contract.Invariant(_totalSlotCount > 0)
            Contract.Invariant(_totalSlotCount <= 12)
            Contract.Invariant(_gameStats IsNot Nothing)
            Contract.Invariant(_name IsNot Nothing)
        End Sub

        Public Shared Function FromArguments(ByVal name As String,
                                             ByVal map As W3Map,
                                             ByVal stats As W3GameStats) As W3GameDescription
            Contract.Requires(name IsNot Nothing)
            Contract.Requires(map IsNot Nothing)
            Contract.Requires(stats IsNot Nothing)
            Contract.Ensures(Contract.Result(Of W3GameDescription)() IsNot Nothing)
            Return New W3GameDescription(name,
                                         stats,
                                         hostPort:=0,
                                         GameId:=1,
                                         entryKey:=0,
                                         playerslotcount:=map.NumPlayerSlots,
                                         GameType:=map.GameType,
                                         state:=0)
        End Function
        Public Sub New(ByVal name As String,
                       ByVal gameStats As W3GameStats,
                       ByVal hostPort As UShort,
                       ByVal gameId As UInt32,
                       ByVal entryKey As UInteger,
                       ByVal playerSlotCount As Integer,
                       ByVal gameType As GameTypes,
                       ByVal state As Bnet.BnetPacket.GameStates)
            Contract.Requires(gameId > 0)
            Contract.Requires(playerSlotCount > 0)
            Contract.Requires(playerSlotCount <= 12)
            Contract.Requires(name IsNot Nothing)
            Contract.Requires(gameStats IsNot Nothing)
            Me._name = name
            Me._gameStats = gameStats
            Me._hostPort = hostPort
            Me._gameType = gameType
            Me._gameId = gameId
            Me._entryKey = entryKey
            Me._creationTick = Environment.TickCount
            Me._totalSlotCount = playerSlotCount
            If gameStats.observers = GameObserverOption.FullObservers OrElse gameStats.observers = GameObserverOption.Referees Then
                Me._totalSlotCount = 12
            End If
        End Sub

        Public ReadOnly Property Name As String Implements ILocalGameDescription.Name
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Return _name
            End Get
        End Property
        Public ReadOnly Property GameStats As W3GameStats Implements ILocalGameDescription.GameStats
            Get
                Contract.Ensures(Contract.Result(Of W3GameStats)() IsNot Nothing)
                Return _gameStats
            End Get
        End Property
        Public ReadOnly Property GameType As GameTypes Implements ILocalGameDescription.Type
            Get
                Return _gameType
            End Get
        End Property
        Public ReadOnly Property TotalSlotCount() As Integer Implements ILocalGameDescription.TotalSlotCount
            Get
                Return _totalSlotCount
            End Get
        End Property
        Public ReadOnly Property AgeSeconds As UInteger Implements ILocalGameDescription.AgeSeconds
            Get
                Return CUInt(Environment.TickCount - _creationTick) \ 1000UI
            End Get
        End Property
        Public ReadOnly Property GameId As UInteger Implements ILocalGameDescription.GameId
            Get
                Return _gameId
            End Get
        End Property
        Public ReadOnly Property UsedSlotCount As Integer Implements ILocalGameDescription.UsedSlotCount
            Get
                Return 0
            End Get
        End Property
        Public Overridable ReadOnly Property GameState As Bnet.BnetPacket.GameStates Implements ILocalGameDescription.State
            Get
                Return _state
            End Get
        End Property
        Public Sub Update(ByVal type As GameTypes, ByVal state As Bnet.BnetPacket.GameStates)
            Me._state = state
            Me._gameType = type
        End Sub
        Public ReadOnly Property Port As UShort Implements ILocalGameDescription.Port
            Get
                Return _hostPort
            End Get
        End Property
        Public ReadOnly Property EntryKey As UInteger Implements ILocalGameDescription.EntryKey
            Get
                Return _entryKey
            End Get
        End Property
    End Class

    Public Interface ILocalGameDescription
        'Location
        ReadOnly Property Port As UInt16
        'Static
        ReadOnly Property Name As String
        ReadOnly Property EntryKey As UInt32
        ReadOnly Property GameId As UInt32
        ReadOnly Property GameStats As W3GameStats
        'Dynamic
        ReadOnly Property Type As GameTypes
        ReadOnly Property State As Bnet.BnetPacket.GameStates
        ReadOnly Property TotalSlotCount As Integer
        ReadOnly Property UsedSlotCount As Integer
        ReadOnly Property AgeSeconds As UInteger

        <ContractClassFor(GetType(ILocalGameDescription))>
        Class ContractClass
            Implements ILocalGameDescription
            Public ReadOnly Property AgeSeconds As UInteger Implements ILocalGameDescription.AgeSeconds
                Get
                    Throw New NotSupportedException
                End Get
            End Property
            Public ReadOnly Property Name As String Implements ILocalGameDescription.Name
                Get
                    Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                    Throw New NotSupportedException
                End Get
            End Property
            Public ReadOnly Property TotalSlotCount As Integer Implements ILocalGameDescription.TotalSlotCount
                Get
                    Contract.Ensures(Contract.Result(Of Integer)() >= 0)
                    Contract.Ensures(Contract.Result(Of Integer)() <= 12)
                    Throw New NotSupportedException
                End Get
            End Property
            Public ReadOnly Property UsedSlotCount As Integer Implements ILocalGameDescription.UsedSlotCount
                Get
                    Contract.Ensures(Contract.Result(Of Integer)() >= 0)
                    Contract.Ensures(Contract.Result(Of Integer)() <= TotalSlotCount)
                    Throw New NotSupportedException
                End Get
            End Property
            Public ReadOnly Property GameId As UInteger Implements ILocalGameDescription.GameId
                Get
                    Contract.Ensures(Contract.Result(Of UInteger)() > 0)
                    Throw New NotSupportedException
                End Get
            End Property
            Public ReadOnly Property GameStats As W3GameStats Implements ILocalGameDescription.GameStats
                Get
                    Throw New NotSupportedException
                End Get
            End Property
            Public ReadOnly Property EntryKey As UInteger Implements ILocalGameDescription.EntryKey
                Get
                    Throw New NotSupportedException
                End Get
            End Property
            Public ReadOnly Property Port As UShort Implements ILocalGameDescription.Port
                Get
                    Throw New NotSupportedException
                End Get
            End Property
            Public ReadOnly Property State As Bnet.BnetPacket.GameStates Implements ILocalGameDescription.State
                Get
                    Throw New NotSupportedException
                End Get
            End Property
            Public ReadOnly Property Type As GameTypes Implements ILocalGameDescription.Type
                Get
                    Throw New NotSupportedException
                End Get
            End Property
        End Class
    End Interface
    Public Interface IRemoteGameDescription
        Inherits ILocalGameDescription
        'Location
        ReadOnly Property Address As Net.IPAddress
    End Interface
End Namespace
