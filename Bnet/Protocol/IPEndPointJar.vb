Imports Tinker.Pickling

Namespace Bnet.Protocol
    Public Class IPEndPointJar
        Inherits BaseJar(Of Net.IPEndPoint)

        Private Shared ReadOnly DataJar As TupleJar = New TupleJar(
                    New UInt16Jar().Named("protocol"),
                    New UInt16Jar(ByteOrder:=ByteOrder.BigEndian).Named("port"),
                    New IPAddressJar().Named("ip"),
                    New DataJar().Fixed(exactDataCount:=8).Named("unknown"))

        <ContractVerification(False)>
        Public Overrides Function Pack(ByVal value As System.Net.IPEndPoint) As IEnumerable(Of Byte)
            Dim addrBytes = value.Address.GetAddressBytes
            Dim vals = New NamedValueMap(New Dictionary(Of InvariantString, Object) From {
                    {"protocol", If(addrBytes.SequenceEqual({0, 0, 0, 0}) AndAlso value.Port = 0, 0US, 2US)},
                    {"ip", value.Address},
                    {"port", CUShort(value.Port)},
                    {"unknown", New Byte() {0, 0, 0, 0, 0, 0, 0, 0}.AsReadableList}})
            Return DataJar.Pack(vals)
        End Function

        'verification disabled due to stupid verifier (1.2.30118.5)
        <ContractVerification(False)>
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As ParsedValue(Of Net.IPEndPoint)
            Dim parsed = DataJar.Parse(data)
            Return parsed.WithValue(New Net.IPEndPoint(parsed.Value.ItemAs(Of Net.IPAddress)("ip"),
                                                       parsed.Value.ItemAs(Of UInt16)("port")))
        End Function

        <ContractVerification(False)>
        Public Overrides Function Parse(ByVal text As String) As Net.IPEndPoint
            Try
                Dim words = text.Split(":"c)
                If words.Length <> 2 Then Throw New ArgumentException("Expected address:port format.", "text")
                Return New Net.IPEndPoint(Net.IPAddress.Parse(words.First),
                                          UInt16.Parse(words.Last, NumberStyles.Integer, CultureInfo.InvariantCulture))
            Catch ex As Exception When TypeOf ex Is FormatException OrElse
                                       TypeOf ex Is ArgumentException
                Throw New PicklingException("'{0}' is not a Net.IPEndPoint".Frmt(text), ex)
            End Try
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of Net.IPEndPoint)
            Dim addressControl = New IPAddressJar().Named("address").MakeControl()
            Dim portControl = New UInt16Jar().Named("port").MakeControl()
            Dim panel = PanelWithControls({addressControl.Control, portControl.Control}, leftToRight:=True)
            Return New DelegatedValueEditor(Of Net.IPEndPoint)(
                Control:=panel,
                eventAdder:=Sub(action)
                                AddHandler addressControl.ValueChanged, Sub() action()
                                AddHandler portControl.ValueChanged, Sub() action()
                            End Sub,
                getter:=Function() New Net.IPEndPoint(addressControl.Value, portControl.Value),
                setter:=Sub(value)
                            addressControl.SetValueIfDifferent(value.Address)
                            portControl.SetValueIfDifferent(CUShort(value.Port))
                        End Sub,
                disposer:=Sub()
                              addressControl.Dispose()
                              portControl.Dispose()
                              panel.Dispose()
                          End Sub)
        End Function
    End Class
End Namespace
