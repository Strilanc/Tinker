Imports Tinker.Pickling

Namespace WC3
    ''' <summary>Stores a unique-per-game-per-instant player index in [1, 12].</summary>
    <DebuggerDisplay("{ToString}")>
    Public Structure PlayerId
        Implements IEquatable(Of PlayerId)

        Private ReadOnly _index As Byte

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_index >= 0)
            Contract.Invariant(_index < 12)
        End Sub

        <CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2233:OperationsShouldNotOverflow", MessageId:="index-1")>
        Public Sub New(ByVal index As Byte)
            Contract.Requires(index > 0)
            Contract.Requires(index <= 12)
            Me._index = index - CByte(1)
        End Sub

        Public ReadOnly Property Index As Byte
            Get
                Contract.Ensures(Contract.Result(Of Byte)() > 0)
                Contract.Ensures(Contract.Result(Of Byte)() <= 12)
                Return _index + CByte(1)
            End Get
        End Property

        Public Shared Operator =(ByVal value1 As PlayerId, ByVal value2 As PlayerId) As Boolean
            Return value1._index = value2._index
        End Operator
        Public Shared Operator <>(ByVal value1 As PlayerId, ByVal value2 As PlayerId) As Boolean
            Return Not value1 = value2
        End Operator

        Public Overrides Function GetHashCode() As Integer
            Return _index.GetHashCode
        End Function
        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            If Not TypeOf obj Is PlayerId Then Return False
            Return Me = DirectCast(obj, PlayerId)
        End Function
        Public Overloads Function Equals(ByVal other As PlayerId) As Boolean Implements IEquatable(Of PlayerId).Equals
            Return Me = other
        End Function

        Public Overrides Function ToString() As String
            Return "pid{0}".Frmt(Index)
        End Function
    End Structure

    Public Class PlayerIdJar
        Inherits BaseJar(Of PlayerId)

        Public Overrides Function Pack(ByVal value As PlayerId) As IEnumerable(Of Byte)
            Return {value.Index}
        End Function
        <ContractVerification(False)>
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As ParsedValue(Of PlayerId)
            If data.Count < 1 Then Throw New PicklingNotEnoughDataException
            If data.First < 1 OrElse data.First > 12 Then Throw New PicklingException("Invalid player id: {0}".Frmt(data.First))
            Return New PlayerId(data.First).ParsedWithDataCount(1)
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of PlayerId)
            Dim label = New Label()
            Dim control = New NumericUpDown()
            control.Minimum = 1
            control.Maximum = 12
            control.Value = 1
            label.AutoSize = True
            label.Text = "pid"
            Dim panel = PanelWithControls({label, control}, leftToRight:=True)
            Return New DelegatedValueEditor(Of PlayerId)(
                control:=panel,
                eventAdder:=Sub(action) AddHandler control.ValueChanged, Sub() action(),
                getter:=Function() New PlayerId(CByte(control.Value)),
                setter:=Sub(value) control.Value = value.Index)
        End Function
    End Class
End Namespace
