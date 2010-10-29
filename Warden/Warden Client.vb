Namespace Warden
    Public NotInheritable Class Client
        Inherits DisposableWithTask

        Public Event ReceivedWardenData(ByVal sender As Warden.Client, ByVal wardenData As IReadableList(Of Byte))
        Public Event Failed(ByVal sender As Warden.Client, ByVal exception As Exception)
        Public Event Disconnected(ByVal sender As Warden.Client, ByVal expected As Boolean, ByVal reason As String)

        Private ReadOnly _socket As Task(Of Warden.Socket)
        Private ReadOnly _activated As New TaskCompletionSource(Of NoValue)()
        Private ReadOnly _logger As Logger

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_activated IsNot Nothing)
            Contract.Invariant(_socket IsNot Nothing)
            Contract.Invariant(_logger IsNot Nothing)
        End Sub

        Public Sub New(ByVal socket As Task(Of Warden.Socket),
                       ByVal activated As TaskCompletionSource(Of NoValue),
                       ByVal logger As Logger)
            Contract.Requires(socket IsNot Nothing)
            Contract.Requires(activated IsNot Nothing)
            Contract.Requires(logger IsNot Nothing)
            Me._socket = socket
            Me._activated = activated
            Me._logger = logger
        End Sub
        <ContractVerification(False)>
        Public Shared Function MakeMock(ByVal logger As Logger) As Warden.Client
            Contract.Requires(logger IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Warden.Client)() IsNot Nothing)
            Dim failedSocket = New TaskCompletionSource(Of Warden.Socket)
            Dim activated = New TaskCompletionSource(Of NoValue)()
            failedSocket.SetException(New ArgumentException("No remote host specified for bnls server."))
            failedSocket.Task.IgnoreExceptions()
            activated.Task.ContinueWithAction(Sub() logger.Log("Warning: No BNLS server set, but received a Warden packet.", LogMessageType.Problem))
            Return New Warden.Client(failedSocket.Task, activated, logger)
        End Function
        Public Shared Function MakeConnect(ByVal remoteHost As InvariantString,
                                           ByVal remotePort As UInt16,
                                           ByVal seed As UInt32,
                                           ByVal cookie As UInt32,
                                           ByVal clock As IClock,
                                           ByVal logger As Logger) As Warden.Client
            Contract.Requires(clock IsNot Nothing)
            Contract.Requires(logger IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Warden.Client)() IsNot Nothing)

            logger.Log("Connecting to bnls server at {0}:{1}...".Frmt(remoteHost, remotePort), LogMessageType.Positive)

            'Initiate connection
            Dim socket = From tcpClient In AsyncTcpConnect(remoteHost, remotePort)
                         Select packetSocket = New PacketSocket(stream:=tcpClient.GetStream,
                                                                localendpoint:=CType(tcpClient.Client.LocalEndPoint, Net.IPEndPoint),
                                                                remoteendpoint:=CType(tcpClient.Client.RemoteEndPoint, Net.IPEndPoint),
                                                                timeout:=5.Minutes,
                                                                preheaderLength:=0,
                                                                sizeHeaderLength:=2,
                                                                logger:=logger,
                                                                Name:="BNLS",
                                                                clock:=clock)
                         Select New Warden.Socket(socket:=packetSocket,
                                                  seed:=seed,
                                                  cookie:=cookie,
                                                  logger:=logger)
            socket.Catch(
                Sub(exception)
                    logger.Log("Error connecting to bnls server at {0}:{1}: {2}".Frmt(remoteHost, remotePort, exception.Summarize), LogMessageType.Problem)
                    exception.RaiseAsUnexpected("Connecting to bnls server.")
                End Sub
            )

            Dim result = New Warden.Client(socket, New TaskCompletionSource(Of NoValue), logger)
            result.Start()
            Return result
        End Function
        Private Async Sub Start()
            Dim receiveForward As Warden.Socket.ReceivedWardenDataEventHandler =
                    Sub(sender, wardenData) RaiseEvent ReceivedWardenData(Me, wardenData)
            Dim failForward As Warden.Socket.FailedEventHandler =
                    Sub(sender, e) RaiseEvent Failed(Me, e)
            Dim disconnectForward As Warden.Socket.DisconnectedEventHandler =
                    Sub(sender, expected, reason) RaiseEvent Disconnected(Me, expected, reason)

            'Wire events
            Dim wardenClient As Socket
            Try
                wardenClient = Await _socket
            Catch ex As Exception
                'socket creation exceptions are handled elsewhere
                Return
            End Try

            _logger.Log("Connected to bnls server.", LogMessageType.Positive)

            AddHandler wardenClient.ReceivedWardenData, receiveForward
            AddHandler wardenClient.Failed, failForward
            AddHandler wardenClient.Disconnected, disconnectForward

            Await wardenClient.DisposalTask
            RemoveHandler wardenClient.ReceivedWardenData, receiveForward
            RemoveHandler wardenClient.Failed, failForward
            RemoveHandler wardenClient.Disconnected, disconnectForward
        End Sub

        Public ReadOnly Property Activated As Task
            Get
                Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
                Return _activated.Task.AssumeNotNull
            End Get
        End Property

        Public Function QueueSendWardenData(ByVal wardenData As IReadableList(Of Byte)) As Task
            Contract.Requires(wardenData IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            _activated.TrySetResult(Nothing)
            Return _socket.ContinueWithFunc(Function(wardenClient) wardenClient.QueueSendWardenData(wardenData))
        End Function

        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As Task
            If finalizing Then Return Nothing
            Return _socket.ContinueWithAction(Sub(wardenClient) wardenClient.Dispose()).IgnoreExceptions()
        End Function
    End Class
End Namespace
