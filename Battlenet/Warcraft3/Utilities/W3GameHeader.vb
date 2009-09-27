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
    Public Enum GameSpeedOption
        Slow
        Medium
        Fast
    End Enum
    Public Enum GameObserverOption
        NoObservers
        ObsOnDefeat
        FullObservers
        Referees
    End Enum
    Public Enum GameVisibilityOption
        MapDefault
        AlwaysVisible
        Explored
        HideTerrain
    End Enum

    Public Interface IW3GameDescription
        ReadOnly Property HostUserName As String
        ReadOnly Property CreationTime As Date
        ReadOnly Property Name As String
        ReadOnly Property BnetId As UInteger
        ReadOnly Property Settings As Warcraft3.W3MapSettings
        ReadOnly Property NumPlayerAndObsSlots As Integer

        'ReadOnly Property Password As String
        'ReadOnly Property UnknownValue1 As UInteger '=1023
        'ReadOnly Property Ladder As Boolean
    End Interface
    Public Interface IW3GameStateDescription
        Inherits IW3GameDescription
        ReadOnly Property GameType As GameTypes
        ReadOnly Property GameState As Bnet.BnetPacket.GameStates
        ReadOnly Property NumFreeSlots As Integer
    End Interface

    Public NotInheritable Class W3GameHeaderAndState
        Inherits W3GameHeader
        Implements IW3GameStateDescription
        Public state As Bnet.BnetPacket.GameStates
        Public freeSlotCount As Integer
        Public ReadOnly type As GameTypes
        Public Sub New(ByVal state As Bnet.BnetPacket.GameStates,
                       ByVal header As W3GameHeader,
                       ByVal type As GameTypes,
                       Optional ByVal freeSlotCount As Integer = -1)
            MyBase.New(header.Name, header.HostUserName, header.Map, header.hostPort, CByte(header.GameId), header.lanKey, header.Options, header.NumPlayerSlots)
            Contract.Requires(header IsNot Nothing)
            Me.state = state
            Me.type = type
            Me.freeSlotCount = If(freeSlotCount = -1, NumPlayerAndObsSlots - 1, freeSlotCount)
        End Sub

        Private ReadOnly Property _FreeSlotCount As Integer Implements IW3GameStateDescription.NumFreeSlots
            Get
                Return freeSlotCount
            End Get
        End Property

        Private ReadOnly Property _GameState As Bnet.BnetPacket.GameStates Implements IW3GameStateDescription.GameState
            Get
                Return state
            End Get
        End Property

        Private ReadOnly Property _GameType As GameTypes Implements IW3GameStateDescription.GameType
            Get
                Return type
            End Get
        End Property
    End Class
    Public Class W3GameHeader
        Implements IW3GameDescription

        Private ReadOnly _name As String
        Private ReadOnly _hostUserName As String
        Private ReadOnly _map As W3MapSettings

        Public ReadOnly hostPort As UShort
        Private ReadOnly _gameId As Byte
        Public ReadOnly lanKey As UInteger
        Private ReadOnly _creationTime As Date

        Private ReadOnly _options As IList(Of String)
        Private ReadOnly _numPlayerSlots As Integer
        Public ReadOnly Property NumPlayerSlots As Integer
            Get
                Contract.Ensures(Contract.Result(Of Integer)() > 0)
                Contract.Ensures(Contract.Result(Of Integer)() <= 12)
                Return _numPlayerSlots
            End Get
        End Property
        Public ReadOnly Property Name As String Implements IW3GameDescription.Name
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Return _name
            End Get
        End Property
        Public ReadOnly Property Map As W3MapSettings
            Get
                Contract.Ensures(Contract.Result(Of W3MapSettings)() IsNot Nothing)
                Return _map
            End Get
        End Property
        Public ReadOnly Property Options As IList(Of String)
            Get
                Contract.Ensures(Contract.Result(Of IList(Of String))() IsNot Nothing)
                Return _options
            End Get
        End Property

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_numPlayerSlots > 0)
            Contract.Invariant(_numPlayerSlots <= 12)
            Contract.Invariant(_map IsNot Nothing)
            Contract.Invariant(_options IsNot Nothing)
            Contract.Invariant(_name IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As String,
                       ByVal host As String,
                       ByVal map As W3MapSettings,
                       ByVal hostPort As UShort,
                       ByVal gameId As Byte,
                       ByVal lanKey As UInteger,
                       ByVal options As IList(Of String),
                       ByVal playerSlotCount As Integer)
            Contract.Requires(playerSlotCount > 0)
            Contract.Requires(playerSlotCount <= 12)
            Contract.Requires(name IsNot Nothing)
            Contract.Requires(map IsNot Nothing)
            Contract.Requires(options IsNot Nothing)
            Me._name = name
            Me._hostUserName = host
            Me._map = map
            Me.hostPort = hostPort
            Me._gameId = gameId
            Me.lanKey = lanKey
            Me._options = options
            Me._numPlayerSlots = playerSlotCount
            Me._creationTime = Now()
        End Sub
        Public ReadOnly Property NumPlayerAndObsSlots() As Integer Implements IW3GameDescription.NumPlayerAndObsSlots
            Get
                Contract.Ensures(Contract.Result(Of Integer)() > 0)
                Contract.Ensures(Contract.Result(Of Integer)() <= 12)
                Contract.Ensures(Contract.Result(Of Integer)() >= NumPlayerSlots)
                Select Case Map.observers
                    Case GameObserverOption.FullObservers, GameObserverOption.Referees
                        Return 12
                    Case Else
                        Return NumPlayerSlots
                End Select
            End Get
        End Property

#Region "Interface"
        Public ReadOnly Property CreationTime As Date Implements IW3GameDescription.CreationTime
            Get
                Return _creationTime
            End Get
        End Property
        Public ReadOnly Property GameId As UInteger Implements IW3GameDescription.BnetId
            Get
                Return _gameId
            End Get
        End Property
        Public ReadOnly Property Settings As W3MapSettings Implements IW3GameDescription.Settings
            Get
                Return _map
            End Get
        End Property
        Public ReadOnly Property HostUserName As String Implements IW3GameDescription.HostUserName
            Get
                Return _hostUserName
            End Get
        End Property
#End Region
    End Class
End Namespace