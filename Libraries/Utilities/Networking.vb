Imports System.Net.NetworkInformation
Imports System.Runtime.CompilerServices
Imports System.IO.Path
Imports System.Text
Imports System.Net.Sockets
Imports System.Net

Public Module NetworkingCommon
    Private cachedExternalIP As Byte() = Nothing
    Private cachedInternalIP As Byte() = Nothing
    Public Sub CacheIPAddresses()
        'Internal IP
        Dim addr = (From nic In NetworkInterface.GetAllNetworkInterfaces
                    Where nic.Supports(NetworkInterfaceComponent.IPv4)
                    Select a = (From address In nic.GetIPProperties.UnicastAddresses
                                Where address.Address.AddressFamily = Net.Sockets.AddressFamily.InterNetwork
                                Where address.Address.ToString <> "127.0.0.1").FirstOrDefault()
                    Where a IsNot Nothing).FirstOrDefault()
        If addr IsNot Nothing Then
            cachedInternalIP = addr.Address.GetAddressBytes
        Else
            cachedInternalIP = New Byte() {127, 0, 0, 1}
        End If

        'External IP
        ThreadedAction(
            Sub()
                Dim utf8 = New UTF8Encoding()
                Dim webClient = New WebClient()
                Dim externalIp = utf8.GetString(webClient.DownloadData("http://whatismyip.com/automation/n09230945.asp"))
                If externalIp.Length < 7 OrElse externalIp.Length > 15 Then  Return  'not correct length for style (#.#.#.# to ###.###.###.###)
                Dim words = externalIp.Split("."c)
                If words.Length <> 4 OrElse (From word In words Where Not Byte.TryParse(word, 0)).Any Then  Return
                cachedExternalIP = (From word In words Select Byte.Parse(word)).ToArray()
            End Sub,
            "CacheExternalIP"
        )
    End Sub

    Public Function GetCachedIpAddressBytes(ByVal external As Boolean) As Byte()
        Contract.Ensures(Contract.Result(Of Byte())() IsNot Nothing)
        If cachedInternalIP Is Nothing Then Throw New InvalidOperationException("IP address not cached.")

        If external AndAlso cachedExternalIP IsNot Nothing Then
            Return cachedExternalIP
        Else
            Return cachedInternalIP
        End If
    End Function
    Public Function GetReadableIpFromBytes(ByVal bytes As Byte()) As String
        Contract.Requires(bytes IsNot Nothing)
        Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
        Return String.Join(".", (From b In bytes Select CStr(b)).ToArray)
    End Function

    Public Function FutureConnectTo(ByVal hostname As String, ByVal port As UShort) As IFuture(Of Outcome(Of TcpClient))
        Contract.Requires(hostname IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IFuture(Of Outcome(Of TcpClient)))() IsNot Nothing)
        Dim f = New Future(Of Outcome(Of TcpClient))
        Try
            Dim client = New TcpClient
            client.BeginConnect(hostname, port, Sub(ar)
                                                    Try
                                                        client.EndConnect(ar)
                                                        f.SetValue(successVal(client, "Connected"))
                                                    Catch e As Exception
                                                        f.SetValue(failure("Failed to connect: {0}".frmt(e.Message)))
                                                    End Try
                                                End Sub, Nothing)
        Catch e As Exception
            f.SetValue(failure("Failed to connect: {0}".frmt(e.Message)))
        End Try
        Return f
    End Function
    <Extension()> Public Function FutureAcceptConnection(ByVal listener As TcpListener) As IFuture(Of Outcome(Of TcpClient))
        Contract.Requires(listener IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IFuture(Of Outcome(Of TcpClient)))() IsNot Nothing)
        Dim f As New Future(Of Outcome(Of TcpClient))
        listener.BeginAcceptTcpClient(Sub(ar)
                                          Try
                                              f.SetValue(successVal(listener.EndAcceptTcpClient(ar), "Accepted client"))
                                          Catch e As Exception
                                              f.SetValue(failure("Failed to accept client: {0}".frmt(e.Message)))
                                          End Try
                                      End Sub,
                                      Nothing)
        Return f
    End Function
End Module
