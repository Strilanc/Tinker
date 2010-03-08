Namespace WC3
    Public NotInheritable Class Map
        Private ReadOnly _streamFactory As Func(Of IRandomReadableStream)
        Private ReadOnly _advertisedPath As InvariantString
        Private ReadOnly _fileSize As UInteger
        Private ReadOnly _fileChecksumCRC32 As UInt32
        Private ReadOnly _mapChecksumXORO As UInt32
        Private ReadOnly _mapChecksumSHA1 As IReadableList(Of Byte)
        Private ReadOnly _slots As IReadableList(Of Slot)
        Private ReadOnly _playableWidth As UInteger
        Private ReadOnly _playableHeight As UInteger
        Private ReadOnly _isMelee As Boolean
        Private ReadOnly _name As InvariantString
        Private ReadOnly _usesFixedPlayerSettings As Boolean
        Private ReadOnly _usesCustomForces As Boolean

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_advertisedPath.StartsWith("Maps\"))
            Contract.Invariant(_fileSize > 0)
            Contract.Invariant(_mapChecksumSHA1 IsNot Nothing)
            Contract.Invariant(_mapChecksumSHA1.Count = 20)
            Contract.Invariant(_slots IsNot Nothing)
            Contract.Invariant(_slots.Count > 0)
            Contract.Invariant(_slots.Count <= 12)
            Contract.Invariant(_playableWidth > 0)
            Contract.Invariant(_playableHeight > 0)
        End Sub

        Public Sub New(ByVal streamFactory As Func(Of IRandomReadableStream),
                       ByVal advertisedPath As InvariantString,
                       ByVal fileSize As UInteger,
                       ByVal fileChecksumCRC32 As UInt32,
                       ByVal mapChecksumXORO As UInt32,
                       ByVal mapChecksumSHA1 As IReadableList(Of Byte),
                       ByVal slots As IReadableList(Of Slot),
                       ByVal playableWidth As UInteger,
                       ByVal playableHeight As UInteger,
                       ByVal isMelee As Boolean,
                       ByVal usesCustomForces As Boolean,
                       ByVal usesFixedPlayerSettings As Boolean,
                       ByVal name As InvariantString)
            Contract.Requires(advertisedPath.StartsWith("Maps\"))
            Contract.Requires(fileSize > 0)
            Contract.Requires(mapChecksumSHA1 IsNot Nothing)
            Contract.Requires(mapChecksumSHA1.Count = 20)
            Contract.Requires(slots IsNot Nothing)
            Contract.Requires(slots.Count > 0)
            Contract.Requires(slots.Count <= 12)
            Contract.Requires(playableWidth > 0)
            Contract.Requires(playableHeight > 0)

            Me._streamFactory = streamFactory
            Me._advertisedPath = advertisedPath
            Me._fileSize = fileSize
            Me._fileChecksumCRC32 = fileChecksumCRC32
            Me._mapChecksumXORO = mapChecksumXORO
            Me._mapChecksumSHA1 = mapChecksumSHA1
            Me._slots = slots
            Me._playableWidth = playableHeight
            Me._playableHeight = playableWidth
            Me._isMelee = isMelee
            Me._usesCustomForces = usesCustomForces
            Me._usesFixedPlayerSettings = usesFixedPlayerSettings
            Me._name = name
        End Sub

        Public Shared Function FromFile(ByVal filePath As InvariantString,
                                        ByVal wc3MapFolder As InvariantString,
                                        ByVal wc3PatchMPQFolder As InvariantString) As Map
            Contract.Ensures(Contract.Result(Of Map)() IsNot Nothing)
            Dim factory = Function() New IO.FileStream(filePath, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read).AsRandomReadableStream
            Dim mapArchive = MPQ.Archive.FromStreamFactory(factory)
            Dim war3PatchArchive = OpenWar3PatchArchive(wc3PatchMPQFolder)
            Dim info = ReadMapInfo(mapArchive)

            Dim basePath As InvariantString = wc3MapFolder.ToString.Replace(IO.Path.AltDirectorySeparatorChar, IO.Path.DirectorySeparatorChar)
            Dim relPath As InvariantString = filePath.ToString.Replace(IO.Path.AltDirectorySeparatorChar, IO.Path.DirectorySeparatorChar)
            If Not basePath.EndsWith(IO.Path.DirectorySeparatorChar) Then basePath += IO.Path.DirectorySeparatorChar
            If relPath.StartsWith(basePath) Then
                relPath = relPath.Substring(basePath.Length)
            Else
                relPath = IO.Path.GetFileName(relPath)
            End If

            Using crcStream = factory()
                Return New Map(streamFactory:=factory,
                               AdvertisedPath:="Maps\" + relPath.ToString.Replace(IO.Path.DirectorySeparatorChar, "\"),
                               FileSize:=CUInt(crcStream.Length),
                               FileChecksumCRC32:=crcStream.CRC32,
                               MapChecksumSHA1:=ComputeMapSha1Checksum(mapArchive, war3PatchArchive),
                               MapChecksumXORO:=CUInt(ComputeMapXoro(mapArchive, war3PatchArchive)),
                               Slots:=info.slots,
                               PlayableWidth:=info.playableWidth,
                               PlayableHeight:=info.playableHeight,
                               IsMelee:=CBool(info.options And MapOptions.Melee),
                               UsesCustomForces:=CBool(info.options And MapOptions.CustomForces),
                               UsesFixedPlayerSettings:=CBool(info.options And MapOptions.FixedPlayerSettings),
                               Name:=info.name)
            End Using
        End Function

        Public Shared Function FromArgument(ByVal arg As String) As Map
            Contract.Requires(arg IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Map)() IsNot Nothing)
            If arg.Length <= 0 Then
                Throw New ArgumentException("Empty argument.")
            ElseIf arg.StartsWith("0x", StringComparison.OrdinalIgnoreCase) Then 'Map specified by HostMapInfo packet data
                'Parse
                If arg Like "0x*[!0-9a-fA-F]" OrElse arg.Length Mod 2 <> 0 Then
                    Throw New ArgumentException("Invalid map meta data. [0x prefix should be followed by hex HostMapInfo packet data].")
                End If
                Dim hexData = From i In Enumerable.Range(1, arg.Length \ 2 - 1)
                              Select CByte(arg.Substring(i * 2, 2).FromHexToUInt64(ByteOrder.BigEndian))
                Dim vals = Protocol.Packets.HostMapInfo.Jar.Parse(hexData.ToReadableList).Value

                'Extract values
                Dim path As InvariantString = CStr(vals("path")).AssumeNotNull
                Dim size = CUInt(vals("size"))
                Dim crc32 = CUInt(vals("crc32"))
                Dim xoro = CUInt(vals("xoro checksum"))
                Dim sha1 = CType(vals("sha1 checksum"), IReadableList(Of Byte)).AssumeNotNull
                Dim slot1 = New Slot(index:=0,
                                     raceUnlocked:=False,
                                     Color:=Protocol.PlayerColor.Red,
                                     contents:=New SlotContentsOpen,
                                     locked:=Slot.LockState.Frozen,
                                     team:=0)
                Dim slot2 = slot1.WithIndex(1).WithColor(Protocol.PlayerColor.Blue)
                Dim slot3 = slot1.WithIndex(2).WithColor(Protocol.PlayerColor.Teal).WithContents(New SlotContentsComputer(Protocol.ComputerLevel.Normal))
                Contract.Assume(sha1.Count = 20)
                If Not path.StartsWith("Maps\") Then Throw New IO.InvalidDataException("Invalid map path.")
                If size <= 0 Then Throw New IO.InvalidDataException("Invalid file size.")

                Return New Map(streamFactory:=Nothing,
                               AdvertisedPath:=path,
                               FileSize:=size,
                               FileChecksumCRC32:=crc32,
                               MapChecksumXORO:=xoro,
                               MapChecksumSHA1:=sha1,
                               Slots:={slot1, slot2, slot3}.AsReadableList,
                               PlayableWidth:=256,
                               PlayableHeight:=256,
                               IsMelee:=True,
                               UsesCustomForces:=False,
                               UsesFixedPlayerSettings:=False,
                               Name:=path)
            Else 'Map specified by path
                Dim mapPath = My.Settings.mapPath.AssumeNotNull
                Return Map.FromFile(filePath:=IO.Path.Combine(mapPath, FindFileMatching("*{0}*".Frmt(arg), "*.[wW]3[mxMX]", mapPath)),
                                    wc3MapFolder:=mapPath,
                                    wc3PatchMPQFolder:=My.Settings.war3path.AssumeNotNull)
            End If
        End Function

#Region "Properties"
        Public ReadOnly Property AdvertisedPath As InvariantString
            Get
                Contract.Ensures(Contract.Result(Of InvariantString)().StartsWith("Maps\"))
                Return _advertisedPath
            End Get
        End Property
        Public ReadOnly Property FileSize As UInteger
            Get
                Contract.Ensures(Contract.Result(Of UInteger)() > 0)
                Return _fileSize
            End Get
        End Property
        Public ReadOnly Property FileChecksumCRC32 As UInt32
            Get
                Return _fileChecksumCRC32
            End Get
        End Property
        Public ReadOnly Property MapChecksumXORO As UInt32
            Get
                Return _mapChecksumXORO
            End Get
        End Property
        Public ReadOnly Property MapChecksumSHA1 As IReadableList(Of Byte)
            Get
                Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))() IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))().Count = 20)
                Return _mapChecksumSHA1
            End Get
        End Property
        Public ReadOnly Property Slots As IReadableList(Of Slot)
            Get
                Contract.Ensures(Contract.Result(Of IReadableList(Of Slot))() IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IReadableList(Of Slot))().Count > 0)
                Contract.Ensures(Contract.Result(Of IReadableList(Of Slot))().Count <= 12)
                Return _slots
            End Get
        End Property
        Public ReadOnly Property PlayableWidth As UInteger
            Get
                Contract.Ensures(Contract.Result(Of UInteger)() > 0)
                Return _playableWidth
            End Get
        End Property
        Public ReadOnly Property PlayableHeight As UInteger
            Get
                Contract.Ensures(Contract.Result(Of UInteger)() > 0)
                Return _playableHeight
            End Get
        End Property
        Public ReadOnly Property IsMelee As Boolean
            Get
                Return _isMelee
            End Get
        End Property
        Public ReadOnly Property UsesFixedPlayerSettings As Boolean
            Get
                Return _usesFixedPlayerSettings
            End Get
        End Property
        Public ReadOnly Property UsesCustomForces As Boolean
            Get
                Return _usesCustomForces
            End Get
        End Property
        Public ReadOnly Property Name As InvariantString
            Get
                Return _name
            End Get
        End Property
        Public ReadOnly Property LayoutStyle As Protocol.LobbyLayoutStyle
            Get
                If Not UsesCustomForces Then Return Protocol.LobbyLayoutStyle.Melee
                If Not UsesFixedPlayerSettings Then Return Protocol.LobbyLayoutStyle.CustomForces
                Return Protocol.LobbyLayoutStyle.FixedPlayerSettings
            End Get
        End Property

        Public Enum SizeClass
            Huge
            Large
            Medium
            Small
            Tiny
        End Enum
        Public ReadOnly Property SizeClassification As SizeClass
            Get
                '[I don't know if area works for irregular sizes; might be max instead]
                Select Case PlayableWidth * PlayableHeight
                    Case Is <= 64 * 64 : Return SizeClass.Tiny
                    Case Is <= 128 * 128 : Return SizeClass.Small
                    Case Is <= 160 * 160 : Return SizeClass.Medium
                    Case Is <= 192 * 192 : Return SizeClass.Large
                    Case Else : Return SizeClass.Huge
                End Select
            End Get
        End Property
        Public ReadOnly Property GameType As Protocol.GameTypes
            Get
                Dim result = Protocol.GameTypes.MakerUser Or If(IsMelee, Protocol.GameTypes.TypeMelee, Protocol.GameTypes.TypeScenario)
                Select Case SizeClassification
                    Case SizeClass.Tiny, SizeClass.Small
                        result = result Or Protocol.GameTypes.SizeSmall
                    Case SizeClass.Medium
                        result = result Or Protocol.GameTypes.SizeMedium
                    Case SizeClass.Large, SizeClass.Huge
                        result = result Or Protocol.GameTypes.SizeLarge
                    Case Else
                        Throw SizeClassification.MakeImpossibleValueException()
                End Select
                Return result
            End Get
        End Property
        Public ReadOnly Property FileAvailable As Boolean
            Get
                Return _streamFactory IsNot Nothing
            End Get
        End Property
#End Region

#Region "Read"
        <ContractVerification(False)>
        Public Function ReadChunk(ByVal pos As Int64, ByVal size As UInt32) As IReadableList(Of Byte)
            Contract.Requires(pos >= 0)
            Contract.Requires(size > 0)
            Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))() IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))().Count <= size)
            If pos > Me.FileSize Then Throw New InvalidOperationException("Attempted to read past end of map file.")
            If Not FileAvailable Then Throw New InvalidOperationException("Attempted to read map file data when no file available.")
            Contract.Assume(_streamFactory IsNot Nothing)

            Using stream = _streamFactory()
                If stream Is Nothing Then Throw New InvalidStateException("Invalid steam factory.")
                stream.Position = pos
                Return stream.Read(CInt(size))
            End Using
        End Function

        Private Shared Function OpenWar3PatchArchive(ByVal war3PatchFolder As String) As MPQ.Archive
            Contract.Requires(war3PatchFolder IsNot Nothing)
            Contract.Ensures(Contract.Result(Of MPQ.Archive)() IsNot Nothing)
            Dim normalPath = IO.Path.Combine(war3PatchFolder, "War3Patch.mpq")
            Dim copyPath = IO.Path.Combine(war3PatchFolder, "TinkerTempCopyWar3Patch{0}.mpq".Frmt(New CachedExternalValues().WC3ExeVersion.StringJoin(".")))
            If IO.File.Exists(copyPath) Then
                Return MPQ.Archive.FromFile(copyPath)
            ElseIf IO.File.Exists(normalPath) Then
                Try
                    Return MPQ.Archive.FromFile(normalPath)
                Catch e As IO.IOException
                    IO.File.Copy(normalPath, copyPath)
                    Return MPQ.Archive.FromFile(copyPath)
                End Try
            Else
                Throw New IO.IOException("Couldn't find War3Patch.mpq")
            End If
        End Function

        '''<summary>Computes one of the checksums used to uniquely identify maps.</summary>
        <ContractVerification(False)>
        Private Shared Function ComputeMapSha1Checksum(ByVal mapArchive As MPQ.Archive,
                                                       ByVal war3PatchArchive As MPQ.Archive) As IReadableList(Of Byte)
            Contract.Requires(mapArchive IsNot Nothing)
            Contract.Requires(war3PatchArchive IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))() IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))().Count = 20)
            Dim streams As New List(Of IReadableStream)

            'Overridable map files from war3patch.mpq
            For Each filename In {"scripts\common.j",
                                  "scripts\blizzard.j"}
                Contract.Assume(filename IsNot Nothing)
                Dim mpqToUse = If(mapArchive.Hashtable.Contains(filename),
                                  mapArchive,
                                  war3PatchArchive)
                streams.Add(mpqToUse.OpenFileByName(filename))
            Next filename

            'Magic value
            streams.Add(New IO.MemoryStream(New Byte() {&H9E, &H37, &HF1, &H3}).AsReadableStream)

            'Important map files
            For Each fileset In {"war3map.j|scripts\war3map.j",
                                 "war3map.w3e",
                                 "war3map.wpm",
                                 "war3map.doo",
                                 "war3map.w3u",
                                 "war3map.w3b",
                                 "war3map.w3d",
                                 "war3map.w3a",
                                 "war3map.w3q"}
                Contract.Assume(fileset IsNot Nothing)
                Dim filenameToUse = (From filename In fileset.Split("|"c)
                                     Where mapArchive.Hashtable.Contains(filename)).FirstOrDefault
                If filenameToUse IsNot Nothing Then
                    streams.Add(mapArchive.OpenFileByName(filenameToUse))
                End If
            Next fileset

            Using stream = New ConcatStream(streams)
                Using sha = New Security.Cryptography.SHA1Managed()
                    Return sha.ComputeHash(stream.AsStream).AsReadableList
                End Using
            End Using
        End Function

        Private Shared Function XoroAccumulate(ByVal accumulator As UInt32, ByVal value As UInt32) As UInt32
            Dim result = accumulator Xor value
            result = (result << 3) Or (result >> 29)
            Return result
        End Function

        '''<summary>Computes parts of the Xoro checksum.</summary>
        Private Shared Function ComputeStreamXoro(ByVal stream As IRandomReadableStream) As UInt32
            Contract.Requires(stream IsNot Nothing)
            Dim val = 0UI

            'Process complete dwords
            For repeat = 1 To stream.Length \ 4
                val = XoroAccumulate(val, stream.ReadUInt32())
            Next repeat

            'Process bytes not in a complete dword
            For repeat = 1 To stream.Length Mod 4
                val = XoroAccumulate(val, stream.ReadByte())
            Next repeat

            Return val
        End Function

        '''<summary>Computes one of the checksums used to uniquely identify maps.</summary>
        Private Shared Function ComputeMapXoro(ByVal mapArchive As MPQ.Archive,
                                               ByVal war3PatchArchive As MPQ.Archive) As ModInt32
            Contract.Requires(mapArchive IsNot Nothing)
            Contract.Requires(war3PatchArchive IsNot Nothing)
            Dim result = 0UI

            'Overridable map files from war3patch.mpq
            For Each filename In {"scripts\common.j",
                                  "scripts\blizzard.j"}
                Contract.Assume(filename IsNot Nothing)
                Dim mpqToUse = If(mapArchive.Hashtable.Contains(filename),
                                  mapArchive,
                                  war3PatchArchive)
                Using stream = mpqToUse.OpenFileByName(filename)
                    result = result Xor ComputeStreamXoro(stream)
                End Using
            Next filename
            result = XoroAccumulate(0, result)

            'Magic value
            result = XoroAccumulate(result, &H3F1379E)

            'Important map files
            For Each fileset In {"war3map.j|scripts\war3map.j",
                                 "war3map.w3e",
                                 "war3map.wpm",
                                 "war3map.doo",
                                 "war3map.w3u",
                                 "war3map.w3b",
                                 "war3map.w3d",
                                 "war3map.w3a",
                                 "war3map.w3q"}
                Contract.Assume(fileset IsNot Nothing)
                Dim filenameToUse = (From filename In fileset.Split("|"c)
                                     Where mapArchive.Hashtable.Contains(filename)).FirstOrDefault
                If filenameToUse IsNot Nothing Then
                    Using stream = mapArchive.OpenFileByName(filenameToUse)
                        result = XoroAccumulate(result, ComputeStreamXoro(stream))
                    End Using
                End If
            Next fileset

            Return result
        End Function

        '''<summary>Finds a string in the war3map.wts file. Returns null if the string is not found.</summary>
        Private Shared Function TryGetMapString(ByVal mapArchive As MPQ.Archive,
                                                ByVal key As InvariantString) As String
            Contract.Requires(mapArchive IsNot Nothing)

            'Open strings file and search for given key
            Using sr = New IO.StreamReader(mapArchive.OpenFileByName("war3map.wts").AsStream)
                Do Until sr.EndOfStream
                    Dim itemKey = sr.ReadLine()
                    If sr.ReadLine <> "{" Then Continue Do
                    Dim itemLines = New List(Of String)
                    Do
                        Dim line = sr.ReadLine()
                        If line = "}" Then Exit Do
                        itemLines.Add(line)
                    Loop
                    If itemKey = key Then
                        Return itemLines.StringJoin(Environment.NewLine)
                    End If
                Loop
            End Using

            'Alternate key
            If key.StartsWith("TRIGSTR_") Then
                Contract.Assume(key.Length >= "TRIGSTR_".Length)
                Dim suffix = key.Substring("TRIGSTR_".Length)
                Dim id As UInteger
                If UInt32.TryParse(suffix, id) Then
                    Return TryGetMapString(mapArchive, "STRING {0}".Frmt(id))
                End If
            End If

            'Not found
            Return Nothing
        End Function

        ''' <summary>
        ''' Finds a string in the war3map.wts file.
        ''' Returns the key if the string is not found.
        ''' Returns the key and an error description if an exception occurs.
        ''' </summary>
        Private Shared Function SafeGetMapString(ByVal mapArchive As MPQ.Archive,
                                                 ByVal nameKey As InvariantString) As String
            Contract.Requires(mapArchive IsNot Nothing)
            Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
            Try
                Return If(TryGetMapString(mapArchive, nameKey), nameKey.ToString)
            Catch e As Exception When TypeOf e Is IO.IOException OrElse
                                      TypeOf e Is IO.InvalidDataException
                e.RaiseAsUnexpected("Error reading map strings file for {0}".Frmt(nameKey))
                Return "{0} (error reading strings file: {1})".Frmt(nameKey, e.Summarize)
            End Try
        End Function

        <Flags()>
        Private Enum MapOptions As UInteger
            HideMinimap = 1 << 0
            ModifyAllyPriorities = 1 << 1
            Melee = 1 << 2
            Large = 1 << 3
            RevealTerrain = 1 << 4
            FixedPlayerSettings = 1 << 5
            CustomForces = 1 << 6
            CustomTechTree = 1 << 7
            CustomAbilities = 1 << 8
            CustomUpgrades = 1 << 9
            CustomMapProperties = 1 << 10
            WaterWavesOnCliffShores = 1 << 11
            WaterWavesOnSlopeShores = 1 << 12
        End Enum
        Private Enum MapInfoFormatVersion As Integer
            ROC = 18
            TFT = 25
        End Enum
        Private Class ReadMapInfoResult
            Public ReadOnly playableWidth As UInteger
            Public ReadOnly playableHeight As UInteger
            Public ReadOnly options As MapOptions
            Public ReadOnly slots As IReadableList(Of Slot)
            Public ReadOnly name As InvariantString

            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(playableWidth > 0)
                Contract.Invariant(playableHeight > 0)
                Contract.Invariant(slots IsNot Nothing)
                Contract.Invariant(slots.Count > 0)
                Contract.Invariant(slots.Count <= 12)
            End Sub

            Public Sub New(ByVal name As InvariantString,
                           ByVal playableWidth As UInteger,
                           ByVal playableHeight As UInteger,
                           ByVal options As MapOptions,
                           ByVal slots As IReadableList(Of Slot))
                Contract.Requires(playableWidth > 0)
                Contract.Requires(playableHeight > 0)
                Contract.Requires(slots IsNot Nothing)
                Contract.Requires(slots.Count > 0)
                Contract.Requires(slots.Count <= 12)
                Me.playableHeight = playableHeight
                Me.playableWidth = playableWidth
                Me.options = options
                Me.slots = slots
                Me.name = name
            End Sub
        End Class
        '''<summary>Reads map information from the "war3map.w3i" file in the map mpq archive.</summary>
        '''<source>war3map.w3i format found at http://www.wc3campaigns.net/tools/specs/index.html by Zepir/PitzerMike</source>
        <ContractVerification(False)>
        Private Shared Function ReadMapInfo(ByVal mapArchive As MPQ.Archive) As ReadMapInfoResult
            Contract.Requires(mapArchive IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ReadMapInfoResult)() IsNot Nothing)

            Using stream = mapArchive.OpenFileByName("war3map.w3i")
                Dim fileFormat = CType(stream.ReadUInt32(), MapInfoFormatVersion)
                If Not fileFormat.EnumValueIsDefined Then
                    Throw New IO.InvalidDataException("Unrecognized war3map.w3i format.")
                End If

                Dim saveCount = stream.ReadUInt32()
                Dim editorVersion = stream.ReadExact(4)

                Dim mapName = SafeGetMapString(mapArchive, nameKey:=stream.ReadNullTerminatedString(maxLength:=256)) 'map description key

                Dim mapAuthor = stream.ReadNullTerminatedString(maxLength:=256)
                Dim mapDescription = stream.ReadNullTerminatedString(maxLength:=256)
                Dim recommendedPlayers = stream.ReadNullTerminatedString(maxLength:=256)
                For repeat = 1 To 8
                    stream.ReadSingle()  '"Camera Bounds" as defined in the JASS file
                Next repeat
                For repeat = 1 To 4
                    stream.ReadUInt32() 'camera bounds complements
                Next repeat

                Dim playableWidth = stream.ReadUInt32()
                Dim playableHeight = stream.ReadUInt32()
                If playableWidth <= 0 Then Throw New IO.InvalidDataException("Non-positive map playable width.")
                If playableHeight <= 0 Then Throw New IO.InvalidDataException("Non-positive map playable height.")
                Dim options = CType(stream.ReadUInt32(), MapOptions)

                Dim mainGoundType = stream.ReadByte()
                If fileFormat = MapInfoFormatVersion.ROC Then
                    Dim campaignBackgroundIndex = stream.ReadUInt32() 'UInt32.MaxValue = none
                End If
                If fileFormat = MapInfoFormatVersion.TFT Then
                    Dim loadScreenBackgroundIndex = stream.ReadUInt32() 'UInt32.MaxValue = none or custom imported file
                    Dim loadScreenModel = stream.ReadNullTerminatedString(maxLength:=256)
                End If
                Dim loadScreenText = stream.ReadNullTerminatedString(maxLength:=4096)
                Dim loadScreenTitle = stream.ReadNullTerminatedString(maxLength:=256)
                Dim loadScreenSubtitle = stream.ReadNullTerminatedString(maxLength:=256)
                If fileFormat = MapInfoFormatVersion.ROC Then
                    Dim mapLoadScreenIndex = stream.ReadUInt32() 'UInt32.MaxValue = none
                End If
                If fileFormat = MapInfoFormatVersion.TFT Then
                    Dim usedGameDataSetIndex = stream.ReadUInt32() '0 = standard
                    Dim prologueScreenPath = stream.ReadNullTerminatedString(maxLength:=256)
                End If
                Dim prologueScreenText = stream.ReadNullTerminatedString(maxLength:=4096)
                Dim prologueScreenTitle = stream.ReadNullTerminatedString(maxLength:=256)
                Dim prologueScreenSubtitle = stream.ReadNullTerminatedString(maxLength:=256)
                If fileFormat = MapInfoFormatVersion.TFT Then
                    Dim terrainFogType = stream.ReadUInt32() '0 = not used
                    Dim fogStartZ = stream.ReadSingle()
                    Dim fogEndZ = stream.ReadSingle()
                    Dim fogDensity = stream.ReadSingle()
                    Dim fogRGBA = stream.ReadExact(4)
                    Dim globalWeatherType = stream.ReadUInt32() '0 = none, else 4-letter-id
                    Dim customSoundEnvironmentPath = stream.ReadNullTerminatedString(maxLength:=256)
                    Dim customLightEnvironmentTilesetId = stream.ReadByte()
                    Dim waterTintRGBA = stream.ReadExact(4)
                End If

                Dim slots = ReadMapInfoSlotsAndForces(stream, options)

                '... more data in the file but it isn't needed ...

                Return New ReadMapInfoResult(mapName, playableWidth, playableHeight, options, slots)
            End Using
        End Function
        <ContractVerification(False)>
        Private Shared Function ReadMapInfoSlotsAndForces(ByVal stream As IReadableStream, ByVal options As MapOptions) As IReadableList(Of Slot)
            Contract.Requires(stream IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IReadableList(Of Slot))() IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IReadableList(Of Slot))().Count > 0)
            Contract.Ensures(Contract.Result(Of IReadableList(Of Slot))().Count <= 12)
            Dim result = New List(Of Slot)()

            'Slots
            Dim numSlotsInFile = stream.ReadUInt32()
            CheckIOData(numSlotsInFile > 0 AndAlso numSlotsInFile <= 12, "Invalid number of slots.")
            For repeat = 0 To numSlotsInFile - 1
                'Read
                Dim colorData = CType(stream.ReadUInt32(), Protocol.PlayerColor)
                Dim typeData = stream.ReadUInt32()
                Dim raceData = stream.ReadUInt32()
                Dim fixedStartPosition = stream.ReadUInt32()
                Dim slotPlayerName = stream.ReadNullTerminatedString(maxLength:=256)
                Dim startPositionX = stream.ReadSingle()
                Dim startPositionY = stream.ReadSingle()
                Dim allyPrioritiesLow = stream.ReadUInt32()
                Dim allyPrioritiesHigh = stream.ReadUInt32()

                'Check
                CheckIOData(colorData.EnumValueIsDefined, "Unrecognized map slot color: {0}.".Frmt(colorData))
                CheckIOData(raceData <= 4, "Unrecognized map slot race data: {0}.".Frmt(raceData))
                CheckIOData(typeData >= 1 AndAlso typeData <= 3, "Unrecognized map slot type data: {0}.".Frmt(typeData))

                'Use
                Dim race = Protocol.Races.Random
                Dim raceUnlocked = Not CBool(options And MapOptions.FixedPlayerSettings)
                Dim contents As SlotContents
                Select Case raceData
                    Case 0 : raceUnlocked = True
                    Case 1 : race = Protocol.Races.Human
                    Case 2 : race = Protocol.Races.Orc
                    Case 3 : race = Protocol.Races.Undead
                    Case 4 : race = Protocol.Races.NightElf
                    Case Else : Throw New UnreachableException
                End Select
                Select Case typeData
                    Case 1 : contents = New SlotContentsOpen()
                    Case 2 : contents = New SlotContentsComputer(Protocol.ComputerLevel.Normal)
                    Case 3 : Continue For 'neutral slots not shown in lobby
                    Case Else : Throw New UnreachableException
                End Select
                result.Add(New Slot(index:=CByte(result.Count),
                                    raceUnlocked:=raceUnlocked,
                                    Color:=colorData,
                                    contents:=contents,
                                    race:=race,
                                    team:=0))
            Next repeat
            Contract.Assert(result.Count <= numSlotsInFile)
            CheckIOData(result.Count > 0, "Map contains no player slots.")

            'Forces
            Dim numForces = stream.ReadUInt32()
            CheckIOData(numForces > 0 AndAlso numForces <= 12, "Invalid number of forces.")
            For teamIndex As Byte = 0 To CByte(numForces - 1)
                'Read
                Dim forceFlags = stream.ReadUInt32()
                Dim memberBitField = stream.ReadUInt32()
                Dim forceName = stream.ReadNullTerminatedString(maxLength:=256)

                'Apply
                Dim teamIndex_ = teamIndex
                result = (From slot In result
                          Select If(CBool(memberBitField And (1UI << slot.Color)), slot.WithTeam(teamIndex_), slot)
                          ).ToList
            Next teamIndex
            '(melee overrides forces and races)
            If CBool(options And MapOptions.Melee) Then
                result = Enumerable.Zip(result, Enumerable.Range(0, result.Count),
                                        Function(slot, team) slot.WithTeam(CByte(team)).WithRace(Protocol.Races.Random)
                                        ).ToList
            End If

            Return result.AsReadableList
        End Function
#End Region
    End Class
End Namespace
