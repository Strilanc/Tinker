Imports Tinker.Pickling

Namespace WC3
    Public NotInheritable Class Map
        Private ReadOnly _name As InvariantString
        Private ReadOnly _playableWidth As Integer
        Private ReadOnly _playableHeight As Integer
        Private ReadOnly _isMelee As Boolean
        Private ReadOnly _numPlayerSlots As Integer
        Private ReadOnly _fileSize As UInteger
        Private ReadOnly _fileChecksumCRC32 As UInt32
        Private ReadOnly _mapChecksumSHA1 As IReadableList(Of Byte)
        Private ReadOnly _mapChecksumXORO As UInt32
        Private ReadOnly _folder As InvariantString
        Private ReadOnly _advertisedPath As InvariantString
        Private ReadOnly _fullPath As InvariantString
        Public ReadOnly fileAvailable As Boolean
        Private ReadOnly _slots As IReadableList(Of Slot)
        Public ReadOnly Property Slots As IReadableList(Of Slot)
            Get
                Contract.Ensures(Contract.Result(Of IReadableList(Of Slot))() IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IReadableList(Of Slot))() Is _slots)
                Return _slots
            End Get
        End Property
        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_playableWidth > 0)
            Contract.Invariant(_playableHeight > 0)
            Contract.Invariant(_fileSize > 0)
            Contract.Invariant(_mapChecksumSHA1 IsNot Nothing)
            Contract.Invariant(_mapChecksumSHA1.Count = 20)
            Contract.Invariant(_slots IsNot Nothing)
            Contract.Invariant(_numPlayerSlots > 0)
            Contract.Invariant(_numPlayerSlots <= 12)
            Contract.Invariant(_advertisedPath.StartsWith("Maps\"))
        End Sub
        Public ReadOnly Property NumPlayerSlots As Integer
            Get
                Contract.Ensures(Contract.Result(Of Integer)() > 0)
                Contract.Ensures(Contract.Result(Of Integer)() <= 12)
                Return _numPlayerSlots
            End Get
        End Property
        Public ReadOnly Property FileSize As UInteger
            Get
                Contract.Ensures(Contract.Result(Of UInteger)() > 0)
                Return _fileSize
            End Get
        End Property
        Public ReadOnly Property MapChecksumSHA1 As IReadableList(Of Byte)
            Get
                Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))() IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))().Count = 20)
                Return _mapChecksumSHA1
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
        Public ReadOnly Property Folder As InvariantString
            Get
                Return _folder
            End Get
        End Property
        Public ReadOnly Property AdvertisedPath As InvariantString
            Get
                Contract.Ensures(Contract.Result(Of InvariantString)().StartsWith("Maps\"))
                Return _advertisedPath
            End Get
        End Property
        Public ReadOnly Property FullPath As InvariantString
            Get
                Return _fullPath
            End Get
        End Property
        Public ReadOnly Property Name As InvariantString
            Get
                Return _name
            End Get
        End Property
        Public ReadOnly Property IsMelee As Boolean
            Get
                Return _isMelee
            End Get
        End Property
        Public ReadOnly Property PlayableWidth As Integer
            Get
                Contract.Ensures(Contract.Result(Of Integer)() > 0)
                Return _playableWidth
            End Get
        End Property
        Public ReadOnly Property PlayableHeight As Integer
            Get
                Contract.Ensures(Contract.Result(Of Integer)() > 0)
                Return _playableHeight
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
        Public ReadOnly Property GameType As GameTypes
            Get
                Dim f = GameTypes.MakerUser
                Select Case SizeClassification
                    Case SizeClass.Tiny, SizeClass.Small
                        f = f Or GameTypes.SizeSmall
                    Case SizeClass.Medium
                        f = f Or GameTypes.SizeMedium
                    Case SizeClass.Large, SizeClass.Huge
                        f = f Or GameTypes.SizeLarge
                End Select
                If IsMelee Then
                    f = f Or GameTypes.TypeMelee
                Else
                    f = f Or GameTypes.TypeScenario
                End If
                Return f
            End Get
        End Property

#Region "New"
        Public Shared Function FromArgument(ByVal arg As String) As Map
            Contract.Requires(arg IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Map)() IsNot Nothing)
            If arg.Length <= 0 Then
                Throw New ArgumentException("Empty argument.")
            ElseIf arg(0) = "-"c Then
                Throw New ArgumentException("Map argument begins with '-', is probably an option. (did you forget an argument?)")
            ElseIf arg.StartsWith("0x", StringComparison.OrdinalIgnoreCase) Then 'Map specified by HostMapInfo packet data
                'Parse
                If arg Like "0x*[!0-9a-fA-F]" OrElse arg.Length Mod 2 <> 0 Then
                    Throw New ArgumentException("Invalid map meta data. [0x prefix should be followed by hex HostMapInfo packet data].")
                End If
                Dim hexData = (From i In Enumerable.Range(1, arg.Length \ 2 - 1)
                               Select CByte(arg.Substring(i * 2, 2).FromHexToUInt64(ByteOrder.BigEndian))
                               ).ToArray
                Dim vals = Protocol.Packets.HostMapInfo.Parse(hexData.AsReadableList).Value

                'Extract values
                Dim path As InvariantString = CStr(vals("path")).AssumeNotNull
                Dim size = CUInt(vals("size"))
                Dim crc32 = CUInt(vals("crc32"))
                Dim xoro = CUInt(vals("xoro checksum"))
                Dim sha1 = CType(vals("sha1 checksum"), IReadableList(Of Byte)).AssumeNotNull
                If Not path.StartsWith("Maps\") Then
                    Throw New IO.InvalidDataException("Invalid map path.")
                End If
                Contract.Assume(path.Length >= "Maps\".Length)
                path = path.Substring("Maps\".Length)
                Contract.Assume(sha1.Count = 20)
                Contract.Assume(size > 0)

                Return New Map(My.Settings.mapPath.AssumeNotNull, path, size, crc32, sha1, xoro, slotCount:=3)
            Else 'Map specified by path
                Return New Map(My.Settings.mapPath.AssumeNotNull,
                               FindFileMatching("*{0}*".Frmt(arg), "*.[wW]3[mxMX]", My.Settings.mapPath.AssumeNotNull),
                               My.Settings.war3path.AssumeNotNull)
            End If
        End Function
        Public Sub New(ByVal folder As InvariantString,
                       ByVal relativePath As InvariantString,
                       ByVal fileSize As UInteger,
                       ByVal fileChecksumCRC32 As UInt32,
                       ByVal mapChecksumSHA1 As IReadableList(Of Byte),
                       ByVal mapChecksumXORO As UInt32,
                       ByVal slotCount As Integer)
            Contract.Requires(mapChecksumSHA1 IsNot Nothing)
            Contract.Requires(mapChecksumSHA1.Count = 20)
            Contract.Requires(slotCount > 0)
            Contract.Requires(slotCount <= 12)
            Contract.Requires(fileSize > 0)
            Contract.Ensures(Me.Slots.Count = slotCount)

            Me._fullPath = IO.Path.Combine(folder, relativePath.ToString.Replace("\", IO.Path.DirectorySeparatorChar))
            Me._advertisedPath = "Maps\" + relativePath
            Me._folder = folder
            Me._playableHeight = 256
            Me._playableWidth = 256
            Me._isMelee = True
            Me._name = relativePath.ToString.Split("\"c).Last.AssumeNotNull
            Me._numPlayerSlots = slotCount
            Me._fileSize = fileSize
            Me._fileChecksumCRC32 = fileChecksumCRC32
            Me._mapChecksumSHA1 = mapChecksumSHA1
            Me._mapChecksumXORO = mapChecksumXORO
            Dim slots = New List(Of Slot)
            For slotId = 1 To slotCount
                Dim slot = New Slot(CByte(slotId), Me.IsMelee)
                slot.color = CType(slotId - 1, Slot.PlayerColor)
                slot.Contents = New SlotContentsOpen(slot)
                slots.Add(slot)
            Next slotId
            Me._slots = slots.AsReadableList
            Contract.Assume(Me.Slots.Count = slotCount)
        End Sub

        'verification disabled due to stupid verifier (1.2.30118.5)
        <ContractVerification(False)>
        Public Sub New(ByVal folder As InvariantString,
                       ByVal relativePath As InvariantString,
                       ByVal wc3PatchMPQFolder As InvariantString)
            Me.fileAvailable = True
            Me._advertisedPath = IO.Path.Combine("Maps", relativePath).Replace(IO.Path.AltDirectorySeparatorChar, IO.Path.DirectorySeparatorChar).
                                                                       Replace(IO.Path.DirectorySeparatorChar, "\")
            Me._fullPath = IO.Path.Combine(folder, relativePath.ToString.Replace("\", IO.Path.DirectorySeparatorChar))
            Me._folder = folder
            Using f = New IO.FileStream(FullPath, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.ReadWrite)
                Me._fileSize = CUInt(f.Length)
                Me._fileChecksumCRC32 = f.ToEnumerator.CRC32
            End Using
            Dim mapArchive = New MPQ.Archive(FullPath)
            Dim war3PatchArchive = OpenWar3PatchArchive(wc3PatchMPQFolder)
            Me._mapChecksumSHA1 = ComputeMapSha1Checksum(mapArchive, war3PatchArchive).AsReadableList
            Me._mapChecksumXORO = CUInt(ComputeMapXoro(mapArchive, war3PatchArchive))

            Dim info = ReadMapInfo(mapArchive)
            Me._slots = info.slots.AsReadableList
            Me._isMelee = info.isMelee
            Me._numPlayerSlots = info.slots.Count
            Me._name = info.name
            Me._playableHeight = info.playableHeight
            Me._playableWidth = info.playableWidth
        End Sub
#End Region

#Region "Read"
        Public Function ReadChunk(ByVal pos As Integer,
                                  Optional ByVal maxLength As Integer = 1442) As IReadableList(Of Byte)
            Contract.Requires(pos >= 0)
            Contract.Requires(maxLength >= 0)
            Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))() IsNot Nothing)
            If pos > Me.FileSize Then Throw New InvalidOperationException("Attempted to read past end of map file.")
            If Not fileAvailable Then Throw New InvalidOperationException("Attempted to read map file data when no file available.")

            Dim buffer(0 To maxLength - 1) As Byte
            Using f = New IO.FileStream(FullPath, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read)
                f.Seek(pos, IO.SeekOrigin.Begin)
                Dim n = f.Read(buffer, 0, maxLength)
                If n < buffer.Length Then ReDim Preserve buffer(0 To n - 1)
                Return buffer.AsReadableList
            End Using
        End Function

        Private Shared Function OpenWar3PatchArchive(ByVal war3PatchFolder As String) As MPQ.Archive
            Contract.Requires(war3PatchFolder IsNot Nothing)
            Contract.Ensures(Contract.Result(Of MPQ.Archive)() IsNot Nothing)
            Dim normalPath = IO.Path.Combine(war3PatchFolder, "War3Patch.mpq")
            Dim copyPath = IO.Path.Combine(war3PatchFolder, "TinkerTempCopyWar3Patch{0}.mpq".Frmt(New CachedExternalValues().WC3ExeVersion.StringJoin(".")))
            If IO.File.Exists(copyPath) Then
                Return New MPQ.Archive(copyPath)
            ElseIf IO.File.Exists(normalPath) Then
                Try
                    Return New MPQ.Archive(normalPath)
                Catch e As IO.IOException
                    IO.File.Copy(normalPath, copyPath)
                    Return New MPQ.Archive(copyPath)
                End Try
            Else
                Throw New IO.IOException("Couldn't find War3Patch.mpq")
            End If
        End Function

        '''<summary>Computes one of the checksums used to uniquely identify maps.</summary>
        Private Shared Function ComputeMapSha1Checksum(ByVal mapArchive As MPQ.Archive,
                                                       ByVal war3PatchArchive As MPQ.Archive) As Byte()
            Contract.Requires(mapArchive IsNot Nothing)
            Contract.Requires(war3PatchArchive IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Byte())() IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Byte())().Length = 20)
            Dim streams As New List(Of IO.Stream)

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
            streams.Add(New IO.MemoryStream(New Byte() {&H9E, &H37, &HF1, &H3}))

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

            Using f = New IO.BufferedStream(New ConcatStream(streams))
                Using sha = New Security.Cryptography.SHA1Managed()
                    Dim result = sha.ComputeHash(f)
                    Contract.Assume(result IsNot Nothing)
                    Contract.Assume(result.Length = 20)
                    Return result
                End Using
            End Using
        End Function

        '''<summary>Computes parts of the Xoro checksum.</summary>
        Private Shared Function ComputeStreamXoro(ByVal stream As IO.Stream) As ModInt32
            Contract.Requires(stream IsNot Nothing)
            Dim val As ModInt32 = 0

            With New IO.BinaryReader(New IO.BufferedStream(stream))
                'Process complete dwords
                For repeat = 1 To stream.Length \ 4
                    val = (val Xor .ReadUInt32()).ShiftRotateLeft(3)
                Next repeat

                'Process bytes not in a complete dword
                For repeat = 1 To stream.Length Mod 4
                    val = (val Xor .ReadByte()).ShiftRotateLeft(3)
                Next repeat
            End With

            Return val
        End Function

        '''<summary>Computes one of the checksums used to uniquely identify maps.</summary>
        Private Shared Function ComputeMapXoro(ByVal mapArchive As MPQ.Archive,
                                               ByVal war3PatchArchive As MPQ.Archive) As ModInt32
            Contract.Requires(mapArchive IsNot Nothing)
            Contract.Requires(war3PatchArchive IsNot Nothing)
            Dim val As ModInt32 = 0

            'Overridable map files from war3patch.mpq
            For Each filename In {"scripts\common.j",
                                  "scripts\blizzard.j"}
                Contract.Assume(filename IsNot Nothing)
                Dim mpqToUse = If(mapArchive.Hashtable.Contains(filename),
                                  mapArchive,
                                  war3PatchArchive)
                Using f = mpqToUse.OpenFileByName(filename)
                    val = val Xor ComputeStreamXoro(f)
                End Using
            Next filename

            'Magic value
            val = val.ShiftRotateLeft(3)
            val = (val Xor &H3F1379E).ShiftRotateLeft(3)

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
                    Using f = mapArchive.OpenFileByName(filenameToUse)
                        val = (val Xor ComputeStreamXoro(f)).ShiftRotateLeft(3)
                    End Using
                End If
            Next fileset

            Return val
        End Function

        '''<summary>Finds a string in the war3map.wts file. Returns null if the string is not found.</summary>
        Private Shared Function TryGetMapString(ByVal mapArchive As MPQ.Archive,
                                                ByVal key As String) As String
            Contract.Requires(mapArchive IsNot Nothing)
            Contract.Requires(key IsNot Nothing)

            'Open strings file and search for given key
            Using sr = New IO.StreamReader(New IO.BufferedStream(mapArchive.OpenFileByName("war3map.wts")))
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
            If key.StartsWith("TRIGSTR_", StringComparison.OrdinalIgnoreCase) Then
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
                                                 ByVal nameKey As String) As String
            Contract.Requires(mapArchive IsNot Nothing)
            Contract.Requires(nameKey IsNot Nothing)
            Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
            Try
                Return If(TryGetMapString(mapArchive, nameKey), nameKey)
            Catch e As Exception
                e.RaiseAsUnexpected("Error reading map strings file for {0}".Frmt(nameKey))
                Return "{0} (error reading strings file: {1})".Frmt(nameKey, e.Message)
            End Try
        End Function

        <Flags()>
        Private Enum MapOptions As UInteger
            HideMinimap = 1 << 0
            ModifyAllyPriorities = 1 << 1
            Melee = 1 << 2

            RevealTerrain = 1 << 4
            FixedForces = 1 << 5
            CustomForces = 1 << 6
            CustomTechTree = 1 << 7
            CustomAbilities = 1 << 8
            CustomUpgrades = 1 << 9

            WaterWavesOnCliffShores = 1 << 11
            WaterWavesOnSlopeShores = 1 << 12
        End Enum
        Private Enum MapInfoFormatVersion As Integer
            ROC = 18
            TFT = 25
        End Enum
        Private Class ReadMapInfoResult
            Public ReadOnly playableWidth As Integer
            Public ReadOnly playableHeight As Integer
            Public ReadOnly isMelee As Boolean
            Public ReadOnly slots As List(Of Slot)
            Public ReadOnly name As InvariantString

            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(playableWidth > 0)
                Contract.Invariant(playableHeight > 0)
                Contract.Invariant(slots IsNot Nothing)
                Contract.Invariant(slots.Count > 0)
                Contract.Invariant(slots.Count <= 12)
            End Sub

            Public Sub New(ByVal name As InvariantString,
                           ByVal playableWidth As Integer,
                           ByVal playableHeight As Integer,
                           ByVal isMelee As Boolean,
                           ByVal slots As List(Of Slot))
                Contract.Requires(playableWidth > 0)
                Contract.Requires(playableHeight > 0)
                Contract.Requires(slots IsNot Nothing)
                Contract.Requires(slots.Count > 0)
                Contract.Requires(slots.Count <= 12)
                Me.playableHeight = playableHeight
                Me.playableWidth = playableWidth
                Me.isMelee = isMelee
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

            Using br = New IO.BinaryReader(New IO.BufferedStream(mapArchive.OpenFileByName("war3map.w3i")))
                Dim fileFormat = CType(br.ReadInt32(), MapInfoFormatVersion)
                If Not fileFormat.EnumValueIsDefined Then
                    Throw New IO.InvalidDataException("Unrecognized war3map.w3i format.")
                End If

                br.ReadInt32() 'number of saves (map version)
                br.ReadInt32() 'editor version (little endian)

                Dim mapName = SafeGetMapString(mapArchive, nameKey:=br.ReadNullTerminatedString()) 'map description key

                br.ReadNullTerminatedString() 'map author
                br.ReadNullTerminatedString() 'map description
                br.ReadNullTerminatedString() 'players recommended
                For repeat = 1 To 8
                    br.ReadSingle()  '"Camera Bounds" as defined in the JASS file
                Next repeat
                For repeat = 1 To 4
                    br.ReadInt32() 'camera bounds complements
                Next repeat

                Dim playableWidth = br.ReadInt32() 'map playable area width
                Dim playableHeight = br.ReadInt32() 'map playable area height
                If playableWidth <= 0 Then Throw New IO.InvalidDataException("Non-positive map playable width.")
                If playableHeight <= 0 Then Throw New IO.InvalidDataException("Non-positive map playable height.")
                Dim options = CType(br.ReadInt32(), MapOptions) 'flags

                br.ReadByte() 'map main ground type
                If fileFormat = MapInfoFormatVersion.ROC Then
                    br.ReadInt32() 'Campaign background number (-1 = none)
                End If
                If fileFormat = MapInfoFormatVersion.TFT Then
                    br.ReadInt32() 'Loading screen background number which is its index in the preset list (-1 = none or custom imported file)
                    br.ReadNullTerminatedString() 'path of custom loading screen model (empty string if none or preset)
                End If
                br.ReadNullTerminatedString() 'Map loading screen text
                br.ReadNullTerminatedString() 'Map loading screen title
                br.ReadNullTerminatedString() 'Map loading screen subtitle
                If fileFormat = MapInfoFormatVersion.ROC Then
                    br.ReadInt32() 'Map loading screen number (-1 = none)
                End If
                If fileFormat = MapInfoFormatVersion.TFT Then
                    br.ReadInt32() 'used game data set (index in the preset list, 0 = standard)
                    br.ReadNullTerminatedString() 'Prologue screen path
                End If
                br.ReadNullTerminatedString() 'Prologue screen text
                br.ReadNullTerminatedString() 'Prologue screen title
                br.ReadNullTerminatedString() 'Prologue screen subtitle
                If fileFormat = MapInfoFormatVersion.TFT Then
                    br.ReadInt32() 'uses terrain fog (0 = not used, greater 0 = index of terrain fog style dropdown box)
                    br.ReadSingle() 'fog start z height
                    br.ReadSingle() 'fog end z height
                    br.ReadSingle() 'fog density
                    br.ReadByte() 'fog red value
                    br.ReadByte() 'fog green value
                    br.ReadByte() 'fog blue value
                    br.ReadByte() 'fog alpha value
                    br.ReadInt32() 'global weather id (0 = none, else it's set to the 4-letter-id of the desired weather found in TerrainArt\Weather.slk)
                    br.ReadNullTerminatedString() 'custom sound environment (set to the desired sound label)
                    br.ReadByte() 'tileset id of the used custom light environment
                    br.ReadByte() 'custom water tinting red value
                    br.ReadByte() 'custom water tinting green value
                    br.ReadByte() 'custom water tinting blue value
                    br.ReadByte() 'custom water tinting alpha value
                End If

                'Player Slots
                Dim numSlotsInFile = br.ReadInt32()
                If numSlotsInFile <= 0 OrElse numSlotsInFile > 12 Then
                    Throw New IO.InvalidDataException("Invalid number of slots.")
                End If
                Dim slots = New List(Of Slot)(capacity:=numSlotsInFile)
                Dim slotColorMap = New Dictionary(Of Slot.PlayerColor, Slot)
                For repeat = 0 To numSlotsInFile - 1
                    Dim slot = New Slot(CByte(slots.Count + 1), (options Or MapOptions.Melee) <> 0)
                    'color
                    slot.color = CType(br.ReadInt32(), Slot.PlayerColor)
                    If Not slot.color.EnumValueIsDefined Then Throw New IO.InvalidDataException("Unrecognized map slot color.")
                    'type
                    Select Case br.ReadInt32() '0=?, 1=available, 2=cpu, 3=unused
                        Case 1 : slot.Contents = New SlotContentsOpen(slot)
                        Case 2 : slot.Contents = New SlotContentsComputer(slot, slot.ComputerLevel.Normal)
                        Case 3 : slot = Nothing
                        Case Else
                            Throw New IO.InvalidDataException("Unrecognized map slot type.")
                    End Select
                    'race
                    Dim race = slot.Races.Random
                    Select Case br.ReadInt32()
                        Case 1 : race = slot.Races.Human
                        Case 2 : race = slot.Races.Orc
                        Case 3 : race = slot.Races.Undead
                        Case 4 : race = slot.Races.NightElf
                        Case Else
                            Throw New IO.InvalidDataException("Unrecognized map slot race.")
                    End Select
                    'player
                    br.ReadInt32() 'fixed start position
                    br.ReadNullTerminatedString() 'slot player name
                    br.ReadSingle() 'start position x
                    br.ReadSingle() 'start position y
                    br.ReadInt32() 'ally low priorities
                    br.ReadInt32() 'ally high priorities

                    If slot IsNot Nothing Then
                        slots.Add(slot)
                        slot.race = race
                        slotColorMap(slot.color) = slot
                    End If
                    Contract.Assume(slots.Count <= numSlotsInFile)
                Next repeat
                If slots.Count <= 0 Then Throw New IO.InvalidDataException("Map contains no player slots.")
                Contract.Assert(slots.Count <= 12)

                'Forces
                Dim numForces = br.ReadInt32()
                If numForces <= 0 OrElse numForces > 12 Then
                    Throw New IO.InvalidDataException("Invalid number of forces.")
                End If
                For teamIndex = CByte(0) To CByte(numForces - 1)
                    br.ReadInt32() 'force flags
                    Dim memberBitField = br.ReadUInt32() 'force members
                    br.ReadNullTerminatedString() 'force name

                    For Each color In EnumValues(Of Slot.PlayerColor)()
                        If Not CBool((memberBitField >> CInt(color)) And &H1) Then Continue For
                        If Not slotColorMap.ContainsKey(color) Then Continue For
                        Contract.Assume(slotColorMap(color) IsNot Nothing)
                        slotColorMap(color).Team = teamIndex
                    Next color
                Next teamIndex

                '... more data in the file but it isn't needed ...

                Dim isMelee = CBool(options And MapOptions.Melee)
                If isMelee Then
                    For i = 0 To slots.Count - 1
                        Contract.Assume(slots(i) IsNot Nothing)
                        slots(i).Team = CByte(i)
                        slots(i).race = Slot.Races.Random
                    Next i
                End If
                Return New ReadMapInfoResult(mapName, playableWidth, playableHeight, isMelee, slots)
            End Using
        End Function
#End Region
    End Class
End Namespace
