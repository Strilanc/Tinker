Imports Tinker.Pickling

Namespace WC3.Replay
    Public Class ReplayReader
        Private ReadOnly _blockCount As UInt32
        Private ReadOnly _firstBlockOffset As UInt32
        Private ReadOnly _dataSize As UInt32
        Private ReadOnly _buildNumber As UInt16
        Private ReadOnly _streamFactory As Func(Of IRandomReadableStream)

        <ContractInvariantMethod()> Private Shadows Sub ObjectInvariant()
            Contract.Invariant(_streamFactory IsNot Nothing)
        End Sub

        Public Sub New(ByVal streamFactory As Func(Of IRandomReadableStream),
                       ByVal blockCount As UInt32,
                       ByVal firstBlockOffset As UInt32,
                       ByVal dataDecompressedSize As UInt32,
                       ByVal buildNumber As UInt16)
            Contract.Requires(streamFactory IsNot Nothing)
            Me._streamFactory = streamFactory
            Me._firstBlockOffset = firstBlockOffset
            Me._dataSize = dataDecompressedSize
            Me._blockCount = blockCount
            Me._buildNumber = buildNumber
        End Sub

        Public Shared Function FromFile(ByVal path As String) As ReplayReader
            Contract.Requires(path IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ReplayReader)() IsNot Nothing)
            Return ReplayReader.FromStreamFactory(Function() New IO.FileStream(path, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read).AsRandomReadableStream)
        End Function
        <ContractVerification(False)>
        Public Shared Function FromStreamFactory(ByVal streamFactory As Func(Of IRandomReadableStream)) As ReplayReader
            Contract.Requires(streamFactory IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ReplayReader)() IsNot Nothing)

            Using stream = streamFactory()
                If stream Is Nothing Then Throw New InvalidStateException("Invalid streamFactory")
                'Read header values
                Dim magic = stream.ReadNullTerminatedString(maxLength:=28)
                Dim headerSize = stream.ReadUInt32()
                Dim compressedSize = stream.ReadUInt32()
                Dim headerVersion = stream.ReadUInt32()
                Dim decompressedSize = stream.ReadUInt32()
                Dim blockCount = stream.ReadUInt32()
                Dim productId = stream.ReadExact(4).ParseChrString(nullTerminated:=False)
                Dim wc3Version = stream.ReadUInt32()
                Dim wc3ReplayBuildNumber = stream.ReadUInt16()
                Dim flags = stream.ReadUInt16()
                Dim lengthInMilliseconds = stream.ReadUInt32()
                Dim headerCRC32 = stream.ReadUInt32()
                Contract.Assume(stream.Position = Format.HeaderSize)

                'Check header values
                If magic <> Format.HeaderMagicValue Then Throw New IO.InvalidDataException("Not a wc3 replay (incorrect magic value).")
                If productId <> "PX3W" Then Throw New IO.InvalidDataException("Not a wc3 replay (incorrect product id).")
                If headerVersion <> Format.HeaderVersion Then Throw New IO.InvalidDataException("Not a recognized wc3 replay (incorrect version).")
                If headerSize <> Format.HeaderSize Then Throw New IO.InvalidDataException("Not a recognized wc3 replay (incorrect header size).")

                'Check header checksum
                Dim actualChecksum = stream.ReadExactAt(position:=0, exactCount:=CInt(headerSize - 4)).Concat({0, 0, 0, 0}).CRC32
                If actualChecksum <> headerCRC32 Then Throw New IO.InvalidDataException("Not a wc3 replay (incorrect checksum).")

                Return New ReplayReader(streamFactory:=streamFactory,
                                        blockCount:=blockCount,
                                        firstBlockOffset:=headerSize,
                                        dataDecompressedSize:=decompressedSize,
                                        BuildNumber:=wc3ReplayBuildNumber)
            End Using
        End Function

        Public ReadOnly Property BuildNumber As UInt16
            Get
                Return _buildNumber
            End Get
        End Property

        '''<summary>Creates an IRandomReadableStream to access the replay's compressed data.</summary>
        Public Function MakeDataStream() As IRandomReadableStream
            Contract.Ensures(Contract.Result(Of IRandomReadableStream)() IsNot Nothing)
            Dim stream = _streamFactory()
            If stream Is Nothing Then Throw New InvalidStateException("Invalid stream factory.")
            Return New ReplayDataReader(stream, _blockCount, _firstBlockOffset, _dataSize)
        End Function

        Public ReadOnly Property Entries() As IEnumerable(Of ReplayEntry)
            Get
                Contract.Ensures(Contract.Result(Of IEnumerable(Of ReplayEntry))() IsNot Nothing)
                Return New Enumerable(Of ReplayEntry)(Function() EnumerateEntries())
            End Get
        End Property
        Private Function EnumerateEntries() As IEnumerator(Of ReplayEntry)
            Contract.Ensures(Contract.Result(Of IEnumerator(Of ReplayEntry))() IsNot Nothing)

            Dim blockJars = New Dictionary(Of ReplayEntryId, IParseJar(Of Object))() From {
                        {ReplayEntryId.ChatMessage, Format.ReplayEntryChatMessage},
                        {ReplayEntryId.GameStarted, Format.ReplayEntryGameStarted},
                        {ReplayEntryId.GameStateChecksum, Format.ReplayEntryGameStateChecksum},
                        {ReplayEntryId.LoadStarted1, Format.ReplayEntryLoadStarted1},
                        {ReplayEntryId.LoadStarted2, Format.ReplayEntryLoadStarted2},
                        {ReplayEntryId.LobbyState, Format.ReplayEntryLobbyState},
                        {ReplayEntryId.PlayerJoined, Format.ReplayEntryPlayerJoined},
                        {ReplayEntryId.PlayerLeft, Format.ReplayEntryPlayerLeft},
                        {ReplayEntryId.StartOfReplay, Format.ReplayEntryStartOfReplay},
                        {ReplayEntryId.Tick, Format.ReplayEntryTick},
                        {ReplayEntryId.TournamentForcedCountdown, Format.ReplayEntryTournamentForcedCountdown},
                        {ReplayEntryId.Unknown0x23, Format.ReplayEntryUnknown0x23}
                    }

            Dim stream = MakeDataStream()
            Return New Enumerator(Of ReplayEntry)(
                Function(controller)
                    If stream.Position >= stream.Length Then Return controller.Break
                    Dim blockId = CType(stream.ReadByte(), ReplayEntryId)
                    If Not blockJars.ContainsKey(blockId) Then
                        Throw New IO.InvalidDataException("Unrecognized {0}: {1}".Frmt(GetType(ReplayEntryId), blockId))
                    End If
                    Return New ReplayEntry(blockId, stream.ReadPickle(blockJars(blockId)))
                End Function,
                disposer:=AddressOf stream.Dispose)
        End Function
    End Class
End Namespace
