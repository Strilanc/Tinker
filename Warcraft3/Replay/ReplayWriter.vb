Imports Tinker.Pickling

Namespace WC3.Replay
    Public Class ReplayWriter
        Inherits FutureDisposable

        Private Const BlockSize As Integer = 8192

        Private ReadOnly _wc3Version As UInt32
        Private ReadOnly _wc3BuildNumber As UInt16
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
            Contract.Invariant(_startPosition >= 0)
            Contract.Invariant(_blockDataBuffer IsNot Nothing)
            Contract.Invariant(_dataCompressor IsNot Nothing)
            Contract.Invariant(_blockSizeRemaining >= 0)
            Contract.Invariant(_blockSizeRemaining <= BlockSize)
        End Sub

        Public Sub New(ByVal stream As IRandomWritableStream,
                       ByVal wc3Version As UInt32,
                       ByVal wc3BuildNumber As UInt16,
                       ByVal primaryPlayer As Player,
                       ByVal secondaryPlayers As IEnumerable(Of Player),
                       ByVal gameDescription As GameDescription,
                       ByVal map As Map,
                       ByVal slots As IEnumerable(Of Slot),
                       ByVal randomSeed As UInt32,
                       Optional ByVal language As UInt32 = &H18F8B0)
            Contract.Requires(stream IsNot Nothing)
            Contract.Requires(primaryPlayer IsNot Nothing)
            Contract.Requires(secondaryPlayers IsNot Nothing)
            Contract.Requires(gameDescription IsNot Nothing)

            Me._stream = stream
            Me._startPosition = stream.Position
            Me._wc3Version = wc3Version
            Me._wc3BuildNumber = wc3BuildNumber

            Start(primaryPlayer, secondaryPlayers, gameDescription, language, map, slots, randomSeed)
        End Sub

        Private Sub Start(ByVal primaryPlayer As Player,
                          ByVal secondaryPlayers As IEnumerable(Of Player),
                          ByVal gameDesc As GameDescription,
                          ByVal language As UInt32,
                          ByVal map As Map,
                          ByVal slots As IEnumerable(Of Slot),
                          ByVal randomSeed As UInt32)
            Contract.Requires(primaryPlayer IsNot Nothing)
            Contract.Requires(secondaryPlayers IsNot Nothing)
            Contract.Requires(gameDesc IsNot Nothing)

            _stream.WriteAt(_startPosition, Enumerable.Repeat(CByte(0), CInt(Format.HeaderSize)).ToReadableList)
            StartBlock()

            WriteReplayEntryPickle(ReplayEntryId.StartOfReplay,
                                   Format.ReplayEntryStartOfReplay,
                                   New Dictionary(Of InvariantString, Object) From {
                                           {"unknown1", 1},
                                           {"primary player pid", primaryPlayer.PID.Index},
                                           {"primary player name", primaryPlayer.Name.ToString},
                                           {"primary player shared data", primaryPlayer.PeerData},
                                           {"game name", gameDesc.Name.ToString},
                                           {"unknown2", 0},
                                           {"game stats", gameDesc.GameStats},
                                           {"player count", secondaryPlayers.Count + 1},
                                           {"game type", gameDesc.GameType},
                                           {"language", language}})

            For Each player In secondaryPlayers
                WriteReplayEntryPickle(ReplayEntryId.PlayerJoined,
                                       Format.ReplayEntryPlayerJoined,
                                       New Dictionary(Of InvariantString, Object) From {
                                               {"pid", player.PID.Index},
                                               {"name", player.Name.ToString},
                                               {"shared data", player.PeerData},
                                               {"unknown", 0}})
            Next player

            WriteReplayEntryPickle(ReplayEntryId.LobbyState,
                                   Format.ReplayEntryLobbyState,
                                   New Dictionary(Of InvariantString, Object) From {
                                        {"slots", (From slot In slots Select Protocol.SlotJar.PackSlot(slot)).ToList},
                                        {"random seed", randomSeed},
                                        {"layout style", map.LayoutStyle},
                                        {"num player slots", map.Slots.Count}})

            WriteReplayEntryPickle(ReplayEntryId.LoadStarted1,
                                   Format.ReplayEntryLoadStarted1,
                                   New Dictionary(Of InvariantString, Object) From {
                                        {"unknown", 1}})
            WriteReplayEntryPickle(ReplayEntryId.LoadStarted2,
                                   Format.ReplayEntryLoadStarted2,
                                   New Dictionary(Of InvariantString, Object) From {
                                        {"unknown", 1}})
        End Sub

        <ContractVerification(False)>
        Private Sub StartBlock()
            _blockSizeRemaining = BlockSize
            _blockDataBuffer.SetLength(0)
            _dataCompressor = New ZLibStream(_blockDataBuffer, IO.Compression.CompressionMode.Compress, leaveOpen:=True).AsWritableStream
        End Sub
        Private Sub EndBlock()
            If _blockSizeRemaining = BlockSize Then Return

            'Finish block [wc3 won't accept replays which don't null-pad the last block like this]
            If _blockSizeRemaining > 0 Then
                _dataCompressor.Write(Enumerable.Repeat(CByte(0), _blockSizeRemaining).ToReadableList)
            End If
            Dim usableDecompressedLength = CUShort(BlockSize - _blockSizeRemaining)
            Dim blockDecompressedLength = CUShort(BlockSize)
            _dataCompressor.Dispose()

            'Get compressed data
            _blockDataBuffer.Position = 0
            Contract.Assume(_blockDataBuffer.CanRead)
            Dim compressedBlockData = _blockDataBuffer.ReadRemaining().AsReadableList
            Dim compressedLength = CUShort(compressedBlockData.Count)

            'compute checksum
            Dim headerCRC32 = Concat(compressedLength.Bytes,
                                     blockDecompressedLength.Bytes,
                                     {0, 0, 0, 0}
                                     ).CRC32
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
        End Sub

        Private Sub WriteData(ByVal data As IReadableList(Of Byte))
            Contract.Requires(data IsNot Nothing)
            If Me.FutureDisposed.State <> FutureState.Unknown Then Throw New ObjectDisposedException(Me.GetType.Name)

            While data.Count >= _blockSizeRemaining
                _dataCompressor.Write(data.SubView(0, _blockSizeRemaining))
                data = data.SubView(_blockSizeRemaining)
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
        Private Sub WriteReplayEntryPickle(Of T)(ByVal id As ReplayEntryId,
                                                 ByVal jar As IJar(Of T),
                                                 ByVal value As T)
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(value IsNot Nothing)
            WriteData(New Byte() {id}.AsReadableList)
            WriteData(jar.Pack(value).Data)
        End Sub

        Public Sub AddGameStarted()
            WriteReplayEntryPickle(ReplayEntryId.GameStarted,
                                   Format.ReplayEntryGameStarted,
                                   New Dictionary(Of InvariantString, Object) From {
                                        {"unknown", 1}})
        End Sub
        Public Sub AddPlayerLeft(ByVal unknown1 As UInt32,
                                 ByVal pid As PlayerID,
                                 ByVal reportedReason As Protocol.PlayerLeaveReason,
                                 ByVal leaveCount As UInt32)
            WriteReplayEntryPickle(ReplayEntryId.PlayerLeft,
                                   Format.ReplayEntryPlayerLeft,
                                   New Dictionary(Of InvariantString, Object) From {
                                        {"unknown1", unknown1},
                                        {"pid", pid.Index},
                                        {"reason", reportedReason},
                                        {"session leave count", leaveCount}})
        End Sub
        Public Sub AddGameStateChecksum(ByVal checksum As UInt32)
            WriteReplayEntryPickle(ReplayEntryId.GameStateChecksum,
                                   Format.ReplayEntryGameStateChecksum,
                                   New Dictionary(Of InvariantString, Object) From {
                                        {"unknown", 4},
                                        {"checksum", checksum}})
        End Sub
        Public Sub AddLobbyChatMessage(ByVal pid As PlayerID, ByVal message As String)
            Contract.Requires(message IsNot Nothing)
            WriteReplayEntryPickle(ReplayEntryId.ChatMessage,
                                   Format.ReplayEntryChatMessage,
                                   New Dictionary(Of InvariantString, Object) From {
                                        {"pid", pid.Index},
                                        {"size", 1 + message.Length + 1},
                                        {"type", Protocol.ChatType.Lobby},
                                        {"message", message}})
        End Sub
        Public Sub AddGameChatMessage(ByVal pid As PlayerID, ByVal message As String, ByVal receivingGroup As WC3.Protocol.ChatGroup)
            Contract.Requires(message IsNot Nothing)
            WriteReplayEntryPickle(ReplayEntryId.ChatMessage,
                                   Format.ReplayEntryChatMessage,
                                   New Dictionary(Of InvariantString, Object) From {
                                        {"pid", pid.Index},
                                        {"size", 1 + 4 + message.Length + 1},
                                        {"type", Protocol.ChatType.Game},
                                        {"receiving group", receivingGroup},
                                        {"message", message}})
        End Sub
        Public Sub AddTick(ByVal duration As UInt16, ByVal actions As IReadableList(Of Protocol.PlayerActionSet))
            Contract.Requires(actions IsNot Nothing)
            _duration += duration

            WriteReplayEntryPickle(
                ReplayEntryId.Tick,
                Format.ReplayEntryTick,
                New Dictionary(Of InvariantString, Object) From {
                        {"time span", duration},
                        {"player action sets", actions}})
        End Sub

        <ContractVerification(False)>
        Private Function GenerateHeader() As IReadableList(Of Byte)
            Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))() IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))().Count = Format.HeaderSize)

            Using header = New IO.MemoryStream().AsRandomAccessStream
                header.WriteNullTerminatedString(Format.HeaderMagicValue)
                header.Write(Format.HeaderSize)
                header.Write(_compressedSize)
                header.Write(Format.HeaderVersion)
                header.Write(_decompressedSize)
                header.Write(_blockCount)
                header.Write("PX3W".ToAscBytes.AsReadableList)
                header.Write(_wc3Version)
                header.Write(_wc3BuildNumber)
                header.Write(1US << 15) 'flags
                header.Write(_duration)

                'checksum
                Contract.Assume(header.Length = Format.HeaderSize - 4)
                header.Write(header.ReadExactAt(0, CInt(header.Length)).Concat({0, 0, 0, 0}).CRC32)
                Contract.Assume(header.Length = Format.HeaderSize)

                Return header.ReadExactAt(0, CInt(header.Length))
            End Using
        End Function

        <ContractVerification(False)>
        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As IFuture
            EndBlock()
            _stream.WriteAt(position:=_startPosition, data:=GenerateHeader())

            _stream.Dispose()
            _blockDataBuffer.Dispose()
            _dataCompressor.Dispose()

            Return Nothing
        End Function
    End Class
End Namespace
