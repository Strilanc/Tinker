Imports Tinker.Pickling

Namespace Bnet.Protocol
    Public Class IPEndPointJar
        Inherits BaseJar(Of Net.IPEndPoint)

        Private Shared ReadOnly DataJar As TupleJar = New TupleJar(
                    New UInt16Jar().Named("protocol"),
                    New UInt16Jar(ByteOrder:=ByteOrder.BigEndian).Named("port"),
                    New IPAddressJar().Named("ip"),
                    New DataJar().Fixed(exactDataCount:=8).Named("unknown"))

        Public Overrides Function Pack(value As Net.IPEndPoint) As IRist(Of Byte)
            Contract.Assume(value IsNot Nothing)
            Dim addrBytes = value.Address.AssumeNotNull().GetAddressBytes()
            Dim vals = New NamedValueMap(New Dictionary(Of InvariantString, Object) From {
                    {"protocol", If(addrBytes.SequenceEqual({0, 0, 0, 0}) AndAlso value.Port = 0, 0US, 2US)},
                    {"ip", value.Address},
                    {"port", CUShort(value.Port)},
                    {"unknown", MakeRist(Of Byte)(0, 0, 0, 0, 0, 0, 0, 0)}})
            Return DataJar.Pack(vals)
        End Function

        Public Overrides Function Parse(data As IRist(Of Byte)) As ParsedValue(Of Net.IPEndPoint)
            Dim parsed = DataJar.Parse(data)
            Dim address = parsed.Value.ItemAs(Of Net.IPAddress)("ip")
            Dim port = parsed.Value.ItemAs(Of UInt16)("port")
            Return parsed.WithValue(address.WithPort(port))
        End Function

        Public Overrides Function Parse(text As String) As Net.IPEndPoint
            Try
                Dim words = text.Split(":"c)
                If words.Length <> 2 Then Throw New ArgumentException("Expected address:port format.", "text")
                Dim address = Net.IPAddress.Parse(words.First).AssumeNotNull()
                Dim port = UInt16.Parse(words.Last.AssumeNotNull(), NumberStyles.Integer, CultureInfo.InvariantCulture)
                Return address.WithPort(port)
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
