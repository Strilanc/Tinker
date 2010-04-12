<DebuggerDisplay("{ToString}")>
Public Structure Maybe(Of T)
    Implements IEquatable(Of Maybe(Of T))

    Private ReadOnly _value As T
    Private ReadOnly _hasValue As Boolean

    Public Sub New(ByVal value As T)
        Contract.Ensures(Me.HasValue)
        Me._hasValue = True
        Me._value = value
    End Sub

    Public ReadOnly Property HasValue As Boolean
        Get
            Return _hasValue
        End Get
    End Property
    Public ReadOnly Property Value As T
        Get
            Contract.Requires(HasValue)
            Return _value
        End Get
    End Property

    Public Shared Widening Operator CType(ByVal value As T) As Maybe(Of T)
        Return New Maybe(Of T)(value)
    End Operator

    Public Overloads Function Equals(ByVal other As Maybe(Of T)) As Boolean Implements IEquatable(Of Maybe(Of T)).Equals
        If Me.HasValue <> other.HasValue Then Return False
        If Not Me.HasValue Then Return True
        If Me.Value Is Nothing Then
            Return other.Value Is Nothing
        Else
            Return Me.Value.Equals(other.Value)
        End If
    End Function
    Public Overrides Function Equals(ByVal obj As Object) As Boolean
        Return TypeOf obj Is Maybe(Of T) AndAlso Me.Equals(DirectCast(obj, Maybe(Of T)))
    End Function
    Public Overrides Function GetHashCode() As Integer
        Return If(HasValue, _value.GetHashCode, 0)
    End Function
    Public Overrides Function ToString() As String
        Return If(HasValue, "Value: {0}".Frmt(Value), "No Value")
    End Function
    Public Shared Operator =(ByVal value1 As Maybe(Of T), ByVal value2 As Maybe(Of T)) As Boolean
        Return value1.Equals(value2)
    End Operator
    Public Shared Operator <>(ByVal value1 As Maybe(Of T), ByVal value2 As Maybe(Of T)) As Boolean
        Return Not value1.Equals(value2)
    End Operator
End Structure
