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

        Public Overrides Function Pack(ByVal value As NamedValueMap) As IEnumerable(Of Byte)
            If value.Count > _subJars.Count Then Throw New PicklingException("Too many keys in dictionary")
            Dim missingKeys = From subJar In _subJars Where Not value.ContainsKey(subJar.Name)
            If missingKeys.Any Then Throw New PicklingException("Missing key in dictionary: {0}.".Frmt(missingKeys.First))
            Return Concat(From subJar In _subJars Select subJar.Pack(value.ItemRaw(subJar.Name)))
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of NamedValueMap)
            'Parse
            Dim vals = New Dictionary(Of InvariantString, Object)
            Dim pickles = New List(Of ISimplePickle)
            Dim curCount = data.Count
            Dim curOffset = 0
            For Each j In _subJars
                Contract.Assume(j IsNot Nothing)
                'Value
                Dim p = j.Parse(data.SubView(curOffset, curCount))
                vals(j.Name) = p.Value
                pickles.Add(p)
                'Size
                Dim n = p.Data.Count
                curCount -= n
                curOffset += n
                If curCount < 0 Then Throw New InvalidStateException("subJar lied about data used.")
            Next j

            Dim value = New NamedValueMap(vals)
            Dim datum = data.SubView(0, curOffset)
            Return value.Pickled(Me, datum)
        End Function

        Public Overrides Function Describe(ByVal value As NamedValueMap) As String
            Return (From subJar In _subJars Select subJar.Describe(value.ItemRaw(subJar.Name))).MakeListDescription(_useSingleLineDescription)
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of NamedValueMap)
            Dim subControls = (From subJar In _subJars Select (subJar.MakeControl())).Cache
            Dim panel = PanelWithControls((From c In subControls Select c.Control),
                                          borderStyle:=BorderStyle.FixedSingle)
            For Each subControl In subControls
                AddHandler subControl.ValueChanged, Sub() LayoutPanel(panel)
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
                                pair.Item2.Value = value.ItemRaw(pair.Item1.Name)
                            Next pair
                        End Sub)
        End Function
    End Class
End Namespace
