Namespace Pickling
    Public Class StringJar
        Inherits BaseJar(Of String)

        Private ReadOnly _maxCharCount As Integer?
        Private ReadOnly _minCharCount As Integer

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_minCharCount >= 0)
            'Contract.Invariant(Not _maxCharCount.HasValue OrElse _maxCharCount.Value >= _minCharCount)
        End Sub

        Public Sub New(Optional ByVal minCharCount As Integer = 0,
                       Optional ByVal maxCharCount As Integer? = Nothing)
            Contract.Requires(minCharCount >= 0)
            Contract.Assume(Not maxCharCount.HasValue OrElse maxCharCount.Value >= minCharCount)
            Me._minCharCount = minCharCount
            Me._maxCharCount = maxCharCount
        End Sub

        Public Overrides Function Pack(Of TValue As String)(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            If value.Length < _minCharCount Then Throw New PicklingException("Need at least {0} characters.".Frmt(_minCharCount))
            If _maxCharCount.HasValue AndAlso value.Length > _maxCharCount Then Throw New PicklingException("Need at most {0} characters.".Frmt(_maxCharCount))
            Dim data = value.ToAscBytes.AsReadableList
            Return value.Pickled(data, Function() """{0}""".Frmt(value))
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of String)
            Dim value = data.ParseChrString(nullTerminated:=False)
            If value.Length < _minCharCount Then Throw New PicklingException("Need at least {0} characters.".Frmt(_minCharCount))
            If _maxCharCount.HasValue AndAlso value.Length > _maxCharCount Then Throw New PicklingException("Need at most {0} characters.".Frmt(_maxCharCount))
            Return value.Pickled(data, Function() """{0}""".Frmt(value))
        End Function
    End Class
End Namespace
