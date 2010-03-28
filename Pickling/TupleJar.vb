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

        Public Overrides Function Pack(Of TValue As NamedValueMap)(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            If value.Count > _subJars.Count Then Throw New PicklingException("Too many keys in dictionary")

            'Pack
            Dim pickles = New List(Of ISimplePickle)
            For Each subJar In _subJars
                Contract.Assume(subJar IsNot Nothing)
                If Not value.ContainsKey(subJar.Name) Then Throw New PicklingException("Key '{0}' missing from tuple dictionary.".Frmt(subJar.Name))
                pickles.Add(subJar.Pack(value.ItemRaw(subJar.Name)))
            Next subJar

            Dim data = Concat(From p In pickles Select (p.Data)).ToReadableList
            Return value.Pickled(Me, data, Function() pickles.MakeListDescription(_useSingleLineDescription))
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
            Return value.Pickled(Me, datum, Function() pickles.MakeListDescription(_useSingleLineDescription))
        End Function

        Public Overrides Function ValueToControl(ByVal value As NamedValueMap) As Control
            Dim control = New TableLayoutPanel()
            control.ColumnCount = 1
            control.AutoSize = True
            control.AutoSizeMode = AutoSizeMode.GrowAndShrink
            control.BorderStyle = BorderStyle.FixedSingle

            For Each subJar In _subJars
                Dim c = subJar.ValueToControl(value.ItemRaw(subJar.Name))
                control.Controls.Add(c)
                c.Width = control.Width
                c.Anchor = AnchorStyles.Left Or AnchorStyles.Top Or AnchorStyles.Right
            Next subJar

            Return control
        End Function
        Public Overrides Function ControlToValue(ByVal control As Control) As NamedValueMap
            Return _subJars.Zip(From i In control.Controls.Count.Range Select control.Controls(i)).ToDictionary(
                        keySelector:=Function(e) e.Item1.Name,
                        elementSelector:=Function(e) e.Item1.ControlToValue(e.Item2))
        End Function
    End Class
End Namespace
