Imports Tinker.Pickling

Namespace WC3.Replay
    Public Class ReplayWriter
        Inherits DisposableWithTask

        Private Shared ReadOnly entryJar As New ReplayEntryJar()
        Private Const BlockSize As Integer = 8192

        Private ReadOnly _wc3Version As UInt32
        Private ReadOnly _replayVersion As UInt16
        Private ReadOnly _stream As IRandomWritableStream
        Private ReadOnly _blockDataBuffer As New IO.MemoryStream
        Private ReadOnly _startPosition As Long
        Private ReadOnly _providedDuration As UInt32?
        Private ReadOnly _settings As ReplaySettings
        Private _dataCompressor As IWritableStream
        Private _blockSizeRemaining As Integer

        Private _decompressedSize As UInt32
        Private _compressedSize As UInt32
        Private _blockCount As UInt32
        Private _measuredDuration As UInt32

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_stream IsNot Nothing)
            Contract.Invariant(_startPosition >= 0)
            Contract.Invariant(_startPosition <= _stream.Length)
            Contract.Invariant(_blockDataBuffer IsNot Nothing)
            Contract.Invariant(_dataCompressor IsNot Nothing)
            Contract.Invariant(_blockSizeRemaining >= 0)
            Contract.Invariant(_blockSizeRemaining <= BlockSize)
        End Sub

        Public Sub New(ByVal stream As IRandomWritableStream,
                       ByVal settings As ReplaySettings,
                       ByVal wc3Version As UInt32,
                       ByVal replayVersion As UInt16,
                       Optional ByVal duration As UInt32? = Nothing)
            Contract.Requires(stream IsNot Nothing)

            Me._stream = stream
            Me._startPosition = stream.Position
            Me._wc3Version = wc3Version
            Me._replayVersion = replayVersion
            Me._settings = settings
            Me._providedDuration = duration

            _stream.WriteAt(_startPosition, CByte(0).Repeated(CInt(Format.HeaderSize)).ToRist)
            StartBlock()
        End Sub

        Private Sub StartBlock()
            _blockSizeRemaining = BlockSize
            _blockDataBuffer.SetLength(0)
            Dim s = MakeZLibStream(_blockDataBuffer, IO.Compression.CompressionMode.Compress, leaveOpen:=True)
            Contract.Assume(s.CanWrite)
            _dataCompressor = s.AsWritableStream
        End Sub
        Private Sub EndBlock()
            If _blockSizeRemaining = BlockSize Then Return

            'Finish block [wc3 won't accept replays which don't null-pad the last block like this]
            If _blockSizeRemaining > 0 Then
                _dataCompressor.Write(CByte(0).Repeated(_blockSizeRemaining).ToRist)
            End If
            Dim usableDecompressedLength = CUShort(BlockSize - _blockSizeRemaining)
            Dim blockDecompressedLength = CUShort(BlockSize)
            _dataCompressor.Dispose()

            'Get compressed data
            _blockDataBuffer.Position = 0
            Contract.Assume(_blockDataBuffer.CanRead)
            Dim compressedBlockData = _blockDataBuffer.ReadRemaining().AsRist
            Dim compressedLength = CUShort(compressedBlockData.Count)

            'compute checksum
            Dim headerCRC32 = Concat(Of Byte)(compressedLength.Bytes,
                                              blockDecompressedLength.Bytes,
                                              {0, 0, 0, 0}).CRC32
            Dim bodyCRC32 = compressedBlockData.CRC32
            Dim checksum = ((bodyCRC32 Xor (bodyCRC32 << 16)) And &HFFFF0000UI) Or
                           ((headerCRC32 Xor (headerCRC32 >> 16)) And &HFFFFUI)

            'write block to file
            _stream.Write(compressedLength)
            _stream.Write(blockDecompressedLength)
            _stream.Write(checksum)
            _stream.Write(compressedBlockData)
            _compressedSize += compressedLength + CUInt(Format.BlockHeaderSize)
            _decompressedSize += usableDecompressedLength
            _blockCount += 1UI
            Contract.Assume(_startPosition <= _stream.Length)
        End Sub

        Public Sub WriteData(ByVal data As IRist(Of Byte))
            Contract.Requires(data IsNot Nothing)
            If Me.IsDisposed Then Throw New ObjectDisposedException(Me.GetType.Name)

            While data.Count >= _blockSizeRemaining
                _dataCompressor.Write(data.TakeExact(_blockSizeRemaining))
                data = data.SkipExact(_blockSizeRemaining)
                _blockSizeRemaining = 0
                EndBlock()
                StartBlock()
            End While

            If data.Count > 0 Then
                _dataCompressor.Write(data)
                _blockSizeRemaining -= data.Count
            End If
            Contract.Assume(_blockSizeRemaining >= 0)
        End Sub
        Public Sub WriteEntry(ByVal entry As ReplayEntry)
            Contract.Requires(entry IsNot Nothing)
            WriteData(entryJar.Pack(entry).ToRist)
        End Sub

        Private Function GenerateHeader() As IRist(Of Byte)
            Contract.Ensures(Contract.Result(Of IRist(Of Byte))() IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IRist(Of Byte))().Count = Format.HeaderSize)

            Using m = New IO.MemoryStream()
                Contract.Assume(m.CanRead)
                Contract.Assume(m.CanWrite)
                Contract.Assume(m.CanSeek)
                Dim header = m.AsRandomAccessStream()
                header.WriteNullTerminatedString(Format.HeaderMagicValue)
                header.Write(Format.HeaderSize)
                header.Write(_compressedSize)
                header.Write(Format.HeaderVersion)
                header.Write(_decompressedSize)
                header.Write(_blockCount)
                header.Write("PX3W".ToAsciiBytes().AsRist())
                header.Write(_wc3Version)
                header.Write(_replayVersion)
                header.Write(_settings)
                header.Write(If(_providedDuration, _measuredDuration))

                'checksum
                Contract.Assume(header.Length = Format.HeaderSize - 4)
                header.Write(header.ReadExactAt(0, CInt(header.Length)).Concat({0, 0, 0, 0}).CRC32)
                Contract.Assume(header.Length = Format.HeaderSize)

                Return header.ReadExactAt(0, CInt(header.Length))
            End Using
        End Function

        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As Task
            EndBlock()
            _stream.WriteAt(position:=_startPosition, data:=GenerateHeader())

            _stream.Dispose()
            _blockDataBuffer.Dispose()
            _dataCompressor.Dispose()

            Contract.Assume(_startPosition <= _stream.Length)
            Return Nothing
        End Function
    End Class
End Namespace
