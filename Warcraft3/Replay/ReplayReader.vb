Imports Tinker.Pickling

Namespace WC3.Replay
    Public Class ReplayReader
        Private ReadOnly _version As UInt32
        Private ReadOnly _gameTimeLength As UInt32
        Private ReadOnly _blockCount As UInt32
        Private ReadOnly _firstBlockOffset As UInt32
        Private ReadOnly _dataSize As UInt32
        Private ReadOnly _streamFactory As Func(Of IO.Stream)

        <ContractInvariantMethod()> Private Shadows Sub ObjectInvariant()
            Contract.Invariant(_streamFactory IsNot Nothing)
        End Sub

        Public Sub New(ByVal streamFactory As Func(Of IO.Stream),
                       ByVal blockCount As UInt32,
                       ByVal version As UInt32,
                       ByVal gameTimeLength As UInt32,
                       ByVal firstBlockOffset As UInt32,
                       ByVal dataDecompressedSize As UInt32)
            Contract.Requires(streamFactory IsNot Nothing)
            Me._streamFactory = streamFactory
            Me._version = version
            Me._gameTimeLength = gameTimeLength
            Me._firstBlockOffset = firstBlockOffset
            Me._dataSize = dataDecompressedSize
            Me._blockCount = blockCount
        End Sub

        Public Shared Function FromFile(ByVal path As String) As ReplayReader
            Contract.Requires(path IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ReplayReader)() IsNot Nothing)
            Return ReplayReader.FromStreamFactory(Function() New IO.FileStream(path, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read))
        End Function
        Public Shared Function FromStreamFactory(ByVal streamFactory As Func(Of IO.Stream)) As ReplayReader
            Contract.Requires(streamFactory IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ReplayReader)() IsNot Nothing)

            Using br = New IO.BinaryReader(streamFactory())
                'Read header values
                Dim magic = br.ReadNullTerminatedString(maxLength:=28)
                Dim headerSize = br.ReadUInt32()
                Dim compressedSize = br.ReadUInt32()
                Dim headerVersion = br.ReadUInt32()
                Dim decompressedSize = br.ReadUInt32()
                Dim blockCount = br.ReadUInt32()
                Dim productId = br.ReadBytes(4).ParseChrString(nullTerminated:=False)
                Dim gameVersion = br.ReadUInt32()
                Dim gameVersion2 = br.ReadUInt16()
                Dim flags = br.ReadUInt16()
                Dim lengthInMilliseconds = br.ReadUInt32()
                Dim headerCRC32 = br.ReadUInt32()
                Contract.Assume(br.BaseStream.Position = Prots.HeaderSize)

                'Re-read to compute checksum
                br.BaseStream.Seek(0, IO.SeekOrigin.Begin)
                Dim actualChecksum = br.ReadBytes(CInt(headerSize - 4)).Concat({0, 0, 0, 0}).CRC32

                'Check header values
                If magic <> Prots.HeaderMagicValue Then Throw New IO.InvalidDataException("Not a wc3 replay (incorrect magic value).")
                If productId <> "PX3W" Then Throw New IO.InvalidDataException("Not a wc3 replay (incorrect product id).")
                If actualChecksum <> headerCRC32 Then Throw New IO.InvalidDataException("Not a wc3 replay (incorrect checksum).")
                If headerSize <> Prots.HeaderSize Then Throw New IO.InvalidDataException("Not a recognized wc3 replay (incorrect version).")
                If headerVersion <> Prots.HeaderVersion Then Throw New IO.InvalidDataException("Not a recognized wc3 replay (incorrect version).")

                Return New ReplayReader(streamFactory:=streamFactory,
                                        blockCount:=blockCount,
                                        Version:=gameVersion,
                                        GameTimeLength:=lengthInMilliseconds,
                                        firstBlockOffset:=headerSize,
                                        dataDecompressedSize:=decompressedSize)
            End Using
        End Function

        Public ReadOnly Property GameTimeLength As TimeSpan
            Get
                Contract.Ensures(Contract.Result(Of TimeSpan)().Ticks >= 0)
                Return _gameTimeLength.Milliseconds
            End Get
        End Property
        Public ReadOnly Property Version As UInt32
            Get
                Return _version
            End Get
        End Property

        Public Function MakeDataStream() As IRandomReadableStream
            Contract.Ensures(Contract.Result(Of IO.Stream)() IsNot Nothing)
            Dim stream = _streamFactory()
            If stream Is Nothing Then Throw New InvalidStateException("Invalid stream factory.")
            Return New ReplayDataReader(stream.AsRandomReadableStream, _blockCount, _firstBlockOffset, _dataSize)
        End Function

        Private Shared Function ReadPlayerRecord(ByVal stream As IRandomReadableStream) As Dictionary(Of InvariantString, Object)
            Contract.Requires(stream IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Dictionary(Of InvariantString, Object))() IsNot Nothing)
            Return stream.ReadPickle(Prots.PlayerRecord).Value
        End Function
        Private Shared Function ReadSlotRecord(ByVal stream As IRandomReadableStream) As Dictionary(Of InvariantString, Object)
            Contract.Requires(stream IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Dictionary(Of InvariantString, Object))() IsNot Nothing)
            Return stream.ReadPickle(New WC3.Protocol.SlotJar("slot")).Value
        End Function

        Private Shared Sub ReadDataHeader(ByVal br As IO.BinaryReader)
            Contract.Requires(br IsNot Nothing)
            Dim unknown1 = br.ReadByte()
            Dim unknown2 = br.ReadUInt32()
            Dim host = ReadPlayerRecord(br.BaseStream.AsRandomReadableStream)
            Dim gameName = br.ReadNullTerminatedString(64)
            Dim unknown3 = br.ReadByte()
            Dim statString = br.ReadNullTerminatedString(256)
            Dim numPlayers = br.ReadUInt32()
            If numPlayers < 1 OrElse numPlayers > 12 Then Throw New IO.InvalidDataException("Invalid number of players.")
            Dim gameType = br.ReadUInt32()
            Dim lang = br.ReadUInt32()
            Do
                Dim b = br.ReadByte() '0 = host, &H16 = other, else keep going
                If b <> 0 AndAlso b <> &H16 Then Exit Do
                Dim player = ReadPlayerRecord(br.BaseStream.AsRandomReadableStream)
                Dim unknown4 = br.ReadUInt32()
            Loop
            Dim unknown5 = br.ReadUInt16() 'byte count?
            Dim slotCount = br.ReadByte()
            Dim slots = (From i In Enumerable.Range(0, slotCount) Select ReadSlotRecord(br.BaseStream.AsRandomReadableStream)).ToList
            Dim randomSeed = br.ReadUInt32()
            Dim selectMode = br.ReadByte() '0 = team/race select, 1 = team not select, 3 = team+race not select, 4 = race fixed rand, &HCC = auto match
            Dim startSpotCount = br.ReadByte()
        End Sub

        Public ReadOnly Property Entries() As IEnumerable(Of ReplayEntry)
            Get
                Contract.Ensures(Contract.Result(Of IEnumerable(Of ReplayEntry))() IsNot Nothing)
                Return New Enumerable(Of ReplayEntry)(Function() EnumerateReplayEntries())
            End Get
        End Property
        Private Function EnumerateReplayEntries() As IEnumerator(Of ReplayEntry)
            Contract.Ensures(Contract.Result(Of IEnumerator(Of ReplayEntry))() IsNot Nothing)
            Dim stream = MakeDataStream()
            Dim br = New IO.BinaryReader(stream.AsStream)
            Try
                ReadDataHeader(br)

                Dim blockTypes = New Dictionary(Of ReplayEntryId, IJar(Of Object))()
                blockTypes(ReplayEntryId.PlayerLeft) = Prots.ReplayEntryPlayerLeft.Weaken
                blockTypes(ReplayEntryId.StartBlock1) = Prots.ReplayEntryStartBlock1.Weaken
                blockTypes(ReplayEntryId.StartBlock2) = Prots.ReplayEntryStartBlock2.Weaken
                blockTypes(ReplayEntryId.StartBlock3) = Prots.ReplayEntryStartBlock3.Weaken
                blockTypes(ReplayEntryId.GameStateChecksum) = Prots.ReplayEntryGameStateChecksum.Weaken
                blockTypes(ReplayEntryId.TournamentForcedCountdown) = Prots.ReplayEntryTournamentForcedCountdown.Weaken
                blockTypes(ReplayEntryId.Unknown0x23) = Prots.ReplayEntryUnknown0x23.Weaken
                blockTypes(ReplayEntryId.ChatMessage) = Prots.ReplayEntryChatMessage.Weaken

                'Enumerate Blocks
                Dim time = 0UI
                Return New Enumerator(Of ReplayEntry)(
                    Function(controller)
                        Try
                            If stream.Position = stream.Length Then Return controller.Break
                            Dim blockId = CType(br.ReadByte(), ReplayEntryId)
                            If blockTypes.ContainsKey(blockId) Then
                                Return New ReplayEntry(blockId, stream.ReadPickle(blockTypes(blockId)))
                            ElseIf blockId = ReplayEntryId.EndOfReplay Then
                                br.Dispose()
                                Return controller.Break()
                            ElseIf blockId = ReplayEntryId.Tick Then
                                Return controller.Sequence(EnumerateTickActionBlocks(br, byref_time:=time))
                            Else
                                Throw New IO.InvalidDataException("Unrecognized {0}: {1}".Frmt(GetType(ReplayEntryId), blockId))
                            End If
                        Catch e As Exception
                            br.Dispose()
                            Throw
                        End Try
                    End Function,
                    disposer:=AddressOf br.Dispose)
            Catch e As Exception
                br.Dispose()
                Throw
            End Try
        End Function
        Private Function EnumerateTickActionBlocks(ByVal br As IO.BinaryReader, ByRef byref_time As UInt32) As IEnumerator(Of ReplayEntry)
            Dim blockSizeLeft = br.ReadUInt16()

            'time
            Dim dt = br.ReadUInt16()
            Dim t = byref_time
            byref_time += dt
            blockSizeLeft -= 2US

            'actions
            Return New Enumerator(Of ReplayEntry)(
                Function(controller)
                    If blockSizeLeft <= 0 Then Return controller.Break()
                    Dim pid = br.ReadByte()

                    Dim subActionSize = br.ReadUInt16()
                    If subActionSize = 0 Then Throw New IO.InvalidDataException("Invalid Action Block Size")
                    blockSizeLeft -= subActionSize + 3US
                    If blockSizeLeft < 0 Then Throw New IO.InvalidDataException("Inconsistent Time Block and Action Block Sizes.")

                    Return New ReplayEntry(ReplayEntryId.Tick, New ReplayGameAction(pid, t, br.ReadBytes(subActionSize).AsReadableList))
                End Function)
        End Function
    End Class
End Namespace
