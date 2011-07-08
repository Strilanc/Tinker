Imports Tinker.Pickling

Namespace Warden
    Public NotInheritable Class Socket
        Inherits DisposableWithTask

        Public Event ReceivedWardenData(sender As Warden.Socket, wardenData As IRist(Of Byte))
        Private ReadOnly _futureDisc As New TaskCompletionSource(Of Tuple(Of Boolean, String))()
        Private ReadOnly _futureFail As New TaskCompletionSource(Of NoValue)()
        Public ReadOnly Property FutureDisconnected As Task
            Get
                Return _futureDisc.Task
            End Get
        End Property
        Public ReadOnly Property FutureFail As Task
            Get
                Return _futureFail.Task
            End Get
        End Property

        Private ReadOnly inQueue As CallQueue = MakeTaskedCallQueue
        Private ReadOnly outQueue As CallQueue = MakeTaskedCallQueue
        Private ReadOnly _socket As PacketSocket
        Private ReadOnly _logger As Logger
        Private ReadOnly _cookie As UInt32
        Private ReadOnly _seed As UInt32
        Private _connected As Boolean

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(inQueue IsNot Nothing)
            Contract.Invariant(outQueue IsNot Nothing)
            Contract.Invariant(_socket IsNot Nothing)
            Contract.Invariant(_logger IsNot Nothing)
        End Sub

        Public Sub New(socket As PacketSocket,
                       seed As UInt32,
                       cookie As UInt32,
                       Optional logger As Logger = Nothing)
            Contract.Assume(socket IsNot Nothing)
            Me._logger = If(logger, New Logger())
            Me._socket = socket
            Me._cookie = cookie
            Me._seed = seed

            AddHandler _socket.Disconnected, Sub(sender, expected, reason) outQueue.QueueAction(Sub() _futureDisc.TrySetResult(Tuple.Create(expected, reason)))

            Start()
        End Sub

        Private Sub Start()
            WritePacket(ClientPacket.MakeFullServiceConnect(_cookie, _seed))
            BeginReading()
        End Sub

        '''<summary>Asynchronously reads packets until an exception occurs, raising events to the outside.</summary>
        Private Async Sub BeginReading()
            Try
                Do
                    Dim data = Await AsyncReadPacket()
                    Await inQueue.QueueAction(Sub() OnReceivePacket(data))
                Loop
            Catch ex As Exception
                _futureFail.SetException(ex)
            End Try
        End Sub
        Private Async Function AsyncReadPacket() As Task(Of ServerPacket)
            Dim packetData = Await _socket.AsyncReadPacket()

            'Check
            If packetData.Count < 3 Then
                Throw New IO.InvalidDataException("Packet doesn't have a header.")
            ElseIf packetData(2) <> BNLSPacketId.Warden Then
                Throw New IO.InvalidDataException("Not a bnls warden packet.")
            End If

            'Parse, log, return
            Dim pk = ServerPacket.FromData(packetData.SkipExact(3))
            _logger.Log(Function() "Received {0} from {1}".Frmt(pk.Id, _socket.Name), LogMessageType.DataEvent)
            _logger.Log(Function() "Received {0} from {1}: {2}".Frmt(pk.Id, _socket.Name, pk), LogMessageType.DataParsed)
            Return pk
        End Function
        Private Sub OnReceivePacket(packet As ServerPacket)
            Contract.Requires(packet IsNot Nothing)

            If packet.Cookie <> _cookie Then
                Throw New IO.InvalidDataException("Incorrect cookie from BNLS server.")
            ElseIf packet.Result <> 0 Then
                Throw New IO.IOException("BNLS server indicated there was a failure: {0}: ""{1}"".".Frmt(packet.Result,
                                                                                                         packet.ResponseData.ToAsciiChars.AsString))
            End If

            Select Case packet.Id
                Case WardenPacketId.FullServiceConnect
                    If _connected Then Throw New IO.InvalidDataException("Unexpected {0} from {1}.".Frmt(packet.Id, _socket.Name))
                    _connected = True
                Case WardenPacketId.FullServiceHandleWardenPacket
                    If Not _connected Then Throw New IO.InvalidDataException("Unexpected {0} from {1}.".Frmt(packet.Id, _socket.Name))
                    outQueue.QueueAction(Sub() RaiseEvent ReceivedWardenData(Me, packet.ResponseData))
                Case Else
                    Throw New IO.InvalidDataException("Unrecognized packet type received from {0}: {1}.".Frmt(_socket.Name, packet.Id))
            End Select
        End Sub

        Private Sub WritePacket(packet As ClientPacket)
            Contract.Requires(packet IsNot Nothing)

            Try
                _logger.Log(Function() "Sending {0} to {1}".Frmt(packet.Id, _socket.Name), LogMessageType.DataEvent)
                _logger.Log(Function() "Sending {0} to {1}: {2}".Frmt(packet.Id, _socket.Name, packet.Payload.Description), LogMessageType.DataParsed)
                _socket.WritePacket({}, New Byte() {BNLSPacketId.Warden, packet.Id}.Concat(packet.Payload.Data))

            Catch e As Exception
                e.RaiseAsUnexpected("Sending {0} to {1}".Frmt(packet.Id, _socket.Name))
                _socket.QueueDisconnect(expected:=False, reason:="Error sending {0} for {1}: {2}".Frmt(packet.Id, _socket.Name, e))
                Throw
            End Try
        End Sub
        Public Function QueueSendWardenData(wardenData As IRist(Of Byte)) As Task
            Contract.Requires(wardenData IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() WritePacket(ClientPacket.MakeFullServiceHandleWardenPacket(_cookie, wardenData)))
        End Function

        Protected Overrides Async Function PerformDispose(finalizing As Boolean) As Task
            If finalizing Then Return
            Await inQueue.AwaitableEntrance()
            _socket.QueueDisconnect(expected:=True, reason:="Disposed")
            _futureFail.TrySetResult(Nothing)
        End Function
    End Class
End Namespace
