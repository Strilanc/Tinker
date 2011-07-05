Namespace Pickling
    Public NotInheritable Class KeyPrefixedJar(Of TKey)
        Inherits BaseJar(Of KeyValuePair(Of TKey, Object))

        Private ReadOnly _keyJar As IJar(Of TKey)
        Private ReadOnly _valueJars As New Dictionary(Of TKey, NonNull(Of IJar(Of Object)))
        Private ReadOnly _useSingleLineDescription As Boolean

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_keyJar IsNot Nothing)
            Contract.Invariant(_valueJars IsNot Nothing)
        End Sub

        Public Sub New(keyJar As IJar(Of TKey),
                       valueJars As Dictionary(Of TKey, IJar(Of Object)),
                       Optional useSingleLineDescription As Boolean = True)
            Contract.Requires(keyJar IsNot Nothing)
            Contract.Requires(valueJars IsNot Nothing)
            Me._keyJar = keyJar
            Me._valueJars = valueJars.ToDictionary(Function(e) e.Key, Function(e) e.Value.AsNonNull)
            Me._useSingleLineDescription = useSingleLineDescription
        End Sub

        Public Overrides Function Pack(value As KeyValuePair(Of TKey, Object)) As IRist(Of Byte)
            If value.Key Is Nothing Then Throw New ArgumentNullException("value.Key")
            If value.Value Is Nothing Then Throw New ArgumentNullException("value.Value")
            If Not _valueJars.ContainsKey(value.Key) Then Throw New PicklingException("No subjar with key {0}.".Frmt(value.Key))
            Dim keyData = _keyJar.Pack(value.Key)
            Dim valueData = _valueJars(value.Key).Value.Pack(value.Value)
            Return keyData.Concat(valueData).ToRist()
        End Function
        Public Overrides Function Parse(data As IRist(Of Byte)) As ParsedValue(Of KeyValuePair(Of TKey, Object))
            Dim parsedKey = _keyJar.Parse(data)
            If Not _valueJars.ContainsKey(parsedKey.Value) Then Throw New PicklingException("No subjar with key {0}.".Frmt(parsedKey.Value))
            Dim parsedValue = _valueJars(parsedKey.Value).Value.Parse(data.SkipExact(parsedKey.UsedDataCount))

            Dim value = parsedKey.Value.KeyValue(parsedValue.Value)
            Return value.ParsedWithDataCount(parsedKey.UsedDataCount + parsedValue.UsedDataCount)
        End Function

        Public Overrides Function Describe(value As KeyValuePair(Of TKey, Object)) As String
            If value.Key Is Nothing Then Throw New ArgumentNullException("value.Key")
            If value.Value Is Nothing Then Throw New ArgumentNullException("value.Value")
            Dim keyDesc = _keyJar.Describe(value.Key)
            Dim valueDesc = _valueJars(value.Key).Value.Describe(value.Value)
            Return If(_useSingleLineDescription,
                      "{0}: {1}".Frmt(keyDesc, valueDesc),
                      "{0}, {1}".Frmt(keyDesc, valueDesc))
        End Function
        <SuppressMessage("Microsoft.Contracts", "Ensures-28-210")>
        Public Overrides Function Parse(text As String) As KeyValuePair(Of TKey, Object)
            Dim divider = If(_useSingleLineDescription, ":", ",")
            Dim p = text.IndexOf(divider, StringComparison.Ordinal)
            If p < 0 Then Throw New PicklingException("Expected key{0}value style.".Frmt(divider))
            Dim key = _keyJar.Parse(text.Substring(0, p).TrimEnd)
            If Not _valueJars.ContainsKey(key) Then Throw New PicklingException("No subjar with key {0}.".Frmt(key))
            Return key.KeyValue(_valueJars(key).Value.Parse(text.Substring(p + divider.Length).TrimStart))
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of KeyValuePair(Of TKey, Object))
            Dim keyControl = _keyJar.MakeControl()
            If Not _valueJars.ContainsKey(keyControl.Value) Then
                keyControl.Value = _valueJars.Keys.First.AssumeNotNull
            End If
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

            Return New DelegatedValueEditor(Of KeyValuePair(Of TKey, Object))(
                Control:=panel,
                eventAdder:=Sub(action)
                                AddHandler keyControl.ValueChanged, Sub() action()
                                AddHandler valueControl.ValueChanged, Sub() action()
                                handlers.Add(action)
                            End Sub,
                getter:=Function() keyControl.Value.KeyValue(valueControl.Value),
                setter:=Sub(value)
                            keyControl.SetValueIfDifferent(value.Key)
                            valueControl.SetValueIfDifferent(value.Value)
                        End Sub,
                disposer:=Sub()
                              keyControl.Dispose()
                              valueControl.Dispose()
                              panel.Dispose()
                          End Sub)
        End Function
    End Class
End Namespace
