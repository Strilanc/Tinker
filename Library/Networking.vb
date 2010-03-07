Imports System.Net.NetworkInformation
Imports System.Runtime.CompilerServices
Imports System.Text
Imports System.Net.Sockets
Imports System.Net
Imports Tinker.Pickling

Public Module NetworkingCommon
    Private cachedExternalIP As Byte()
    Private cachedInternalIP As Byte()
    Public Sub CacheIPAddresses()
        'Internal IP
        cachedInternalIP = GetInternalIPAddress.GetAddressBytes

        'External IP
        ThreadedAction(
            Sub()
                Using webClient = New WebClient()
                    Dim externalIp = New UTF8Encoding().GetString(webClient.DownloadData("http://whatismyip.com/automation/n09230945.asp"))
                    If externalIp.Length < "#.#.#.#".Length Then Return
                    If externalIp.Length > "###.###.###.###".Length Then Return
                    Dim words = externalIp.Split("."c)
                    If words.Length <> 4 Then Return
                    If (From word In words Where Not Byte.TryParse(word, 0)).Any Then Return
                    cachedExternalIP = (From word In words Select Byte.Parse(word, CultureInfo.InvariantCulture)).ToArray()
                End Using
            End Sub
        )
    End Sub

    Private Function GetInternalIPAddress() As Net.IPAddress
        Contract.Ensures(Contract.Result(Of Net.IPAddress)() IsNot Nothing)

        Dim interfaces = NetworkInterface.GetAllNetworkInterfaces
        Contract.Assume(interfaces IsNot Nothing)
        Dim addr = (From nic In interfaces
                    Where nic.Supports(NetworkInterfaceComponent.IPv4)
                    Select a = (From address In nic.GetIPProperties.UnicastAddresses
                                Where address.Address.AddressFamily = Net.Sockets.AddressFamily.InterNetwork
                                Where Not address.Address.GetAddressBytes.SequenceEqual({127, 0, 0, 1})
                                ).FirstOrDefault()
                    Where a IsNot Nothing
                    ).FirstOrDefault()
        If addr IsNot Nothing Then
            Return addr.Address.AssumeNotNull
        Else
            Return Net.IPAddress.Loopback
        End If
    End Function
    Public Function GetCachedIPAddressBytes(ByVal external As Boolean) As Byte()
        Contract.Ensures(Contract.Result(Of Byte())() IsNot Nothing)
        If cachedInternalIP Is Nothing Then Return GetInternalIPAddress().GetAddressBytes

        If external AndAlso cachedExternalIP IsNot Nothing Then
            Return cachedExternalIP
        Else
            Return cachedInternalIP
        End If
    End Function

    '''<summary>Asynchronously creates and connects a TcpClient to the given remote endpoint.</summary>
    <Pure()>
    Public Function AsyncTcpConnect(ByVal host As String,
                                    ByVal port As UShort) As Task(Of TcpClient)
        Contract.Requires(host IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Task(Of TcpClient))() IsNot Nothing)
        Dim result = New TaskCompletionSource(Of TcpClient)
        Dim client = New TcpClient
        result.DependentCall(Sub() client.BeginConnect(host:=host, port:=port, state:=Nothing,
            requestCallback:=Sub(ar) result.SetByEvaluating(
                Function()
                    client.EndConnect(ar)
                    Return client
                End Function)))
        Return result.Task.AssumeNotNull
    End Function

    '''<summary>Asynchronously creates and connects a TcpClient to the given remote endpoint.</summary>
    <Pure()>
    Public Function AsyncTcpConnect(ByVal address As Net.IPAddress,
                                    ByVal port As UShort) As Task(Of TcpClient)
        Contract.Requires(address IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Task(Of TcpClient))() IsNot Nothing)
        Dim result = New TaskCompletionSource(Of TcpClient)
        Dim client = New TcpClient
        result.DependentCall(Sub() client.BeginConnect(address:=address, port:=port, state:=Nothing,
            requestCallback:=Sub(ar) result.SetByEvaluating(
                Function()
                    client.EndConnect(ar)
                    Return client
                End Function)))
        Return result.Task.AssumeNotNull
    End Function

    '''<summary>Asynchronously accepts an incoming connection attempt.</summary>
    <Extension()>
    Public Function AsyncAcceptConnection(ByVal listener As TcpListener) As Task(Of TcpClient)
        Contract.Requires(listener IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Task(Of TcpClient))() IsNot Nothing)
        Dim result = New TaskCompletionSource(Of TcpClient)
        result.DependentCall(Sub() listener.BeginAcceptTcpClient(state:=Nothing,
            callback:=Sub(ar) result.SetByEvaluating(Function() listener.EndAcceptTcpClient(ar))))
        Return result.Task.AssumeNotNull
    End Function

    Public Function AsyncDnsLookup(ByVal remoteHost As String) As Task(Of Net.IPHostEntry)
        Contract.Requires(remoteHost IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Task(Of Net.IPHostEntry))() IsNot Nothing)
        Dim result = New TaskCompletionSource(Of Net.IPHostEntry)()
        result.DependentCall(Sub() Net.Dns.BeginGetHostEntry(hostNameOrAddress:=remoteHost, stateObject:=Nothing,
            requestCallback:=Sub(ar) result.SetByEvaluating(Function() Net.Dns.EndGetHostEntry(ar))))
        Return result.Task.AssumeNotNull
    End Function
End Module
