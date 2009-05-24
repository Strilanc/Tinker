Namespace MPQ.Compression.Wave
#Region "Decoder"
    Public Class Decoder
        Implements IBlockConverter
        Private ReadOnly numChannels As Integer
        Private stepShift As Integer = 0
        Private initBitBuffer As New BitBuffer()
        Private ReadOnly outBitBuffer As New BitBuffer()
        Private channelStepIndex(0 To 1) As Integer
        Private channelPrediction(0 To 1) As Integer
        Private chan As Integer = -1 'current channel [increased at start of each iteration, so first channel will be 0]

        Public Sub New(ByVal numChannels As Integer)
            If numChannels < 1 Or numChannels > 2 Then Throw New ArgumentException("numChannels must be 1 or 2")
            Me.numChannels = numChannels
        End Sub

        Public Function needs(ByVal outputSize As Integer) As Integer Implements IBlockConverter.needs
            Return outputSize \ 2 + 5
        End Function
        Public Sub convert(ByVal ReadView As ReadOnlyArrayView(Of Byte), _
                           ByVal WriteView As ArrayView(Of Byte), _
                           ByRef OutReadCount As Integer, _
                           ByRef OutWriteCount As Integer) _
                           Implements IBlockConverter.convert
            OutWriteCount = 0
            OutReadCount = 0

            'initial values
            If initBitBuffer IsNot Nothing Then
                For repeat = initBitBuffer.numBits \ 8 To 2 * (1 + numChannels) - 1
                    If OutReadCount >= ReadView.length Then Return
                    initBitBuffer.queueByte(ReadView(OutReadCount))
                    OutReadCount += 1
                Next repeat

                initBitBuffer.takeByte()
                stepShift = initBitBuffer.takeByte()
                For i = 0 To numChannels - 1
                    channelStepIndex(i) = &H2C
                    channelPrediction(i) = initBitBuffer.takeShort()
                    outBitBuffer.queueShort(CShort(channelPrediction(i)))
                Next i
                initBitBuffer = Nothing
            End If

            Do
                'write processed values
                While outBitBuffer.numBits >= 8 And OutWriteCount < WriteView.length
                    WriteView(OutWriteCount) = outBitBuffer.takeByte()
                    OutWriteCount += 1
                End While
                If OutWriteCount >= WriteView.length Or OutReadCount >= ReadView.length Then Exit Do

                'read next value
                Dim b = ReadView(OutReadCount)
                OutReadCount += 1

                'process value
                chan = (chan + 1) Mod numChannels
                If (b And &H80) <> 0 Then 'special cases
                    Select Case (b And &H7F)
                        Case 0 'small step adjustment, with repetition of last prediction
                            channelStepIndex(chan) -= 1
                        Case 2 'dead value
                            Continue Do
                        Case Else 'large step adjustment
                            If (b And &H7F) = 1 Then
                                channelStepIndex(chan) += 8
                            Else
                                channelStepIndex(chan) -= 8
                            End If
                            channelStepIndex(chan) = between(0, channelStepIndex(chan), stepSizeTable.Length - 1)
                            chan -= 1 'use this channel again in the next iteration
                            Continue Do
                    End Select
                Else 'update predictions
                    'deltas
                    Dim stepSize = stepSizeTable(channelStepIndex(chan))
                    Dim deltaPrediction = stepSize >> stepShift
                    For i = 0 To 5 '[b is big endian]
                        If (b >> i And 1) <> 0 Then deltaPrediction += stepSize
                        stepSize >>= 1
                    Next i
                    'update
                    If (b And &H40) <> 0 Then 'sign bit
                        channelPrediction(chan) -= deltaPrediction
                    Else
                        channelPrediction(chan) += deltaPrediction
                    End If
                    channelStepIndex(chan) += stepIndexDeltaTable(b And &H1F)
                End If

                'keep channel states from going out of range
                channelPrediction(chan) = between(Of Integer)(Short.MinValue, channelPrediction(chan), Short.MaxValue)
                channelStepIndex(chan) = between(0, channelStepIndex(chan), stepSizeTable.Length - 1)

                'output prediction
                outBitBuffer.queueShort(CShort(channelPrediction(chan)))
            Loop
        End Sub
    End Class

    Friend Module Common 'ADPCM
        Friend ReadOnly stepIndexDeltaTable() As Integer = { _
            -1, 0, -1, 4, -1, 2, -1, 6, _
            -1, 1, -1, 5, -1, 3, -1, 7, _
            -1, 1, -1, 5, -1, 3, -1, 7, _
            -1, 2, -1, 4, -1, 6, -1, 8 _
        }
        Friend ReadOnly stepSizeTable() As Integer = { _
            7, 8, 9, 10, 11, 12, 13, 14, 16, 17, 19, 21, 23, 25, 28, 31, 34, 37, 41, 45, 50, 55, 60, 66, 73, 80, 88, 97, 107, 118, _
            130, 143, 157, 173, 190, 209, 230, 253, 279, 307, 337, 371, 408, 449, 494, 544, 598, 658, 724, 796, 876, 963, 1060, _
            1166, 1282, 1411, 1552, 1707, 1878, 2066, 2272, 2499, 2749, 3024, 3327, 3660, 4026, 4428, 4871, 5358, 5894, 6484, _
            7132, 7845, 8630, 9493, 10442, 11487, 12635, 13899, 15289, 16818, 18500, 20350, 22385, 24623, 27086, 29794, 32767 _
        }
    End Module
#End Region
End Namespace