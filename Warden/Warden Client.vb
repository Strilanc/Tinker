Namespace Warden
    Public NotInheritable Class Client
        Inherits FutureDisposable

        Public Event ReceivedWardenData(ByVal sender As Warden.Client, ByVal wardenData As IReadableList(Of Byte))
        Public Event Failed(ByVal sender As Warden.Client, ByVal e As Exception)
        Public Event Disconnected(ByVal sender As Warden.Client, ByVal expected As Boolean, ByVal reason As String)

        Private ReadOnly _socket As IFuture(Of Warden.Socket)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_socket IsNot Nothing)
        End Sub

        Public Sub New(ByVal remoteHost As String,
                       ByVal remotePort As UInt16,
                       ByVal seed As UInt32,
                       ByVal cookie As UInt32,
                       Optional ByVal logger As Logger = Nothing)
            Contract.Assume(remoteHost IsNot Nothing)
            logger = If(logger, New Logger)

            If remoteHost = "" Then
                Dim result = New FutureFunction(Of Warden.Socket)
                result.SetFailed(New ArgumentException("No remote host specified."))
                _socket = result
                Return
            End If

            logger.Log("Connecting to bnls server at {0}:{1}...".Frmt(remoteHost, remotePort), LogMessageType.Positive)

            Dim futureSocket = From tcpClient In AsyncTcpConnect(remoteHost, remotePort)
                               Select New PacketSocket(stream:=tcpClient.GetStream,
                                                       localendpoint:=CType(tcpClient.Client.LocalEndPoint, Net.IPEndPoint),
                                                       remoteendpoint:=CType(tcpClient.Client.RemoteEndPoint, Net.IPEndPoint),
                                                       timeout:=5.Minutes,
                                                       numBytesBeforeSize:=0,
                                                       numSizeBytes:=2,
                                                       logger:=logger,
                                                       Name:="BNLS")
            _socket = From socket In futureSocket
                      Select New Warden.Socket(socket, seed, seed, logger)

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

        Public Function QueueSendWardenData(ByVal wardenData As IReadableList(Of Byte)) As IFuture
            Contract.Requires(wardenData IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ifuture)() IsNot Nothing)
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
