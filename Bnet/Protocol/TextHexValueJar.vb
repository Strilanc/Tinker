Imports Tinker.Pickling

Namespace Bnet.Protocol
    Public NotInheritable Class TextHexUInt32Jar
        Inherits BaseJar(Of UInt32)
        Private ReadOnly digitCount As Integer
        Private ReadOnly byteOrder As ByteOrder

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(digitCount > 0)
        End Sub

        Public Sub New(ByVal digitCount As Integer,
                       Optional ByVal byteOrder As ByteOrder = byteOrder.LittleEndian)
            Contract.Requires(digitCount > 0)
            Contract.Requires(digitCount <= 16)
            Me.digitCount = digitCount
            Me.byteOrder = byteOrder
        End Sub

        Public Overrides Function Pack(Of TValue As UInt32)(ByVal value As TValue) As IPickle(Of TValue)
            Dim u = DirectCast(value, UInt32)
            Dim digits As IList(Of Byte) = u.ToString("x{0}".Frmt(digitCount), CultureInfo.InvariantCulture).ToAscBytes
            Contract.Assume(digits.Count >= digitCount)
            If digits.Count > digitCount Then Throw New PicklingException("Value {0} is too large to fit into {1} hex digits.".Frmt(value, digitCount))

            Select Case byteOrder
                Case byteOrder.BigEndian
                    'no change
                Case byteOrder.LittleEndian
                    digits = digits.Reverse
                Case Else
                    Throw byteOrder.MakeImpossibleValueException()
            End Select

            Return value.Pickled(Me, digits.AsReadableList)
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of UInt32)
            If data.Count < digitCount Then Throw New PicklingNotEnoughDataException()
            Dim datum = data.SubView(0, digitCount)
            Dim value = CUInt(datum.ParseChrString(nullTerminated:=False).FromHexToUInt64(byteOrder))
            Return value.Pickled(Me, datum)
        End Function
    End Class
End Namespace
