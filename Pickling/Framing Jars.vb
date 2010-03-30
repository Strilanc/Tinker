Namespace Pickling
    Public MustInherit Class BaseFramingJar(Of T)
        Inherits BaseJar(Of T)
        Private ReadOnly _subJar As IJar(Of T)
        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_subJar IsNot Nothing)
        End Sub
        Public Sub New(ByVal subJar As IJar(Of T))
            Contract.Requires(subJar IsNot Nothing)
            Me._subJar = subJar
        End Sub
        Protected ReadOnly Property SubJar As IJar(Of T)
            Get
                Contract.Ensures(Contract.Result(Of IJar(Of T))() IsNot Nothing)
                Return _subJar
            End Get
        End Property
        Public Overrides Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue)
            Return _subJar.Pack(value)
        End Function
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T)
            Return _subJar.Parse(data)
        End Function
        Public Overrides Function Describe(ByVal value As T) As String
            Return _subJar.Describe(value)
        End Function
        Public Overrides Function MakeControl() As IValueEditor(Of T)
            Return _subJar.MakeControl()
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

        Public Overrides Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue)
            Dim pickle = SubJar.Pack(value)
            If pickle.Data.Count <> _dataSize Then Throw New PicklingException("Packed data did not take exactly {0} bytes.".Frmt(_dataSize))
            Return pickle
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T)
            If data.Count < _dataSize Then Throw New PicklingNotEnoughDataException()
            Dim result As IPickle(Of T)
            Try
                result = SubJar.Parse(data.SubView(0, _dataSize))
            Catch ex As PicklingException
                '[Only wrap the exception as 'too limited data' if allowing all data causes it to go away]
                Try
                    Dim pickle = SubJar.Parse(data)
                    Throw New PicklingException("Pickled value could not be parsed from limited data.", ex)
                Catch exIgnored As PicklingException
                End Try
                Throw
            End Try
            If result.Data.Count <> _dataSize Then Throw New PicklingException("Parsed value did not use exactly {0} bytes.".Frmt(_dataSize))
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

        Public Overrides Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue)
            Dim pickle = SubJar.Pack(value)
            If pickle.Data.Count > _maxDataCount Then Throw New PicklingException("Packed data did not fit in {0} bytes.".Frmt(_maxDataCount))
            Return pickle
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T)
            Try
                Return SubJar.Parse(data.SubView(0, Math.Min(data.Count, _maxDataCount)))
            Catch ex As PicklingException
                '[Only wrap the exception as 'too limited data' if allowing all data causes it to go away]
                Try
                    Dim pickle = SubJar.Parse(data)
                    Throw New PicklingException("Pickled value could not be parsed from limited data.", ex)
                Catch exIgnored As PicklingException
                End Try
                Throw
            End Try
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

        Public Overrides Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue)
            Dim pickle = SubJar.Pack(value)
            Dim sizeBytes = CULng(pickle.Data.Count).Bytes.Take(_prefixSize)
            If sizeBytes.Take(_prefixSize).ToUValue <> pickle.Data.Count Then Throw New PicklingException("Unable to fit byte count into size prefix.")
            Dim data = sizeBytes.Concat(pickle.Data).ToReadableList
            Return pickle.With(jar:=Me, data:=data)
        End Function

        <ContractVerification(False)>
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T)
            If data.Count < _prefixSize Then Throw New PicklingNotEnoughDataException()
            Dim dataSize = data.SubView(0, _prefixSize).ToUValue
            If data.Count < _prefixSize + dataSize Then Throw New PicklingNotEnoughDataException()

            Dim datum = data.SubView(0, CInt(_prefixSize + dataSize))
            Dim pickle = SubJar.Parse(datum.SubView(_prefixSize))
            If pickle.Data.Count < dataSize Then Throw New PicklingException("Fragmented data.")
            Return pickle.With(jar:=Me, data:=datum)
        End Function
    End Class

    '''<summary>Pickles values with data followed by a null terminator.</summary>
    Public NotInheritable Class NullTerminatedFramingJar(Of T)
        Inherits BaseFramingJar(Of T)

        Public Sub New(ByVal subJar As IJar(Of T))
            MyBase.new(subJar)
            Contract.Requires(subJar IsNot Nothing)
        End Sub

        Public Overrides Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue)
            Dim pickle = SubJar.Pack(value)
            Return pickle.With(jar:=Me, data:=pickle.Data.Append(0).ToReadableList)
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T)
            'Find terminator
            Dim p = data.IndexOf(0)
            If p < 0 Then Throw New PicklingException("No null terminator found.")
            'Parse
            Dim pickle = SubJar.Parse(data.SubView(0, p))
            If pickle.Data.Count <> p Then Throw New PicklingException("Leftover data before null terminator.")
            Return pickle.With(jar:=Me, data:=data.SubView(0, p + 1))
        End Function
    End Class

    '''<summary>Pickles values which may or may not be included in the data.</summary>
    Public NotInheritable Class OptionalFramingJar(Of T)
        Inherits BaseJar(Of Tuple(Of Boolean, T))

        Private ReadOnly _subJar As IJar(Of T)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_subJar IsNot Nothing)
        End Sub

        Public Sub New(ByVal subJar As IJar(Of T))
            Contract.Requires(subJar IsNot Nothing)
            Me._subJar = subJar
        End Sub

        Public Overrides Function Pack(Of TValue As Tuple(Of Boolean, T))(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            If value.Item1 Then
                Contract.Assume(value.Item2 IsNot Nothing)
                Dim pickle = _subJar.Pack(value.Item2)
                Return pickle.With(jar:=Me, value:=value)
            Else
                Return value.Pickled(Me, New Byte() {}.AsReadableList)
            End If
        End Function

        <ContractVerification(False)>
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of Tuple(Of Boolean, T))
            If data.Count > 0 Then
                Dim pickle = _subJar.Parse(data)
                Return pickle.With(jar:=Me, value:=Tuple.Create(True, pickle.Value))
            Else
                Dim value = Tuple.Create(False, CType(Nothing, T))
                Return value.Pickled(Me, data)
            End If
        End Function

        Public Overrides Function Describe(ByVal value As Tuple(Of Boolean, T)) As String
            If Not value.Item1 Then Return "[Not Included]"
            Return _subJar.Describe(value.Item2)
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of Tuple(Of Boolean, T))
            Dim checkControl = New CheckBox()
            checkControl.Text = "Included"
            checkControl.Checked = True
            Dim valueControl = _subJar.MakeControl()
            Dim panel = PanelWithControls({checkControl, valueControl.Control})
            AddHandler checkControl.CheckedChanged, Sub() valueControl.Control.Enabled = checkControl.Checked
            AddHandler valueControl.ValueChanged, Sub() LayoutPanel(panel)

            Return New DelegatedValueEditor(Of Tuple(Of Boolean, T))(
                Control:=panel,
                eventAdder:=Sub(action)
                                AddHandler checkControl.CheckedChanged, Sub() action()
                                AddHandler valueControl.ValueChanged, Sub() action()
                            End Sub,
                getter:=Function() If(checkControl.Checked, Tuple.Create(True, valueControl.Value), Tuple.Create(False, DirectCast(Nothing, T))),
                setter:=Sub(value)
                            checkControl.Checked = value.Item1
                            If value.Item1 Then valueControl.Value = value.Item2
                        End Sub)
        End Function
    End Class

    '''<summary>Pickles values with data prefixed by a checksum.</summary>
    Public NotInheritable Class ChecksumPrefixedFramingJar(Of T)
        Inherits BaseFramingJar(Of T)

        Private ReadOnly _checksumFunction As Func(Of IReadableList(Of Byte), IReadableList(Of Byte))
        Private ReadOnly _checksumSize As Integer

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_checksumFunction IsNot Nothing)
            Contract.Invariant(_checksumSize > 0)
        End Sub

        Public Sub New(ByVal subJar As IJar(Of T),
                       ByVal checksumSize As Integer,
                       ByVal checksumFunction As Func(Of IReadableList(Of Byte), IReadableList(Of Byte)))
            MyBase.new(subJar)
            Contract.Requires(checksumSize > 0)
            Contract.Requires(subJar IsNot Nothing)
            Contract.Requires(checksumFunction IsNot Nothing)
            Me._checksumSize = checksumSize
            Me._checksumFunction = checksumFunction
        End Sub

        Public Overrides Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue)
            Dim pickle = SubJar.Pack(value)
            Dim checksum = _checksumFunction(pickle.Data)
            Contract.Assume(checksum IsNot Nothing)
            Contract.Assume(checksum.Count = _checksumSize)
            Dim data = checksum.Concat(pickle.Data).ToReadableList
            Return pickle.With(jar:=Me, data:=data)
        End Function

        <ContractVerification(False)>
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T)
            If data.Count < _checksumSize Then Throw New PicklingNotEnoughDataException()
            Dim checksum = data.SubView(0, _checksumSize)
            Dim pickle = SubJar.Parse(data.SubView(_checksumSize))
            If Not _checksumFunction(pickle.Data).SequenceEqual(checksum) Then Throw New PicklingException("Checksum didn't match.")
            Dim datum = data.SubView(0, _checksumSize + pickle.Data.Count)
            Return pickle.With(jar:=Me, data:=datum)
        End Function
    End Class

    '''<summary>Pickles values with reversed data.</summary>
    Public NotInheritable Class ReversedFramingJar(Of T)
        Inherits BaseFramingJar(Of T)

        Public Sub New(ByVal subJar As IJar(Of T))
            MyBase.New(subJar)
            Contract.Requires(subJar IsNot Nothing)
        End Sub

        Public Overrides Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue)
            Dim pickle = SubJar.Pack(value)
            Dim data = pickle.Data.Reverse.ToReadableList
            Return pickle.With(jar:=Me, data:=data)
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T)
            Dim pickle = SubJar.Parse(data.Reverse.ToReadableList)
            If pickle.Data.Count <> data.Count Then Throw New PicklingException("Leftover reversed data.")
            Return pickle.With(jar:=Me, data:=data)
        End Function
    End Class
End Namespace
