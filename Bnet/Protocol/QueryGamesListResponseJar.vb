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
                    Dim vals = pickle.Value
                    Dim totalSlots = CInt(vals.ItemAs(Of UInt32)("num free slots"))
                    Dim usedSlots = 0
                    games.Add(New WC3.RemoteGameDescription(Name:=vals.ItemAs(Of String)("game name"),
                                                            gamestats:=vals.ItemAs(Of WC3.GameStats)("game statstring"),
                                                            location:=vals.ItemAs(Of Net.IPEndPoint)("host address"),
                                                            gameId:=CUInt(vals.ItemAs(Of UInt32)("game id")),
                                                            entryKey:=0,
                                                            totalSlotCount:=totalSlots,
                                                            gameType:=vals.ItemAs(Of WC3.Protocol.GameTypes)("game type"),
                                                            state:=vals.ItemAs(Of GameStates)("game state"),
                                                            usedSlotCount:=usedSlots,
                                                            baseAge:=vals.ItemAs(Of UInt32)("elapsed seconds").Seconds,
                                                            clock:=_clock))
                Next repeat
            End If

            Return New QueryGamesListResponse(result, games).Pickled(data.SubView(0, offset),
                                                                     Function() pickles.MakeListDescription(useSingleLineDescription:=False))
        End Function

        Public Overrides Function Pack(Of TValue As QueryGamesListResponse)(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            Dim pickles = New List(Of ISimplePickle)
            If value.Games.Count = 0 Then
                pickles.Add(queryResultJar.Pack(value.Result))
            Else
                pickles.AddRange(From game In value.Games
                                 Select gameDataJar.Pack(New NamedValueMap(New Dictionary(Of InvariantString, Object) From {
                                     {"game type", game.GameType},
                                     {"language id", 0UI},
                                     {"host address", New Net.IPEndPoint(game.Address, game.Port)},
                                     {"game state", game.GameState},
                                     {"elapsed seconds", CUInt(game.Age.TotalSeconds)},
                                     {"game name", game.Name.ToString},
                                     {"game password", ""},
                                     {"num free slots", CUInt(game.TotalSlotCount - game.UsedSlotCount)},
                                     {"game id", game.GameId},
                                     {"game statstring", game.GameStats}})))
            End If
            Dim data = CUInt(value.Games.Count).Bytes.Concat(Concat(From pickle In pickles Select (pickle.Data))).ToReadableList
            Return value.Pickled(data, Function() pickles.MakeListDescription(useSingleLineDescription:=False))
        End Function
    End Class
End Namespace
