Namespace Pickling.Jars
    '''<summary>Pickles fixed-size lists of bytes.</summary>
    Public Class RawDataJar
        Inherits BaseJar(Of IReadableList(Of Byte))
        Private ReadOnly _size As Integer

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_size > 0)
        End Sub

        Public Sub New(ByVal name As InvariantString, ByVal size As Integer)
            MyBase.New(name)
            Contract.Requires(size > 0)
            Me._size = size
        End Sub

        Public Overrides Function Pack(Of TValue As IReadableList(Of Byte))(ByVal value As TValue) As IPickle(Of TValue)
            If value.Count <> _size Then Throw New PicklingException("Byte array is not of the correct length.")
            Dim data = value
            Return New Pickle(Of TValue)(Me.Name, value, data, Function() "[{0}]".Frmt(value.ToHexString))
        End Function
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of IReadableList(Of Byte))
            If data.Count < _size Then Throw New PicklingException("Not enough data to parse array. Data ended before size prefix could be read.")
            Dim datum = data.SubView(0, _size)
            Dim value = datum
            Return New Pickle(Of IReadableList(Of Byte))(Me.Name, value, datum, Function() "[{0}]".Frmt(value.ToHexString))
        End Function
    End Class
End Namespace
