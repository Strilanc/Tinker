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
        Private Shared ReadOnly gameDataJar As INamedJar(Of IReadableList(Of NamedValueMap)) = New TupleJar(
                New EnumUInt32Jar(Of WC3.Protocol.GameTypes)().Named("game type"),
                New UInt32Jar().Named("language id"),
                New IPEndPointJar().Named("host address"),
                New EnumUInt32Jar(Of GameStates)().Named("game state"),
                New UInt32Jar().Named("elapsed seconds"),
                New UTF8Jar().NullTerminated.Named("game name"),
                New UTF8Jar().NullTerminated.Named("game password"),
                New TextHexUInt32Jar(digitCount:=1).Named("num free slots"),
                New TextHexUInt32Jar(digitCount:=8).Named("game id"),
                New WC3.Protocol.GameStatsJar().Named("game statstring")).Named("game").RepeatedWithCountPrefix(prefixSize:=4).Named("games")

        Private ReadOnly _clock As IClock

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_clock IsNot Nothing)
        End Sub

        Public Sub New(ByVal clock As IClock)
            Contract.Requires(clock IsNot Nothing)
            Me._clock = clock
        End Sub

        Public Overrides Function Pack(ByVal value As QueryGamesListResponse) As IEnumerable(Of Byte)
            If value.Games.Count = 0 Then
                Return 0UI.Bytes.Concat(queryResultJar.Pack(value.Result))
            Else
                Return gameDataJar.Pack(PackRawGameDescriptions(value.Games))
            End If
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As Pickling.IPickle(Of QueryGamesListResponse)
            If data.Count < 4 Then Throw New PicklingNotEnoughDataException()
            If data.SubView(0, 4).ToUInt32 = 0 Then
                'result of a single-game query
                Dim pickle = queryResultJar.Parse(data.SubView(4))
                Dim value = New QueryGamesListResponse(pickle.Value, {})
                Dim datum = data.SubView(0, 8)
                Return value.Pickled(Me, datum)
            Else
                'result of a game search
                Dim pickle = gameDataJar.Parse(data)
                Dim value = New QueryGamesListResponse(QueryGameResponse.Ok,
                                                       ParseRawGameDescriptions(pickle.Value, _clock))
                Return pickle.With(jar:=Me, value:=value)
            End If
        End Function

        Public Overrides Function Describe(ByVal value As QueryGamesListResponse) As String
            Return MakeListDescription({queryResultJar.Describe(value.Result),
                                        gameDataJar.Describe(PackRawGameDescriptions(value.Games))})
        End Function

        Private Shared Function PackRawGameDescriptions(ByVal games As IEnumerable(Of WC3.RemoteGameDescription)) As IReadableList(Of NamedValueMap)
            Contract.Requires(games IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IReadableList(Of NamedValueMap))() IsNot Nothing)
            Return (From game In games Select PackRawGameDescription(game)).ToReadableList
        End Function
        Private Shared Function ParseRawGameDescriptions(ByVal games As IEnumerable(Of NamedValueMap),
                                                         ByVal clock As IClock) As IReadableList(Of WC3.RemoteGameDescription)
            Contract.Requires(games IsNot Nothing)
            Contract.Requires(clock IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IReadableList(Of WC3.RemoteGameDescription))() IsNot Nothing)
            Return (From game In games Select ParseRawGameDescription(game, clock)).ToReadableList
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

        Public Overrides Function MakeControl() As IValueEditor(Of QueryGamesListResponse)
            Dim resultControl = queryResultJar.MakeControl()
            Dim gamesControl = gameDataJar.MakeControl()
            Dim panel = PanelWithControls({resultControl.Control, gamesControl.Control})
            Return New DelegatedValueEditor(Of QueryGamesListResponse)(
                Control:=panel,
                eventAdder:=Sub(action)
                                AddHandler resultControl.ValueChanged, Sub() action()
                                AddHandler gamesControl.ValueChanged, Sub() action()
                            End Sub,
                getter:=Function() New QueryGamesListResponse(resultControl.Value,
                                                              ParseRawGameDescriptions(gamesControl.Value, _clock)),
                setter:=Sub(value)
                            resultControl.Value = value.Result
                            gamesControl.Value = PackRawGameDescriptions(value.Games)
                        End Sub)
        End Function
    End Class
End Namespace
