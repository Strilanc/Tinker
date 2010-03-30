Imports Tinker.Pickling

Namespace WC3.Replay
    Public Class ReplayReader
        Private ReadOnly _streamFactory As Func(Of IRandomReadableStream)
        Private ReadOnly _description As Lazy(Of String)
        Private ReadOnly _headerSize As UInt32
        Private ReadOnly _dataDecompressedSize As UInt32
        Private ReadOnly _dataBlockCount As UInt32
        Private ReadOnly _wc3Version As UInt32
        Private ReadOnly _replayVersion As UInt16
        Private ReadOnly _settings As ReplaySettings
        Private ReadOnly _gameDuration As TimeSpan

        <ContractInvariantMethod()> Private Shadows Sub ObjectInvariant()
            Contract.Invariant(_streamFactory IsNot Nothing)
            Contract.Invariant(_description IsNot Nothing)
            Contract.Invariant(_gameDuration.Ticks >= 0)
        End Sub

        Public Sub New(ByVal streamFactory As Func(Of IRandomReadableStream),
                       ByVal description As Lazy(Of String),
                       ByVal headerSize As UInt32,
                       ByVal dataDecompressedSize As UInt32,
                       ByVal dataBlockCount As UInt32,
                       ByVal wc3Version As UInt32,
                       ByVal replayVersion As UInt16,
                       ByVal settings As ReplaySettings,
                       ByVal gameDuration As TimeSpan)
            Contract.Requires(streamFactory IsNot Nothing)
            Contract.Requires(description IsNot Nothing)
            Contract.Requires(gameDuration.Ticks >= 0)
            Me._streamFactory = streamFactory
            Me._description = description
            Me._headerSize = headerSize
            Me._dataDecompressedSize = dataDecompressedSize
            Me._dataBlockCount = dataBlockCount
            Me._wc3Version = wc3Version
            Me._replayVersion = replayVersion
            Me._settings = settings
            Me._gameDuration = gameDuration
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
                Dim pickle = stream.ReadPickle(Format.ReplayHeader)
                Dim header = pickle.Value
                Dim headerSize = header.ItemAs(Of UInt32)("header size")
                Contract.Assume(stream.Position = Format.HeaderSize)

                'Check header values
                If header.ItemAs(Of String)("magic") <> Format.HeaderMagicValue Then Throw New IO.InvalidDataException("Not a wc3 replay (incorrect magic value).")
                If header.ItemAs(Of String)("product id") <> "W3XP" Then Throw New IO.InvalidDataException("Not a wc3 replay (incorrect product id).")
                If header.ItemAs(Of UInt32)("header version") <> Format.HeaderVersion Then Throw New IO.InvalidDataException("Not a recognized wc3 replay (incorrect version).")
                If headerSize <> Format.HeaderSize Then Throw New IO.InvalidDataException("Not a recognized wc3 replay (incorrect header size).")

                'Check header checksum
                Dim actualChecksum = stream.ReadExactAt(position:=0, exactCount:=CInt(headerSize - 4)).Concat({0, 0, 0, 0}).CRC32
                If actualChecksum <> header.ItemAs(Of UInt32)("header crc32") Then Throw New IO.InvalidDataException("Not a wc3 replay (incorrect checksum).")

                Return New ReplayReader(streamFactory:=streamFactory,
                                        Description:=New Lazy(Of String)(Function() pickle.Description),
                                        headerSize:=headerSize,
                                        DataDecompressedSize:=header.ItemAs(Of UInt32)("data decompressed size"),
                                        DataBlockCount:=header.ItemAs(Of UInt32)("data block count"),
                                        WC3Version:=header.ItemAs(Of UInt32)("wc3 version"),
                                        ReplayVersion:=header.ItemAs(Of UInt16)("replay version"),
                                        Settings:=header.ItemAs(Of ReplaySettings)("settings"),
                                        GameDuration:=header.ItemAs(Of UInt32)("duration in game milliseconds").Milliseconds)
            End Using
        End Function

        Public ReadOnly Property Description As Lazy(Of String)
            Get
                Contract.Ensures(Contract.Result(Of Lazy(Of String))() IsNot Nothing)
                Return _description
            End Get
        End Property
        Public ReadOnly Property HeaderSize As UInt32
            Get
                Return _headerSize
            End Get
        End Property
        Public ReadOnly Property DataDecompressedSize As UInt32
            Get
                Return _dataDecompressedSize
            End Get
        End Property
        Public ReadOnly Property DataBlockCount As UInt32
            Get
                Return _dataBlockCount
            End Get
        End Property
        Public ReadOnly Property WC3Version As UInt32
            Get
                Return _wc3Version
            End Get
        End Property
        Public ReadOnly Property ReplayVersion As UInt16
            Get
                Return _replayVersion
            End Get
        End Property
        Public ReadOnly Property Settings As ReplaySettings
            Get
                Return _settings
            End Get
        End Property
        Public ReadOnly Property GameDuration As TimeSpan
            Get
                Contract.Ensures(Contract.Result(Of TimeSpan)().Ticks >= 0)
                Return _gameDuration
            End Get
        End Property

        '''<summary>Creates an IRandomReadableStream to access the replay's compressed data.</summary>
        Public Function MakeDataStream() As IRandomReadableStream
            Contract.Ensures(Contract.Result(Of IRandomReadableStream)() IsNot Nothing)
            Dim stream = _streamFactory()
            If stream Is Nothing Then Throw New InvalidStateException("Invalid stream factory.")
            Return New ReplayDataReader(stream, _dataBlockCount, _headerSize, _dataDecompressedSize)
        End Function

        Public ReadOnly Property Entries() As IEnumerable(Of ReplayEntry)
            Get
                Contract.Ensures(Contract.Result(Of IEnumerable(Of ReplayEntry))() IsNot Nothing)
                Return New Enumerable(Of ReplayEntry)(Function() EnumerateEntries())
            End Get
        End Property
        Private Function EnumerateEntries() As IEnumerator(Of ReplayEntry)
            Contract.Ensures(Contract.Result(Of IEnumerator(Of ReplayEntry))() IsNot Nothing)

            Dim jar = New ReplayEntryJar()
            Dim stream = MakeDataStream()
            Return New Enumerator(Of ReplayEntry)(
                Function(controller)
                    If stream.Position >= stream.Length Then Return controller.Break
                    Return stream.ReadPickle(jar).Value
                End Function,
                disposer:=AddressOf stream.Dispose)
        End Function
    End Class
End Namespace
