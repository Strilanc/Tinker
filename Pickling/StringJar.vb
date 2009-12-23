Namespace Pickling
    '''<summary>Pickles strings [can be null-terminated or fixed-size, and reversed]</summary>
    Public Class StringJar
        Inherits BaseJar(Of String)
        Private ReadOnly nullTerminated As Boolean
        Private ReadOnly maximumContentSize As Integer
        Private ReadOnly expectedSize As Integer

        Public Sub New(ByVal name As InvariantString,
                       Optional ByVal nullTerminated As Boolean = True,
                       Optional ByVal expectedSize As Integer = 0,
                       Optional ByVal maximumContentSize As Integer = 0,
                       Optional ByVal info As String = "No Info")
            MyBase.New(name)
            Contract.Requires(info IsNot Nothing)
            Contract.Requires(maximumContentSize >= 0)
            If expectedSize < 0 Then Throw New ArgumentOutOfRangeException("expectedSize")
            If expectedSize = 0 And Not nullTerminated Then Throw New ArgumentException(Me.GetType.Name + " must be either nullTerminated or have an expectedSize")
            Me.nullTerminated = nullTerminated
            Me.expectedSize = expectedSize
            Me.maximumContentSize = maximumContentSize
        End Sub

        Public Overrides Function Pack(Of TValue As String)(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            Dim size = value.Length + If(nullTerminated, 1, 0)
            If expectedSize <> 0 And size <> expectedSize Then Throw New PicklingException("Size doesn't match expected size")
            If maximumContentSize <> 0 AndAlso value.Length > maximumContentSize Then Throw New PicklingException("Size exceeds maximum size.")
            'Pack
            Dim data(0 To size - 1) As Byte
            If nullTerminated Then size -= 1
            Dim i = 0
            While size > 0
                size -= 1
                data(i) = CByte(Asc(value(i)))
                i += 1
            End While

            Return New Pickle(Of TValue)(Me.Name, value, data.AsReadableList(), Function() """{0}""".Frmt(value))
        End Function

        'verification disabled due to stupid verifier
        <ContractVerification(False)>
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of String)
            'Get sizes
            Dim inputSize = expectedSize
            If nullTerminated Then
                If data.Count = 0 Then 'empty strings at the end of data are sometimes simply omitted
                    Return New Pickle(Of String)(Me.Name, "", data, Function() """")
                End If
                For j = 0 To data.Count - 1
                    If data(j) = 0 Then
                        If inputSize <> 0 And inputSize <> j + 1 Then
                            Throw New PicklingException("String size doesn't match expected size")
                        End If
                        inputSize = j + 1
                        Exit For
                    End If
                Next j
            End If
            Dim outputSize = inputSize - If(nullTerminated, 1, 0)
            'Validate
            If data.Count < inputSize Then Throw New PicklingException("Not enough data")
            If maximumContentSize <> 0 AndAlso outputSize > maximumContentSize Then Throw New PicklingException("Size exceeds maximum size.")

            'Parse string data
            Dim cc(0 To outputSize - 1) As Char
            Dim i = 0
            While outputSize > 0
                outputSize -= 1
                Contract.Assume(i < data.Count)
                cc(i) = Chr(data(i))
                i += 1
            End While

            Return New Pickle(Of String)(Me.Name, cc, data.SubView(0, inputSize), Function() """{0}""".Frmt(CStr(cc)))
        End Function
    End Class
End Namespace
