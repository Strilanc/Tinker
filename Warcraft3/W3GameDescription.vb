Imports HostBot.Bnet.BnetPacket

Namespace Warcraft3
    <Flags()>
    Public Enum GameTypes As UInteger
        CreateGameUnknown0 = 1 << 0
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
        Private ReadOnly _name As String
        Private ReadOnly _gameStats As W3GameStats

        Public ReadOnly hostPort As UShort
        Private ReadOnly _gameId As UInt32
        Public ReadOnly lanKey As UInteger
        Private ReadOnly _creationTime As Date
        Private _gameType As GameTypes
        Private _state As Bnet.BnetPacket.GameStates

        Private ReadOnly _options As IList(Of String)
        Private ReadOnly _numPlayerSlots As Integer

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_numPlayerSlots > 0)
            Contract.Invariant(_numPlayerSlots <= 12)
            Contract.Invariant(_gameStats IsNot Nothing)
            Contract.Invariant(_options IsNot Nothing)
            Contract.Invariant(_name IsNot Nothing)
        End Sub

        Public Shared Function FromArguments(ByVal name As String,
                                             ByVal mapArgument As String,
                                             ByVal hostName As String,
                                             ByVal arguments As IList(Of String)) As W3GameDescription
            Contract.Requires(name IsNot Nothing)
            Contract.Requires(mapArgument IsNot Nothing)
            Contract.Requires(hostName IsNot Nothing)
            Contract.Requires(arguments IsNot Nothing)
            Contract.Ensures(Contract.Result(Of W3GameDescription)() IsNot Nothing)
            Dim map = W3Map.FromArgument(mapArgument)
            Return New W3GameDescription(name,
                                         New W3GameStats(map, hostName, arguments),
                                         0, 0, 0, arguments, map.NumPlayerSlots, map.GameType, 0)
        End Function
        Public Sub New(ByVal subDesc As W3GameDescription,
                       Optional ByVal gameId As UInt32? = Nothing,
                       Optional ByVal lanKey As UInt32? = Nothing,
                       Optional ByVal gameType As GameTypes? = Nothing,
                       Optional ByVal state As Bnet.BnetPacket.GameStates? = Nothing)
            Me.New(subDesc.Name,
                   subDesc.GameStats,
                   subDesc.hostPort,
                   If(gameId, subDesc.GameId),
                   If(lanKey, subDesc.lanKey),
                   subDesc.Options,
                   subDesc._numPlayerSlots,
                   If(gameType, subDesc.GameType),
                   If(state, subDesc.GameState))
        End Sub
        Public Sub New(ByVal name As String,
                       ByVal gameStats As W3GameStats,
                       ByVal hostPort As UShort,
                       ByVal gameId As UInt32,
                       ByVal lanKey As UInteger,
                       ByVal options As IList(Of String),
                       ByVal playerSlotCount As Integer,
                       ByVal gameType As GameTypes,
                       ByVal state As Bnet.BnetPacket.GameStates)
            Contract.Requires(playerSlotCount > 0)
            Contract.Requires(playerSlotCount <= 12)
            Contract.Requires(name IsNot Nothing)
            Contract.Requires(gameStats IsNot Nothing)
            Contract.Requires(options IsNot Nothing)
            Me._name = name
            Me._gameStats = gameStats
            Me.hostPort = hostPort
            Me._gameType = gameType
            Me._gameId = gameId
            Me.lanKey = lanKey
            Me._options = options
            Me._numPlayerSlots = playerSlotCount
            Me._creationTime = Now()
        End Sub

        Public ReadOnly Property Name As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Return _name
            End Get
        End Property
        Public ReadOnly Property GameStats As W3GameStats
            Get
                Contract.Ensures(Contract.Result(Of W3GameStats)() IsNot Nothing)
                Return _gameStats
            End Get
        End Property
        Public ReadOnly Property Options As IList(Of String)
            Get
                Contract.Ensures(Contract.Result(Of IList(Of String))() IsNot Nothing)
                Return _options
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
                Select Case GameStats.observers
                    Case GameObserverOption.FullObservers, GameObserverOption.Referees
                        Return 12
                    Case GameObserverOption.NoObservers, GameObserverOption.ObsOnDefeat
                        Return _numPlayerSlots
                    Case Else
                        Throw GameStats.observers.MakeImpossibleValueException()
                End Select
            End Get
        End Property

        Public ReadOnly Property CreationTime As Date
            Get
                Return _creationTime
            End Get
        End Property
        Public ReadOnly Property GameId As UInteger
            Get
                Return _gameId
            End Get
        End Property

        Public ReadOnly Property UsedSlotCount As Integer
            Get
                Contract.Ensures(Contract.Result(Of Integer)() >= 0)
                Contract.Ensures(Contract.Result(Of Integer)() <= TotalSlotCount)
                Return 0
            End Get
        End Property

        Public Overridable ReadOnly Property GameState As Bnet.BnetPacket.GameStates
            Get
                Contract.Ensures(Contract.Result(Of W3GameStats)() IsNot Nothing)
                Return _state
            End Get
        End Property
        Public Sub Update(ByVal type As GameTypes, ByVal state As Bnet.BnetPacket.GameStates)
            Me._state = state
            Me._gameType = type
        End Sub
    End Class
End Namespace
