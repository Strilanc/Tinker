Namespace Pickling
    '''<summary>Pickles tuples of values as dictionaries keyed by jar name.</summary>
    Public Class TupleJar
        Inherits BaseJar(Of NamedValueMap)

        Private ReadOnly _subJars As IEnumerable(Of INamedJar(Of Object))
        Private ReadOnly _useSingleLineDescription As Boolean

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_subJars IsNot Nothing)
        End Sub

        Public Sub New(Optional subJars As IEnumerable(Of INamedJar(Of Object)) = Nothing,
                       Optional useSingleLineDescription As Boolean = False)
            Me._subJars = If(subJars, {})
            Me._useSingleLineDescription = useSingleLineDescription
        End Sub
        Public Function [Then](Of T)(jar As INamedJar(Of T)) As TupleJar
            Contract.Requires(jar IsNot Nothing)
            Contract.Ensures(Contract.Result(Of TupleJar)() IsNot Nothing)
            Return New TupleJar(Me._subJars.Append(jar.Weaken()).ToArray())
        End Function
        Public Function WithSingleLineDescription(Optional value As Boolean = True) As TupleJar
            Contract.Ensures(Contract.Result(Of TupleJar)() IsNot Nothing)
            Return New TupleJar(Me._subJars, value)
        End Function

        Public Overrides Function Pack(value As NamedValueMap) As IRist(Of Byte)
            Contract.Assume(value IsNot Nothing)
            If value.Count > _subJars.Count Then Throw New PicklingException("Too many keys in dictionary")
            Dim missingKeys = From subJar In _subJars Where Not value.ContainsKey(subJar.Name)
            If missingKeys.Any Then Throw New PicklingException("Missing key in dictionary: {0}.".Frmt(missingKeys.First))
            Return Concat(From subJar In _subJars Select subJar.Pack(value.ItemRaw(subJar.Name))).ToRist()
        End Function

        Public Overrides Function Parse(data As IRist(Of Byte)) As ParsedValue(Of NamedValueMap)
            Dim vals = New Dictionary(Of InvariantString, Object)
            Dim usedDataCount = 0
            For Each subjar In _subJars
                Contract.Assume(subjar IsNot Nothing)
                Dim parsed = subjar.Parse(data.SkipExact(usedDataCount))
                vals(subjar.Name) = parsed.Value
                usedDataCount += parsed.UsedDataCount
                Contract.Assume(usedDataCount <= data.Count)
            Next subjar

            Return New NamedValueMap(vals).ParsedWithDataCount(usedDataCount)
        End Function

        Public Overrides Function Describe(value As NamedValueMap) As String
            Return (From subJar In _subJars Select subJar.Describe(value.ItemRaw(subJar.Name))).MakeListDescription(_useSingleLineDescription)
        End Function
        Public Overrides Function Parse(text As String) As NamedValueMap
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
                                pair.Item2.SetValueIfDifferent(value.ItemRaw(pair.Item1.Name))
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
