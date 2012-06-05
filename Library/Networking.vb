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
        cachedInternalIP = GetInternalIPAddress.GetAddressBytes()

        'External IP
        Try
            Using webClient = New WebClient()
                'workaround large sync delay from webClient.DownloadDataTaskAsync
                '(a bug on Microsoft's end, may be fixed in the future)
                Await Task.Yield()

                Dim data = Await webClient.DownloadDataTaskAsync("http://automation.whatismyip.com/n09230945.asp")
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
        If cachedInternalIP Is Nothing Then Return GetInternalIPAddress().GetAddressBytes()

        If external AndAlso cachedExternalIP IsNot Nothing Then
            Return cachedExternalIP
        Else
            Return cachedInternalIP
        End If
    End Function

    '''<summary>Asynchronously creates and connects a TcpClient to the given remote endpoint.</summary>
    <Pure()>
    Public Async Function AsyncTcpConnect(host As String, port As UShort) As Task(Of TcpClient)
        Contract.Requires(host IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Task(Of TcpClient))() IsNot Nothing)
        Dim client = New TcpClient()
        Await client.ConnectAsync(host, port)
        Return client
    End Function

    '''<summary>Asynchronously creates and connects a TcpClient to the given remote endpoint.</summary>
    <Pure()>
    Public Async Function AsyncTcpConnect(address As Net.IPAddress, port As UShort) As Task(Of TcpClient)
        Contract.Requires(address IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Task(Of TcpClient))() IsNot Nothing)
        Dim client = New TcpClient()
        Await client.ConnectAsync(address, port)
        Return client
    End Function

    <Pure()>
    Public Async Function DNSLookupAddressAsync(remoteHost As String, addressSelector As Random) As Task(Of Net.IPAddress)
        Contract.Assume(remoteHost IsNot Nothing)
        Contract.Assume(addressSelector IsNot Nothing)
        'Contract.Ensures(Contract.Result(Of Task(Of Net.IPAddress))() IsNot Nothing)
        Dim hostEntry = Await Net.Dns.GetHostEntryAsync(remoteHost)
        Return hostEntry.AddressList(addressSelector.Next(hostEntry.AddressList.Count))
    End Function

    <Pure()>
    Public Async Function TCPConnectAsync(remoteHost As String, addressSelector As Random, port As UShort) As Task(Of TcpClient)
        Contract.Assume(remoteHost IsNot Nothing)
        Contract.Assume(addressSelector IsNot Nothing)
        'Contract.Ensures(Contract.Result(Of Task(Of TcpClient))() IsNot Nothing)
        Dim address = Await DNSLookupAddressAsync(remoteHost, addressSelector)
        Return Await AsyncTcpConnect(address, port)
    End Function
End Module
