Namespace WC3
    <ContractClass(GetType(SlotContents.ContractClass))>
    Public MustInherit Class SlotContents
        Public Enum Type
            Empty = 0
            Computer = 1
            Player = 2
        End Enum
        Public Overridable ReadOnly Property ContentType() As Type
            Get
                Return Type.Empty
            End Get
        End Property
        Public Overridable ReadOnly Property Moveable() As Boolean
            Get
                Return True
            End Get
        End Property
        Public Overridable ReadOnly Property PlayerIndex() As PlayerId?
            Get
                Return Nothing
            End Get
        End Property

        Public Overridable ReadOnly Property DataState(Optional receiver As Player = Nothing) As Protocol.SlotState
            Get
                Return Protocol.SlotState.Open
            End Get
        End Property
        Public Overridable ReadOnly Property DataComputerLevel() As Protocol.ComputerLevel
            Get
                Return Protocol.ComputerLevel.Normal
            End Get
        End Property
        Public Overridable ReadOnly Property DataPlayerIndex(Optional receiver As Player = Nothing) As PlayerId?
            Get
                Return Nothing
            End Get
        End Property
        Public Overridable ReadOnly Property DataDownloadPercent(Optional receiver As Player = Nothing) As Byte
            Get
                Return 255
            End Get
        End Property

        Public Enum WantPlayerPriority
            Filled = 0
            Closed = 1
            Open = 2
            ReservationForPlayer = 3
        End Enum
        <Pure()>
        Public Overridable Function WantPlayer(Optional name As InvariantString? = Nothing) As WantPlayerPriority
            Return WantPlayerPriority.Filled
        End Function
        <Pure()>
        Public Overridable Function WithPlayer(player As Player) As SlotContents
            Contract.Requires(player IsNot Nothing)
            Contract.Ensures(Contract.Result(Of SlotContents)() IsNot Nothing)
            Throw New InvalidOperationException()
        End Function
        <Pure()>
        Public Overridable Function WithoutPlayer(player As Player) As SlotContents
            Contract.Requires(player IsNot Nothing)
            Contract.Ensures(Contract.Result(Of SlotContents)() IsNot Nothing)
            Throw New InvalidOperationException()
        End Function
        <Pure()>
        Public Overridable Function EnumPlayers() As IEnumerable(Of Player)
            Contract.Ensures(Contract.Result(Of IEnumerable(Of Player))() IsNot Nothing)
            Return New Player() {}
        End Function

        <Pure()>
        Public MustOverride Function AsyncGenerateDescription() As Task(Of String)
        <Pure()>
        Public Overridable Function Matches(query As InvariantString) As Boolean
            Return False
        End Function

        <ContractClassFor(GetType(SlotContents))>
        Public MustInherit Class ContractClass
            Inherits SlotContents

            Protected Sub New()
                Throw New NotSupportedException
            End Sub
            Public Overrides Function AsyncGenerateDescription() As Task(Of String)
                Contract.Ensures(Contract.Result(Of Task(Of String))() IsNot Nothing)
                Throw New NotSupportedException
            End Function
        End Class
    End Class

    Public Class SlotContentsOpen
        Inherits SlotContents
        Public Overrides Function WithPlayer(player As Player) As SlotContents
            Return New SlotContentsPlayer(player)
        End Function
        Public Overrides Function WantPlayer(Optional name As InvariantString? = Nothing) As WantPlayerPriority
            Return WantPlayerPriority.Open
        End Function
        Public Overrides Function AsyncGenerateDescription() As Task(Of String)
            Return "Open".AsTask
        End Function
    End Class

    Public Class SlotContentsClosed
        Inherits SlotContentsOpen
        Public Overrides Function WantPlayer(Optional name As InvariantString? = Nothing) As WantPlayerPriority
            Return WantPlayerPriority.Closed
        End Function
        Public Overrides Function AsyncGenerateDescription() As Task(Of String)
            Return "Closed".AsTask
        End Function
        Public Overrides ReadOnly Property DataState(Optional receiver As Player = Nothing) As Protocol.SlotState
            Get
                Return Protocol.SlotState.Closed
            End Get
        End Property
    End Class

    Public NotInheritable Class SlotContentsComputer
        Inherits SlotContentsClosed
        Private ReadOnly level As Protocol.ComputerLevel
        Public Sub New(level As Protocol.ComputerLevel)
            Me.level = level
        End Sub
        Public Overrides Function AsyncGenerateDescription() As Task(Of String)
            Return "Computer ({0})".Frmt(DataComputerLevel).AsTask
        End Function
        Public Overrides ReadOnly Property ContentType() As Type
            Get
                Return Type.Computer
            End Get
        End Property
        Public Overrides ReadOnly Property DataComputerLevel() As Protocol.ComputerLevel
            Get
                Return level
            End Get
        End Property
        Public Overrides ReadOnly Property DataState(Optional receiver As Player = Nothing) As Protocol.SlotState
            Get
                Return Protocol.SlotState.Occupied
            End Get
        End Property
    End Class

    Public Class SlotContentsPlayer
        Inherits SlotContents
        Protected ReadOnly _player As Player

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_player IsNot Nothing)
        End Sub

        Public Sub New(player As Player)
            Contract.Requires(player IsNot Nothing)
            Me._player = player
        End Sub
        Public Overrides Function EnumPlayers() As IEnumerable(Of Player)
            Return New Player() {_player}
        End Function
        Public Overrides Function AsyncGenerateDescription() As Task(Of String)
            Return If(_player.IsFake, "(Fake){0} pid={1}".Frmt(_player.Name, _player.Id.Index).AsTask, _player.AsyncDescription.AssumeNotNull())
        End Function
        Public Overrides ReadOnly Property PlayerIndex() As PlayerId?
            Get
                Return _player.Id
            End Get
        End Property
        Public Overrides Function WantPlayer(Optional name As InvariantString? = Nothing) As SlotContents.WantPlayerPriority
            If _player IsNot Nothing AndAlso name IsNot Nothing AndAlso
                                            _player.isFake AndAlso
                                            _player.Name = name.Value Then
                Return WantPlayerPriority.ReservationForPlayer
            Else
                Return WantPlayerPriority.Filled
            End If
        End Function
        Public Overrides Function Matches(query As InvariantString) As Boolean
            Return query = _player.Name
        End Function
        Public Overrides Function WithoutPlayer(player As Player) As SlotContents
            If Me._player Is player Then
                Return New SlotContentsOpen()
            Else
                Throw New InvalidOperationException()
            End If
        End Function
        Public Overrides ReadOnly Property ContentType() As Type
            Get
                Return Type.Player
            End Get
        End Property
        Public Overrides ReadOnly Property DataPlayerIndex(Optional receiver As Player = Nothing) As PlayerId?
            Get
                Return _player.Id
            End Get
        End Property
        Public Overrides ReadOnly Property DataDownloadPercent(Optional receiver As Player = Nothing) As Byte
            Get
                Return _player.AdvertisedDownloadPercent
            End Get
        End Property
        Public Overrides ReadOnly Property DataState(Optional receiver As Player = Nothing) As Protocol.SlotState
            Get
                Return Protocol.SlotState.Occupied
            End Get
        End Property
    End Class

    Public NotInheritable Class SlotContentsCovering
        Inherits SlotContentsPlayer
        Private ReadOnly _coveredSlotId As InvariantString

        Public ReadOnly Property CoveredSlotId As InvariantString
            Get
                Return _coveredSlotId
            End Get
        End Property

        Public Sub New(coveredSlotId As InvariantString, player As Player)
            MyBase.New(player)
            Contract.Requires(player IsNot Nothing)
            Me._coveredSlotId = coveredSlotId
        End Sub
        <SuppressMessage("Microsoft.Contracts", "Ensures-11-60")>
        Public Overrides Async Function AsyncGenerateDescription() As Task(Of String)
            Dim desc = Await MyBase.AsyncGenerateDescription
            Return "[Covering {0}] {1}".Frmt(CoveredSlotId, desc)
        End Function
        Public Overrides Function WithoutPlayer(player As Player) As SlotContents
            Throw New InvalidOperationException()
        End Function
        Public Overrides ReadOnly Property DataDownloadPercent(Optional receiver As Player = Nothing) As Byte
            Get
                If receiver Is Nothing Then Return MyBase.DataDownloadPercent(receiver)
                Return receiver.AdvertisedDownloadPercent
            End Get
        End Property
        Public Overrides ReadOnly Property Moveable() As Boolean
            Get
                Return False
            End Get
        End Property
    End Class

    Public NotInheritable Class SlotContentsCovered
        Inherits SlotContents
        Private ReadOnly _coveringSlotId As InvariantString
        Private ReadOnly _playerIndex As PlayerId
        Private ReadOnly _players As List(Of Player)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_players IsNot Nothing)
        End Sub

        Public Sub New(coveringSlotId As InvariantString,
                       playerIndex As PlayerId,
                       players As IEnumerable(Of Player))
            Contract.Requires(players IsNot Nothing)
            Me._coveringSlotId = coveringSlotId
            Me._players = players.ToList
            Me._playerIndex = playerIndex
        End Sub

        Public ReadOnly Property CoveringSlotId As InvariantString
            Get
                Return _coveringSlotId
            End Get
        End Property
        Public Overrides Function AsyncGenerateDescription() As Task(Of String)
            Return "[Covered by {0}] Players: {1}".Frmt(_coveringSlotId, (From player In _players Select player.Name).StringJoin(", ")).AsTask
        End Function
        Public Overrides Function WithoutPlayer(player As Player) As SlotContents
            If Not _players.Contains(player) Then Throw New InvalidOperationException()
            Return New SlotContentsCovered(_coveringSlotId, _playerIndex, (From p In _players Where p IsNot player))
        End Function
        Public Overrides Function WithPlayer(player As Player) As SlotContents
            If _players.Contains(player) Then Throw New InvalidOperationException()
            Return New SlotContentsCovered(_coveringSlotId, _playerIndex, _players.Concat(New Player() {player}))
        End Function
        Public Overrides ReadOnly Property DataPlayerIndex(Optional receiver As Player = Nothing) As PlayerId?
            Get
                If receiver Is Nothing Then Return Nothing
                Return If(_players.Contains(receiver), receiver.Id, Nothing)
            End Get
        End Property
        Public Overrides ReadOnly Property PlayerIndex() As PlayerId?
            Get
                Return _playerIndex
            End Get
        End Property
        Public Overrides ReadOnly Property DataDownloadPercent(Optional receiver As Player = Nothing) As Byte
            Get
                Return CByte(_players.Count)
            End Get
        End Property
        Public Overrides ReadOnly Property DataState(Optional receiver As Player = Nothing) As Protocol.SlotState
            Get
                Return If(_players.Contains(receiver), Protocol.SlotState.Occupied, Protocol.SlotState.Closed)
            End Get
        End Property
        Public Overrides ReadOnly Property ContentType() As Type
            Get
                Return Type.Player
            End Get
        End Property
        Public Overrides ReadOnly Property Moveable() As Boolean
            Get
                Return False
            End Get
        End Property
    End Class
End Namespace
