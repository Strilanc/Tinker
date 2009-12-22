Namespace Pickling.Jars
    '''<summary>Pickles byte arrays [can be size-prefixed, fixed-size, or full-sized]</summary>
    Public Class ArrayJar
        Inherits BaseJar(Of Byte())
        Private ReadOnly expectedSize As Integer
        Private ReadOnly sizePrefixSize As Integer
        Private ReadOnly takeRest As Boolean

        Public Sub New(ByVal name As InvariantString,
                       Optional ByVal expectedSize As Integer = 0,
                       Optional ByVal sizePrefixSize As Integer = 0,
                       Optional ByVal takeRest As Boolean = False)
            MyBase.New(name)
            If expectedSize < 0 Then Throw New ArgumentOutOfRangeException("expectedSize")
            If takeRest Then
                If expectedSize <> 0 Or sizePrefixSize > 0 Then
                    Throw New ArgumentException(Me.GetType.Name + " can't combine takeRest with hasSizePrefix or expectedSize")
                End If
            ElseIf expectedSize = 0 And sizePrefixSize = 0 Then
                Throw New ArgumentException(Me.GetType.Name + " must be either size prefixed or have an expectedSize")
            End If
            Me.expectedSize = expectedSize
            Me.sizePrefixSize = sizePrefixSize
            Me.takeRest = takeRest
        End Sub

        Protected Overridable Function DescribeValue(ByVal value As Byte()) As String
            Contract.Requires(value IsNot Nothing)
            Return "[{0}]".Frmt(value.ToHexString)
        End Function

        'verification disabled due to stupid verifier
        <ContractVerification(False)>
        Public Overrides Function Pack(Of TValue As Byte())(ByVal value As TValue) As IPickle(Of TValue)
            Dim val = CType(value, Byte()).AssumeNotNull
            Dim offset = 0
            Dim size = val.Length
            If sizePrefixSize > 0 Then
                size += sizePrefixSize
                offset = sizePrefixSize
            End If
            If expectedSize <> 0 And size <> expectedSize Then Throw New PicklingException("Array size doesn't match expected size.")

            'Pack
            Dim data(0 To size - 1) As Byte
            If sizePrefixSize > 0 Then
                size -= sizePrefixSize
                Dim ds = CUInt(size).Bytes.SubArray(0, sizePrefixSize)
                If ds.Length <> sizePrefixSize Then Throw New PicklingException("Unable to fit size into prefix.")
                For i = 0 To sizePrefixSize - 1
                    data(i) = ds(i)
                Next i
            End If
            For i = 0 To size - 1
                data(i + offset) = val(i)
            Next i

            Return New Pickle(Of TValue)(Me.Name, value, data.AsReadableList(), Function() DescribeValue(val))
        End Function
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of Byte())
            'Sizes
            Dim inputSize = expectedSize
            Dim outputSize = expectedSize
            Dim pos = 0
            If takeRest Then
                inputSize = data.Count
                outputSize = data.Count
            ElseIf sizePrefixSize > 0 Then
                'Validate
                If data.Count < sizePrefixSize Then
                    Throw New PicklingException("Not enough data to parse array. Data ended before size prefix could be read.")
                End If
                'Read size prefix
                outputSize = CInt(data.SubView(pos, sizePrefixSize).ToUInt32())
                inputSize = outputSize + sizePrefixSize
                If expectedSize <> 0 And expectedSize <> inputSize Then
                    Throw New PicklingException("Array size doesn't match expected size")
                End If
                pos += sizePrefixSize
            End If
            'Validate
            If inputSize > data.Count Then
                Throw New PicklingException("Not enough data to parse array. Need {0} more bytes but only have {1}.".Frmt(inputSize, data.Count))
            End If

            'Parse
            Contract.Assume(outputSize + pos <= data.Count)
            Dim val(0 To outputSize - 1) As Byte
            For i = 0 To outputSize - 1
                val(i) = data(pos + i)
            Next i

            Return New Pickle(Of Byte())(Me.Name, val, data.SubView(0, inputSize), Function() DescribeValue(val))
        End Function
    End Class
End Namespace
