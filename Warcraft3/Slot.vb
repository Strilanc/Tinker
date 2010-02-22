Namespace WC3
    Public NotInheritable Class Slot
        Public Const ObserverTeamIndex As Byte = 12
        Public Enum LockState
            Unlocked = 0
            Sticky = 1
            Frozen = 2
        End Enum

        Private ReadOnly _index As Byte
        Private ReadOnly _color As Protocol.PlayerColor
        Private ReadOnly _team As Byte
        Private ReadOnly _handicap As Byte
        Private ReadOnly _race As Protocol.Races
        Private ReadOnly _raceUnlocked As Boolean
        Private ReadOnly _locked As LockState
        Private ReadOnly _contents As SlotContents

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_index > 0)
            Contract.Invariant(_index <= 12)
            Contract.Invariant(_team <= 12)
            Contract.Invariant(_contents IsNot Nothing)
        End Sub

        Public Sub New(ByVal index As Byte,
                       ByVal contents As SlotContents,
                       ByVal color As Protocol.PlayerColor,
                       ByVal raceUnlocked As Boolean,
                       ByVal team As Byte,
                       Optional ByVal race As Protocol.Races = Protocol.Races.Random,
                       Optional ByVal handicap As Byte = 100,
                       Optional ByVal locked As LockState = LockState.Unlocked)
            Contract.Requires(index > 0)
            Contract.Requires(index <= 12)
            Contract.Requires(team <= 12)
            Contract.Requires(contents IsNot Nothing)
            Me._index = index
            Me._color = color
            Me._team = team
            Me._handicap = handicap
            Me._race = race
            Me._locked = locked
            Me._raceUnlocked = raceUnlocked
            Me._contents = contents
        End Sub
        Private Sub New(ByVal template As Slot,
                        Optional ByVal index As Byte? = Nothing,
                        Optional ByVal color As Protocol.PlayerColor? = Nothing,
                        Optional ByVal team As Byte? = Nothing,
                        Optional ByVal handicap As Byte? = Nothing,
                        Optional ByVal race As Protocol.Races? = Nothing,
                        Optional ByVal locked As LockState? = Nothing,
                        Optional ByVal raceUnlocked As Boolean? = Nothing,
                        Optional ByVal contents As SlotContents = Nothing)
            Contract.Requires(template IsNot Nothing)
            Me._index = If(index, template._index)
            Me._color = If(color, template._color)
            Me._team = If(team, template._team)
            Me._handicap = If(handicap, template._handicap)
            Me._race = If(race, template._race)
            Me._locked = If(locked, template._locked)
            Me._raceUnlocked = If(raceUnlocked, template._raceUnlocked)
            Me._contents = If(contents, template._contents)
        End Sub

        Public ReadOnly Property Index As Byte
            Get
                Contract.Ensures(Contract.Result(Of Byte)() > 0)
                Contract.Ensures(Contract.Result(Of Byte)() <= 12)
                Return _index
            End Get
        End Property
        Public ReadOnly Property Color As Protocol.PlayerColor
            Get
                Return _color
            End Get
        End Property
        Public ReadOnly Property Team As Byte
            Get
                Contract.Ensures(Contract.Result(Of Byte)() <= 12)
                Return _team
            End Get
        End Property
        Public ReadOnly Property Handicap As Byte
            Get
                Return _handicap
            End Get
        End Property
        Public ReadOnly Property Race As Protocol.Races
            Get
                Return _race
            End Get
        End Property
        Public ReadOnly Property RaceUnlocked As Boolean
            Get
                Return _raceUnlocked
            End Get
        End Property
        Public ReadOnly Property Locked As LockState
            Get
                Return _locked
            End Get
        End Property
        Public ReadOnly Property Contents As SlotContents
            Get
                Contract.Ensures(Contract.Result(Of SlotContents)() IsNot Nothing)
                Return _contents
            End Get
        End Property

        <Pure()>
        Public Function WithIndex(ByVal index As Byte) As Slot
            Contract.Ensures(Contract.Result(Of Slot)() IsNot Nothing)
            Return New Slot(Me, index:=index)
        End Function
        <Pure()>
        Public Function WithColor(ByVal color As Protocol.PlayerColor) As Slot
            Contract.Ensures(Contract.Result(Of Slot)() IsNot Nothing)
            Return New Slot(Me, color:=color)
        End Function
        <Pure()>
        Public Function WithHandicap(ByVal handicap As Byte) As Slot
            Contract.Ensures(Contract.Result(Of Slot)() IsNot Nothing)
            Return New Slot(Me, handicap:=handicap)
        End Function
        <Pure()>
        Public Function WithTeam(ByVal team As Byte) As Slot
            Contract.Ensures(Contract.Result(Of Slot)() IsNot Nothing)
            Return New Slot(Me, team:=team)
        End Function
        <Pure()>
        Public Function WithRace(ByVal race As Protocol.Races) As Slot
            Contract.Ensures(Contract.Result(Of Slot)() IsNot Nothing)
            Return New Slot(Me, race:=race)
        End Function
        <Pure()>
        Public Function WithLock(ByVal locked As LockState) As Slot
            Contract.Ensures(Contract.Result(Of Slot)() IsNot Nothing)
            Return New Slot(Me, locked:=locked)
        End Function
        <Pure()>
        Public Function WithContents(ByVal contents As SlotContents) As Slot
            Contract.Requires(contents IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Slot)() IsNot Nothing)
            Return New Slot(Me, contents:=contents)
        End Function

        Public Enum Match
            None = 0
            Team = 1
            Contents = 2
            Color = 3
            Index = 4
        End Enum
        <Pure()>
        Public Function Matches(ByVal query As InvariantString) As Match
            '[checked in decreasing order of importance]
            If query = (Index + 1).ToString(CultureInfo.InvariantCulture) Then Return Match.Index
            If query = Color.ToString Then Return Match.Color
            If Contents.Matches(query) Then Return Match.Contents
            If query = "team{0}".Frmt(Me.Team + 1) Then Return Match.Team
            If Me.Team = ObserverTeamIndex AndAlso (query = "obs" OrElse query = "observer") Then Return Match.Team
            Return Match.None
        End Function
        Public ReadOnly Property MatchableId() As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Return (Index + 1).ToString(CultureInfo.InvariantCulture)
            End Get
        End Property
        <Pure()>
        Public Function AsyncGenerateDescription() As IFuture(Of String)
            Dim result = ""
            If Locked <> LockState.Unlocked Then result += "({0}) ".Frmt(Locked)
            result += If(Team = ObserverTeamIndex, "Observer", "Team {0}, {1}, {2}".Frmt(Team + 1, Race, Color))
            result = result.Padded(30)
            Return Contents.AsyncGenerateDescription.Select(Function(desc) result + desc)
        End Function
    End Class
End Namespace
