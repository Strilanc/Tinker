Imports Tinker.Pickling

Namespace Bnet.Protocol
    Public NotInheritable Class FileTimeJar
        Inherits BaseFixedSizeJar(Of DateTime)

        Public Overrides Function Pack(value As DateTime) As IRist(Of Byte)
            Return value.ToFileTime.BitwiseToUInt64.Bytes()
        End Function

        Protected Overrides ReadOnly Property DataSize As UInt16
            Get
                Return 8
            End Get
        End Property
        Protected Overrides Function FixedSizeParse(data As IRist(Of Byte)) As DateTime
            Contract.Assume(data.Count = 8)
            Dim fileTime = data.ToUInt64.BitwiseToInt64
            Contract.Assume(fileTime >= 0)
            If fileTime > &H24C85A5ED1C03FFFL Then Throw New PicklingException("Invalid time.")
            Return DateTime.FromFileTime(fileTime)
        End Function

        Public Overrides Function Describe(value As Date) As String
            Return value.ToString(CultureInfo.InvariantCulture)
        End Function
        <SuppressMessage("Microsoft.Contracts", "Ensures-28-90")>
        Public Overrides Function Parse(text As String) As DateTime
            Try
                Return DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.None)
            Catch ex As Exception When TypeOf ex Is ArgumentException OrElse
                                       TypeOf ex Is FormatException
                Throw New PicklingException("'{0}' is not a DateTime.".Frmt(text), ex)
            End Try
        End Function
    End Class
End Namespace
