Namespace Pickling
    '''<summary>Pickles empty values as 0-length data.</summary>
    Public NotInheritable Class EmptyJar
        Inherits BaseJar(Of EmptyValue)

        Public Structure EmptyValue
            Implements IEquatable(Of EmptyValue)
            Public Overrides Function ToString() As String
                Return "[No Data]"
            End Function
            Public Overrides Function GetHashCode() As Integer
                Return 0
            End Function
            Public Overrides Function Equals(ByVal obj As Object) As Boolean
                Return TypeOf obj Is EmptyValue
            End Function
            Public Overloads Function Equals(ByVal other As EmptyValue) As Boolean Implements IEquatable(Of EmptyValue).Equals
                Return True
            End Function
            Public Shared Operator =(ByVal value1 As EmptyValue, ByVal value2 As EmptyValue) As Boolean
                Return value1.Equals(value2)
            End Operator
            Public Shared Operator <>(ByVal value1 As EmptyValue, ByVal value2 As EmptyValue) As Boolean
                Return Not value1.Equals(value2)
            End Operator
        End Structure

        Public Overrides Function Pack(ByVal value As EmptyValue) As IEnumerable(Of Byte)
            Return New Byte() {}
        End Function
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As ParsedValue(Of EmptyValue)
            Return New EmptyValue().ParsedWithDataCount(0)
        End Function
        <ContractVerification(False)>
        Public Overrides Function Parse(ByVal text As String) As EmptyValue
            If text <> "[No Data]" Then Throw New PicklingException("Not [No Data].")
            Return New EmptyValue()
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of EmptyValue)
            Dim control = New Label()
            control.Text = "[No Data]"
            Return New DelegatedValueEditor(Of EmptyValue)(
                control:=control,
                eventAdder:=Sub(action)
                            End Sub,
                getter:=Function() New EmptyValue(),
                setter:=Sub(value)
                        End Sub,
                disposer:=Sub() control.Dispose())
        End Function
    End Class
End Namespace
