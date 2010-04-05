Namespace Pickling
    '''<summary>The identity jar. Pickles data as itself.</summary>
    Public Class DataJar
        Inherits BaseJar(Of IReadableList(Of Byte))

        Public Overrides Function Pack(ByVal value As IReadableList(Of Byte)) As IEnumerable(Of Byte)
            Return value.AssumeNotNull
        End Function
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As ParsedValue(Of IReadableList(Of Byte))
            Return data.ParsedWithDataCount(data.Count)
        End Function

        <ContractVerification(False)>
        Public Overrides Function Describe(ByVal value As IReadableList(Of Byte)) As String
            Return "[{0}]".Frmt(value.ToHexString)
        End Function
        Public Overrides Function Parse(ByVal text As String) As IReadableList(Of Byte)
            Try
                Dim byteText = text
                If byteText.StartsWith("[") Then byteText = byteText.Substring(1)
                If byteText.EndsWith("]") Then byteText = byteText.Substring(0, byteText.Length - 1)
                Return (From word In byteText.Split({" "}, StringSplitOptions.RemoveEmptyEntries)
                        Select Byte.Parse(word, NumberStyles.HexNumber, CultureInfo.InvariantCulture)
                        ).ToReadableList
            Catch ex As Exception When TypeOf ex Is ArgumentException OrElse
                                       TypeOf ex Is FormatException
                Throw New PicklingException("Invalid hex data.", ex)
            End Try
        End Function
    End Class
End Namespace
