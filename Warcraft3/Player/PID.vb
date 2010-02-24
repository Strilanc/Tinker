Namespace WC3
    ''' <summary>Stores a unique-per-game-per-instant player index in [1, 12].</summary>
    <DebuggerDisplay("{ToString}")>
    Public Structure PlayerID
        Implements IEquatable(Of PlayerID)

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

        Public Shared Operator =(ByVal value1 As PlayerID, ByVal value2 As PlayerID) As Boolean
            Return value1._index = value2._index
        End Operator
        Public Shared Operator <>(ByVal value1 As PlayerID, ByVal value2 As PlayerID) As Boolean
            Return Not value1 = value2
        End Operator

        Public Overrides Function GetHashCode() As Integer
            Return _index.GetHashCode
        End Function
        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            If Not TypeOf obj Is PlayerID Then Return False
            Return Me = CType(obj, PlayerID)
        End Function
        Public Overloads Function Equals(ByVal other As PlayerID) As Boolean Implements IEquatable(Of PlayerID).Equals
            Return Me = other
        End Function

        Public Overrides Function ToString() As String
            Return Index.ToString(CultureInfo.InvariantCulture)
        End Function
    End Structure
End Namespace
