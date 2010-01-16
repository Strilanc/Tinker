Imports Tinker.Pickling

Namespace Bnet.Protocol
    Public NotInheritable Class TextHexValueJar
        Inherits BaseJar(Of ULong)
        Private ReadOnly numDigits As Integer
        Private ReadOnly byteOrder As ByteOrder

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(numDigits > 0)
        End Sub

        Public Sub New(ByVal name As InvariantString,
                       ByVal numDigits As Integer,
                       Optional ByVal byteOrder As ByteOrder = byteOrder.LittleEndian)
            MyBase.New(name)
            Contract.Requires(numDigits > 0)
            Contract.Requires(numDigits <= 16)
            Me.numDigits = numDigits
            Me.byteOrder = byteOrder
        End Sub

        Public Overrides Function Pack(Of TValue As ULong)(ByVal value As TValue) As IPickle(Of TValue)
            Dim u = CULng(value)
            Dim digits As IList(Of Byte) = CULng(value).ToString("x{0}".Frmt(numDigits)).ToAscBytes
            Contract.Assume(digits.Count >= numDigits)
            If digits.Count > numDigits Then Throw New PicklingException("Value {0} is too large to fit into {1} hex digits.".Frmt(value, numDigits))

            Select Case byteOrder
                Case byteOrder.BigEndian
                    'no change
                Case byteOrder.LittleEndian
                    digits = digits.Reverse
                Case Else
                    Throw byteOrder.MakeImpossibleValueException()
            End Select

            Return New Pickle(Of TValue)(Me.Name, value, digits.AsReadableList)
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of ULong)
            If data.Count < numDigits Then Throw New PicklingException("Not enough data")
            data = data.SubView(0, numDigits)
            Dim value = data.ParseChrString(nullTerminated:=False).FromHexToUInt64(byteOrder)
            Return New Pickle(Of ULong)(Me.Name, value, data)
        End Function
    End Class
End Namespace
