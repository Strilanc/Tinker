Imports Tinker.Pickling

Namespace Bnet.Protocol
    Public NotInheritable Class TextHexUInt32Jar
        Inherits BaseJar(Of UInt32)
        Private ReadOnly _digitCount As Integer
        Private ReadOnly _byteOrder As ByteOrder

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_digitCount > 0)
        End Sub

        Public Sub New(ByVal digitCount As Integer,
                       Optional ByVal byteOrder As ByteOrder = ByteOrder.LittleEndian)
            Contract.Requires(digitCount > 0)
            Contract.Requires(digitCount <= 8)
            Me._digitCount = digitCount
            Me._byteOrder = byteOrder
        End Sub

        Public Overrides Function Pack(ByVal value As UInteger) As IEnumerable(Of Byte)
            Dim digits = value.ToString("x{0}".Frmt(_digitCount), CultureInfo.InvariantCulture).ToAscBytes
            Contract.Assume(digits.Length >= _digitCount)
            If digits.Length > _digitCount Then Throw New PicklingException("Value {0} is too large to fit into {1} hex digits.".Frmt(value, _digitCount))
            Select Case _byteOrder
                Case ByteOrder.BigEndian : Return digits  'no change
                Case ByteOrder.LittleEndian : Return digits.Reverse
                Case Else : Throw _byteOrder.MakeImpossibleValueException()
            End Select
            Return digits
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of UInt32)
            If data.Count < _digitCount Then Throw New PicklingNotEnoughDataException()
            Dim datum = data.SubView(0, _digitCount)
            Dim value = CUInt(datum.ParseChrString(nullTerminated:=False).FromHexToUInt64(_byteOrder))
            Return value.Pickled(Me, datum)
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of UInteger)
            Return New UInt32Jar().MakeControl()
        End Function
    End Class
End Namespace
