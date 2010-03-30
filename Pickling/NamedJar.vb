Namespace Pickling
    <DebuggerDisplay("{ToString}")>
    Public NotInheritable Class NamedJar(Of T)
        Inherits BaseJar(Of T)
        Implements INamedJar(Of T)
        Private ReadOnly _name As InvariantString
        Private ReadOnly _subJar As IJar(Of T)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_subJar IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As InvariantString, ByVal subJar As IJar(Of T))
            Contract.Requires(subJar IsNot Nothing)
            Me._name = name
            Me._subJar = subJar
        End Sub

        Public ReadOnly Property Name As InvariantString Implements INamedJar(Of T).Name
            Get
                Return _name
            End Get
        End Property

        Public Overrides Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue)
            Return _subJar.Pack(value).With(jar:=Me)
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T)
            Return _subJar.Parse(data).With(jar:=Me)
        End Function

        Public Overrides Function Describe(ByVal value As T) As String
            Return "{0}: {1}".Frmt(Name, _subJar.Describe(value))
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of T)
            Dim label = New Label()
            Dim subControl = _subJar.MakeControl()
            label.AutoSize = True
            label.Text = Me.Name
            Dim panel = PanelWithControls({label, subControl.Control}, spacing:=0)
            AddHandler subControl.ValueChanged, Sub() LayoutPanel(panel, spacing:=0)
            Return New DelegatedValueEditor(Of T)(
                Control:=panel,
                eventAdder:=Sub(action) AddHandler subControl.ValueChanged, Sub() action(),
                getter:=Function() subControl.Value,
                setter:=Sub(value) subControl.Value = value)
        End Function

        Public Overrides Function ToString() As String
            Return _name
        End Function
    End Class
End Namespace
