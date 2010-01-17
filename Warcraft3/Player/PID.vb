Namespace WC3
    ''' <summary>Stores a unique-per-game-per-instance player index in [1, 12].</summary>
    <DebuggerDisplay("{ToString}")>
    Public Structure PID
        Implements IEquatable(Of PID)

        Private ReadOnly _index As Byte

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_index >= 0)
            Contract.Invariant(_index < 12)
        End Sub

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

        Public Shared Operator =(ByVal value1 As PID, ByVal value2 As PID) As Boolean
            Return value1._index = value2._index
        End Operator
        Public Shared Operator <>(ByVal value1 As PID, ByVal value2 As PID) As Boolean
            Return Not value1 = value2
        End Operator

        Public Overrides Function GetHashCode() As Integer
            Return _index.GetHashCode
        End Function
        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            If Not TypeOf obj Is PID Then Return False
            Return Me = CType(obj, PID)
        End Function
        Public Overloads Function Equals(ByVal other As PID) As Boolean Implements System.IEquatable(Of PID).Equals
            Return Me = other
        End Function

        Public Overrides Function ToString() As String
            Return Index.ToString(CultureInfo.InvariantCulture)
        End Function
    End Structure
End Namespace
