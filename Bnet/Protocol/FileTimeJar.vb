Imports Tinker.Pickling

Namespace Bnet.Protocol
    Public NotInheritable Class FileTimeJar
        Inherits BaseFixedSizeJar(Of DateTime)

        Public Overrides Function Pack(ByVal value As DateTime) As IEnumerable(Of Byte)
            Return value.ToFileTime.BitwiseToUInt64.Bytes()
        End Function

        Protected Overrides ReadOnly Property DataSize As UInt16
            Get
                Return 8
            End Get
        End Property
        <ContractVerification(False)>
        Protected Overrides Function FixedSizeParse(ByVal data As IRist(Of Byte)) As DateTime
            Return DateTime.FromFileTime(data.ToUInt64.BitwiseToInt64)
        End Function

        Public Overrides Function Describe(ByVal value As Date) As String
            Return value.ToString(CultureInfo.InvariantCulture)
        End Function
        <ContractVerification(False)>
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
