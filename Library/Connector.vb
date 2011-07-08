Public Interface IConnecter
    Function ConnectAsync(logger As Logger) As Task(Of PacketSocket)
End Interface
Public NotInheritable Class BnetHostPortConnecter
    Implements IConnecter
    Public Const BnetServerPort As UShort = 6112

    Private ReadOnly host As String
    Private ReadOnly port As UInt16
    Private ReadOnly clock As IClock
    Private ReadOnly rng As Random
    Public Sub New(host As String, clock As IClock, Optional port As UInt16? = Nothing, Optional rng As Random = Nothing)
        Contract.Requires(host IsNot Nothing)
        Contract.Requires(clock IsNot Nothing)
        Contract.Requires(rng IsNot Nothing)
        Me.host = host
        Me.port = If(port, BnetServerPort)
        Me.clock = clock
        Me.rng = If(rng, New Random())
    End Sub
    Public Async Function ConnectAsync(logger As Logger) As Task(Of PacketSocket) Implements IConnecter.ConnectAsync
        logger.Log("Connecting to {0}:{1}...".Frmt(host, port), LogMessageType.Typical)
        Dim tcpClient = Await TCPConnectAsync(host, New Random(), port)
        Dim stream = New ThrottledWriteStream(subStream:=tcpClient.GetStream,
                                              initialSlack:=1000,
                                              costEstimator:=Function(data) 100 + data.Length,
                                              costLimit:=400,
                                              costRecoveredPerMillisecond:=0.048,
                                              clock:=clock)
        Return New PacketSocket(stream:=stream,
                                localEndPoint:=DirectCast(tcpClient.Client.LocalEndPoint, Net.IPEndPoint),
                                remoteEndPoint:=DirectCast(tcpClient.Client.RemoteEndPoint, Net.IPEndPoint),
                                clock:=clock,
                                Timeout:=60.Seconds,
                                logger:=logger,
                                bufferSize:=PacketSocket.DefaultBufferSize * 10)
    End Function
End Class
