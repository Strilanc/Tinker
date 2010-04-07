Namespace Pickling
    '''<summary>Pickles tuples of values as dictionaries keyed by jar name.</summary>
    Public Class TupleJar
        Inherits BaseJar(Of NamedValueMap)

        Private ReadOnly _subJars As IEnumerable(Of ISimpleNamedJar)
        Private ReadOnly _useSingleLineDescription As Boolean

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_subJars IsNot Nothing)
        End Sub

        Public Sub New(ByVal useSingleLineDescription As Boolean,
                       ByVal ParamArray subJars() As ISimpleNamedJar)
            Contract.Requires(subJars IsNot Nothing)
            Me._subJars = subJars
            Me._useSingleLineDescription = useSingleLineDescription
        End Sub
        Public Sub New(ByVal ParamArray subJars() As ISimpleNamedJar)
            Me.New(False, subJars)
            Contract.Requires(subJars IsNot Nothing)
        End Sub

        <ContractVerification(False)>
        Public Overrides Function Pack(ByVal value As NamedValueMap) As IEnumerable(Of Byte)
            If value.Count > _subJars.Count Then Throw New PicklingException("Too many keys in dictionary")
            Dim missingKeys = From subJar In _subJars Where Not value.ContainsKey(subJar.Name)
            If missingKeys.Any Then Throw New PicklingException("Missing key in dictionary: {0}.".Frmt(missingKeys.First))
            Return Concat(From subJar In _subJars Select subJar.Pack(value.ItemRaw(subJar.Name)))
        End Function

        <ContractVerification(False)>
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As ParsedValue(Of NamedValueMap)
            Dim vals = New Dictionary(Of InvariantString, Object)
            Dim usedDataCount = 0
            For Each subjar In _subJars
                Contract.Assume(subjar IsNot Nothing)
                Dim parsed = subjar.Parse(data.SubView(usedDataCount))
                vals(subjar.Name) = parsed.Value
                usedDataCount += parsed.UsedDataCount
                Contract.Assume(usedDataCount <= data.Count)
            Next subjar

            Return New NamedValueMap(vals).ParsedWithDataCount(usedDataCount)
        End Function

        Public Overrides Function Describe(ByVal value As NamedValueMap) As String
            Return (From subJar In _subJars Select subJar.Describe(value.ItemRaw(subJar.Name))).MakeListDescription(_useSingleLineDescription)
        End Function
        Public Overrides Function Parse(ByVal text As String) As NamedValueMap
            Dim lines = text.SplitListDescription(_useSingleLineDescription)
            Dim result = _subJars.Zip(lines).ToDictionary(
                keySelector:=Function(pair) pair.Item1.Name,
                elementSelector:=Function(pair) pair.Item1.Parse(pair.Item2))
            If lines.Count > _subJars.Count Then Throw New PicklingException("Extra lines found after tuple finished.")
            Return result
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of NamedValueMap)
            Dim subControls = (From subJar In _subJars Select (subJar.MakeControl())).Cache
            Dim panel = PanelWithControls((From c In subControls Select c.Control),
                                          borderStyle:=BorderStyle.FixedSingle)
            For Each subControl In subControls
                AddHandler subControl.AssumeNotNull.ValueChanged, Sub() LayoutPanel(panel)
            Next subControl
            Return New DelegatedValueEditor(Of NamedValueMap)(
                Control:=panel,
                eventAdder:=Sub(action)
                                For Each subControl In subControls
                                    AddHandler subControl.ValueChanged, Sub() action()
                                Next subControl
                            End Sub,
                getter:=Function() _subJars.Zip(subControls).ToDictionary(Function(e) e.Item1.Name, Function(e) e.Item2.Value),
                setter:=Sub(value)
                            For Each pair In _subJars.Zip(subControls)
                                Dim c = pair.Item2
                                Dim v = value.ItemRaw(pair.Item1.Name)
                                If Not c.Value.Equals(v) Then c.Value = v
                            Next pair
                        End Sub,
                disposer:=Sub()
                              For Each subControl In subControls
                                  subControl.Dispose()
                              Next subControl
                              panel.Dispose()
                          End Sub)
        End Function
    End Class
End Namespace
