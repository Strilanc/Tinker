Namespace WC3
    Public NotInheritable Class Map
        '''<summary>Creates streams for accessing the file data. Null when no file is available.</summary>
        Private ReadOnly _streamFactory As Func(Of NonNull(Of IRandomReadableStream))
        '''<summary>The machine-independent path that wc3 will use when communicating (eg. "Maps\PlunderIsle.w3m").</summary>
        Private ReadOnly _advertisedPath As InvariantString
        '''<summary>The size of the file data.</summary>
        Private ReadOnly _fileSize As UInteger
        '''<summary>The crc32 checksum of the file data.</summary>
        Private ReadOnly _fileChecksumCRC32 As UInt32
        '''<summary>A custom checksum of some of the map archive's files' data.</summary>
        Private ReadOnly _mapChecksumXORO As UInt32
        '''<summary>The sha1 hash of some of the map archive's files' data.</summary>
        Private ReadOnly _mapChecksumSHA1 As IRist(Of Byte)
        '''<summary>The slot layout used for game lobbies.</summary>
        Private ReadOnly _lobbySlots As IRist(Of Slot)
        '''<summary>The width of the map's playable area.</summary>
        Private ReadOnly _playableWidth As UInt16
        '''<summary>The height of the map's playable area.</summary>
        Private ReadOnly _playableHeight As UInt16
        '''<summary>Indicates if the map is a melee map (as opposed to a custom map).</summary>
        Private ReadOnly _isMelee As Boolean
        '''<summary>The map's name.</summary>
        Private ReadOnly _name As InvariantString
        '''<summary>Indicates if player teams should be kept fixed.</summary>
        Private ReadOnly _usesCustomForces As Boolean
        '''<summary>Indicates if player colors and races should be kept fixed.</summary>
        Private ReadOnly _usesFixedPlayerSettings As Boolean

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_advertisedPath.StartsWith("Maps\"))
            Contract.Invariant(_fileSize > 0)
            Contract.Invariant(_mapChecksumSHA1 IsNot Nothing)
            Contract.Invariant(_mapChecksumSHA1.Count = 20)
            Contract.Invariant(_lobbySlots IsNot Nothing)
            Contract.Invariant(_lobbySlots.Count > 0)
            Contract.Invariant(_lobbySlots.Count <= 12)
            Contract.Invariant(_playableWidth > 0)
            Contract.Invariant(_playableHeight > 0)
        End Sub

        '''<summary>Trivial constructor.</summary>
        Public Sub New(ByVal streamFactory As Func(Of NonNull(Of IRandomReadableStream)),
                       ByVal advertisedPath As InvariantString,
                       ByVal fileSize As UInteger,
                       ByVal fileChecksumCRC32 As UInt32,
                       ByVal mapChecksumXORO As UInt32,
                       ByVal mapChecksumSHA1 As IRist(Of Byte),
                       ByVal lobbySlots As IRist(Of Slot),
                       ByVal playableWidth As UInt16,
                       ByVal playableHeight As UInt16,
                       ByVal isMelee As Boolean,
                       ByVal usesCustomForces As Boolean,
                       ByVal usesFixedPlayerSettings As Boolean,
                       ByVal name As InvariantString)
            Contract.Requires(advertisedPath.StartsWith("Maps\"))
            Contract.Requires(fileSize > 0)
            Contract.Requires(mapChecksumSHA1 IsNot Nothing)
            Contract.Requires(mapChecksumSHA1.Count = 20)
            Contract.Requires(lobbySlots IsNot Nothing)
            Contract.Requires(lobbySlots.Count > 0)
            Contract.Requires(lobbySlots.Count <= 12)
            Contract.Requires(playableWidth > 0)
            Contract.Requires(playableHeight > 0)

            Me._streamFactory = streamFactory
            Me._advertisedPath = advertisedPath
            Me._fileSize = fileSize
            Me._fileChecksumCRC32 = fileChecksumCRC32
            Me._mapChecksumXORO = mapChecksumXORO
            Me._mapChecksumSHA1 = mapChecksumSHA1
            Me._lobbySlots = lobbySlots
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

            Dim factory = Function() New IO.FileStream(filePath, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read).AsRandomReadableStream.AsNonNull
            Dim mapArchive = MPQ.Archive.FromStreamFactory(factory)
            Dim war3PatchArchive = MPQ.Archive.FromFile(IO.Path.Combine(wc3PatchMPQFolder, "War3Patch.mpq"))

            Dim info = ReadMapInfo(mapArchive)
            Dim relPath = If(FilePathRelativeToDirectoryPath(filePath, wc3MapFolder),
                             IO.Path.GetFileName(filePath))

            Dim fileSize As UInt32
            Dim fileChecksumCRC32 As UInt32
            Using crcStream = factory().Value
                fileSize = CUInt(crcStream.Length)
                fileChecksumCRC32 = crcStream.ReadCRC32
            End Using

            Contract.Assume(info.slots IsNot Nothing)
            Contract.Assume(info.slots.Count > 0)
            Contract.Assume(info.slots.Count <= 12)
            Contract.Assume(info.playableWidth > 0)
            Contract.Assume(info.playableHeight > 0)
            Return New Map(streamFactory:=factory,
                           AdvertisedPath:=IO.Path.Combine("Maps", relPath).ReplaceDirectorySeparatorWith("\"c),
                           fileSize:=fileSize,
                           fileChecksumCRC32:=fileChecksumCRC32,
                           MapChecksumSHA1:=ComputeMapSha1Checksum(mapArchive, war3PatchArchive),
                           MapChecksumXORO:=ComputeMapXoro(mapArchive, war3PatchArchive),
                           LobbySlots:=info.slots,
                           PlayableWidth:=info.playableWidth,
                           PlayableHeight:=info.playableHeight,
                           IsMelee:=info.options.EnumUInt32Includes(MapOptions.Melee),
                           UsesCustomForces:=info.options.EnumUInt32Includes(MapOptions.CustomForces),
                           UsesFixedPlayerSettings:=info.options.EnumUInt32Includes(MapOptions.FixedPlayerSettings),
                           Name:=info.name)
        End Function

        Public Shared Function FromHostMapInfoPacket(ByVal data As IEnumerable(Of Byte)) As Map
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Map)() IsNot Nothing)

            'Parse map values
            Dim vals = Protocol.ServerPackets.HostMapInfo.Jar.Parse(data.ToRist).Value
            Dim path = vals.ItemAs(Of String)("path").ToInvariant
            Dim size = vals.ItemAs(Of UInt32)("size")
            Dim crc32 = vals.ItemAs(Of UInt32)("crc32")
            Dim xoro = vals.ItemAs(Of UInt32)("xoro checksum")
            Dim sha1 = vals.ItemAs(Of IRist(Of Byte))("sha1 checksum")
            Contract.Assume(sha1.Count = 20)
            If Not path.StartsWith("Maps\") Then Throw New IO.InvalidDataException("Invalid map path.")
            If size <= 0 Then Throw New IO.InvalidDataException("Invalid file size.")

            'Mock the remaining values
            Dim name = path.ToString.Split("\"c).Last
            Dim isMelee = True
            Dim usesCustomForces = False
            Dim usesFixedPlayerSettings = False
            Dim playableDiameter = 256US
            Dim slot1 = New Slot(index:=0,
                                 raceUnlocked:=False,
                                 Color:=Protocol.PlayerColor.Red,
                                 contents:=New SlotContentsOpen,
                                 locked:=Slot.LockState.Frozen,
                                 team:=0)
            Dim slot2 = slot1.With(index:=1,
                                   color:=Protocol.PlayerColor.Blue)
            Dim slot3 = slot1.With(index:=2,
                                   color:=Protocol.PlayerColor.Teal,
                                   contents:=New SlotContentsComputer(Protocol.ComputerLevel.Normal))
            Dim lobbySlots = MakeRist(slot1, slot2, slot3)
            Contract.Assume(lobbySlots.Count = 3)

            'Finish
            Return New Map(streamFactory:=Nothing,
                           AdvertisedPath:=path,
                           FileSize:=size,
                           FileChecksumCRC32:=crc32,
                           MapChecksumXORO:=xoro,
                           MapChecksumSHA1:=sha1,
                           lobbySlots:=lobbySlots,
                           PlayableWidth:=playableDiameter,
                           PlayableHeight:=playableDiameter,
                           isMelee:=isMelee,
                           usesCustomForces:=usesCustomForces,
                           usesFixedPlayerSettings:=usesFixedPlayerSettings,
                           name:=name)
        End Function

        Public Shared Function FromArgument(ByVal arg As String) As Map
            Contract.Requires(arg IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Map)() IsNot Nothing)
            If arg.Length <= 0 Then
                Throw New ArgumentException("Empty argument.")
            ElseIf arg.StartsWith("0x", StringComparison.OrdinalIgnoreCase) Then
                'Map specified by HostMapInfo packet data
                Try
                    Return FromHostMapInfoPacket(From hexValue In arg.Partitioned(partitionSize:=2).Skip(1)
                                                 Select Byte.Parse(hexValue.AsString, NumberStyles.HexNumber, CultureInfo.InvariantCulture))
                Catch ex As Exception When TypeOf ex Is FormatException OrElse
                                           TypeOf ex Is ArgumentException OrElse
                                           TypeOf ex Is Pickling.PicklingException
                    Throw New ArgumentException("Invalid map meta data. [0x prefix should be followed by hex HostMapInfo packet data].", ex)
                End Try
            Else
                'Map specified by path
                Dim mapFolderPath = My.Settings.mapPath.AssumeNotNull
                Dim wc3FolderPath = My.Settings.war3path.AssumeNotNull
                Return Map.FromFile(filePath:=IO.Path.Combine(mapFolderPath, FindFileMatching("*{0}*".Frmt(arg), "*.[wW]3[mxMX]", mapFolderPath)),
                                    wc3MapFolder:=mapFolderPath,
                                    wc3PatchMPQFolder:=wc3FolderPath)
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
        Public ReadOnly Property MapChecksumSHA1 As IRist(Of Byte)
            Get
                Contract.Ensures(Contract.Result(Of IRist(Of Byte))() IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IRist(Of Byte))().Count = 20)
                Return _mapChecksumSHA1
            End Get
        End Property
        Public ReadOnly Property LobbySlots As IRist(Of Slot)
            Get
                Contract.Ensures(Contract.Result(Of IRist(Of Slot))() IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IRist(Of Slot))().Count > 0)
                Contract.Ensures(Contract.Result(Of IRist(Of Slot))().Count <= 12)
                Return _lobbySlots
            End Get
        End Property
        Public ReadOnly Property PlayableWidth As UInt16
            Get
                Contract.Ensures(Contract.Result(Of UInt16)() > 0)
                Return _playableWidth
            End Get
        End Property
        Public ReadOnly Property PlayableHeight As UInt16
            Get
                Contract.Ensures(Contract.Result(Of UInt16)() > 0)
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

        Public Enum SizeClass As Byte
            Tiny = 0
            Small = 1
            Medium = 2
            Large = 3
            Huge = 4
        End Enum
        '''<summary>Determines the size class shown to the user by warcraft 3.</summary>
        Public ReadOnly Property SizeClassification As SizeClass
            Get
                'todo: check that these are correct for non-square sizes
                Select Case PlayableWidth * PlayableHeight
                    Case Is <= 64 * 64 : Return SizeClass.Tiny
                    Case Is <= 128 * 128 : Return SizeClass.Small
                    Case Is <= 160 * 160 : Return SizeClass.Medium
                    Case Is <= 192 * 192 : Return SizeClass.Large
                    Case Else : Return SizeClass.Huge
                End Select
            End Get
        End Property
        '''<summary>Determines the size class used for filtering by warcraft 3.</summary>
        Private ReadOnly Property FilterSizeClassification As Protocol.GameTypes
            Get
                Select Case SizeClassification
                    Case Is <= SizeClass.Medium : Return Protocol.GameTypes.SizeSmall
                    Case Is = SizeClass.Medium : Return Protocol.GameTypes.SizeMedium
                    Case Is >= SizeClass.Medium : Return Protocol.GameTypes.SizeLarge
                    Case Else : Throw New UnreachableException()
                End Select
            End Get
        End Property
        '''<summary>Determines the game type flags used for filtering by warcraft 3.</summary>
        Public ReadOnly Property FilterGameType As Protocol.GameTypes
            Get
                'todo: distinguish blizzard maps from user maps
                Return Protocol.GameTypes.MakerUser Or
                       If(IsMelee, Protocol.GameTypes.TypeMelee, Protocol.GameTypes.TypeScenario) Or
                       FilterSizeClassification
            End Get
        End Property
        Public ReadOnly Property FileAvailable As Boolean
            Get
                Return _streamFactory IsNot Nothing
            End Get
        End Property
#End Region

#Region "Read"
        Public Function ReadChunk(ByVal pos As Int64, ByVal size As UInt32) As IRist(Of Byte)
            Contract.Requires(pos >= 0)
            Contract.Requires(size > 0)
            Contract.Ensures(Contract.Result(Of IRist(Of Byte))() IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IRist(Of Byte))().Count <= size)
            If pos > Me.FileSize Then Throw New InvalidOperationException("Attempted to read past end of map file.")
            If Not FileAvailable Then Throw New InvalidOperationException("Attempted to read map file data when no file available.")
            Contract.Assume(_streamFactory IsNot Nothing)

            Using stream = _streamFactory().Value
                If stream Is Nothing Then Throw New InvalidStateException("Invalid stream factory.")
                If Me.FileSize <> stream.Length Then Throw New InvalidStateException("Modified map file.")
                Contract.Assume(pos < stream.Length)
                stream.Position = pos
                Return stream.Read(CInt(size))
            End Using
        End Function

        Private Shared Function MapChecksumOverridableFileStreams(ByVal mapArchive As MPQ.Archive,
                                                                  ByVal war3PatchArchive As MPQ.Archive) As IEnumerable(Of IReadableStream)
            Contract.Requires(mapArchive IsNot Nothing)
            Contract.Requires(war3PatchArchive IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IEnumerable(Of IReadableStream))() IsNot Nothing)

            Return From filename In {"scripts\common.j", "scripts\blizzard.j"}
                   Let archive = If(mapArchive.Hashtable.Contains(filename), mapArchive, war3PatchArchive)
                   Select archive.OpenFileByName(filename)
        End Function
        Private Shared Function MapChecksumFileStreams(ByVal mapArchive As MPQ.Archive) As IEnumerable(Of IReadableStream)
            Contract.Requires(mapArchive IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IEnumerable(Of IReadableStream))() IsNot Nothing)

            Dim scriptFile = From filename In {"war3map.j", "scripts\war3map.j"}
                             Where mapArchive.Hashtable.Contains(filename)
                             Take 1
                             Select mapArchive.OpenFileByName(filename)

            Dim dataFiles = From filename In {"war3map.w3e",
                                              "war3map.wpm",
                                              "war3map.doo",
                                              "war3map.w3u",
                                              "war3map.w3b",
                                              "war3map.w3d",
                                              "war3map.w3a",
                                              "war3map.w3q"}
                            Where mapArchive.Hashtable.Contains(filename)
                            Select mapArchive.OpenFileByName(filename)

            Return scriptFile.Concat(dataFiles)
        End Function

        '''<summary>Computes one of the checksums used to uniquely identify maps.</summary>
        Private Shared Function ComputeMapSha1Checksum(ByVal mapArchive As MPQ.Archive,
                                                       ByVal war3PatchArchive As MPQ.Archive) As IRist(Of Byte)
            Contract.Requires(mapArchive IsNot Nothing)
            Contract.Requires(war3PatchArchive IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IRist(Of Byte))() IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IRist(Of Byte))().Count = 20)
            Dim m = New IO.MemoryStream(&H3F1379EUI.Bytes().ToArray())
            Contract.Assume(m.CanRead)
            Using sha = New Security.Cryptography.SHA1Managed(),
                  stream = New ConcatStream(Concat(MapChecksumOverridableFileStreams(mapArchive, war3PatchArchive),
                                                   {m.AsReadableStream},
                                                   MapChecksumFileStreams(mapArchive)))
                Dim r = sha.ComputeHash(stream.AsStream()).AsRist()
                Contract.Assume(r.Count = 20)
                Return r
            End Using
        End Function

        '''<summary>Combines parts of the Xoro checksum.</summary>
        Private Shared Function XoroAccumulate(ByVal accumulator As UInt32, ByVal value As UInt32) As UInt32
            Dim result = accumulator Xor value
            result = (result << 3) Or (result >> 29)
            Return result
        End Function
        '''<summary>Computes parts of the Xoro checksum.</summary>
        Private Shared Function ComputeStreamXoro(ByVal stream As IReadableStream) As UInt32
            Contract.Requires(stream IsNot Nothing)
            Dim result = 0UI
            Do
                Dim data = stream.ReadBestEffort(maxCount:=4)
                If data.Count = 4 Then
                    result = XoroAccumulate(result, data.ToUInt32)
                Else
                    For Each b In data
                        result = XoroAccumulate(result, b)
                    Next b
                    Return result
                End If
            Loop
        End Function
        '''<summary>Computes one of the checksums used to uniquely identify maps.</summary>
        Private Shared Function ComputeMapXoro(ByVal mapArchive As MPQ.Archive,
                                               ByVal war3PatchArchive As MPQ.Archive) As UInt32
            Contract.Requires(mapArchive IsNot Nothing)
            Contract.Requires(war3PatchArchive IsNot Nothing)

            Dim overridableChecksum = (From stream In MapChecksumOverridableFileStreams(mapArchive, war3PatchArchive)
                                       Select ComputeStreamXoro(stream)
                                       ).Aggregate(Function(e1, e2) e1 Xor e2)
            Dim magicChecksum = &H3F1379EUI
            Dim fileChecksums = From stream In MapChecksumFileStreams(mapArchive)
                                Select ComputeStreamXoro(stream)

            Dim allChecksums = {overridableChecksum, magicChecksum}.Concat(fileChecksums)
            Return allChecksums.Aggregate(0UI, AddressOf XoroAccumulate)
        End Function

        Private Shared Function NormalizeMapStringKey(ByVal key As InvariantString) As InvariantString
            For Each prefix In {"TRIGSTR_", "STRING "}
                If key.StartsWith(prefix) Then
                    Contract.Assume(prefix.Length <= key.Length)
                    Return NormalizeMapStringKey(key.Substring(prefix.Length))
                End If
            Next prefix

            Dim u As UInt32
            If UInt32.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, u) Then
                Return u.ToString(CultureInfo.InvariantCulture)
            Else
                Return key
            End If
        End Function
        '''<summary>Finds a string in the war3map.wts file. Returns null if the string is not found.</summary>
        Private Shared Function TryGetMapString(ByVal mapArchive As MPQ.Archive,
                                                ByVal key As InvariantString) As String
            Contract.Requires(mapArchive IsNot Nothing)
            key = NormalizeMapStringKey(key)

            'Open strings file and search for given key
            Using sr = New IO.StreamReader(mapArchive.OpenFileByName("war3map.wts").AsStream)
                Do Until sr.EndOfStream
                    Dim itemKey = NormalizeMapStringKey(sr.ReadLine().AssumeNotNull)
                    If itemKey = "" Then Continue Do
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
            Public ReadOnly playableWidth As UInt16
            Public ReadOnly playableHeight As UInt16
            Public ReadOnly options As MapOptions
            Public ReadOnly slots As IRist(Of Slot)
            Public ReadOnly name As InvariantString

            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(playableWidth > 0)
                Contract.Invariant(playableHeight > 0)
                Contract.Invariant(slots IsNot Nothing)
                Contract.Invariant(slots.Count > 0)
                Contract.Invariant(slots.Count <= 12)
            End Sub

            Public Sub New(ByVal name As InvariantString,
                           ByVal playableWidth As UInt16,
                           ByVal playableHeight As UInt16,
                           ByVal options As MapOptions,
                           ByVal slots As IRist(Of Slot))
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
        Private Shared Function ReadMapInfo(ByVal mapArchive As MPQ.Archive) As ReadMapInfoResult
            Contract.Requires(mapArchive IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ReadMapInfoResult)() IsNot Nothing)

            Contract.Assume(mapArchive IsNot Nothing)
            Using stream = mapArchive.OpenFileByName("war3map.w3i")
                Dim fileFormat = CType(stream.ReadUInt32(), MapInfoFormatVersion)
                If Not fileFormat.EnumValueIsDefined Then
                    Throw New IO.InvalidDataException("Unrecognized war3map.w3i format.")
                End If

                Dim saveCount = stream.ReadUInt32()
                Dim editorVersion = stream.ReadExact(4)

                Dim mapName = SafeGetMapString(mapArchive, nameKey:=stream.ReadNullTerminatedString(maxLength:=256))

                Dim mapAuthor = stream.ReadNullTerminatedString(maxLength:=256)
                Dim mapDescription = stream.ReadNullTerminatedString(maxLength:=256)
                Dim recommendedPlayers = stream.ReadNullTerminatedString(maxLength:=256)
                Dim cameraBounds = (From i In 8.Range Select stream.ReadSingle).ToArray
                Dim cameraBoundComplements = (From i In 4.Range Select stream.ReadUInt32).ToArray

                Dim playableWidth = stream.ReadUInt32()
                Dim playableHeight = stream.ReadUInt32()
                CheckIOData(playableWidth > 0 AndAlso playableWidth <= UInt16.MaxValue, "Invalid map playable width.")
                CheckIOData(playableWidth > 0 AndAlso playableWidth <= UInt16.MaxValue, "Invalid map playable width.")
                CheckIOData(playableHeight > 0 AndAlso playableHeight <= UInt16.MaxValue, "Invalid map playable height.")
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

                Dim slots = ReadMapInfoLobbySlots(stream, options)
                Dim teamedSlots = ReadMapInfoForces(stream, slots, options)

                '... more data in the file but it isn't needed ...

                Contract.Assume(CUShort(playableWidth) > 0)
                Contract.Assume(CUShort(playableHeight) > 0)
                Contract.Assume(teamedSlots.Count > 0)
                Contract.Assume(teamedSlots.Count <= 12)
                Return New ReadMapInfoResult(mapName, CUShort(playableWidth), CUShort(playableHeight), options, teamedSlots)
            End Using
        End Function
        Private Shared Function ReadMapInfoLobbySlots(ByVal stream As IReadableStream,
                                                      ByVal options As MapOptions) As IRist(Of Slot)
            Contract.Requires(stream IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IRist(Of Slot))() IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IRist(Of Slot))().Count > 0)
            Contract.Ensures(Contract.Result(Of IRist(Of Slot))().Count <= 12)

            'Read Slots
            Dim rawSlotCount = stream.ReadUInt32()
            CheckIOData(rawSlotCount > 0 AndAlso rawSlotCount <= 12, "Invalid number of slots.")
            Dim slotData = (From index In rawSlotCount.Range
                            Let colorData = CType(stream.ReadUInt32, Protocol.PlayerColor)
                            Let typeData = stream.ReadUInt32
                            Let raceData = stream.ReadUInt32
                            Let fixedStartPosition = stream.ReadUInt32
                            Let slotPlayerName = stream.ReadNullTerminatedString(maxLength:=256)
                            Let startPositionX = stream.ReadSingle
                            Let startPositionY = stream.ReadSingle
                            Let allyPrioritiesLow = stream.ReadUInt32
                            Let allyPrioritiesHigh = stream.ReadUInt32
                            ).ToArray

            'Determine Lobby Slots
            Dim lobbySlots = New List(Of Slot)()
            For Each item In slotData
                'Check
                Contract.Assume(item IsNot Nothing)
                CheckIOData(item.colorData.EnumValueIsDefined, "Unrecognized map slot color: {0}.".Frmt(item.colorData))
                CheckIOData(item.raceData <= 4, "Unrecognized map slot race data: {0}.".Frmt(item.raceData))
                CheckIOData(item.typeData >= 1 AndAlso item.typeData <= 3, "Unrecognized map slot type data: {0}.".Frmt(item.typeData))

                'Convert
                Dim race = Protocol.Races.Random
                Dim raceUnlocked = Not options.EnumUInt32Includes(MapOptions.FixedPlayerSettings)
                Select Case item.raceData
                    Case 0 : raceUnlocked = True
                    Case 1 : race = Protocol.Races.Human
                    Case 2 : race = Protocol.Races.Orc
                    Case 3 : race = Protocol.Races.Undead
                    Case 4 : race = Protocol.Races.NightElf
                    Case Else : Throw New UnreachableException
                End Select
                Dim contents As SlotContents
                Select Case item.typeData
                    Case 1 : contents = New SlotContentsOpen()
                    Case 2 : contents = New SlotContentsComputer(Protocol.ComputerLevel.Normal)
                    Case 3 : Continue For 'neutral slots not shown in lobby
                    Case Else : Throw New UnreachableException
                End Select

                'Use
                Contract.Assume(CByte(lobbySlots.Count) < 12)
                lobbySlots.Add(New Slot(index:=CByte(lobbySlots.Count),
                                        raceUnlocked:=raceUnlocked,
                                        Color:=item.colorData,
                                        contents:=contents,
                                        race:=race,
                                        team:=0))
            Next item

            Contract.Assume(lobbySlots.Count <= rawSlotCount)
            CheckIOData(lobbySlots.Count > 0, "Map contains no lobby slots.")
            Return lobbySlots.AsRist()
        End Function
        <SuppressMessage("Microsoft.Contracts", "EnsuresInMethod-Contract.Result(Of IRist(Of Slot))().Count = lobbySlots.Count")>
        Private Shared Function ReadMapInfoForces(ByVal stream As IReadableStream,
                                                  ByVal lobbySlots As IRist(Of Slot),
                                                  ByVal options As MapOptions) As IRist(Of Slot)
            Contract.Requires(stream IsNot Nothing)
            Contract.Requires(lobbySlots IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IRist(Of Slot))() IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IRist(Of Slot))().Count = lobbySlots.Count)

            'Read Forces
            Dim forceCount = stream.ReadUInt32()
            CheckIOData(forceCount > 0 AndAlso forceCount <= 12, "Invalid number of forces.")
            Dim forceData = (From index In CByte(forceCount).Range
                             Let flags = stream.ReadUInt32
                             Let memberBitField = stream.ReadUInt32
                             Let name = stream.ReadNullTerminatedString(maxLength:=256)
                             ).ToArray

            'Assign Teams and Return
            If options.EnumUInt32Includes(MapOptions.Melee) Then
                Return (From pair In lobbySlots.ZipWithIndexes
                        Let slot = pair.Item1
                        Let team = pair.Item2
                        Select slot.With(team:=CByte(team),
                                         race:=Protocol.Races.Random)
                        ).ToRist
            Else
                Return (From slot In lobbySlots
                        Let team = (From force In forceData
                                    Where force.memberBitField.Bits.ElementAt(slot.Color)
                                    Select force.index
                                    ).Single
                        Select slot.With(team:=team)
                        ).ToRist
            End If
        End Function
#End Region
    End Class
End Namespace
