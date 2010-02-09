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
            Contract.Invariant(_blockDataBuffer IsNot Nothing)
            Contract.Invariant(_dataCompressor IsNot Nothing)
            Contract.Invariant(_blockSizeRemaining >= 0)
            Contract.Invariant(_blockSizeRemaining <= BlockSize)
        End Sub

        Public Sub New(ByVal stream As IRandomWritableStream,
                       ByVal wc3Version As UInt32,
                       ByVal wc3BuildNumber As UInt16,
                       ByVal host As Player,
                       ByVal players As IEnumerable(Of Player),
                       ByVal gameDesc As GameDescription,
                       Optional ByVal language As UInt32 = &H18F8B0)
            Contract.Requires(stream IsNot Nothing)
            Contract.Requires(host IsNot Nothing)
            Contract.Requires(players IsNot Nothing)
            Contract.Requires(gameDesc IsNot Nothing)

            Me._stream = stream
            Me._startPosition = stream.Position
            Me._wc3Version = wc3Version
            Me._wc3BuildNumber = wc3BuildNumber

            Start(host, players, gameDesc, language)
        End Sub

        Private Sub Start(ByVal host As Player,
                          ByVal players As IEnumerable(Of Player),
                          ByVal gameDesc As GameDescription,
                          ByVal language As UInt32)
            Contract.Requires(host IsNot Nothing)
            Contract.Requires(players IsNot Nothing)
            Contract.Requires(gameDesc IsNot Nothing)

            _stream.Position = _startPosition + Prots.HeaderSize
            StartBlock()

            WriteReplayEntryPickle(ReplayEntryId.StartOfReplay,
                                   Prots.ReplayEntryStartOfReplay,
                                   New Dictionary(Of InvariantString, Object) From {
                                           {"unknown1", 1},
                                           {"host pid", host.PID.Index},
                                           {"host name", host.Name},
                                           {"host peer data", host.PeerData},
                                           {"game name", gameDesc.Name},
                                           {"unknown2", 0},
                                           {"game stats", gameDesc.GameStats},
                                           {"player count", players.Count + 1},
                                           {"game type", gameDesc.GameType},
                                           {"language", language}})

            For Each player In players
                WriteReplayEntryPickle(ReplayEntryId.PlayerJoined,
                                       Prots.ReplayEntryPlayerJoined,
                                       New Dictionary(Of InvariantString, Object) From {
                                               {"pid", player.PID.Index},
                                               {"name", player.Name},
                                               {"peer data", player.PeerData},
                                               {"unknown", 0}})
            Next player

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

        Public Sub AddGameStarted()
            WriteReplayEntryPickle(ReplayEntryId.GameStarted,
                                   Prots.ReplayEntryGameStarted,
                                   New Dictionary(Of InvariantString, Object) From {
                                        {"unknown", 1}})
        End Sub
        Public Sub AddGameStateChecksum(ByVal checksum As UInt32)
            WriteReplayEntryPickle(ReplayEntryId.GameStateChecksum,
                                   Prots.ReplayEntryGameStateChecksum,
                                   New Dictionary(Of InvariantString, Object) From {
                                        {"unknown", 4},
                                        {"checksum", checksum}})
        End Sub
        Public Sub AddLobbyChatMessage(ByVal pid As PID, ByVal message As String)
            WriteReplayEntryPickle(ReplayEntryId.ChatMessage,
                                   Prots.ReplayEntryChatMessage,
                                   New Dictionary(Of InvariantString, Object) From {
                                        {"pid", pid.Index},
                                        {"size", 1 + message.Length + 1},
                                        {"type", Protocol.ChatType.Lobby},
                                        {"message", message}})
        End Sub
        Public Sub AddGameChatMessage(ByVal pid As PID, ByVal message As String, ByVal receiver As WC3.Protocol.ChatReceiverType)
            WriteReplayEntryPickle(ReplayEntryId.ChatMessage,
                                   Prots.ReplayEntryChatMessage,
                                   New Dictionary(Of InvariantString, Object) From {
                                        {"pid", pid.Index},
                                        {"size", 1 + 4 + message.Length + 1},
                                        {"type", Protocol.ChatType.Game},
                                        {"receiver type", receiver},
                                        {"message", message}})
        End Sub
        Public Sub AddTick(ByVal duration As UInt16, ByVal actions As IEnumerable(Of ReplayGameAction))
            _duration += duration

            WriteReplayEntryPickle(
                ReplayEntryId.Tick,
                Prots.ReplayEntryTick,
                New Dictionary(Of InvariantString, Object) From {
                        {"time span", duration},
                        {"player action set", (From action In actions
                                               Select New Dictionary(Of InvariantString, Object) From {
                                                       {"pid", action.pid},
                                                       {"actions", action.actions}
                                                   }).ToArray.AsReadableList}
                    })
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
                header.Write(_wc3Version)
                header.Write(_wc3BuildNumber)
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
            EndBlock()
            _stream.WriteAt(position:=_startPosition, data:=GenerateHeader())

            _stream.Dispose()
            _blockDataBuffer.Dispose()
            _dataCompressor.Dispose()

            Return Nothing
        End Function
    End Class
End Namespace
