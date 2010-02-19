Imports Tinker.Pickling

Namespace Bnet.Protocol
    Public Class QueryGamesListResponse
        Private ReadOnly _games As WC3.RemoteGameDescription()
        Private ReadOnly _result As QueryGameResponse

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_games IsNot Nothing)
        End Sub

        Public Sub New(ByVal result As QueryGameResponse, ByVal games As IEnumerable(Of WC3.RemoteGameDescription))
            Contract.Requires(games IsNot Nothing)
            Me._games = games.ToArray
            Me._result = result
        End Sub

        Public ReadOnly Property Games As IList(Of WC3.RemoteGameDescription)
            Get
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

        Private Shared ReadOnly gameDataJar As New TupleJar("game",
                New EnumUInt32Jar(Of WC3.Protocol.GameTypes)("game type").Weaken,
                New UInt32Jar("language id").Weaken,
                New IPEndPointJar("host address").Weaken,
                New EnumUInt32Jar(Of GameStates)("game state").Weaken,
                New UInt32Jar("elapsed seconds").Weaken,
                New NullTerminatedStringJar("game name").Weaken,
                New NullTerminatedStringJar("game password").Weaken,
                New TextHexValueJar("num free slots", digitCount:=1).Weaken,
                New TextHexValueJar("game id", digitCount:=8).Weaken,
                New WC3.Protocol.GameStatsJar("game statstring").Weaken)

        Public Sub New()
            MyBase.new(PacketId.QueryGamesList.ToString)
        End Sub

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As Pickling.IPickle(Of QueryGamesListResponse)
            Dim count = data.SubView(0, 4).ToUInt32
            Dim games = New List(Of WC3.RemoteGameDescription)(capacity:=CInt(count))
            Dim pickles = New List(Of IPickle(Of Object))(capacity:=CInt(count + 1))
            Dim result = QueryGameResponse.Ok
            Dim offset = 4
            If count = 0 Then
                'result of single-game query
                result = CType(data.SubView(4, 4).ToUInt32, QueryGameResponse)
                offset += 4
                pickles.Add(New Pickle(Of Object)("Result", result, data.SubView(4, 4)))
            Else
                'games matching query
                For repeat = 1UI To count
                    Dim pickle = gameDataJar.Parse(data.SubView(offset))
                    pickles.Add(pickle)
                    offset += pickle.Data.Count
                    Dim vals = pickle.Value
                    Dim totalSlots = CInt(vals("num free slots"))
                    Dim usedSlots = 0
                    games.Add(New WC3.RemoteGameDescription(Name:=CStr(vals("game name")).AssumeNotNull,
                                                            gamestats:=CType(vals("game statstring"), WC3.GameStats).AssumeNotNull,
                                                            location:=CType(vals("host address"), Net.IPEndPoint).AssumeNotNull,
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

            Return New Pickle(Of QueryGamesListResponse)(jarname:=Me.Name,
                                                         value:=New QueryGamesListResponse(result, games),
                                                         data:=data.SubView(0, offset),
                                                         valueDescription:=Function() Pickle(Of Object).MakeListDescription(pickles))
        End Function

        Public Overrides Function Pack(Of TValue As QueryGamesListResponse)(ByVal value As TValue) As Pickling.IPickle(Of TValue)
            Throw New NotImplementedException
        End Function
    End Class
End Namespace
