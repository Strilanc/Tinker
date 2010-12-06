Namespace Pickling
    '''<summary>Pickles empty values as 0-length data.</summary>
    Public NotInheritable Class EmptyJar
        Inherits BaseJar(Of NoValue)

        Public Overrides Function Pack(ByVal value As NoValue) As IEnumerable(Of Byte)
            Return New Byte() {}
        End Function
        Public Overrides Function Parse(ByVal data As IRist(Of Byte)) As ParsedValue(Of NoValue)
            Return New NoValue().ParsedWithDataCount(0)
        End Function
        <ContractVerification(False)>
        Public Overrides Function Parse(ByVal text As String) As NoValue
            If text <> "[No Data]" Then Throw New PicklingException("Not [No Data].")
            Return New NoValue()
        End Function
        Public Overrides Function Describe(ByVal value As NoValue) As String
            Return "[No Data]"
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of NoValue)
            Dim control = New Label()
            control.Text = "[No Data]"
            Return New DelegatedValueEditor(Of NoValue)(
                control:=control,
                eventAdder:=Sub(action)
                            End Sub,
                getter:=Function() New NoValue(),
                setter:=Sub(value)
                        End Sub,
                disposer:=Sub() control.Dispose())
        End Function
    End Class
End Namespace
