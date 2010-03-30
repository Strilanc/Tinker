Namespace Pickling
    Public NotInheritable Class InteriorSwitchJar(Of TKey, TValue)
        Inherits BaseJar(Of TValue)
        Private ReadOnly _subJars As New Dictionary(Of TKey, NonNull(Of IJar(Of TValue)))
        Private ReadOnly _valueKeyExtractor As Func(Of TValue, TKey)
        Private ReadOnly _dataKeyExtractor As Func(Of IReadableList(Of Byte), TKey)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_subJars IsNot Nothing)
            Contract.Invariant(_valueKeyExtractor IsNot Nothing)
            Contract.Invariant(_dataKeyExtractor IsNot Nothing)
        End Sub

        Public Sub New(ByVal valueKeyExtractor As Func(Of TValue, TKey),
                       ByVal dataKeyExtractor As Func(Of IReadableList(Of Byte), TKey),
                       ByVal subJars As Dictionary(Of TKey, NonNull(Of IJar(Of TValue))))
            Contract.Requires(valueKeyExtractor IsNot Nothing)
            Contract.Requires(dataKeyExtractor IsNot Nothing)
            Contract.Requires(subJars IsNot Nothing)
            Me._valueKeyExtractor = valueKeyExtractor
            Me._dataKeyExtractor = dataKeyExtractor
            Me._subJars = subJars
        End Sub

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of TValue)
            Dim key = _dataKeyExtractor(data)
            If Not _subJars.ContainsKey(key) Then Throw New PicklingException("No subjar with key {0}.".Frmt(key))
            Return _subJars(key).Value.Parse(data).With(jar:=Me)
        End Function
        Public Overrides Function Pack(Of T As TValue)(ByVal value As T) As IPickle(Of T)
            Dim key = _valueKeyExtractor(value)
            If Not _subJars.ContainsKey(key) Then Throw New PicklingException("No subjar with key {0}.".Frmt(key))
            Return _subJars(key).Value.Pack(value).With(jar:=Me)
        End Function

        Public Overrides Function Describe(ByVal value As TValue) As String
            Dim key = _valueKeyExtractor(value)
            If Not _subJars.ContainsKey(key) Then Throw New PicklingException("No subjar with key {0}.".Frmt(key))
            Return _subJars(key).Value.Describe(value)
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of TValue)
            Dim keyControl = New ComboBox()
            If DirectCast(Nothing, TKey) IsNot Nothing Then
                keyControl.Items.Add(DirectCast(Nothing, TKey))
            Else
                keyControl.Items.Add(New Object())
            End If
            keyControl.Visible = False
            Dim valueControl = New Label()
            valueControl.Text = "[No Value Entered]"

            Dim panel = PanelWithControls({keyControl, valueControl})
            panel.AutoScroll = True
            Return New DelegatedValueEditor(Of TValue)(
                Control:=panel,
                eventAdder:=Sub()
                            End Sub,
                getter:=Function() DirectCast(keyControl.Items(0), TValue),
                setter:=Sub(value)
                            keyControl.Items(0) = value
                            valueControl.Text = Pack(value).Description
                        End Sub)
        End Function
    End Class
End Namespace
