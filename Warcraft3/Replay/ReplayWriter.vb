Imports Tinker.Pickling

Namespace WC3.Replay
    Public Class ReplayWriter
        Inherits FutureDisposable

        Private Const BlockSize As Integer = 8192

        Private ReadOnly _gameVersion As UInt32
        Private ReadOnly _gameVersion2 As UInt16
        Private ReadOnly _stream As IRandomWritableStream
        Private ReadOnly _blockDataBuffer As New IO.MemoryStream
        Private ReadOnly _startPosition As Long
        Private _dataCompressor As IWritableStream
        Private _blockSizeRemaining As Integer

        Private _decompressedSize As UInt32
        Private _compressedSize As UInt32
        Private _blockCount As UInt32
        Private _duration As UInt32

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_stream IsNot Nothing)
            Contract.Invariant(_blockDataBuffer IsNot Nothing)
            Contract.Invariant(_dataCompressor IsNot Nothing)
            Contract.Invariant(_blockSizeRemaining >= 0)
            Contract.Invariant(_blockSizeRemaining <= BlockSize)
        End Sub

        Public Sub New(ByVal stream As IRandomWritableStream,
                       ByVal gameVersion As UInt32,
                       ByVal gameVersion2 As UInt16)
            Contract.Requires(stream IsNot Nothing)
            Me._stream = stream
            Me._startPosition = stream.Position
            Me._gameVersion = gameVersion
            Me._gameVersion2 = gameVersion2

            _stream.Position += Prots.HeaderSize
        End Sub

        Private Function GeneratePlayerRecord(ByVal player As Player) As IReadableList(Of Byte)
            Contract.Requires(player IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))() IsNot Nothing)
            Throw New NotImplementedException
        End Function
        Private Function GeneratePlayerRecords(ByVal players As IEnumerable(Of Player)) As IReadableList(Of Byte)
            Contract.Requires(players IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))() IsNot Nothing)
            Throw New NotImplementedException
        End Function

        Private Function GenerateDataHeader(ByVal host As Player,
                                            ByVal players As IEnumerable(Of Player),
                                            ByVal gameType As WC3.Protocol.GameTypes,
                                            ByVal lang As UInt32) As IReadableList(Of Byte)
            Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))() IsNot Nothing)
            Using header = New IO.MemoryStream().AsRandomAccessStream
                header.Write(CByte(16)) 'unknown1
                header.Write(1UI) 'unknown2
                header.Write(GeneratePlayerRecord(host))
                header.Write("game name".ToAscBytes.AsReadableList)
                header.Write(CByte(0)) 'unknown3
                header.WritePickle(New WC3.GameStatsJar("game stats"), Nothing)
                header.Write(CUInt(players.Count + 1))
                header.Write(gameType)
                header.Write(lang)
                header.Write(GeneratePlayerRecords(players))
                header.WritePickle(WC3.Protocol.Packets.LobbyState, Nothing) 'lobby state

                Return header.ReadExactAt(0, CInt(header.Length))
            End Using
        End Function

        Private Sub Start()
            StartBlock()



            WriteReplayEntryPickle(ReplayEntryId.LoadStarted1,
                                   Prots.ReplayEntryLoadStarted1,
                                   New Dictionary(Of InvariantString, Object) From {
                                        {"unknown", 1}})
            WriteReplayEntryPickle(ReplayEntryId.LoadStarted2,
                                   Prots.ReplayEntryLoadStarted2,
                                   New Dictionary(Of InvariantString, Object) From {
                                        {"unknown", 1}})
        End Sub

        Private Sub StartBlock()
            _blockSizeRemaining = BlockSize
            _blockDataBuffer.SetLength(0)
            _dataCompressor = New ZLibStream(_blockDataBuffer, IO.Compression.CompressionMode.Compress, leaveOpen:=True).AsWritableStream
        End Sub
        Private Sub EndBlock()
            If _blockSizeRemaining = BlockSize Then Return

            'finish block data
            _dataCompressor.Dispose()
            _blockDataBuffer.Position = 0
            Dim compressedBlockData = _blockDataBuffer.ReadRemaining().AsReadableList

            'compute checksum
            Dim headerCRC32 = Concat(CUShort(compressedBlockData.Count).Bytes,
                                     CUShort(BlockSize).Bytes,
                                     {0, 0, 0, 0}
                                     ).CRC32
            Dim bodyCRC32 = compressedBlockData.CRC32
            Dim checksum = ((bodyCRC32 Xor (bodyCRC32 << 16)) And &HFFFF0000UI) Or
                           ((headerCRC32 Xor (headerCRC32 >> 16)) And &HFFFFUI)

            'write block to file
            _stream.Write(CUShort(compressedBlockData.Count))
            _stream.Write(CUShort(BlockSize))
            _stream.Write(checksum)
            _stream.Write(compressedBlockData)
            _decompressedSize += CUInt(BlockSize - _blockSizeRemaining)
            _compressedSize += CUInt(compressedBlockData.Count + 8)
            _blockCount += 1UI
        End Sub

        Private Sub WriteData(ByVal data As IReadableList(Of Byte))
            If Me.FutureDisposed.State <> FutureState.Unknown Then Throw New ObjectDisposedException(Me.GetType.Name)

            While data.Count >= _blockSizeRemaining
                _dataCompressor.Write(data.SubView(0, _blockSizeRemaining))
                data = data.SubView(_blockSizeRemaining)
                EndBlock()
                StartBlock()
            End While

            If data.Count > 0 Then
                _dataCompressor.Write(data)
            End If
        End Sub
        Private Sub WriteReplayEntryPickle(Of T)(ByVal id As ReplayEntryId,
                                 ByVal jar As IJar(Of T),
                                 ByVal vals As T)
            WriteData(New Byte() {id}.AsReadableList)
            WriteData(jar.Pack(vals).Data)
        End Sub

        Private Sub AddGameStarted()
            WriteReplayEntryPickle(ReplayEntryId.GameStarted,
                                   Prots.ReplayEntryGameStarted,
                                   New Dictionary(Of InvariantString, Object) From {
                                        {"unknown", 1}})
        End Sub
        Private Sub AddGameStateChecksum(ByVal checksum As UInt32)
            WriteReplayEntryPickle(ReplayEntryId.GameStateChecksum,
                                   Prots.ReplayEntryGameStateChecksum,
                                   New Dictionary(Of InvariantString, Object) From {
                                        {"unknown", 4},
                                        {"checksum", checksum}})
        End Sub
        Private Sub AddLobbyChatMessage(ByVal pid As PID, ByVal message As String)
            WriteReplayEntryPickle(ReplayEntryId.ChatMessage,
                                   Prots.ReplayEntryChatMessage,
                                   New Dictionary(Of InvariantString, Object) From {
                                        {"pid", pid.Index},
                                        {"size", 1 + message.Length + 1},
                                        {"type", Protocol.ChatType.Lobby},
                                        {"message", message}})
        End Sub
        Private Sub AddGameChatMessage(ByVal pid As PID, ByVal message As String, ByVal receiver As WC3.Protocol.ChatReceiverType)
            WriteReplayEntryPickle(ReplayEntryId.ChatMessage,
                                   Prots.ReplayEntryChatMessage,
                                   New Dictionary(Of InvariantString, Object) From {
                                        {"pid", pid.Index},
                                        {"size", 1 + 4 + message.Length + 1},
                                        {"type", Protocol.ChatType.Game},
                                        {"receiver type", receiver},
                                        {"message", message}})
        End Sub
        Private Sub AddTick(ByVal duration As UInt16, ByVal actions As IEnumerable(Of ReplayGameAction))
            _duration += duration
            Dim actionData = (From a1 In actions
                              Let d = (From a2 In a1.actions Select (a2.Payload.Data)).Fold.ToArray
                              Select Concat({a1.pid}, CUShort(d.Count).Bytes, d)).Fold.ToArray
            WriteData(Concat(duration.Bytes,
                             CUShort(actionData.Count).Bytes,
                             actionData).AsReadableList)
        End Sub

        Private Function GenerateHeader() As IReadableList(Of Byte)
            Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))() IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))().Count = Prots.HeaderSize)

            Using header = New IO.MemoryStream().AsRandomAccessStream
                header.WriteNullTerminatedString(Prots.HeaderMagicValue)
                header.Write(Prots.HeaderSize)
                header.Write(_compressedSize)
                header.Write(Prots.HeaderVersion)
                header.Write(_decompressedSize)
                header.Write(_blockCount)
                header.Write("PX3W".ToAscBytes.AsReadableList)
                header.Write(_gameVersion)
                header.Write(_gameVersion2)
                header.Write(1US << 15) 'flags
                header.Write(_duration)

                'checksum
                Contract.Assume(header.Length = Prots.HeaderSize - 4)
                header.Write(header.ReadExactAt(0, CInt(header.Length)).Concat({0, 0, 0, 0}).CRC32)
                Contract.Assume(header.Length = Prots.HeaderSize)

                Return header.ReadExactAt(0, CInt(header.Length))
            End Using
        End Function

        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As IFuture
            WriteData(New Byte() {ReplayEntryId.EndOfReplay}.AsReadableList)
            EndBlock()
            _stream.WriteAt(position:=_startPosition, data:=GenerateHeader())

            _stream.Dispose()
            _blockDataBuffer.Dispose()
            _dataCompressor.Dispose()

            Return Nothing
        End Function
    End Class
End Namespace
