Imports Tinker.Pickling

Namespace Bnet.Protocol
    Public Class QueryGamesListResponse
        Implements IEquatable(Of QueryGamesListResponse)

        Private ReadOnly _games As IReadableList(Of WC3.RemoteGameDescription)
        Private ReadOnly _result As QueryGameResponse

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_games IsNot Nothing)
        End Sub

        Public Sub New(ByVal result As QueryGameResponse, ByVal games As IEnumerable(Of WC3.RemoteGameDescription))
            Contract.Requires(games IsNot Nothing)
            Me._games = games.ToReadableList
            Me._result = result
        End Sub

        Public ReadOnly Property Games As IReadableList(Of WC3.RemoteGameDescription)
            Get
                Contract.Ensures(Contract.Result(Of IReadableList(Of WC3.RemoteGameDescription))() IsNot Nothing)
                Return _games
            End Get
        End Property
        Public ReadOnly Property Result As QueryGameResponse
            Get
                Return _result
            End Get
        End Property

        Public Overloads Function Equals(ByVal other As QueryGamesListResponse) As Boolean Implements IEquatable(Of QueryGamesListResponse).Equals
            If other Is Nothing Then Return False
            If Me.Result <> other.Result Then Return False
            If Not Me.Games.SequenceEqual(other.Games) Then Return False
            Return True
        End Function
        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return Me.Equals(TryCast(obj, QueryGamesListResponse))
        End Function
        Public Overrides Function GetHashCode() As Integer
            Return _result.GetHashCode Xor _games.Aggregate(0, Function(acc, e) acc Xor e.GetHashCode)
        End Function
    End Class
    Public Class QueryGamesListResponseJar
        Inherits BaseJar(Of QueryGamesListResponse)

        Private Shared ReadOnly queryResultJar As INamedJar(Of QueryGameResponse) = New EnumUInt32Jar(Of QueryGameResponse)().Named("result")
        Private Shared ReadOnly gameDataJar As New TupleJar(
                New EnumUInt32Jar(Of WC3.Protocol.GameTypes)().Named("game type"),
                New UInt32Jar().Named("language id"),
                New IPEndPointJar().Named("host address"),
                New EnumUInt32Jar(Of GameStates)().Named("game state"),
                New UInt32Jar().Named("elapsed seconds"),
                New UTF8Jar().NullTerminated.Named("game name"),
                New UTF8Jar().NullTerminated.Named("game password"),
                New TextHexUInt32Jar(digitCount:=1).Named("num free slots"),
                New TextHexUInt32Jar(digitCount:=8).Named("game id"),
                New WC3.Protocol.GameStatsJar().Named("game statstring"))

        Private ReadOnly _clock As IClock

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_clock IsNot Nothing)
        End Sub

        Public Sub New(ByVal clock As IClock)
            Contract.Requires(clock IsNot Nothing)
            Me._clock = clock
        End Sub

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As Pickling.IPickle(Of QueryGamesListResponse)
            If data.Count < 4 Then Throw New PicklingNotEnoughDataException()
            Dim count = data.SubView(0, 4).ToUInt32
            Dim games = New List(Of WC3.RemoteGameDescription)(capacity:=CInt(count))
            Dim pickles = New List(Of ISimplePickle)(capacity:=CInt(count + 1))
            Dim result = QueryGameResponse.Ok
            Dim offset = 4
            If count = 0 Then
                'result of single-game query
                If data.Count < 8 Then Throw New PicklingNotEnoughDataException()
                result = DirectCast(data.SubView(4, 4).ToUInt32, QueryGameResponse)
                offset += 4
                pickles.Add(queryResultJar.Pack(result))
            Else
                'games matching query
                For Each repeat In count.Range
                    Dim pickle = gameDataJar.Parse(data.SubView(offset))
                    pickles.Add(pickle)
                    offset += pickle.Data.Count
                    games.Add(ParseRawGameDescription(pickle.Value, _clock))
                Next repeat
            End If

            Dim value = New QueryGamesListResponse(result, games)
            Dim datum = data.SubView(0, offset)
            Return value.Pickled(Me, datum, Function() pickles.MakeListDescription(useSingleLineDescription:=False))
        End Function

        Public Overrides Function Pack(Of TValue As QueryGamesListResponse)(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            Dim pickles = New List(Of ISimplePickle)
            If value.Games.Count = 0 Then
                pickles.Add(queryResultJar.Pack(value.Result))
            Else
                pickles.AddRange(From game In value.Games
                                 Select gameDataJar.Pack(PackRawGameDescription(game)))
            End If
            Dim data = CUInt(value.Games.Count).Bytes.Concat(Concat(From pickle In pickles Select (pickle.Data))).ToReadableList
            Return value.Pickled(Me, data, Function() pickles.MakeListDescription(useSingleLineDescription:=False))
        End Function

        Private Shared Function PackRawGameDescription(ByVal game As WC3.RemoteGameDescription) As NamedValueMap
            Contract.Requires(game IsNot Nothing)
            Contract.Ensures(Contract.Result(Of NamedValueMap)() IsNot Nothing)
            Return New Dictionary(Of InvariantString, Object) From {
                    {"game type", game.GameType},
                    {"language id", 0UI},
                    {"host address", New Net.IPEndPoint(game.Address, game.Port)},
                    {"game state", game.GameState},
                    {"elapsed seconds", CUInt(game.Age.TotalSeconds)},
                    {"game name", game.Name.ToString},
                    {"game password", ""},
                    {"num free slots", CUInt(game.TotalSlotCount - game.UsedSlotCount)},
                    {"game id", game.GameId},
                    {"game statstring", game.GameStats}}
        End Function
        Private Shared Function ParseRawGameDescription(ByVal vals As NamedValueMap, ByVal clock As IClock) As WC3.RemoteGameDescription
            Contract.Requires(vals IsNot Nothing)
            Contract.Ensures(Contract.Result(Of WC3.RemoteGameDescription)() IsNot Nothing)
            Dim totalSlots = CInt(vals.ItemAs(Of UInt32)("num free slots"))
            Dim usedSlots = 0
            Return New WC3.RemoteGameDescription(Name:=vals.ItemAs(Of String)("game name"),
                                                 gamestats:=vals.ItemAs(Of WC3.GameStats)("game statstring"),
                                                 location:=vals.ItemAs(Of Net.IPEndPoint)("host address"),
                                                 gameId:=CUInt(vals.ItemAs(Of UInt32)("game id")),
                                                 entryKey:=0,
                                                 totalSlotCount:=totalSlots,
                                                 gameType:=vals.ItemAs(Of WC3.Protocol.GameTypes)("game type"),
                                                 state:=vals.ItemAs(Of GameStates)("game state"),
                                                 usedSlotCount:=usedSlots,
                                                 baseAge:=vals.ItemAs(Of UInt32)("elapsed seconds").Seconds,
                                                 clock:=clock)
        End Function

        Public Overrides Function ValueToControl(ByVal value As QueryGamesListResponse) As Control
            Dim resultControl = queryResultJar.ValueToControl(value.Result)
            Dim gameControls = From game In value.Games
                               Select gameDataJar.ValueToControl(PackRawGameDescription(game))
            Return PanelWithControls({resultControl}.Concat(gameControls),
                                     borderStyle:=BorderStyle.FixedSingle)
        End Function
        Public Overrides Function ControlToValue(ByVal control As Control) As QueryGamesListResponse
            Dim queryResult = queryResultJar.ControlToValue(control.Controls(0))
            Dim gameResults = (From i In control.Controls.Count.Range.Skip(1)
                               Select ParseRawGameDescription(gameDataJar.ControlToValue(control.Controls(i)), _clock)
                               ).Cache
            Return New QueryGamesListResponse(queryResult, gameResults)
        End Function
    End Class
End Namespace
