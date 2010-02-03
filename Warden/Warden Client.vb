Namespace Warden
    Public NotInheritable Class Client
        Inherits FutureDisposable

        Public Event ReceivedWardenData(ByVal sender As Warden.Client, ByVal wardenData As IReadableList(Of Byte))
        Public Event Failed(ByVal sender As Warden.Client, ByVal exception As Exception)
        Public Event Disconnected(ByVal sender As Warden.Client, ByVal expected As Boolean, ByVal reason As String)

        Private ReadOnly _socket As IFuture(Of Warden.Socket)
        Private ReadOnly _activated As New FutureAction()
        Private ReadOnly _clock As IClock

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_activated IsNot Nothing)
            Contract.Invariant(_socket IsNot Nothing)
            Contract.Invariant(_clock IsNot Nothing)
        End Sub

        Public Sub New(ByVal remoteHost As InvariantString,
                       ByVal remotePort As UInt16,
                       ByVal seed As UInt32,
                       ByVal cookie As UInt32,
                       ByVal clock As IClock,
                       Optional ByVal logger As Logger = Nothing)
            Contract.Assume(clock IsNot Nothing)
            logger = If(logger, New Logger)
            _activated.SetHandled()
            Me._clock = clock

            If remoteHost = "" Then
                Dim result = New FutureFunction(Of Warden.Socket)
                result.SetFailed(New ArgumentException("No remote host specified."))
                result.SetHandled()
                _activated.CallOnSuccess(Sub() logger.Log("Warning: No BNLS server set, but received a Warden packet.", LogMessageType.Problem)).SetHandled()
                _socket = result
                Return
            End If
            logger.Log("Connecting to bnls server at {0}:{1}...".Frmt(remoteHost, remotePort), LogMessageType.Positive)

            'Initiate connection
            Me._socket = From tcpClient In AsyncTcpConnect(remoteHost, remotePort)
                         Select packetSocket = New PacketSocket(stream:=tcpClient.GetStream,
                                                                localendpoint:=CType(tcpClient.Client.LocalEndPoint, Net.IPEndPoint),
                                                                remoteendpoint:=CType(tcpClient.Client.RemoteEndPoint, Net.IPEndPoint),
                                                                timeout:=5.Minutes,
                                                                numBytesBeforeSize:=0,
                                                                numSizeBytes:=2,
                                                                logger:=logger,
                                                                Name:="BNLS",
                                                                clock:=_clock)
                         Select New Warden.Socket(Socket:=packetSocket,
                                                  seed:=seed,
                                                  cookie:=cookie,
                                                  logger:=logger)

            'Register events (and setup unregister-on-dispose)
            Dim receiveForward As Warden.Socket.ReceivedWardenDataEventHandler = Sub(sender, wardenData) RaiseEvent ReceivedWardenData(Me, wardenData)
            Dim failForward As Warden.Socket.FailedEventHandler = Sub(sender, e) RaiseEvent Failed(Me, e)
            Dim disconnectForward As Warden.Socket.DisconnectedEventHandler = Sub(sender, expected, reason) RaiseEvent Disconnected(Me, expected, reason)
            _socket.CallOnValueSuccess(
                Sub(wardenClient)
                    logger.Log("Connected to bnls server.", LogMessageType.Positive)

                    AddHandler wardenClient.ReceivedWardenData, receiveForward
                    AddHandler wardenClient.Failed, failForward
                    AddHandler wardenClient.Disconnected, disconnectForward
                    wardenClient.FutureDisposed.CallWhenReady(
                        Sub()
                            RemoveHandler wardenClient.ReceivedWardenData, receiveForward
                            RemoveHandler wardenClient.Failed, failForward
                            RemoveHandler wardenClient.Disconnected, disconnectForward
                        End Sub)
                        End Sub
            ).Catch(
                Sub(exception)
                    logger.Log("Error connecting to bnls server at {0}:{1}: {2}".Frmt(remoteHost, remotePort, exception.Message), LogMessageType.Problem)
                    exception.RaiseAsUnexpected("Connecting to bnls server.")
                End Sub
            )
        End Sub

        Public ReadOnly Property Activated As IFuture
            Get
                Contract.Ensures(Contract.Result(Of ifuture)() IsNot Nothing)
                Return _activated
            End Get
        End Property

        Public Function QueueSendWardenData(ByVal wardenData As IReadableList(Of Byte)) As IFuture
            Contract.Requires(wardenData IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ifuture)() IsNot Nothing)
            _activated.TrySetSucceeded()
            Return _socket.Select(Function(wardenClient) wardenClient.QueueSendWardenData(wardenData))
        End Function

        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As Strilbrary.Threading.IFuture
            If finalizing Then Return Nothing
            Dim result = _socket.CallOnValueSuccess(Sub(wardenClient) wardenClient.Dispose())
            result.SetHandled()
            Return result
        End Function
    End Class
End Namespace
