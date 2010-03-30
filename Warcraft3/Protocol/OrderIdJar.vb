Imports Tinker.Pickling

Namespace WC3.Protocol
    Public NotInheritable Class OrderIdJar
        Inherits BaseJar(Of OrderId)

        Private Shared ReadOnly DataJar As New EnumUInt32Jar(Of OrderId)(checkDefined:=False)

        Public Overrides Function Pack(ByVal value As OrderId) As IEnumerable(Of Byte)
            Return DataJar.Pack(value)
        End Function
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of OrderId)
            Return DataJar.Parse(data).With(jar:=Me)
        End Function

        Public Overrides Function Describe(ByVal value As OrderId) As String
            If value >= &HD0000 AndAlso value < &HE0000 Then
                If value.EnumValueIsDefined() Then
                    Return value.ToString
                Else
                    Return "0x" + CUInt(value).ToString("X5", CultureInfo.InvariantCulture)
                End If
            Else
                Return GameActions.TypeIdString(value)
            End If
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of OrderId)
            Return DataJar.MakeControl()
        End Function
    End Class
End Namespace
