Namespace Mpq.Compression.Wave
#Region "Decoder"
    Public Class Decoder
        Implements IConverter(Of Byte)

        Private ReadOnly numChannels As Integer

        Public Sub New(ByVal numChannels As Integer)
            If numChannels < 1 Or numChannels > 2 Then Throw New ArgumentOutOfRangeException("numChannels must be 1 or 2", "numChannels")
            Me.numChannels = numChannels
        End Sub

        Public Function Convert(ByVal sequence As IEnumerator(Of Byte)) As IEnumerator(Of Byte) Implements IConverter(Of Byte).Convert
            Dim outBitBuffer = New BitBuffer()
            Dim stepIndex(0 To numChannels - 1) As Integer
            Dim prediction(0 To numChannels - 1) As Integer
            Dim nextChannel = 0

            sequence.MoveNextAndReturn()
            Dim stepShift = sequence.MoveNextAndReturn()
            For i = 0 To numChannels - 1
                stepIndex(i) = &H2C
                prediction(i) = sequence.MoveNextAndReturn()
                outBitBuffer.QueueUInt16(CType(prediction(i), ModInt16))
            Next i

            Return New Enumerator(Of Byte)(
                Function(controller)
                    Do
                        'write processed values
                        If outBitBuffer.NumBufferedBits >= 8 Then  Return outBitBuffer.TakeByte()

                        'read next value
                        If Not sequence.MoveNext() Then  Return controller.Break()
                        Dim b = sequence.Current()

                        'process value
                        Dim channel = nextChannel
                        nextChannel = (nextChannel + 1) Mod numChannels
                        If (b And &H80) <> 0 Then 'special cases
                            Select Case (b And &H7F)
                                Case 0 'small step adjustment, with repetition of last prediction
                                    stepIndex(channel) -= 1
                                Case 2 'dead value
                                    Continue Do
                                Case Else 'large step adjustment
                                    If (b And &H7F) = 1 Then
                                        stepIndex(channel) += 8
                                    Else
                                        stepIndex(channel) -= 8
                                    End If
                                    stepIndex(channel) = stepIndex(channel).Between(0, stepSizeTable.Length - 1)
                                    nextChannel = channel 'use this channel again in the next iteration
                                    Continue Do
                            End Select
                        Else 'update predictions
                            'deltas
                            Dim stepSize = stepSizeTable(stepIndex(channel))
                            Dim deltaPrediction = stepSize >> stepShift
                            For i = 0 To 5 '[b is big endian]
                                If (b >> i And 1) <> 0 Then  deltaPrediction += stepSize
                                stepSize >>= 1
                            Next i
                            'update
                            If (b And &H40) <> 0 Then 'sign bit
                                prediction(channel) -= deltaPrediction
                            Else
                                prediction(channel) += deltaPrediction
                            End If
                            stepIndex(channel) += stepIndexDeltaTable(b And &H1F)
                        End If

                        'keep channel states from going out of range
                        prediction(channel) = prediction(channel).Between(Short.MinValue, Short.MaxValue)
                        stepIndex(channel) = stepIndex(channel).Between(0, stepSizeTable.Length - 1)

                        'output prediction
                        outBitBuffer.QueueUInt16(CType(prediction(channel), ModInt16))
                    Loop
                End Function)
        End Function
    End Class

    Friend Module Common 'ADPCM
        Friend ReadOnly stepIndexDeltaTable() As Integer = {
            -1, 0, -1, 4, -1, 2, -1, 6,
            -1, 1, -1, 5, -1, 3, -1, 7,
            -1, 1, -1, 5, -1, 3, -1, 7,
            -1, 2, -1, 4, -1, 6, -1, 8
        }
        Friend ReadOnly stepSizeTable() As Integer = {
            7, 8, 9, 10, 11, 12, 13, 14, 16, 17, 19, 21, 23, 25, 28, 31, 34, 37, 41, 45, 50, 55, 60, 66, 73, 80, 88, 97, 107, 118,
            130, 143, 157, 173, 190, 209, 230, 253, 279, 307, 337, 371, 408, 449, 494, 544, 598, 658, 724, 796, 876, 963, 1060,
            1166, 1282, 1411, 1552, 1707, 1878, 2066, 2272, 2499, 2749, 3024, 3327, 3660, 4026, 4428, 4871, 5358, 5894, 6484,
            7132, 7845, 8630, 9493, 10442, 11487, 12635, 13899, 15289, 16818, 18500, 20350, 22385, 24623, 27086, 29794, 32767
        }
    End Module
#End Region
End Namespace