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
                Using webClient = New WebClient()
                    Dim externalIp = New UTF8Encoding().GetString(webClient.DownloadData("http://whatismyip.com/automation/n09230945.asp"))
                    If externalIp.Length < 7 OrElse externalIp.Length > 15 Then  Return  'not correct length for style (#.#.#.# to ###.###.###.###)
                    Dim words = externalIp.Split("."c)
                    If words.Length <> 4 OrElse (From word In words Where Not Byte.TryParse(word, 0)).Any Then  Return
                    cachedExternalIP = (From word In words Select Byte.Parse(word)).ToArray()
                End Using
            End Sub
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

    '''<summary>Asynchronously creates and connects a TcpClient to the given remote endpoint.</summary>
    Public Function FutureCreateConnectedTcpClient(ByVal host As String,
                                                   ByVal port As UShort) As IFuture(Of TcpClient)
        Dim result = New FutureFunction(Of TcpClient)
        Dim client = New TcpClient
        Try
            client.BeginConnect(host:=host, port:=port, state:=Nothing,
                requestCallback:=Sub(ar)
                                     Try
                                         client.EndConnect(ar)
                                         result.SetSucceeded(client)
                                     Catch e As Exception
                                         result.SetFailed(e)
                                     End Try
                                 End Sub)
        Catch e As Exception
            result.SetFailed(e)
        End Try
        Return result
    End Function

    '''<summary>Asynchronously creates and connects a TcpClient to the given remote endpoint.</summary>
    Public Function FutureCreateConnectedTcpClient(ByVal address As Net.IPAddress,
                                                   ByVal port As UShort) As IFuture(Of TcpClient)
        Dim result = New FutureFunction(Of TcpClient)
        Dim client = New TcpClient
        Try
            client.BeginConnect(address:=address, port:=port, state:=Nothing,
                requestCallback:=Sub(ar)
                                     Try
                                         client.EndConnect(ar)
                                         result.SetSucceeded(client)
                                     Catch e As Exception
                                         result.SetFailed(e)
                                     End Try
                                 End Sub)
        Catch e As Exception
            result.SetFailed(e)
        End Try
        Return result
    End Function

    <Extension()>
    Public Function FutureAcceptConnection(ByVal listener As TcpListener) As IFuture(Of TcpClient)
        Contract.Requires(listener IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IFuture(Of TcpClient))() IsNot Nothing)
        Dim result = New FutureFunction(Of TcpClient)
        Try
            listener.BeginAcceptTcpClient(state:=Nothing,
                callback:=Sub(ar) result.SetByEvaluating(Function() listener.EndAcceptTcpClient(ar)))
        Catch e As Exception
            result.SetFailed(e)
        End Try
        Return result
    End Function
End Module
