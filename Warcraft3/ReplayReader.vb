Namespace WC3
    Public Class Replay
        Private ReadOnly _version As UInt32
        Private ReadOnly _gameTimeLength As UInt32
        Private ReadOnly _blockCount As UInt32
        Private ReadOnly _firstBlockOffset As UInt32
        Private ReadOnly _streamFactory As Func(Of IO.Stream)

        Private Const HeaderMagicValue As String = "Warcraft III recorded game" + Microsoft.VisualBasic.Chr(&H1A)

        <ContractInvariantMethod()> Private Shadows Sub ObjectInvariant()
            Contract.Invariant(_streamFactory IsNot Nothing)
        End Sub

        Public Sub New(ByVal path As String)
            Contract.Requires(path IsNot Nothing)
            Me._streamFactory = Function() New IO.FileStream(path, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read)

            Using br = New IO.BinaryReader(_streamFactory())
                Dim magic = br.ReadNullTerminatedString(maxLength:=28)
                Dim headerSize = br.ReadUInt32() 'header size
                Dim compressedSize = br.ReadUInt32()
                Dim headerVersion = br.ReadUInt32()
                Dim decompressedSize = br.ReadUInt32() 'overall size of decompressed file
                Dim blockCount = br.ReadUInt32()
                Dim productId = br.ReadBytes(4).ParseChrString(nullTerminated:=False)
                Dim gameVersion = br.ReadUInt32() 'version
                Dim lengthInMilliseconds = br.ReadUInt32()
                Dim crc32 = br.ReadUInt32() 'CRC32 checksum for the header

                If magic <> HeaderMagicValue Then Throw New IO.InvalidDataException("Not a wc3 replay stream.")
                If productId <> "PX3W" Then Throw New IO.InvalidDataException("Not a wc3 replay stream.")
                Me._blockCount = blockCount
                Me._version = gameVersion
                Me._gameTimeLength = lengthInMilliseconds
                Me._firstBlockOffset = headerSize
            End Using
        End Sub

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

        Public Function MakeDataStream() As IO.Stream
            Contract.Ensures(Contract.Result(Of IO.Stream)() IsNot Nothing)
            Dim stream = _streamFactory()
            If stream Is Nothing Then Throw New InvalidStateException("Invalid stream factory.")
            Return New ReplayDataReader(stream, _blockCount, _firstBlockOffset)
        End Function
    End Class

    Public Enum ReplayEntryId As Byte
        EndOfReplay = &H0
        PlayerLeft = &H17
        StartBlock1 = &H1A
        StartBlock2 = &H1B
        StartBlock3 = &H1C
        Tick = &H1F
        ChatMessage = &H20
        GameStateChecksum = &H22
        Unknown_0x23 = &H23
        TournamentForcedCountdown = &H2F
    End Enum

    <DebuggerDisplay("{ToString}")>
    Public Class ReplayEntry
        Private _id As ReplayEntryId
        Private _payload As Object

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_payload IsNot Nothing)
        End Sub

        Public Sub New(ByVal id As ReplayEntryId, ByVal payload As Object)
            Contract.Requires(payload IsNot Nothing)
            Me._id = id
            Me._payload = payload
        End Sub

        Public ReadOnly Property Id As ReplayEntryId
            Get
                Return _id
            End Get
        End Property
        Public ReadOnly Property Payload As Object
            Get
                Contract.Ensures(Contract.Result(Of Object)() IsNot Nothing)
                Return _payload
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return _id.ToString
        End Function
    End Class

    Public Class ReplayDataReader
        Inherits WrappedReadOnlyStream
        Private _blocksRemaining As UInt32
        Private _nextBlockOffset As UInt32
        Private _blockRemainingBytes As Integer
        Private _blockStream As IO.Stream

        <ContractInvariantMethod()> Private Shadows Sub ObjectInvariant()
            Contract.Invariant(_blockRemainingBytes >= 0)
            Contract.Invariant(_blockStream IsNot Nothing OrElse _blockRemainingBytes = 0)
        End Sub

        Public Sub New(ByVal substream As IO.Stream, ByVal blockCount As UInt32, ByVal firstBlockOffset As UInt32)
            MyBase.New(substream)
            Contract.Requires(substream IsNot Nothing)
            Me._nextBlockOffset = firstBlockOffset
            Me._blocksRemaining = blockCount
        End Sub

        Public Overrides Function Read(ByVal buffer As Byte(), ByVal offset As Integer, ByVal count As Integer) As Integer
            Dim numRead = 0
            While numRead < count
                'Load next block when necessary
                Do Until _blockRemainingBytes > 0
                    If _blocksRemaining <= 0 Then Exit While
                    _blocksRemaining -= 1UI

                    substream.Seek(_nextBlockOffset, IO.SeekOrigin.Begin)
                    Dim br = New IO.BinaryReader(substream)
                    Dim compressedSize = br.ReadUInt16()
                    _blockRemainingBytes = br.ReadUInt16()
                    Contract.Assume(_blockRemainingBytes >= 0)
                    br.ReadUInt32() 'checksum

                    _nextBlockOffset += compressedSize + 8UI
                    _blockStream = New ZLibStream(substream, IO.Compression.CompressionMode.Decompress)
                Loop
                Contract.Assert(_blockStream IsNot Nothing)

                'Read from current block
                Dim n = _blockStream.Read(buffer, offset, Math.Min(count - numRead, _blockRemainingBytes))
                Contract.Assume(numRead + n <= count)
                Contract.Assume(n <= _blockRemainingBytes)
                If n = 0 Then Throw New IO.InvalidDataException("Replay block ended before expected.")
                numRead += n
                offset += n
                _blockRemainingBytes -= n
                'Contract.Assume(_blockRemainingBytes >= 0)
            End While
            Return numRead
        End Function
    End Class

    ''' <summary>
    ''' Represents a player action at a particular time.
    ''' </summary>
    Public Class ReplayGameAction
        Public ReadOnly time As UInteger
        Public ReadOnly pid As Byte
        Public ReadOnly data As IReadableList(Of Byte)
        Public Sub New(ByVal pid As Byte, ByVal time As UInteger, ByVal data As IReadableList(Of Byte))
            Me.pid = pid
            Me.time = time
            Me.data = data
        End Sub
    End Class

    Public Module Extensions
        Private Sub ReadPlayerRecord(ByVal br As IO.BinaryReader)
            Contract.Requires(br IsNot Nothing)
            br.ReadByte() 'pid
            ReadNullTerminatedString(br, 16) 'pname
            For i = br.ReadByte() - 1 To 0 Step -1
                br.ReadByte()
            Next i
        End Sub
        Private Sub ReadSlotRecord(ByVal br As IO.BinaryReader)
            Contract.Requires(br IsNot Nothing)
            br.ReadByte() 'pid
            br.ReadByte() 'dl%
            br.ReadByte() 'slot status
            br.ReadByte() 'cpu flags
            br.ReadByte() 'team id
            br.ReadByte() 'color
            br.ReadByte() 'race flags
            br.ReadByte() 'cpu strength
            br.ReadByte() 'handicap
        End Sub

        Public Function ReplayEntries(ByVal replay As Replay) As IEnumerable(Of ReplayEntry)
            Contract.Requires(replay IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IEnumerable(Of ReplayEntry))() IsNot Nothing)
            Return New Enumerable(Of ReplayEntry)(Function() EnumerateReplayEntries(replay))
        End Function
        Public Function EnumerateReplayEntries(ByVal replay As Replay) As IEnumerator(Of ReplayEntry)
            Contract.Requires(replay IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IEnumerator(Of ReplayEntry))() IsNot Nothing)
            Dim br = New IO.BinaryReader(replay.MakeDataStream())
            Try
                'Header
                br.ReadInt32() 'unknown
                br.ReadByte()
                ReadPlayerRecord(br) 'host
                ReadNullTerminatedString(br, 64) 'game name
                br.ReadByte() 'null
                ReadNullTerminatedString(br, 256) 'stat string
                Dim numPlayers = br.ReadUInt32()
                If numPlayers < 1 Or numPlayers > 12 Then Throw New IO.InvalidDataException("Invalid number of players.")
                br.ReadInt32() 'game type
                br.ReadInt32() 'lang
                Do
                    Dim b = br.ReadByte() '0 = host, &H16 = other, else keep going
                    If b <> 0 And b <> &H16 Then Exit Do
                    ReadPlayerRecord(br)
                    br.ReadUInt32()
                Loop
                br.ReadUInt16() '# bytes (for what???)
                For i = br.ReadByte() - 1 To 0 Step -1
                    ReadSlotRecord(br) 'slots
                Next i
                br.ReadUInt32() 'random seed
                br.ReadByte() 'select mode (0 = team/race select, 1 = team not select, 3 = team+race not select, 4 = race fixed rand, &HCC = auto match)
                br.ReadByte() 'start spot count

                'Blocks
                Dim time = 0UI
                Return New Enumerator(Of ReplayEntry)(
                    Function(controller)
                        Try
                            Dim blockId = CType(br.ReadByte(), ReplayEntryId)
                            Select Case blockId
                                Case ReplayEntryId.EndOfReplay
                                    br.Dispose()
                                    Return controller.Break()

                                Case ReplayEntryId.PlayerLeft
                                    Return New ReplayEntry(blockId,
                                                           New Dictionary(Of InvariantString, Object) From {
                                                                    {"reason", br.ReadUInt32()},
                                                                    {"pid", br.ReadByte()},
                                                                    {"result", br.ReadUInt32()},
                                                                    {"unknown", br.ReadUInt32()}
                                                               })

                                Case ReplayEntryId.StartBlock1
                                    Return New ReplayEntry(blockId,
                                                           New Dictionary(Of InvariantString, Object) From {
                                                                    {"value", br.ReadUInt32()}
                                                               })
                                Case ReplayEntryId.StartBlock2
                                    Return New ReplayEntry(blockId,
                                                           New Dictionary(Of InvariantString, Object) From {
                                                                    {"value", br.ReadUInt32()}
                                                               })
                                Case ReplayEntryId.StartBlock3
                                    Return New ReplayEntry(blockId,
                                                           New Dictionary(Of InvariantString, Object) From {
                                                                    {"value", br.ReadUInt32()}
                                                               })

                                Case ReplayEntryId.Tick
                                    Dim blockSizeLeft = br.ReadUInt16()

                                    'time
                                    Dim dt = br.ReadUInt16()
                                    Dim t = time
                                    time += dt
                                    blockSizeLeft -= 2US

                                    'actions
                                    Return controller.Sequence(New Enumerator(Of ReplayEntry)(
                                        Function(subcontroller)
                                            If blockSizeLeft <= 0 Then Return subcontroller.Break()
                                            Dim pid = br.ReadByte()

                                            Dim subActionSize = br.ReadUInt16()
                                            If subActionSize = 0 Then Throw New IO.InvalidDataException("Invalid Action Block Size")
                                            blockSizeLeft -= subActionSize + 3US
                                            If blockSizeLeft < 0 Then Throw New IO.InvalidDataException("Inconsistent Time Block and Action Block Sizes.")

                                            Return New ReplayEntry(blockId, New ReplayGameAction(pid, t, br.ReadBytes(subActionSize).AsReadableList))
                                        End Function
                                    ))

                                                    Case ReplayEntryId.ChatMessage
                                                        Return New ReplayEntry(blockId,
                                                                               New Dictionary(Of InvariantString, Object) From {
                                                                                        {"pid", br.ReadByte()},
                                                                                        {"size", br.ReadUInt16()},
                                                                                        {"flags", br.ReadByte()},
                                                                                        {"chatMode", br.ReadUInt32()},
                                                                                        {"text", br.ReadNullTerminatedString(256)}
                                                                                   })

                                                    Case ReplayEntryId.GameStateChecksum
                                                        Return New ReplayEntry(blockId,
                                                                               New Dictionary(Of InvariantString, Object) From {
                                                                                        {"size", br.ReadByte()},
                                                                                        {"checksum", br.ReadUInt32()}
                                                                                   })

                                                    Case ReplayEntryId.Unknown_0x23
                                                        Return New ReplayEntry(blockId,
                                                                               New Dictionary(Of InvariantString, Object) From {
                                                                                        {"unknown1", br.ReadUInt32()},
                                                                                        {"unknown2", br.ReadByte()},
                                                                                        {"unknown3", br.ReadUInt32()},
                                                                                        {"unknown4", br.ReadByte()}
                                                                                   })

                                                    Case ReplayEntryId.TournamentForcedCountdown
                                                        Return New ReplayEntry(blockId,
                                                                               New Dictionary(Of InvariantString, Object) From {
                                                                                        {"counter state", br.ReadUInt32()},
                                                                                        {"counter time", br.ReadUInt32()}
                                                                                   })

                                                    Case Else
                                                        Throw New IO.InvalidDataException("Unrecognized Command Block ID.")
                                                End Select
                                            Catch e As Exception
                                                br.Dispose()
                                                Throw
                                            End Try
                                        End Function
                )
            Catch e As Exception
                br.Dispose()
                Throw
            End Try
        End Function
    End Module
End Namespace
