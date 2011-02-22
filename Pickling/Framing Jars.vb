Namespace Pickling
    Public MustInherit Class BaseFramingJar(Of T)
        Inherits BaseJar(Of T)
        Private ReadOnly _subJar As IJar(Of T)
        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_subJar IsNot Nothing)
        End Sub
        Protected Sub New(ByVal subJar As IJar(Of T))
            Contract.Requires(subJar IsNot Nothing)
            Me._subJar = subJar
        End Sub
        Protected ReadOnly Property SubJar As IJar(Of T)
            Get
                Contract.Ensures(Contract.Result(Of IJar(Of T))() IsNot Nothing)
                Return _subJar
            End Get
        End Property
        Public Overrides Function Pack(ByVal value As T) As IEnumerable(Of Byte)
            Return _subJar.Pack(value)
        End Function
        Public Overrides Function Parse(ByVal data As IRist(Of Byte)) As ParsedValue(Of T)
            Return _subJar.Parse(data)
        End Function
        Public Overrides Function Describe(ByVal value As T) As String
            Return _subJar.Describe(value)
        End Function
        Public Overrides Function MakeControl() As IValueEditor(Of T)
            Return _subJar.MakeControl()
        End Function
        Public Overrides Function Parse(ByVal text As String) As T
            Return _subJar.Parse(text)
        End Function
    End Class

    '''<summary>Pickles values with data of a specified size.</summary>
    Public NotInheritable Class FixedSizeFramingJar(Of T)
        Inherits BaseFramingJar(Of T)
        Private ReadOnly _dataSize As Integer

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_dataSize >= 0)
        End Sub

        Public Sub New(ByVal subJar As IJar(Of T),
                       ByVal dataSize As Integer)
            MyBase.New(subJar)
            Contract.Requires(subJar IsNot Nothing)
            Contract.Requires(dataSize >= 0)
            Me._dataSize = dataSize
        End Sub

        Public Overrides Function Pack(ByVal value As T) As IEnumerable(Of Byte)
            Dim data = SubJar.Pack(value).ToRist
            If data.Count <> _dataSize Then Throw New PicklingException("Packed data did not take exactly {0} bytes.".Frmt(_dataSize))
            Return data
        End Function

        Public Overrides Function Parse(ByVal data As IRist(Of Byte)) As ParsedValue(Of T)
            If data.Count < _dataSize Then Throw New PicklingNotEnoughDataException("The fixed-size data requires {0} bytes.".Frmt(_dataSize))
            Dim result = SubJar.Parse(data.TakeExact(_dataSize))
            If result.UsedDataCount <> _dataSize Then Throw New PicklingException("Parsed value did not use exactly {0} bytes.".Frmt(_dataSize))
            Return result
        End Function
    End Class

    '''<summary>Pickles values with data up to a maximum size.</summary>
    Public NotInheritable Class LimitedSizeFramingJar(Of T)
        Inherits BaseFramingJar(Of T)

        Private ReadOnly _maxDataCount As Integer

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_maxDataCount >= 0)
        End Sub

        Public Sub New(ByVal subJar As IJar(Of T),
                       ByVal maxDataCount As Integer)
            MyBase.New(subJar)
            Contract.Requires(subJar IsNot Nothing)
            Contract.Requires(maxDataCount >= 0)
            Me._maxDataCount = maxDataCount
        End Sub

        Public Overrides Function Pack(ByVal value As T) As IEnumerable(Of Byte)
            Dim data = SubJar.Pack(value).ToRist
            If data.Count > _maxDataCount Then Throw New PicklingException("Packed data did not fit in {0} bytes.".Frmt(_maxDataCount))
            Return data
        End Function

        Public Overrides Function Parse(ByVal data As IRist(Of Byte)) As ParsedValue(Of T)
            Return SubJar.Parse(data.Take(_maxDataCount))
        End Function
    End Class

    '''<summary>Pickles values with data prefixed by a count of the number of bytes (not counting the prefix).</summary>
    Public NotInheritable Class SizePrefixedFramingJar(Of T)
        Inherits BaseFramingJar(Of T)

        Private ReadOnly _prefixSize As Integer

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_prefixSize > 0)
            Contract.Invariant(_prefixSize <= 8)
        End Sub

        Public Sub New(ByVal subJar As IJar(Of T),
                       ByVal prefixSize As Integer)
            MyBase.New(subJar)
            Contract.Requires(prefixSize > 0)
            Contract.Requires(subJar IsNot Nothing)
            If prefixSize > 8 Then Throw New ArgumentOutOfRangeException("prefixSize", "prefixSize must be less than or equal to 8.")
            Me._prefixSize = prefixSize
        End Sub

        Public Overrides Function Pack(ByVal value As T) As IEnumerable(Of Byte)
            Dim subData = SubJar.Pack(value).ToRist
            Dim sizeBytes = CULng(subData.Count).Bytes.Take(_prefixSize)
            If sizeBytes.Take(_prefixSize).ToUValue <> subData.Count Then Throw New PicklingException("Unable to fit byte count into size prefix.")
            Return sizeBytes.Concat(subData)
        End Function

        Public Overrides Function Parse(ByVal data As IRist(Of Byte)) As ParsedValue(Of T)
            If data.Count < _prefixSize Then Throw New PicklingNotEnoughDataException("The size prefix requires {0} bytes.".Frmt(_prefixSize))
            Dim dataSize = data.TakeExact(_prefixSize).ToUValue
            If data.Count < _prefixSize + dataSize Then Throw New PicklingNotEnoughDataException("The size-prefixed data requires the {0} bytes specified by the prefix.".Frmt(dataSize))

            Contract.Assume(CInt(dataSize) >= 0)
            Contract.Assume(_prefixSize + CInt(dataSize) <= data.Count)
            Dim parsed = SubJar.Parse(data.SubList(_prefixSize, CInt(dataSize)))
            If parsed.UsedDataCount < dataSize Then Throw New PicklingException("Fragmented data.")
            Return parsed.Value.ParsedWithDataCount(_prefixSize + parsed.UsedDataCount)
        End Function
    End Class

    '''<summary>Pickles values with data followed by a null terminator.</summary>
    Public NotInheritable Class NullTerminatedFramingJar(Of T)
        Inherits BaseFramingJar(Of T)

        Public Sub New(ByVal subJar As IJar(Of T))
            MyBase.new(subJar)
            Contract.Requires(subJar IsNot Nothing)
        End Sub

        Public Overrides Function Pack(ByVal value As T) As IEnumerable(Of Byte)
            Return SubJar.Pack(value).Append(0)
        End Function

        Public Overrides Function Parse(ByVal data As IRist(Of Byte)) As ParsedValue(Of T)
            'Find terminator
            Dim p = data.IndexOf(0)
            If Not p.HasValue Then Throw New PicklingException("No null terminator found.")
            'Parse
            Contract.Assume(p.Value >= 0)
            Contract.Assume(p.Value < data.Count)
            Dim parsed = SubJar.Parse(data.TakeExact(p.Value))
            If parsed.UsedDataCount <> p.Value Then Throw New PicklingException("Leftover data before null terminator.")
            Return parsed.Value.ParsedWithDataCount(parsed.UsedDataCount + 1)
        End Function
    End Class

    '''<summary>Pickles values which may or may not be included in the data.</summary>
    Public NotInheritable Class OptionalFramingJar(Of T)
        Inherits BaseJar(Of Maybe(Of T))

        Private ReadOnly _subJar As IJar(Of T)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_subJar IsNot Nothing)
        End Sub

        Public Sub New(ByVal subJar As IJar(Of T))
            Contract.Requires(subJar IsNot Nothing)
            Me._subJar = subJar
        End Sub

        Public Overrides Function Pack(ByVal value As Maybe(Of T)) As IEnumerable(Of Byte)
            If value.HasValue Then
                Return _subJar.Pack(value.Value)
            Else
                Return New Byte() {}
            End If
        End Function

        Public Overrides Function Parse(ByVal data As IRist(Of Byte)) As ParsedValue(Of Maybe(Of T))
            If data.Count > 0 Then
                Dim parsed = _subJar.Parse(data)
                Return parsed.WithValue(Of Maybe(Of T))(parsed.Value)
            Else
                Return New Maybe(Of T)().ParsedWithDataCount(0)
            End If
        End Function

        Public Overrides Function Describe(ByVal value As Maybe(Of T)) As String
            If Not value.HasValue Then Return "[Not Included]"
            Return _subJar.Describe(value.Value)
        End Function
        <SuppressMessage("Microsoft.Contracts", "Ensures-28-16")>
        <SuppressMessage("Microsoft.Contracts", "Ensures-28-34")>
        Public Overrides Function Parse(ByVal text As String) As Maybe(Of T)
            If text = "[Not Included]" Then Return Nothing
            Return _subJar.Parse(text)
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of Maybe(Of T))
            Dim checkControl = New CheckBox()
            checkControl.Text = "Included"
            checkControl.Checked = False
            Dim valueControl = _subJar.MakeControl()
            Dim panel = PanelWithControls({checkControl, valueControl.Control})
            AddHandler checkControl.CheckedChanged, Sub() valueControl.Control.Enabled = checkControl.Checked
            AddHandler valueControl.ValueChanged, Sub() LayoutPanel(panel)

            Return New DelegatedValueEditor(Of Maybe(Of T))(
                Control:=panel,
                eventAdder:=Sub(action)
                                AddHandler checkControl.CheckedChanged, Sub() action()
                                AddHandler valueControl.ValueChanged, Sub() action()
                            End Sub,
                getter:=Function() If(checkControl.Checked, valueControl.Value.Maybe, New Maybe(Of T)()),
                setter:=Sub(value)
                            checkControl.Checked = value.HasValue
                            If value.HasValue Then valueControl.SetValueIfDifferent(value.Value)
                        End Sub,
                disposer:=Sub()
                              checkControl.Dispose()
                              valueControl.Dispose()
                              panel.Dispose()
                          End Sub)
        End Function
    End Class

    '''<summary>Pickles values with data prefixed by a checksum.</summary>
    Public NotInheritable Class ChecksumPrefixedFramingJar(Of T)
        Inherits BaseFramingJar(Of T)

        Private ReadOnly _checksumFunction As Func(Of IRist(Of Byte), IRist(Of Byte))
        Private ReadOnly _checksumSize As Integer

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_checksumFunction IsNot Nothing)
            Contract.Invariant(_checksumSize > 0)
        End Sub

        Public Sub New(ByVal subJar As IJar(Of T),
                       ByVal checksumSize As Integer,
                       ByVal checksumFunction As Func(Of IRist(Of Byte), IRist(Of Byte)))
            MyBase.new(subJar)
            Contract.Requires(checksumSize > 0)
            Contract.Requires(subJar IsNot Nothing)
            Contract.Requires(checksumFunction IsNot Nothing)
            Me._checksumSize = checksumSize
            Me._checksumFunction = checksumFunction
        End Sub

        Public Overrides Function Pack(ByVal value As T) As IEnumerable(Of Byte)
            Dim subData = SubJar.Pack(value).ToRist
            Dim checksum = _checksumFunction(subData)
            Contract.Assume(checksum IsNot Nothing)
            Contract.Assume(checksum.Count = _checksumSize)
            Return checksum.Concat(subData)
        End Function

        Public Overrides Function Parse(ByVal data As IRist(Of Byte)) As ParsedValue(Of T)
            If data.Count < _checksumSize Then Throw New PicklingNotEnoughDataException("The checksum requires {0} bytes.".Frmt(_checksumSize))
            Dim checksum = data.TakeExact(_checksumSize)
            Dim parsed = SubJar.Parse(data.SkipExact(_checksumSize))
            Dim expectedChecksum = _checksumFunction(data.SubList(_checksumSize, parsed.UsedDataCount))
            Contract.Assume(expectedChecksum IsNot Nothing)
            If Not expectedChecksum.SequenceEqual(checksum) Then
                Throw New PicklingException("Checksum didn't match. Should be [{0}], not [{1}].".Frmt(expectedChecksum.ToHexString, checksum.ToHexString))
            End If
            Return parsed.Value.ParsedWithDataCount(_checksumSize + parsed.UsedDataCount)
        End Function
    End Class

    '''<summary>Pickles values with reversed data.</summary>
    Public NotInheritable Class ReversedFramingJar(Of T)
        Inherits BaseFramingJar(Of T)

        Public Sub New(ByVal subJar As IJar(Of T))
            MyBase.New(subJar)
            Contract.Requires(subJar IsNot Nothing)
        End Sub

        Public Overrides Function Pack(ByVal value As T) As IEnumerable(Of Byte)
            Return SubJar.Pack(value).Reverse
        End Function

        Public Overrides Function Parse(ByVal data As IRist(Of Byte)) As ParsedValue(Of T)
            Dim parsed = SubJar.Parse(data.Reverse.ToRist)
            If parsed.UsedDataCount <> data.Count Then Throw New PicklingException("Leftover reversed data.")
            Return parsed
        End Function
    End Class
End Namespace
