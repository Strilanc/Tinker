Imports Tinker.Pickling

Namespace WC3.Protocol
    Public NotInheritable Class OrderIdJar
        Inherits BaseJar(Of OrderId)

        Private Shared ReadOnly DataJar As New EnumUInt32Jar(Of OrderId)(checkDefined:=False)

        Public Overrides Function Pack(Of TValue As OrderId)(ByVal value As TValue) As IPickle(Of TValue)
            Return DataJar.Pack(value).With(jar:=Me, description:=Function() DescribeValue(value))
        End Function
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of OrderId)
            Dim pickle = DataJar.Parse(data)
            Return pickle.With(jar:=Me, description:=Function() DescribeValue(pickle.Value))
        End Function

        Private Function DescribeValue(ByVal value As OrderId) As String
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
