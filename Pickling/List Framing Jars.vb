Namespace Pickling
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

        Public Overrides Function Pack(ByVal value As IReadableList(Of T)) As IEnumerable(Of Byte)
            Return Concat(From item In value Select _subJar.Pack(item))
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
            Return value.Pickled(Me, datum)
        End Function

        Public Overrides Function Describe(ByVal value As IReadableList(Of T)) As String
            Return (From item In value Select _subJar.Describe(item)).MakeListDescription(_useSingleLineDescription)
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of IReadableList(Of T))
            Return New ListValueEditor(Of T)(_subJar)
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

        Public Overrides Function Pack(ByVal value As IReadableList(Of T)) As IEnumerable(Of Byte)
            Dim sizeData = CULng(value.Count).Bytes.Take(_prefixSize)
            Dim itemData = Concat(From item In value Select _subJar.Pack(item))
            Return sizeData.Concat(itemData)
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
            Return value.Pickled(Me, datum)
        End Function

        Public Overrides Function Describe(ByVal value As IReadableList(Of T)) As String
            Return (From item In value Select _subJar.Describe(item)).MakeListDescription(_useSingleLineDescription)
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of IReadableList(Of T))
            Return New ListValueEditor(Of T)(_subJar)
        End Function
    End Class

    Public NotInheritable Class ListValueEditor(Of T)
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
End Namespace
