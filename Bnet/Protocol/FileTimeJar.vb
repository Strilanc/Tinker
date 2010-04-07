Imports Tinker.Pickling

Namespace Bnet.Protocol
    Public NotInheritable Class FileTimeJar
        Inherits BaseJar(Of DateTime)

        Public Overrides Function Pack(ByVal value As DateTime) As IEnumerable(Of Byte)
            Return value.ToFileTime.BitwiseToUInt64.Bytes()
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As ParsedValue(Of DateTime)
            If data.Count < 8 Then Throw New PicklingNotEnoughDataException()
            Return DateTime.FromFileTime(data.Take(8).ToUInt64.BitwiseToInt64).ParsedWithDataCount(8)
        End Function

        Public Overrides Function Describe(ByVal value As Date) As String
            Return value.ToString(CultureInfo.InvariantCulture)
        End Function
        Public Overrides Function Parse(ByVal text As String) As DateTime
            Try
                Return DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.None)
            Catch ex As Exception When TypeOf ex Is ArgumentException OrElse
                                       TypeOf ex Is FormatException
                Throw New PicklingException("'{0}' is not a DateTime.".Frmt(text), ex)
            End Try
        End Function
    End Class
End Namespace
