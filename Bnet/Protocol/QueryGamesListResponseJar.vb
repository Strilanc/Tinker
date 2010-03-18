Imports Tinker.Pickling

Namespace Bnet.Protocol
    Public Class QueryGamesListResponse
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
    End Class
    Public Class QueryGamesListResponseJar
        Inherits BaseJar(Of QueryGamesListResponse)

        Private Shared ReadOnly gameDataJar As New TupleJar(
                New EnumUInt32Jar(Of WC3.Protocol.GameTypes)().Named("game type"),
                New UInt32Jar().Named("language id"),
                New IPEndPointJar().Named("host address"),
                New EnumUInt32Jar(Of GameStates)().Named("game state"),
                New UInt32Jar().Named("elapsed seconds"),
                New UTF8Jar().NullTerminated.Named("game name"),
                New UTF8Jar().NullTerminated.Named("game password"),
                New TextHexValueJar(digitCount:=1).Named("num free slots"),
                New TextHexValueJar(digitCount:=8).Named("game id"),
                New WC3.Protocol.GameStatsJar().Named("game statstring"))

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As Pickling.IPickle(Of QueryGamesListResponse)
            Dim count = data.SubView(0, 4).ToUInt32
            Dim games = New List(Of WC3.RemoteGameDescription)(capacity:=CInt(count))
            Dim pickles = New List(Of ISimplePickle)(capacity:=CInt(count + 1))
            Dim result = QueryGameResponse.Ok
            Dim offset = 4
            If count = 0 Then
                'result of single-game query
                result = CType(data.SubView(4, 4).ToUInt32, QueryGameResponse)
                offset += 4
                pickles.Add(result.Pickled(data.SubView(4, 4), New Lazy(Of String)(Function() "result: {0}".Frmt(result))))
            Else
                'games matching query
                For Each repeat In count.Range
                    Dim pickle = gameDataJar.Parse(data.SubView(offset))
                    pickles.Add(pickle)
                    offset += pickle.Data.Count
                    Dim vals = pickle.Value
                    Dim totalSlots = CInt(vals("num free slots"))
                    Dim usedSlots = 0
                    games.Add(New WC3.RemoteGameDescription(Name:=CStr(vals("game name")),
                                                            gamestats:=CType(vals("game statstring"), WC3.GameStats),
                                                            location:=CType(vals("host address"), Net.IPEndPoint),
                                                            gameid:=CUInt(vals("game id")),
                                                            entryKey:=0,
                                                            totalSlotCount:=totalSlots,
                                                            gameType:=CType(vals("game type"), WC3.Protocol.GameTypes),
                                                            state:=CType(vals("game state"), GameStates),
                                                            usedSlotCount:=usedSlots,
                                                            baseAge:=CUInt(vals("elapsed seconds")).Seconds,
                                                            clock:=New SystemClock()))
                Next repeat
            End If

            Return New QueryGamesListResponse(result, games).Pickled(data.SubView(0, offset),
                                                                     Function() pickles.MakeListDescription(useSingleLineDescription:=False))
        End Function

        Public Overrides Function Pack(Of TValue As QueryGamesListResponse)(ByVal value As TValue) As Pickling.IPickle(Of TValue)
            Throw New NotImplementedException
        End Function
    End Class
End Namespace
