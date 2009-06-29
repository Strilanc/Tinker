Namespace Warcraft3
    Public Enum HostTestResults As Integer
        Fail = -1
        Test = 0
        Pass = 1
    End Enum
    Public Class W3PlayerPingRecord
        Public ReadOnly salt As UInteger
        Public ReadOnly time As ModInt32
        Public Sub New(ByVal salt As UInteger, ByVal time As ModInt32)
            Me.salt = salt
            Me.time = time
        End Sub
    End Class
    Public Enum W3PlayerLeaveTypes As Byte
        Disconnect = 1
        Lose = 7
        MeleeLose = 8
        Win = 9
        Draw = 10
        Observer = 11
        Lobby = 13
    End Enum

    <ContractClass(GetType(ContractClassForIW3Player))>
    Public Interface IW3Player
        ReadOnly Property name() As String
        ReadOnly Property game() As IW3Game
        ReadOnly Property index() As Byte
        ReadOnly Property IsFake() As Boolean
        ReadOnly Property numPeerConnections() As Integer
        ReadOnly Property canHost() As HostTestResults
        ReadOnly Property latency() As Double
        ReadOnly Property peerKey() As UInteger
        ReadOnly Property ListenPort() As UShort
        ReadOnly Property RemoteEndPoint As Net.IPEndPoint
        Property NumAdminTries() As Integer
        Property HasVotedToStart() As Boolean

        Function f_SendPacket(ByVal pk As W3Packet) As IFuture(Of Outcome)
        Function f_QueuePing(ByVal record As W3PlayerPingRecord) As IFuture
        Function Disconnect(ByVal expected As Boolean, ByVal leave_type As W3PlayerLeaveTypes, ByVal reason As String) As IFuture

        ReadOnly Property GetDownloadPercent() As Byte
        Sub ReceivePacket(ByVal id As W3PacketId, ByVal vals As Dictionary(Of String, Object))

        Function Description() As String
        Property IsGettingMapFromBot() As Boolean
        ReadOnly Property MapDownloadPosition() As Integer

        Function f_BufferMap() As IFuture
        Function f_StartCountdown() As IFuture
        ReadOnly Property overcounted() As Boolean
        Function f_StartLoading() As IFuture
        Property Ready() As Boolean
        ReadOnly Property TockTime() As Integer
        Function f_StartPlaying() As IFuture
        Function f_StopPlaying() As IFuture
        Function f_QueueTick(ByVal record As TickRecord) As IFuture
    End Interface
    <ContractClassfor(GetType(IW3Player))>
    Public Class ContractClassForIW3Player
        Implements IW3Player

        Public ReadOnly Property canHost As HostTestResults Implements IW3Player.canHost
            Get
                Throw New NotSupportedException
            End Get
        End Property

        Public Function Disconnect(ByVal expected As Boolean, ByVal leaveType As W3PlayerLeaveTypes, ByVal reason As String) As IFuture Implements IW3Player.Disconnect
            Contract.Requires(reason IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function f_QueuePing(ByVal record As W3PlayerPingRecord) As IFuture Implements IW3Player.f_QueuePing
            Contract.Requires(record IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function f_SendPacket(ByVal pk As W3Packet) As IFuture(Of Outcome) Implements IW3Player.f_SendPacket
            Contract.Requires(pk IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public ReadOnly Property game As IW3Game Implements IW3Player.game
            Get
                Contract.Ensures(Contract.Result(Of IW3Game)() IsNot Nothing)
                Throw New NotSupportedException
            End Get
        End Property

        Public Property HasVotedToStart As Boolean Implements IW3Player.HasVotedToStart
            Get
                Throw New NotSupportedException
            End Get
            Set(ByVal value As Boolean)
                Throw New NotSupportedException
            End Set
        End Property
        Public ReadOnly Property index As Byte Implements IW3Player.index
            Get
                Contract.Ensures(Contract.Result(Of Byte)() > 0)
                Contract.Ensures(Contract.Result(Of Byte)() <= 12)
                Throw New NotSupportedException
            End Get
        End Property
        Public ReadOnly Property IsFake As Boolean Implements IW3Player.IsFake
            Get
                Throw New NotSupportedException
            End Get
        End Property
        Public ReadOnly Property latency As Double Implements IW3Player.latency
            Get
                Contract.Ensures(Contract.Result(Of Double)() >= 0)
                Contract.Ensures(Not Double.IsNaN(Contract.Result(Of Double)()))
                Contract.Ensures(Not Double.IsInfinity(Contract.Result(Of Double)()))
                Throw New NotSupportedException
            End Get
        End Property

        Public ReadOnly Property ListenPort As UShort Implements IW3Player.ListenPort
            Get
                Throw New NotSupportedException
            End Get
        End Property

        Public ReadOnly Property name As String Implements IW3Player.name
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Throw New NotSupportedException
            End Get
        End Property

        Public Property NumAdminTries As Integer Implements IW3Player.NumAdminTries
            Get
                Contract.Ensures(Contract.Result(Of Integer)() >= 0)
                Throw New NotSupportedException
            End Get
            Set(ByVal value As Integer)
                Contract.Requires(value >= 0)
                Throw New NotSupportedException
            End Set
        End Property

        Public ReadOnly Property numPeerConnections As Integer Implements IW3Player.numPeerConnections
            Get
                Contract.Ensures(Contract.Result(Of Integer)() >= 0)
                Contract.Ensures(Contract.Result(Of Integer)() <= 12)
                Throw New NotSupportedException
            End Get
        End Property

        Public ReadOnly Property peerKey As UInteger Implements IW3Player.peerKey
            Get
                Throw New NotSupportedException
            End Get
        End Property

        Public ReadOnly Property RemoteEndPoint As Net.IPEndPoint Implements IW3Player.RemoteEndPoint
            Get
                Contract.Ensures(Contract.Result(Of Net.IPEndPoint)() IsNot Nothing)
                Contract.Ensures(Contract.Result(Of Net.IPEndPoint)().Address IsNot Nothing)
                Throw New NotSupportedException
            End Get
        End Property

        Public Function f_QueueTick(ByVal record As TickRecord) As IFuture Implements IW3Player.f_QueueTick
            Contract.Requires(record IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Throw New NotSupportedException()
        End Function

        Public Function f_StartPlaying() As Functional.Futures.IFuture Implements IW3Player.f_StartPlaying
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Throw New NotSupportedException()
        End Function

        Public Function f_StopPlaying() As Functional.Futures.IFuture Implements IW3Player.f_StopPlaying
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Throw New NotSupportedException()
        End Function

        Public ReadOnly Property TockTime As Integer Implements IW3Player.TockTime
            Get
                Contract.Ensures(Contract.Result(Of Integer)() >= 0)
                Throw New NotSupportedException()
            End Get
        End Property

        Public Function f_StartLoading() As Functional.Futures.IFuture Implements IW3Player.f_StartLoading
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Throw New NotSupportedException()
        End Function

        Public Property Ready As Boolean Implements IW3Player.Ready
            Get
                Throw New NotSupportedException()
            End Get
            Set(ByVal value As Boolean)
                Throw New NotSupportedException()
            End Set
        End Property

        Public ReadOnly Property MapDownloadPosition As Integer Implements IW3Player.MapDownloadPosition
            Get
                Contract.Ensures(Contract.Result(Of Integer)() >= 0)
                Throw New NotSupportedException()
            End Get
        End Property

        Public Function f_BufferMap() As Functional.Futures.IFuture Implements IW3Player.f_BufferMap
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Throw New NotSupportedException()
        End Function

        Public Function f_StartCountdown() As Functional.Futures.IFuture Implements IW3Player.f_StartCountdown
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Throw New NotSupportedException()
        End Function

        Public Property IsGettingMapFromBot As Boolean Implements IW3Player.IsGettingMapFromBot
            Get
                Throw New NotSupportedException()
            End Get
            Set(ByVal value As Boolean)
                Throw New NotSupportedException()
            End Set
        End Property

        Public ReadOnly Property overcounted As Boolean Implements IW3Player.overcounted
            Get
                Throw New NotSupportedException()
            End Get
        End Property

        Public Function Description() As String Implements IW3Player.Description
            Throw New NotSupportedException()
        End Function

        Public ReadOnly Property GetDownloadPercent As Byte Implements IW3Player.GetDownloadPercent
            Get
                Throw New NotSupportedException()
            End Get
        End Property

        Public Sub ReceivePacket(ByVal id As W3PacketId,
                                 ByVal vals As Dictionary(Of String, Object)) Implements IW3Player.ReceivePacket
            Contract.Requires(vals IsNot Nothing)
            Throw New NotSupportedException()
        End Sub
    End Class
End Namespace
