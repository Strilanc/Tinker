Imports System.Net.NetworkInformation
Imports System.Runtime.CompilerServices
Imports System.Text
Imports System.Net.Sockets
Imports System.Net
Imports Tinker.Pickling

Public Module NetworkingCommon
    Private cachedExternalIP As Byte()
    Private cachedInternalIP As Byte()
    Public Async Sub CacheIPAddresses()
        'Internal IP
        cachedInternalIP = GetInternalIPAddress.GetAddressBytes

        'External IP
        Try
            Using webClient = New WebClient()
                Dim data = Await webClient.DownloadDataTaskAsync("http://whatismyip.com/automation/n09230945.asp")
                Dim externalIp = New UTF8Encoding().GetString(data)
                If externalIp.Length < "#.#.#.#".Length Then Return
                If externalIp.Length > "###.###.###.###".Length Then Return
                Dim words = externalIp.Split("."c)
                If words.Length <> 4 Then Return
                If (From word In words Where Not Byte.TryParse(word, 0)).Any Then Return
                cachedExternalIP = (From word In words Select Byte.Parse(word, CultureInfo.InvariantCulture)).ToArray()
            End Using
        Catch ex As WebException
            'no action
        End Try
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
    Public Function GetCachedIPAddressBytes(external As Boolean) As Byte()
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
    Public Function AsyncTcpConnect(host As String,
                                    port As UShort) As Task(Of TcpClient)
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
    Public Function AsyncTcpConnect(address As Net.IPAddress,
                                    port As UShort) As Task(Of TcpClient)
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
    Public Function AsyncAcceptConnection(listener As TcpListener) As Task(Of TcpClient)
        Contract.Requires(listener IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Task(Of TcpClient))() IsNot Nothing)
        Dim result = New TaskCompletionSource(Of TcpClient)
        result.DependentCall(Sub() listener.BeginAcceptTcpClient(state:=Nothing,
            callback:=Sub(ar) result.SetByEvaluating(Function() listener.EndAcceptTcpClient(ar))))
        Return result.Task.AssumeNotNull
    End Function

    Public Function AsyncDnsLookup(remoteHost As String) As Task(Of Net.IPHostEntry)
        Contract.Requires(remoteHost IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Task(Of Net.IPHostEntry))() IsNot Nothing)
        Dim result = New TaskCompletionSource(Of Net.IPHostEntry)()
        result.DependentCall(Sub() Net.Dns.BeginGetHostEntry(hostNameOrAddress:=remoteHost, stateObject:=Nothing,
            requestCallback:=Sub(ar) result.SetByEvaluating(Function() Net.Dns.EndGetHostEntry(ar))))
        Return result.Task.AssumeNotNull
    End Function

    Public Async Function DNSLookupAddressAsync(remoteHost As String,
                                                addressSelector As Random) As Task(Of Net.IPAddress)
        Contract.Assume(remoteHost IsNot Nothing)
        Contract.Assume(addressSelector IsNot Nothing)
        'Contract.Ensures(Contract.Result(Of Task(Of Net.IPAddress))() IsNot Nothing)
        Dim hostEntry = Await AsyncDnsLookup(remoteHost)
        Return hostEntry.AddressList(addressSelector.Next(hostEntry.AddressList.Count))
    End Function

    <Pure()>
    Public Async Function TCPConnectAsync(remoteHost As String,
                                          addressSelector As Random,
                                          port As UShort) As Task(Of TcpClient)
        Contract.Assume(remoteHost IsNot Nothing)
        Contract.Assume(addressSelector IsNot Nothing)
        'Contract.Ensures(Contract.Result(Of Task(Of TcpClient))() IsNot Nothing)
        Dim address = Await DNSLookupAddressAsync(remoteHost, addressSelector)
        Return Await AsyncTcpConnect(address, port)
    End Function
End Module
