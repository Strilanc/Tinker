Namespace Pickling
    Public NotInheritable Class KeyPrefixedJar(Of TKey)
        Inherits BaseJar(Of KeyValuePair(Of TKey, ISimplePickle))

        Private ReadOnly _keyJar As IJar(Of TKey)
        Private ReadOnly _valueJars As New Dictionary(Of TKey, NonNull(Of ISimpleJar))

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_keyJar IsNot Nothing)
            Contract.Invariant(_valueJars IsNot Nothing)
        End Sub

        Public Sub New(ByVal keyJar As IJar(Of TKey),
                       ByVal valueJars As Dictionary(Of TKey, NonNull(Of ISimpleJar)))
            Contract.Requires(keyJar IsNot Nothing)
            Contract.Requires(valueJars IsNot Nothing)
            Me._keyJar = keyJar
            Me._valueJars = valueJars
        End Sub

        <ContractVerification(False)>
        Public Overrides Function Pack(Of TValue As KeyValuePair(Of TKey, ISimplePickle))(ByVal value As TValue) As IPickle(Of TValue)
            Dim v = CType(value, KeyValuePair(Of TKey, ISimplePickle))
            If Not _valueJars.ContainsKey(v.Key) Then Throw New PicklingException("No subjar with key {0}.".Frmt(v.Key))
            Dim keyPickle = _keyJar.Pack(v.Key)
            Dim valuePickle = _valueJars(v.Key).Value.Pack(v.Value.Value)

            Dim data = keyPickle.Data.Concat(valuePickle.Data).ToReadableList
            Dim desc = Function() "{0}: {1}".Frmt(keyPickle.Description.Value, valuePickle.Description.Value)
            Return value.Pickled(Me, data, desc)
        End Function
        <ContractVerification(False)>
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of KeyValuePair(Of TKey, ISimplePickle))
            Dim keyPickle = _keyJar.Parse(data)
            If Not _valueJars.ContainsKey(keyPickle.Value) Then Throw New PicklingException("No subjar with key {0}.".Frmt(keyPickle.Value))
            Dim valuePickle = _valueJars(keyPickle.Value).Value.Parse(data.SubView(keyPickle.Data.Count))

            Dim value = New KeyValuePair(Of TKey, ISimplePickle)(keyPickle.Value, valuePickle)
            Dim datum = keyPickle.Data.Concat(valuePickle.Data).ToReadableList
            Dim desc = Function() "{0}: {1}".Frmt(keyPickle.Description.Value, valuePickle.Description.Value)
            Return value.Pickled(Me, datum, desc)
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of KeyValuePair(Of TKey, ISimplePickle))
            Dim keyControl = _keyJar.MakeControl()
            Dim valueControl = _valueJars(keyControl.Value).Value.MakeControl()
            Dim panel = PanelWithControls({keyControl.Control, valueControl.Control}, borderStyle:=BorderStyle.FixedSingle)
            Dim handlers = New List(Of Action)

            Dim updateValueControl = Sub()
                                         valueControl = _valueJars(keyControl.Value).Value.MakeControl()
                                         panel.Controls.RemoveAt(1)
                                         panel.Controls.Add(valueControl.Control)
                                         LayoutPanel(panel, borderStyle:=BorderStyle.FixedSingle)
                                         For Each handler In handlers
                                             Dim h = handler
                                             AddHandler valueControl.ValueChanged, Sub() h()
                                         Next handler
                                         AddHandler valueControl.ValueChanged, Sub() LayoutPanel(panel, borderStyle:=BorderStyle.FixedSingle)
                                     End Sub
            AddHandler keyControl.ValueChanged, Sub() updateValueControl()

            Return New DelegatedValueEditor(Of KeyValuePair(Of TKey, ISimplePickle))(
                Control:=panel,
                eventAdder:=Sub(action)
                                AddHandler keyControl.ValueChanged, Sub() action()
                                AddHandler valueControl.ValueChanged, Sub() action()
                                handlers.Add(action)
                            End Sub,
                getter:=Function() New KeyValuePair(Of TKey, ISimplePickle)(keyControl.Value, _valueJars(keyControl.Value).Value.Pack(valueControl.Value)),
                setter:=Sub(value)
                            keyControl.Value = value.Key
                            valueControl.Value = value.Value.Value
                        End Sub)
        End Function
    End Class
End Namespace
