Imports Tinker.Bnet

Namespace CKL
    '''<summary>Asynchronously connects to a CKLServer and requests a response to a cd key authentication challenge from bnet.</summary>
    Public NotInheritable Class Client
        Implements IProductAuthenticator
        Private Shared ReadOnly jar As New Protocol.ProductCredentialsJar()

        Private ReadOnly _logger As Logger
        Private ReadOnly _remoteHost As String
        Private ReadOnly _remotePort As UInt16
        Private ReadOnly _clock As IClock

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_remoteHost IsNot Nothing)
            Contract.Invariant(_clock IsNot Nothing)
            Contract.Invariant(_logger IsNot Nothing)
        End Sub

        Public Sub New(ByVal remoteHost As String,
                       ByVal remotePort As UInt16,
                       ByVal clock As IClock,
                       Optional ByVal logger As Logger = Nothing)
            Contract.Requires(remoteHost IsNot Nothing)
            Contract.Requires(clock IsNot Nothing)
            Me._remoteHost = remoteHost
            Me._remotePort = remotePort
            Me._clock = clock
            Me._logger = If(logger, New Logger)
        End Sub

        <ContractVerification(False)>
        Public Async Function AsyncAuthenticate(ByVal clientSalt As IEnumerable(Of Byte),
                                                ByVal serverSalt As IEnumerable(Of Byte)) As Task(Of ProductCredentialPair) Implements IProductAuthenticator.AsyncAuthenticate

            'Connect to CKL server
            Dim socket = Await PacketSocket.AsyncConnect(_remoteHost, _remotePort, _clock, timeout:=10.Seconds)
            Try
                'Send request
                socket.WritePacket(preheader:={CKL.Server.PacketPrefixValue, CKLPacketId.Keys},
                                   payload:=Concat(clientSalt, serverSalt))
                'Process response
                Dim packet = Await socket.AsyncReadPacket
                Dim result = ParseResponse(packet)

                socket.QueueDisconnect(expected:=True, reason:="Finished")
                _logger.Log("Succesfully borrowed keys from CKL server.", LogMessageType.Positive)
                Return result
            Catch ex As Exception
                socket.QueueDisconnect(expected:=False, reason:=ex.Message)
                Throw
            End Try
        End Function

        Private Shared Function ParseResponse(ByVal packetData As IRist(Of Byte)) As ProductCredentialPair
            Contract.Requires(packetData IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ProductCredentialPair)() IsNot Nothing)

            'Read header
            Contract.Assume(packetData.Count >= 4)
            Dim flag = packetData(0)
            Dim id = packetData(1)
            If flag <> CKL.Server.PacketPrefixValue Then
                Throw New IO.InvalidDataException("Incorrect header id in data returned from CKL server.")
            End If

            'Read body
            Dim body = packetData.SubView(4)
            Select Case DirectCast(id, CKLPacketId)
                Case CKLPacketId.[Error]
                    Throw New IO.IOException("CKL server returned an error: {0}.".Frmt(System.Text.UTF8Encoding.UTF8.GetString(body.ToArray)))
                Case CKLPacketId.Keys
                    Dim rocAuthentication = jar.Parse(body.SubView(0, body.Count \ 2)).Value
                    Dim tftAuthentication = jar.Parse(body.SubView(body.Count \ 2)).Value
                    If rocAuthentication.Product <> Bnet.ProductType.Warcraft3ROC _
                                OrElse tftAuthentication.Product <> Bnet.ProductType.Warcraft3TFT Then
                        Throw New IO.InvalidDataException("CKL server returned invalid credentials.")
                    End If
                    Return New ProductCredentialPair(rocAuthentication, tftAuthentication)
                Case Else
                    Throw New IO.InvalidDataException("Incorrect packet id in data returned from CKL server.")
            End Select
        End Function
    End Class
End Namespace
