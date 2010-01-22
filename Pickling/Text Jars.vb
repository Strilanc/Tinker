Namespace Pickling
    '''<summary>Pickles fixed-length strings.</summary>
    Public Class FixedSizeStringJar
        Inherits BaseJar(Of String)
        Private ReadOnly _size As Integer

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_size > 0)
        End Sub

        Public Sub New(ByVal name As InvariantString,
                       ByVal size As Integer)
            MyBase.New(name)
            Contract.Requires(size > 0)
            Me._size = size
        End Sub

        Public Overrides Function Pack(Of TValue As String)(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            If value.Length <> _size Then Throw New PicklingException("Requires strings of size {0}.".Frmt(_size))
            Dim data = value.ToAscBytes.AsReadableList
            Return New Pickle(Of TValue)(Me.Name, value, data, Function() """{0}""".Frmt(value))
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of String)
            If data.Count < _size Then Throw New PicklingException("Not enough data.")
            Dim datum = data.Take(_size).ToArray.AsReadableList
            Dim value = datum.ParseChrString(nullTerminated:=False)
            Return New Pickle(Of String)(Me.Name, value, datum, Function() """{0}""".Frmt(value))
        End Function
    End Class

    '''<summary>Pickles variable-length strings.</summary>
    Public Class NullTerminatedStringJar
        Inherits BaseJar(Of String)
        Private ReadOnly _maximumContentSize As Integer
        Private ReadOnly _allowMissingNullTerminator As Boolean

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_maximumContentSize >= 0)
        End Sub

        Public Sub New(ByVal name As InvariantString,
                       Optional ByVal allowMissingNullTerminator As Boolean = False,
                       Optional ByVal maximumContentSize As Integer = 0)
            MyBase.New(name)
            Contract.Requires(maximumContentSize >= 0)
            Me._maximumContentSize = maximumContentSize
            Me._allowMissingNullTerminator = allowMissingNullTerminator
        End Sub

        Public Overrides Function Pack(Of TValue As String)(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            If _maximumContentSize > 0 AndAlso value.Length > _maximumContentSize Then Throw New PicklingException("String length exceeded maximum size.")
            Dim data = value.ToAscBytes.Concat({0}).ToArray.AsReadableList
            Return New Pickle(Of TValue)(Me.Name, value, data, Function() """{0}""".Frmt(value))
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of String)
            Dim p = data.IndexOf(0)
            If p < 0 Then
                If _allowMissingNullTerminator Then
                    p = data.Count - 1
                Else
                    Throw New PicklingException("Data ended before null terminator found for string {0}".Frmt(Name))
                End If
            End If
            Contract.Assume(p < data.Count)
            Dim datum = data.SubView(0, p + 1)
            Dim value = datum.ParseChrString(nullTerminated:=True)
            If _maximumContentSize > 0 AndAlso value.Length > _maximumContentSize Then Throw New PicklingException("String length exceeded maximum size.")
            Return New Pickle(Of String)(Me.Name, value, datum, Function() """{0}""".Frmt(value))
        End Function
    End Class
End Namespace
