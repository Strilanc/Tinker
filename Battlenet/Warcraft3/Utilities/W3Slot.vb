Namespace Warcraft3
    Public Class W3Slot
#Region "Enums"
        Public Const OBS_TEAM As Byte = 12
        Public Enum ComputerLevel As Byte
            Easy = 0
            Normal = 1
            Insane = 2
        End Enum
        Public Enum RaceFlags As Byte
            Human = 1
            Orc = 2
            NightElf = 4
            Undead = 8
            Random = 32
            Unlocked = 64
        End Enum
        Public Enum PlayerColor As Byte
            Red = 0
            Blue = 1
            Teal = 2
            Purple = 3
            Yellow = 4
            Orange = 5
            Green = 6
            Pink = 7
            Grey = 8
            LightBlue = 9
            DarkGreen = 10
            Brown = 11
        End Enum
        Public Enum Lock
            unlocked = 0
            sticky = 1
            frozen = 2
        End Enum
#End Region

#Region "Variables"
        Public ReadOnly index As Byte
        Public ReadOnly game As IW3Game
        Public color As PlayerColor
        Public team As Byte
        Public handicap As Byte = 100
        Public race As RaceFlags = RaceFlags.Random
        Public contents As W3SlotContents
        Public locked As Lock
#End Region

        Public Sub New(ByVal game As IW3Game, ByVal index As Byte)
            Me.game = game
            Me.index = index
        End Sub
        Public Enum Match
            None = 0
            Contents = 1
            Color = 2
            Index = 3
        End Enum
        Public ReadOnly Property MatchableId() As String
            Get
                Return (index + 1).ToString
            End Get
        End Property
        Public Function Matches(ByVal query As String) As Match
            If query = (index + 1).ToString Then Return Match.Index
            If query.ToLower = color.ToString.ToLower Then Return Match.Color
            If contents.Matches(query) Then Return Match.Contents
            Return Match.None
        End Function
        Public Function Cloned(ByVal game As IW3Game) As W3Slot
            Dim slot = New W3Slot(game, index)
            slot.color = color
            slot.team = team
            slot.handicap = handicap
            slot.race = race
            slot.contents = contents.Clone(slot)
            slot.locked = locked
            Return slot
        End Function
        Public Overrides Function toString() As String
            Dim s = ""
            If team = OBS_TEAM Then
                s = "Observer"
            Else
                s = "Team {0}, {1}, {2}".frmt(team + 1, race, color)
            End If
            Select Case locked
                Case W3Slot.Lock.frozen
                    s = "(LOCKED) " + s
                Case W3Slot.Lock.sticky
                    s = "(STICKY) " + s
            End Select
            Return padded(s, 30) + contents.ToString()
        End Function
    End Class

    Public MustInherit Class W3SlotContents
        Public ReadOnly parent As W3Slot
        Public Sub New(ByVal parent As W3Slot)
            Me.parent = parent
        End Sub

#Region "Properties"
        Public Enum ContentType
            Empty
            Computer
            Player
        End Enum
        Public Overridable ReadOnly Property Type() As ContentType
            Get
                Return ContentType.Empty
            End Get
        End Property
        Public Overridable ReadOnly Property Moveable() As Boolean
            Get
                Return True
            End Get
        End Property
        Public Overridable ReadOnly Property PlayerIndex() As Byte
            Get
                Return 0
            End Get
        End Property

        Public Enum State As Byte
            Open = 0
            Closed = 1
            Occupied = 2
        End Enum
        Public Overridable ReadOnly Property DataState(ByVal receiver As IW3Player) As State
            Get
                Return State.Open
            End Get
        End Property
        Public Overridable ReadOnly Property DataComputerLevel() As W3Slot.ComputerLevel
            Get
                Return W3Slot.ComputerLevel.Normal
            End Get
        End Property
        Public Overridable ReadOnly Property DataPlayerIndex(ByVal receiver As IW3Player) As Byte
            Get
                Return 0
            End Get
        End Property
        Public Overridable ReadOnly Property DataDownloadPercent(ByVal receiver As IW3Player) As Byte
            Get
                Return 255
            End Get
        End Property
#End Region

#Region "Player Methods"
        Public Enum WantPlayerPriority
            Reject = 0
            Reluctant = 1
            Accept_low = 2
            Accept_medium = 3
            Accept_high = 4
            Accept_reservation = 5
        End Enum
        Public Overridable Function WantPlayer(ByVal name As String) As WantPlayerPriority
            Return WantPlayerPriority.Reject
        End Function
        Public Overridable Function TakePlayer(ByVal player As IW3Player) As W3SlotContents
            Throw New InvalidOperationException()
        End Function
        Public Overridable Function RemovePlayer(ByVal player As IW3Player) As W3SlotContents
            Throw New InvalidOperationException()
        End Function
        Public Overridable Function EnumPlayers() As IEnumerable(Of IW3Player)
            Return New IW3Player() {}
        End Function
#End Region

#Region "Misc Methods"
        Public MustOverride Overrides Function ToString() As String
        Public MustOverride Function Clone(ByVal parent As W3Slot) As W3SlotContents
        Public Overridable Function Matches(ByVal query As String) As Boolean
            Return False
        End Function
#End Region
    End Class

    Public Class W3SlotContentsOpen
        Inherits W3SlotContents
        Public Sub New(ByVal parent As W3Slot)
            MyBase.New(parent)
        End Sub
        Public Overrides Function TakePlayer(ByVal player As IW3Player) As W3SlotContents
            Return New W3SlotContentsPlayer(parent, player)
        End Function
        Public Overrides Function WantPlayer(ByVal name As String) As WantPlayerPriority
            Return WantPlayerPriority.Accept_medium
        End Function
        Public Overrides Function ToString() As String
            Return "Open"
        End Function
        Public Overrides Function Clone(ByVal parent As W3Slot) As W3SlotContents
            Return New W3SlotContentsOpen(parent)
        End Function
    End Class

    Public Class W3SlotContentsClosed
        Inherits W3SlotContentsOpen
        Public Sub New(ByVal parent As W3Slot)
            MyBase.New(parent)
        End Sub
        Public Overrides Function WantPlayer(ByVal name As String) As WantPlayerPriority
            Return WantPlayerPriority.Reluctant
        End Function
        Public Overrides Function ToString() As String
            Return "Closed"
        End Function
        Public Overrides Function Clone(ByVal parent As W3Slot) As W3SlotContents
            Return New W3SlotContentsClosed(parent)
        End Function
        Public Overrides ReadOnly Property DataState(ByVal receiver As IW3Player) As State
            Get
                Return State.Closed
            End Get
        End Property
    End Class

    Public Class W3SlotContentsComputer
        Inherits W3SlotContentsClosed
        Private ReadOnly level As W3Slot.ComputerLevel
        Public Sub New(ByVal parent As W3Slot, ByVal level As W3Slot.ComputerLevel)
            MyBase.New(parent)
            Me.level = level
        End Sub
        Public Overrides Function ToString() As String
            Return "Computer ({0})".frmt(DataComputerLevel)
        End Function
        Public Overrides Function Clone(ByVal parent As W3Slot) As W3SlotContents
            Return New W3SlotContentsComputer(parent, DataComputerLevel)
        End Function
        Public Overrides ReadOnly Property Type() As ContentType
            Get
                Return ContentType.Computer
            End Get
        End Property
        Public Overrides ReadOnly Property DataComputerLevel() As W3Slot.ComputerLevel
            Get
                Return level
            End Get
        End Property
        Public Overrides ReadOnly Property DataState(ByVal receiver As IW3Player) As State
            Get
                Return State.Occupied
            End Get
        End Property
    End Class

    Public Class W3SlotContentsPlayer
        Inherits W3SlotContents
        Protected ReadOnly player As IW3Player
        Public Sub New(ByVal parent As W3Slot, ByVal player As IW3Player)
            MyBase.New(parent)
            If player Is Nothing Then Throw New ArgumentNullException("player")
            Me.player = player
        End Sub
        Public Overrides Function EnumPlayers() As IEnumerable(Of IW3Player)
            Return New IW3Player() {player}
        End Function
        Public Overrides Function ToString() As String
            Return If(player.is_fake, "(Fake)" + player.name, player.soul.Description)
        End Function
        Public Overrides ReadOnly Property PlayerIndex() As Byte
            Get
                Return player.index
            End Get
        End Property
        Public Overrides Function WantPlayer(ByVal name As String) As W3SlotContents.WantPlayerPriority
            If player IsNot Nothing AndAlso name IsNot Nothing AndAlso player.is_fake AndAlso player.name.ToLower = name.ToLower Then
                Return WantPlayerPriority.Accept_reservation
            Else
                Return WantPlayerPriority.Reject
            End If
        End Function
        Public Overrides Function Clone(ByVal parent As W3Slot) As W3SlotContents
            Return New W3SlotContentsPlayer(parent, player)
        End Function
        Public Overrides Function Matches(ByVal query As String) As Boolean
            Return query.ToLower = player.name.ToLower
        End Function
        Public Overrides Function RemovePlayer(ByVal player As IW3Player) As W3SlotContents
            If Me.player Is player Then
                Return New W3SlotContentsOpen(parent)
            Else
                Throw New InvalidOperationException()
            End If
        End Function
        Public Overrides ReadOnly Property Type() As ContentType
            Get
                Return ContentType.Player
            End Get
        End Property
        Public Overrides ReadOnly Property DataPlayerIndex(ByVal receiver As IW3Player) As Byte
            Get
                Return player.index
            End Get
        End Property
        Public Overrides ReadOnly Property DataDownloadPercent(ByVal receiver As IW3Player) As Byte
            Get
                Return player.soul.get_percent_dl
            End Get
        End Property
        Public Overrides ReadOnly Property DataState(ByVal receiver As IW3Player) As State
            Get
                Return State.Occupied
            End Get
        End Property
    End Class

    Public Class W3SlotContentsCovering
        Inherits W3SlotContentsPlayer
        Public ReadOnly covered_slot As W3Slot
        Public Sub New(ByVal parent As W3Slot, ByVal covered_slot As W3Slot, ByVal player As IW3Player)
            MyBase.New(parent, player)
            Me.covered_slot = covered_slot
        End Sub
        Public Overrides Function ToString() As String
            Return "[Covering {0}] {1}".frmt(covered_slot.color, MyBase.ToString)
        End Function
        Public Overrides Function Clone(ByVal parent As W3Slot) As W3SlotContents
            Return New W3SlotContentsCovering(parent, covered_slot, player)
        End Function
        Public Overrides Function RemovePlayer(ByVal player As IW3Player) As W3SlotContents
            Throw New InvalidOperationException()
        End Function
        Public Overrides ReadOnly Property DataDownloadPercent(ByVal receiver As IW3Player) As Byte
            Get
                Return CByte(covered_slot.contents.EnumPlayers.Count)
            End Get
        End Property
        Public Overrides ReadOnly Property Moveable() As Boolean
            Get
                Return False
            End Get
        End Property
    End Class

    Public Class W3SlotContentsCovered
        Inherits W3SlotContents
        Public ReadOnly covering_slot As W3Slot
        Public ReadOnly player_index As Byte
        Protected ReadOnly players As List(Of IW3Player)
        Public Sub New(ByVal parent As W3Slot, ByVal covering_slot As W3Slot, ByVal player_index As Byte, ByVal players As IEnumerable(Of IW3Player))
            MyBase.New(parent)
            Me.covering_slot = covering_slot
            Me.players = players.ToList
            Me.player_index = player_index
        End Sub
        Public Overrides Function ToString() As String
            Return "[Covered by {0}] Players: {1}".frmt(covering_slot.color, String.Join(", ", (From player In players Select player.name).ToArray()))
        End Function
        Public Overrides Function Clone(ByVal parent As W3Slot) As W3SlotContents
            Return New W3SlotContentsCovered(parent, covering_slot, player_index, players)
        End Function
        Public Overrides Function RemovePlayer(ByVal player As IW3Player) As W3SlotContents
            If Not players.Contains(player) Then Throw New InvalidOperationException()
            Return New W3SlotContentsCovered(parent, covering_slot, player_index, (From p In players Where p IsNot player))
        End Function
        Public Overrides Function TakePlayer(ByVal player As IW3Player) As W3SlotContents
            If players.Contains(player) Then Throw New InvalidOperationException()
            Return New W3SlotContentsCovered(parent, covering_slot, player_index, players.Concat(New IW3Player() {player}))
        End Function
        Public Overrides ReadOnly Property DataPlayerIndex(ByVal receiver As IW3Player) As Byte
            Get
                Return If(players.Contains(receiver), receiver.index, CByte(0))
            End Get
        End Property
        Public Overrides ReadOnly Property PlayerIndex() As Byte
            Get
                Return player_index
            End Get
        End Property
        Public Overrides ReadOnly Property DataDownloadPercent(ByVal receiver As IW3Player) As Byte
            Get
                If players.Contains(receiver) Then
                    Return CType(receiver, IW3PlayerLobby).get_percent_dl
                Else
                    Return 100
                End If
            End Get
        End Property
        Public Overrides ReadOnly Property DataState(ByVal receiver As IW3Player) As State
            Get
                Return If(players.Contains(receiver), State.Occupied, State.Closed)
            End Get
        End Property
        Public Overrides ReadOnly Property Type() As ContentType
            Get
                Return ContentType.Player
            End Get
        End Property
        Public Overrides ReadOnly Property Moveable() As Boolean
            Get
                Return False
            End Get
        End Property
    End Class
End Namespace
