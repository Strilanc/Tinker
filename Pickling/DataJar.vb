Namespace Pickling
    '''<summary>The identity jar. Pickles data as itself.</summary>
    Public Class DataJar
        Inherits BaseJar(Of IRist(Of Byte))

        Public Overrides Function Pack(ByVal value As IRist(Of Byte)) As IRist(Of Byte)
            Return value.AssumeNotNull
        End Function
        Public Overrides Function Parse(ByVal data As IRist(Of Byte)) As ParsedValue(Of IRist(Of Byte))
            Return data.ParsedWithDataCount(data.Count)
        End Function

        Public Overrides Function Describe(ByVal value As IRist(Of Byte)) As String
            Contract.Assume(value IsNot Nothing)
            Return "[{0}]".Frmt(value.ToHexString)
        End Function
        Public Overrides Function Parse(ByVal text As String) As IRist(Of Byte)
            Try
                Dim byteText = text
                If byteText.StartsWith("[", StringComparison.Ordinal) Then byteText = byteText.Substring(1)
                If byteText.EndsWith("]", StringComparison.Ordinal) Then byteText = byteText.Substring(0, byteText.Length - 1)
                Return (From word In byteText.Split({" "}, StringSplitOptions.RemoveEmptyEntries)
                        Select Byte.Parse(word, NumberStyles.HexNumber, CultureInfo.InvariantCulture)
                        ).ToRist()
            Catch ex As Exception When TypeOf ex Is ArgumentException OrElse
                                       TypeOf ex Is FormatException
                Throw New PicklingException("Invalid hex data.", ex)
            End Try
        End Function
    End Class
End Namespace
