Namespace WC3.Replay
    ''' <summary>
    ''' Exposes warcraft 3 replay data as an IRandomReadableStream.
    ''' </summary>
    Public NotInheritable Class ReplayDataReader
        Inherits FutureDisposable
        Implements IRandomReadableStream

        Private Const BlockHeaderSize As Integer = 8

        Private Structure BlockInfo
            Public ReadOnly BlockPosition As Long
            Public ReadOnly BlockLength As Long
            Public ReadOnly DataPosition As Long
            Public ReadOnly DataLength As Long

            Public Sub New(ByVal blockPosition As Long,
                           ByVal blockLength As Long,
                           ByVal dataPosition As Long,
                           ByVal dataLength As Long)
                Me.BlockPosition = blockPosition
                Me.BlockLength = blockLength
                Me.DataPosition = dataPosition
                Me.DataLength = dataLength
            End Sub

            Public ReadOnly Property NextBlockPosition As Long
                Get
                    Return BlockPosition + BlockLength
                End Get
            End Property
            Public ReadOnly Property NextDataPosition As Long
                Get
                    Return DataPosition + DataLength
                End Get
            End Property
        End Structure

        Private ReadOnly _stream As IRandomReadableStream
        Private ReadOnly _blockCount As UInt32
        Private ReadOnly _length As Long
        Private ReadOnly _blockInfoTable As New List(Of BlockInfo)()

        Private _position As Long
        Private _loadedBlockData As IReadableList(Of Byte)
        Private _loadedBlockIndex As Integer

        <ContractInvariantMethod()> Private Shadows Sub ObjectInvariant()
            Contract.Invariant(_stream IsNot Nothing)
            Contract.Invariant(_blockInfoTable IsNot Nothing)
            Contract.Invariant(_blockInfoTable.Count <= _blockCount)
            Contract.Invariant(_position <= _length)
            Contract.Invariant(_loadedBlockIndex >= 0)
            Contract.Invariant(_loadedBlockIndex < _blockInfoTable.Count)
            Contract.Invariant(_loadedBlockData Is Nothing OrElse _position >= _blockInfoTable(_loadedBlockIndex).DataPosition)
            Contract.Invariant(_loadedBlockData Is Nothing OrElse _position <= _blockInfoTable(_loadedBlockIndex).NextDataPosition)
        End Sub

        Public Sub New(ByVal subStream As IRandomReadableStream,
                       ByVal blockCount As UInt32,
                       ByVal firstBlockOffset As UInt32,
                       ByVal decompressedSize As UInt32)
            Contract.Requires(subStream IsNot Nothing)
            Contract.Requires(blockCount >= 0)
            Contract.Requires(firstBlockOffset >= 0)
            Me._stream = subStream
            Me._blockCount = blockCount
            Me._length = decompressedSize
            If blockCount > 0 Then
                LoadNextBlockInfo(blockPosition:=firstBlockOffset, dataPosition:=0)
            End If
        End Sub

        ''' <summary>
        ''' Reads the header of the next unexplored block and adds the details to the block info table.
        ''' </summary>
        ''' <param name="blockPosition">The starting position of the block, as determined by the previous block's end.</param>
        ''' <param name="dataPosition">The logical starting position of the data stored in the block.</param>
        Private Sub LoadNextBlockInfo(ByVal blockPosition As Long, ByVal dataPosition As Long)
            Contract.Requires(_blockInfoTable.Count < _blockCount)
            Contract.Ensures(_blockInfoTable.Count = Contract.OldValue(_blockInfoTable.Count) + 1)
            'Read block header
            _stream.Position = blockPosition
            Dim compressedDataSize = _stream.AsStream.ReadUInt16()
            Dim decompressedDataSize = _stream.AsStream.ReadUInt16()
            Dim checksum = _stream.AsStream.ReadUInt32()
            'Remember
            Dim block = New BlockInfo(blockPosition:=blockPosition,
                                      blockLength:=BlockHeaderSize + compressedDataSize,
                                      dataPosition:=dataPosition,
                                      dataLength:=decompressedDataSize)
            _blockInfoTable.Add(block)
            If _blockInfoTable.Count = _blockCount AndAlso block.NextDataPosition < _length Then
                Throw New IO.InvalidDataException("Less data than indicated by header.")
            End If
        End Sub

        ''' <summary>
        ''' Determines the block info for the given block, filling the block info table as necessary.
        ''' </summary>
        Private Function ReadBlockInfo(ByVal blockIndex As Integer) As BlockInfo
            Contract.Requires(blockIndex >= 0)
            Contract.Requires(blockIndex < _blockCount)
            Contract.Ensures(_blockInfoTable.Count > blockIndex)
            'Add to table until it contains the desired block
            While _blockInfoTable.Count <= blockIndex
                Dim prev = _blockInfoTable.Last
                LoadNextBlockInfo(prev.NextBlockPosition, prev.NextDataPosition)
            End While
            'Retrieve from table
            Return _blockInfoTable(blockIndex)
        End Function
        ''' <summary>
        ''' Determines the block data for the given block, filling the block info table as necessary.
        ''' </summary>
        Private Function ReadBlockData(ByVal blockIndex As Integer) As IReadableList(Of Byte)
            Contract.Requires(blockIndex >= 0)
            Contract.Requires(blockIndex < _blockCount)
            Contract.Ensures(_blockInfoTable.Count > blockIndex)
            Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))() IsNot Nothing)
            'Locate
            Dim block = ReadBlockInfo(blockIndex)
            _stream.Position = block.BlockPosition + BlockHeaderSize
            'Retrieve
            Return New ZLibStream(_stream.AsStream, IO.Compression.CompressionMode.Decompress).ReadBytesExact(length:=CInt(block.DataLength)).AsReadableList
        End Function

        '''<summary>Determines the block which contains the given position.</summary>
        Private Function FindBlockIndexAt(ByVal position As Long) As Integer
            Contract.Requires(position >= 0)
            Contract.Requires(position < _length)

            'Optimistic local check
            For i = _loadedBlockIndex - 1 To _loadedBlockIndex + 1
                If i < 0 Then Continue For
                If i >= _blockCount Then Exit For
                Dim block = ReadBlockInfo(i)
                If position >= block.DataPosition AndAlso position < block.NextDataPosition Then
                    Return i
                End If
            Next i

            'Binary search
            Dim min = 0
            Dim max = CInt(_blockCount) - 1
            Do Until max < min
                Dim med = (min + max) \ 2
                Dim block = ReadBlockInfo(med)
                If position < block.DataPosition Then
                    max = med - 1
                ElseIf position >= block.NextDataPosition Then
                    min = med + 1
                Else
                    Return med
                End If
            Loop

            Throw New UnreachableException("A valid position was not contained in any block.")
        End Function

        Public Function Read(ByVal maxCount As Integer) As IReadableList(Of Byte) Implements IReadableStream.Read
            Dim result = New List(Of Byte)
            If _blockCount = 0 Then Return result.AsReadableList

            'Load first block on first read
            If _loadedBlockData Is Nothing Then
                Contract.Assume(_loadedBlockIndex = 0)
                _loadedBlockData = ReadBlockData(0)
            End If

            While result.Count < maxCount AndAlso _position < _length
                Dim blockInfo = _blockInfoTable(_loadedBlockIndex)

                'Advance to next block as necessary
                Contract.Assume(_position >= blockInfo.DataPosition)
                Contract.Assume(_position <= blockInfo.NextDataPosition)
                If _position = blockInfo.NextDataPosition Then
                    _loadedBlockIndex += 1
                    Contract.Assume(_loadedBlockIndex < _blockCount)
                    blockInfo = ReadBlockInfo(_loadedBlockIndex)
                    _loadedBlockData = ReadBlockData(_loadedBlockIndex)
                End If
                Contract.Assume(_position >= blockInfo.DataPosition)
                Contract.Assume(_position < blockInfo.NextDataPosition)

                'Append block data to result
                Dim relativePosition = CInt(_position - blockInfo.DataPosition)
                Dim remainingBlockData = _loadedBlockData.SubView(relativePosition)
                Dim n = Math.Min(maxCount - result.Count, remainingBlockData.Count)
                result.AddRange(remainingBlockData.SubView(0, n))
                _position += n
            End While

            Return result.AsReadableList
        End Function

        Public ReadOnly Property Length As Long Implements ISeekableStream.Length
            Get
                Return _length
            End Get
        End Property

        Public Property Position As Long Implements ISeekableStream.Position
            Get
                Return _position
            End Get
            Set(ByVal value As Long)
                Dim newBlockIndex = FindBlockIndexAt(value)
                If _loadedBlockData Is Nothing OrElse _loadedBlockIndex <> newBlockIndex Then
                    _loadedBlockData = ReadBlockData(newBlockIndex)
                    _loadedBlockIndex = newBlockIndex
                End If
                _position = value
            End Set
        End Property

        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As IFuture
            _stream.Dispose()
            Return Nothing
        End Function
    End Class
End Namespace
