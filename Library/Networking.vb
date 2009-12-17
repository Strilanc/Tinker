Imports System.Net.NetworkInformation
Imports System.Runtime.CompilerServices
Imports System.Text
Imports System.Net.Sockets
Imports System.Net

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
        Dim interfaces = NetworkInterface.GetAllNetworkInterfaces
        Contract.Assume(interfaces IsNot Nothing)
        Dim addr = (From nic In interfaces
                    Where nic.Supports(NetworkInterfaceComponent.IPv4)
                    Select a = (From address In nic.GetIPProperties.UnicastAddresses
                                Where address.Address.AddressFamily = Net.Sockets.AddressFamily.InterNetwork
                                Where Not address.Address.GetAddressBytes.HasSameItemsAs({127, 0, 0, 1})
                                ).FirstOrDefault()
                    Where a IsNot Nothing
                    ).FirstOrDefault()
        If addr IsNot Nothing Then
            Return addr.Address
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
                                    ByVal port As UShort) As IFuture(Of TcpClient)
        Contract.Requires(host IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IFuture(Of TcpClient))() IsNot Nothing)
        Dim result = New FutureFunction(Of TcpClient)
        Dim client = New TcpClient
        result.DependentCall(Sub() client.BeginConnect(host:=host, port:=port, state:=Nothing,
            requestCallback:=Sub(ar) result.SetByEvaluating(
                Function()
                    client.EndConnect(ar)
                    Return client
                End Function)))
        Return result
    End Function

    '''<summary>Asynchronously creates and connects a TcpClient to the given remote endpoint.</summary>
    <Pure()>
    Public Function AsyncTcpConnect(ByVal address As Net.IPAddress,
                                    ByVal port As UShort) As IFuture(Of TcpClient)
        Contract.Requires(address IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IFuture(Of TcpClient))() IsNot Nothing)
        Dim result = New FutureFunction(Of TcpClient)
        Dim client = New TcpClient
        result.DependentCall(Sub() client.BeginConnect(address:=address, port:=port, state:=Nothing,
            requestCallback:=Sub(ar) result.SetByEvaluating(
                Function()
                    client.EndConnect(ar)
                    Return client
                End Function)))
        Return result
    End Function

    '''<summary>Asynchronously accepts an incoming connection attempt.</summary>
    <Extension()>
    Public Function AsyncAcceptConnection(ByVal listener As TcpListener) As IFuture(Of TcpClient)
        Contract.Requires(listener IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IFuture(Of TcpClient))() IsNot Nothing)
        Dim result = New FutureFunction(Of TcpClient)
        result.DependentCall(Sub() listener.BeginAcceptTcpClient(state:=Nothing,
            callback:=Sub(ar) result.SetByEvaluating(Function() listener.EndAcceptTcpClient(ar))))
        Return result
    End Function
End Module

Public Class NetIPAddressJar
    Inherits Jar(Of Net.IPAddress)

    Public Sub New(ByVal name As InvariantString)
        MyBase.New(name)
    End Sub

    Public Overrides Function Pack(Of TValue As System.Net.IPAddress)(ByVal value As TValue) As Pickling.IPickle(Of TValue)
        Dim data = value.GetAddressBytes()
        Return New Pickle(Of TValue)(Name, value, data.AsReadableList)
    End Function
    Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As Pickling.IPickle(Of System.Net.IPAddress)
        If data.Count < 4 Then Throw New PicklingException("Not enough data.")
        Dim datum = data.SubView(0, 4)
        Dim value = New Net.IPAddress(datum.ToArray)
        Return New Pickle(Of Net.IPAddress)(Name, value, datum)
    End Function
End Class

Public Class NetIPEndPointJar
    Inherits Jar(Of Net.IPEndPoint)

    Private Shared ReadOnly DataJar As TupleJar = New TupleJar("NetIPEndPoint",
                New UInt16Jar("protocol").Weaken,
                New UInt16Jar("port", ByteOrder:=ByteOrder.BigEndian).Weaken,
                New NetIPAddressJar("ip").Weaken,
                New ArrayJar("unknown", 8).Weaken)

    Public Sub New(ByVal name As InvariantString)
        MyBase.New(name)
    End Sub

    Public Overrides Function Pack(Of TValue As Net.IPEndPoint)(ByVal value As TValue) As Pickling.IPickle(Of TValue)
        Dim vals = New Dictionary(Of InvariantString, Object) From {
                {"protocol", If(value.Address.GetAddressBytes.HasSameItemsAs({0, 0, 0, 0}) AndAlso value.Port = 0, 0, 2)},
                {"ip", value.Address},
                {"port", value.Port},
                {"unknown", New Byte() {0, 0, 0, 0, 0, 0, 0, 0}}}
        Dim pickle = DataJar.Pack(vals)
        Return New Pickle(Of TValue)(Name, value, pickle.Data)
    End Function

    Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As Pickling.IPickle(Of Net.IPEndPoint)
        Dim pickle = DataJar.Parse(data)
        Dim vals = pickle.Value
        Dim value = New Net.IPEndPoint(CType(vals("ip"), Net.IPAddress).AssumeNotNull, CUShort(vals("port")))
        Return New Pickle(Of Net.IPEndPoint)(Name, value, pickle.Data)
    End Function
End Class
