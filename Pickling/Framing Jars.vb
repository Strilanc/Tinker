Namespace Pickling
    '''<summary>Pickles values with data of a specified size.</summary>
    Public NotInheritable Class FixedSizeFramingJar(Of T)
        Inherits BaseJar(Of T)

        Private ReadOnly _subJar As IJar(Of T)
        Private ReadOnly _dataSize As Integer

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_subJar IsNot Nothing)
            Contract.Invariant(_dataSize >= 0)
        End Sub

        Public Sub New(ByVal subJar As IJar(Of T),
                       ByVal dataSize As Integer)
            Contract.Requires(subJar IsNot Nothing)
            Contract.Requires(dataSize >= 0)
            Me._subJar = subJar
            Me._dataSize = dataSize
        End Sub

        Public Overrides Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue)
            Dim pickle = _subJar.Pack(value)
            If pickle.Data.Count <> _dataSize Then Throw New PicklingException("Packed data did not take exactly {0} bytes.".Frmt(_dataSize))
            Return pickle
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T)
            If data.Count < _dataSize Then Throw New PicklingNotEnoughDataException()
            Dim result As IPickle(Of T)
            Try
                result = _subJar.Parse(data.SubView(0, _dataSize))
            Catch ex As PicklingException
                '[Only wrap the exception as 'too limited data' if allowing all data causes it to go away]
                Try
                    Dim pickle = _subJar.Parse(data)
                    Throw New PicklingException("Pickled value could not be parsed from limited data.", ex)
                Catch exIgnored As PicklingException
                End Try
                Throw
            End Try
            If result.Data.Count <> _dataSize Then Throw New PicklingException("Parsed value did not use exactly {0} bytes.".Frmt(_dataSize))
            Return result
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of T)
            Return _subJar.MakeControl()
        End Function
    End Class

    '''<summary>Pickles values with data up to a maximum size.</summary>
    Public Class LimitedSizeFramingJar(Of T)
        Inherits BaseJar(Of T)

        Private ReadOnly _subJar As IJar(Of T)
        Private ReadOnly _maxDataCount As Integer

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_subJar IsNot Nothing)
            Contract.Invariant(_maxDataCount >= 0)
        End Sub

        Public Sub New(ByVal subJar As IJar(Of T),
                       ByVal maxDataCount As Integer)
            Contract.Requires(subJar IsNot Nothing)
            Contract.Requires(maxDataCount >= 0)
            Me._subJar = subJar
            Me._maxDataCount = maxDataCount
        End Sub

        Public Overrides Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue)
            Dim pickle = _subJar.Pack(value)
            If pickle.Data.Count > _maxDataCount Then Throw New PicklingException("Packed data did not fit in {0} bytes.".Frmt(_maxDataCount))
            Return pickle
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T)
            Try
                Return _subJar.Parse(data.SubView(0, Math.Min(data.Count, _maxDataCount)))
            Catch ex As PicklingException
                '[Only wrap the exception as 'too limited data' if allowing all data causes it to go away]
                Try
                    Dim pickle = _subJar.Parse(data)
                    Throw New PicklingException("Pickled value could not be parsed from limited data.", ex)
                Catch exIgnored As PicklingException
                End Try
                Throw
            End Try
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of T)
            Return _subJar.MakeControl()
        End Function
    End Class

    '''<summary>Pickles values with data prefixed by a count of the number of bytes (not counting the prefix).</summary>
    Public NotInheritable Class SizePrefixedFramingJar(Of T)
        Inherits BaseJar(Of T)

        Private ReadOnly _subJar As IJar(Of T)
        Private ReadOnly _prefixSize As Integer

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_subJar IsNot Nothing)
            Contract.Invariant(_prefixSize > 0)
            Contract.Invariant(_prefixSize <= 8)
        End Sub

        Public Sub New(ByVal subJar As IJar(Of T),
                       ByVal prefixSize As Integer)
            Contract.Requires(prefixSize > 0)
            Contract.Requires(subJar IsNot Nothing)
            If prefixSize > 8 Then Throw New ArgumentOutOfRangeException("prefixSize", "prefixSize must be less than or equal to 8.")
            Me._subJar = subJar
            Me._prefixSize = prefixSize
        End Sub

        Public Overrides Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue)
            Dim pickle = _subJar.Pack(value)
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
            Dim pickle = _subJar.Parse(datum.SubView(_prefixSize))
            If pickle.Data.Count < dataSize Then Throw New PicklingException("Fragmented data.")
            Return pickle.With(jar:=Me, data:=datum)
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of T)
            Return _subJar.MakeControl()
        End Function
    End Class

    '''<summary>Pickles lists of values, where the serialized form is prefixed by the number of items.</summary>
    Public NotInheritable Class ItemCountPrefixedFramingJar(Of T)
        Inherits BaseJar(Of IReadableList(Of T))
        Private ReadOnly _subJar As IJar(Of T)
        Private ReadOnly _prefixSize As Integer
        Private ReadOnly _useSingleLineDescription As Boolean

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_prefixSize > 0)
            Contract.Invariant(_prefixSize <= 8)
            Contract.Invariant(_subJar IsNot Nothing)
        End Sub

        Public Sub New(ByVal subJar As IJar(Of T),
                       ByVal prefixSize As Integer,
                       Optional ByVal useSingleLineDescription As Boolean = False)
            Contract.Requires(subJar IsNot Nothing)
            Contract.Requires(prefixSize > 0)
            If prefixSize > 8 Then Throw New ArgumentOutOfRangeException("prefixSize", "prefixSize must be less than or equal to 8.")
            Me._subJar = subJar
            Me._prefixSize = prefixSize
            Me._useSingleLineDescription = useSingleLineDescription
        End Sub

        Public Overrides Function Pack(Of TValue As IReadableList(Of T))(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            Dim pickles = (From e In value Select _subJar.Pack(e)).Cache
            Dim sizeData = CULng(value.Count).Bytes.Take(_prefixSize)
            Dim pickleData = Concat(From p In pickles Select (p.Data))
            Dim data = Concat(sizeData, pickleData).ToReadableList
            Return value.Pickled(Me, data, Function() pickles.MakeListDescription(_useSingleLineDescription))
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of IReadableList(Of T))
            'Parse
            Dim pickles = New List(Of IPickle(Of T))
            Dim curOffset = 0
            'List Size
            If data.Count < _prefixSize Then Throw New PicklingNotEnoughDataException()
            Dim numElements = data.SubView(0, _prefixSize).ToUValue
            curOffset += _prefixSize
            'List Elements
            For repeat = 1UL To numElements
                'Value
                Dim p = _subJar.Parse(data.SubView(curOffset, data.Count - curOffset))
                pickles.Add(p)
                'Size
                Dim n = p.Data.Count
                curOffset += n
                If curOffset > data.Count Then Throw New InvalidStateException("Subjar '{0}' reported taking more data than was available.".Frmt(_subJar.GetType.Name))
            Next repeat

            Dim value = (From p In pickles Select (p.Value)).ToReadableList
            Dim datum = data.SubView(0, curOffset)
            Dim desc = Function() pickles.MakeListDescription(_useSingleLineDescription)
            Return value.Pickled(Me, datum, desc)
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of IReadableList(Of T))
            Return New ListValueEditor(Of T)(_subJar)
        End Function
    End Class

    '''<summary>Pickles values with data followed by a null terminator.</summary>
    Public Class NullTerminatedFramingJar(Of T)
        Inherits BaseJar(Of T)

        Private ReadOnly _subJar As IJar(Of T)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_subJar IsNot Nothing)
        End Sub

        Public Sub New(ByVal subJar As IJar(Of T))
            Contract.Requires(subJar IsNot Nothing)
            Me._subJar = subJar
        End Sub

        Public Overrides Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue)
            Dim pickle = _subJar.Pack(value)
            Return pickle.With(jar:=Me, data:=pickle.Data.Append(0).ToReadableList)
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T)
            'Find terminator
            Dim p = data.IndexOf(0)
            If p < 0 Then Throw New PicklingException("No null terminator found.")
            'Parse
            Dim pickle = _subJar.Parse(data.SubView(0, p))
            If pickle.Data.Count <> p Then Throw New PicklingException("Leftover data before null terminator.")
            Return pickle.With(jar:=Me, data:=data.SubView(0, p + 1))
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of T)
            Return _subJar.MakeControl()
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
                Return value.Pickled(Me, New Byte() {}.AsReadableList, Function() "[Not Included]")
            End If
        End Function

        <ContractVerification(False)>
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of Tuple(Of Boolean, T))
            If data.Count > 0 Then
                Dim pickle = _subJar.Parse(data)
                Return pickle.With(jar:=Me, value:=Tuple.Create(True, pickle.Value))
            Else
                Dim value = Tuple.Create(False, CType(Nothing, T))
                Return value.Pickled(Me, data, Function() "[Not Included]")
            End If
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

    '''<summary>Pickles values which may be included side-by-side in the data multiple times (including 0 times).</summary>
    Public NotInheritable Class RepeatedFramingJar(Of T)
        Inherits BaseJar(Of IReadableList(Of T))
        Private ReadOnly _subJar As IJar(Of T)
        Private ReadOnly _useSingleLineDescription As Boolean

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_subJar IsNot Nothing)
        End Sub

        Public Sub New(ByVal subJar As IJar(Of T),
                       Optional ByVal useSingleLineDescription As Boolean = False)
            Contract.Requires(subJar IsNot Nothing)
            Me._subJar = subJar
            Me._useSingleLineDescription = useSingleLineDescription
        End Sub

        Public Overrides Function Pack(Of TValue As IReadableList(Of T))(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            Dim pickles = (From e In value Select CType(_subJar.Pack(e), IPickle(Of T))).Cache
            Dim data = Concat(From p In pickles Select (p.Data)).ToReadableList
            Return value.Pickled(Me, data, Function() pickles.MakeListDescription(_useSingleLineDescription))
        End Function

        'verification disabled due to stupid verifier (1.2.30118.5)
        <ContractVerification(False)>
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of IReadableList(Of T))
            'Parse
            Dim pickles = New List(Of IPickle(Of T))
            Dim curCount = data.Count
            Dim curOffset = 0
            'List Elements
            While curOffset < data.Count
                'Value
                Dim p = _subJar.Parse(data.SubView(curOffset, curCount))
                pickles.Add(p)
                'Size
                Dim n = p.Data.Count
                curCount -= n
                curOffset += n
            End While

            Dim datum = data.SubView(0, curOffset)
            Dim value = (From p In pickles Select (p.Value)).ToReadableList
            Return value.Pickled(Me, datum, Function() pickles.MakeListDescription(_useSingleLineDescription))
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of IReadableList(Of T))
            Return New ListValueEditor(Of T)(_subJar)
        End Function
    End Class
    Public Class ListValueEditor(Of T)
        Implements IValueEditor(Of IReadableList(Of T))

        Private ReadOnly subControls As New List(Of Entry)
        Private ReadOnly mainControl As New Panel()
        Private ReadOnly addButton As New Button() With {.Text = "Add"}
        Private ReadOnly _subJar As IJar(Of T)
        Private _ignoreValueChanged As Boolean

        Public Event ValueChanged(ByVal sender As IValueEditor(Of IReadableList(Of T))) Implements IValueEditor(Of IReadableList(Of T)).ValueChanged
        Public Event ValueChangedSimple(ByVal sender As ISimpleValueEditor) Implements ISimpleValueEditor.ValueChanged

        Private Class Entry
            Public ReadOnly SubControl As IValueEditor(Of T)
            Public ReadOnly RemoveControl As New Button() With {.Text = "Remove"}
            Public ReadOnly InsertControl As New Button() With {.Text = "Insert"}
            Public ReadOnly MoveUpControl As New Button() With {.Text = "Move Up"}
            Public ReadOnly CommandPanel As Panel = PanelWithControls({RemoveControl, InsertControl, MoveUpControl}, leftToRight:=True, margin:=0)
            Public ReadOnly FullPanel As Panel

            Public Sub New(ByVal jar As IJar(Of T))
                Me.SubControl = jar.MakeControl()
                Me.FullPanel = PanelWithControls({SubControl.Control, CommandPanel}, borderStyle:=BorderStyle.FixedSingle)
            End Sub
        End Class

        Private Sub RaiseValueChanged()
            If _ignoreValueChanged Then Return
            RaiseEvent ValueChanged(Me)
            RaiseEvent ValueChangedSimple(Me)
        End Sub

        Private Sub RefreshLayout(Optional ByVal controlsChanged As Boolean = True,
                                  Optional ByVal raise As Boolean = True)
            If controlsChanged Then
                mainControl.Controls.Clear()
                For Each e In subControls
                    mainControl.Controls.Add(e.FullPanel)
                Next e
                mainControl.Controls.Add(addButton)
            End If
            LayoutPanel(mainControl, margin:=6, spacing:=0)
            If raise Then
                RaiseValueChanged()
            End If
        End Sub
        Private Sub ChangedValueSubControl(ByVal entry As Entry)
            LayoutPanel(entry.FullPanel, borderStyle:=BorderStyle.FixedSingle)
            RefreshLayout(controlsChanged:=False)
        End Sub
        Private Sub RemoveSubControl(ByVal entry As Entry)
            subControls.Remove(entry)
            RefreshLayout()
        End Sub
        Private Sub MoveUpSubControl(ByVal entry As Entry)
            Dim p = subControls.IndexOf(entry)
            subControls(p) = subControls(p - 1)
            subControls(p - 1) = entry
            subControls(p).MoveUpControl.Enabled = True
            entry.MoveUpControl.Enabled = p > 1
            RefreshLayout()
        End Sub
        Private Sub InsertAboveSubControl(ByVal entry As Entry)
            Dim r = AddEntry(layout:=False)
            Dim p = subControls.IndexOf(entry)
            subControls(subControls.Count - 1) = subControls(p)
            subControls(p) = r
            RefreshLayout()
        End Sub

        Private Function AddEntry(ByVal layout As Boolean) As Entry
            Dim entry = New Entry(_subJar)

            AddHandler entry.SubControl.ValueChanged, Sub() ChangedValueSubControl(entry)
            AddHandler entry.RemoveControl.Click, Sub() RemoveSubControl(entry)
            AddHandler entry.InsertControl.Click, Sub() InsertAboveSubControl(entry)
            AddHandler entry.MoveUpControl.Click, Sub() MoveUpSubControl(entry)
            entry.MoveUpControl.Enabled = subControls.Count > 0
            subControls.Add(entry)
            If layout Then RefreshLayout()

            Return entry
        End Function

        Public Sub New(ByVal subJar As IJar(Of T))
            Me._subJar = subJar
            Me.mainControl = PanelWithControls({Me.addButton}, margin:=0)
            AddHandler addButton.Click, Sub() AddEntry(layout:=True)
        End Sub
        Public ReadOnly Property Control As Control Implements ISimpleValueEditor.Control
            Get
                Return mainControl
            End Get
        End Property

        Public Property Value As IReadableList(Of T) Implements IValueEditor(Of IReadableList(Of T)).Value
            Get
                Return (From e In subControls Select (e.SubControl.Value)).ToReadableList
            End Get
            Set(ByVal value As IReadableList(Of T))
                Try
                    _ignoreValueChanged = True
                    subControls.Clear()
                    For Each item In value
                        AddEntry(layout:=False).SubControl.Value = item
                    Next item
                Finally
                    _ignoreValueChanged = False
                End Try
                RefreshLayout()
            End Set
        End Property
        Private Property ValueSimple As Object Implements ISimpleValueEditor.Value
            Get
                Return Me.Value
            End Get
            Set(ByVal value As Object)
                Me.Value = DirectCast(value, IReadableList(Of T))
            End Set
        End Property
    End Class

    '''<summary>Pickles values with data prefixed by a checksum.</summary>
    Public NotInheritable Class ChecksumPrefixedFramingJar(Of T)
        Inherits BaseJar(Of T)

        Private ReadOnly _subJar As IJar(Of T)
        Private ReadOnly _checksumFunction As Func(Of IReadableList(Of Byte), IReadableList(Of Byte))
        Private ReadOnly _checksumSize As Integer

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_subJar IsNot Nothing)
            Contract.Invariant(_checksumFunction IsNot Nothing)
            Contract.Invariant(_checksumSize > 0)
        End Sub

        Public Sub New(ByVal subJar As IJar(Of T),
                       ByVal checksumSize As Integer,
                       ByVal checksumFunction As Func(Of IReadableList(Of Byte), IReadableList(Of Byte)))
            Contract.Requires(checksumSize > 0)
            Contract.Requires(subJar IsNot Nothing)
            Contract.Requires(checksumFunction IsNot Nothing)
            Me._subJar = subJar
            Me._checksumSize = checksumSize
            Me._checksumFunction = checksumFunction
        End Sub

        Public Overrides Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue)
            Dim pickle = _subJar.Pack(value)
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
            Dim pickle = _subJar.Parse(data.SubView(_checksumSize))
            If Not _checksumFunction(pickle.Data).SequenceEqual(checksum) Then Throw New PicklingException("Checksum didn't match.")
            Dim datum = data.SubView(0, _checksumSize + pickle.Data.Count)
            Return pickle.With(jar:=Me, data:=datum)
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of T)
            Return _subJar.MakeControl()
        End Function
    End Class

    '''<summary>Pickles values with reversed data.</summary>
    Public Class ReversedFramingJar(Of T)
        Inherits BaseJar(Of T)

        Private ReadOnly _subJar As IJar(Of T)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_subJar IsNot Nothing)
        End Sub

        Public Sub New(ByVal subJar As IJar(Of T))
            Contract.Requires(subJar IsNot Nothing)
            Me._subJar = subJar
        End Sub

        Public Overrides Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue)
            Dim pickle = _subJar.Pack(value)
            Dim data = pickle.Data.Reverse.ToReadableList
            Return pickle.With(jar:=Me, data:=data)
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T)
            Dim pickle = _subJar.Parse(data.Reverse.ToReadableList)
            If pickle.Data.Count <> data.Count Then Throw New PicklingException("Leftover reversed data.")
            Return pickle.With(jar:=Me, data:=data)
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of T)
            Return _subJar.MakeControl()
        End Function
    End Class
End Namespace
