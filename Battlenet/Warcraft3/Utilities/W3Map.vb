Namespace Warcraft3
    Public Class W3Map
        Public playableWidth As Integer
        Public playableHeight As Integer
        Public isMelee As Boolean
        Public name As String
        Public numForces As Integer
        Private _numPlayerSlots As Integer
        Private ReadOnly _crc32 As ViewableList(Of Byte)
        Private ReadOnly _fileSize As UInteger
        Private ReadOnly _contentChecksumSha1 As ViewableList(Of Byte)
        Private ReadOnly _contentChecksumXORO As ViewableList(Of Byte)
        Private ReadOnly _folder As String
        Private ReadOnly _relativePath As String
        Private ReadOnly _fullPath As String
        Public fileAvailable As Boolean
        Public slots As New List(Of W3Slot)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_fileSize > 0)
            Contract.Invariant(_contentChecksumSha1 IsNot Nothing)
            Contract.Invariant(_contentChecksumSha1.Length = 20)
            Contract.Invariant(_contentChecksumXORO IsNot Nothing)
            Contract.Invariant(_contentChecksumXORO.Length = 4)
            Contract.Invariant(_crc32 IsNot Nothing)
            Contract.Invariant(_crc32.Length = 4)
            Contract.Invariant(_folder IsNot Nothing)
            Contract.Invariant(_relativePath IsNot Nothing)
            Contract.Invariant(_fullPath IsNot Nothing)
            Contract.Invariant(_numPlayerSlots > 0)
            Contract.Invariant(_numPlayerSlots <= 12)
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
        Public ReadOnly Property ChecksumSHA1 As ViewableList(Of Byte)
            Get
                Contract.Ensures(Contract.Result(Of ViewableList(Of Byte))() IsNot Nothing)
                Contract.Ensures(Contract.Result(Of ViewableList(Of Byte))().Length = 20)
                Return _contentChecksumSha1
            End Get
        End Property
        Public ReadOnly Property ChecksumCRC32 As ViewableList(Of Byte)
            Get
                Contract.Ensures(Contract.Result(Of ViewableList(Of Byte))() IsNot Nothing)
                Contract.Ensures(Contract.Result(Of ViewableList(Of Byte))().Length = 4)
                Return _crc32
            End Get
        End Property
        Public ReadOnly Property ChecksumXORO As ViewableList(Of Byte)
            Get
                Contract.Ensures(Contract.Result(Of ViewableList(Of Byte))() IsNot Nothing)
                Contract.Ensures(Contract.Result(Of ViewableList(Of Byte))().Length = 4)
                Return _contentChecksumXORO
            End Get
        End Property
        Public ReadOnly Property Folder As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Return _folder
            End Get
        End Property
        Public ReadOnly Property RelativePath As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Return _relativePath
            End Get
        End Property
        Public ReadOnly Property FullPath As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Return _fullPath
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
                Select Case playableWidth * playableHeight
                    Case Is <= 64 * 64
                        Return SizeClass.Tiny
                    Case Is <= 128 * 128
                        Return SizeClass.Small
                    Case Is <= 160 * 160
                        Return SizeClass.Medium
                    Case Is <= 192 * 192
                        Return SizeClass.Large
                    Case Else
                        Return SizeClass.Huge
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
                If isMelee Then
                    f = f Or GameTypes.TypeMelee
                Else
                    f = f Or GameTypes.TypeScenario
                End If
                Return f
            End Get
        End Property

#Region "Properties"
        Public ReadOnly Property PlayerAndObsSlotCount(ByVal mapSettings As W3MapSettings) As Integer
            Get
                Select Case mapSettings.observers
                    Case GameObserverOption.FullObservers, GameObserverOption.Referees
                        Return 12
                    Case Else
                        Return numPlayerSlots
                End Select
            End Get
        End Property
#End Region

#Region "New"
        Public Shared Function FromArgument(ByVal arg As String) As W3Map
            If arg(0) = "-"c Then
                Throw New ArgumentException("Map argument begins with '-', is probably an option. (did you forget an argument?)")
            ElseIf arg Like "0[xX]*" Then
                Dim out_fail = New ArgumentException("Invalid map meta data. [0x prefix should be followed by hex MAP_INFO packet data].")
                If arg Like "0x*[!0-9a-fA-F]" Then Throw out_fail
                If arg.Length Mod 2 <> 0 Then Throw out_fail

                Dim vals As Dictionary(Of String, Object)
                Try
                    Dim hexData(0 To arg.Length \ 2 - 1 - 1) As Byte
                    For i = 0 To hexData.Length - 1
                        hexData(i) = CByte(arg.Substring(i * 2 + 2, 2).FromHexToUInt64(ByteOrder.BigEndian))
                    Next i

                    Dim packet = W3Packet.FromData(W3PacketId.HostMapInfo, hexData.ToView())
                    vals = CType(packet.payload.Value, Dictionary(Of String, Object))
                Catch e As Exception
                    Throw out_fail
                End Try

                Dim path = CStr(vals("path"))
                Dim size = CUInt(vals("size"))
                Dim crc32 = CType(vals("crc32"), Byte())
                Dim xoro = CType(vals("xoro checksum"), Byte())
                Dim sha1 = CType(vals("sha1 checksum"), Byte())
                Return New W3Map(My.Settings.mapPath, path, size, crc32, sha1, xoro, 3)
            Else
                Return New W3Map(My.Settings.mapPath,
                                 FindFileMatching("*" + arg + "*", "*.[wW]3[mxMX]", My.Settings.mapPath),
                                 My.Settings.war3path)
            End If
        End Function
        Public Sub New(ByVal folder As String,
                       ByVal relativePath As String,
                       ByVal fileSize As UInteger,
                       ByVal contentChecksumCRC32 As Byte(),
                       ByVal contentChecksumSHA1 As Byte(),
                       ByVal contentChecksumXORO As Byte(),
                       ByVal slotCount As Integer)
            Contract.Requires(folder IsNot Nothing)
            Contract.Requires(relativePath IsNot Nothing)
            Contract.Requires(contentChecksumSHA1 IsNot Nothing)
            Contract.Requires(contentChecksumSHA1.Length = 20)
            Contract.Requires(contentChecksumCRC32 IsNot Nothing)
            Contract.Requires(contentChecksumXORO IsNot Nothing)
            Contract.Requires(contentChecksumXORO.Length = 4)
            Contract.Requires(contentChecksumCRC32 IsNot Nothing)
            Contract.Requires(contentChecksumCRC32.Length = 4)
            Contract.Requires(slotCount > 0)
            Contract.Requires(slotCount <= 12)
            Contract.Requires(fileSize > 0)
            Contract.Ensures(Me.slots.Count = slotCount)
            Me._fullPath = folder + relativePath.Substring(5)
            Me._relativePath = relativePath
            Me._folder = folder
            Me.playableHeight = 256
            Me.playableWidth = 256
            Me.isMelee = True
            Me.name = Mpq.Common.GetFileNameSlash(relativePath)
            Me._numPlayerSlots = slotCount
            Me._fileSize = fileSize
            Me._crc32 = contentChecksumCRC32.ToView
            Me._contentChecksumSha1 = contentChecksumSHA1.ToView
            Me._contentChecksumXORO = contentChecksumXORO.ToView
            For i = 1 To slotCount
                Dim slot = New W3Slot(Nothing, CByte(i))
                slot.color = CType(i - 1, W3Slot.PlayerColor)
                slot.contents = New W3SlotContentsOpen(slot)
                slots.Add(slot)
            Next i
        End Sub
        Public Sub New(ByVal folder As String,
                       ByVal relativePath As String,
                       ByVal wc3PatchMPQFolder As String)
            Me.fileAvailable = True
            Me._relativePath = relativePath
            Me._fullPath = folder + relativePath
            Me._folder = folder
            Using f = New IO.BufferedStream(New IO.FileStream(fullPath, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read))
                Me._fileSize = CUInt(f.Length)
                Me._crc32 = Bnet.Crypt.CRC32(f).Bytes().ToView
            End Using
            Dim mpqa = New Mpq.MpqArchive(fullPath)
            Dim mpq_war3path = OpenWar3PatchArchive(wc3PatchMPQFolder)
            Me._contentChecksumSha1 = ComputeMapSha1Checksum(mpqa, mpq_war3path).ToView
            Me._contentChecksumXORO = CUInt(ComputeMapXoro(mpqa, mpq_war3path)).Bytes().ToView

            ReadMapInfo(mpqa)

            If isMelee Then
                For i = 0 To slots.Count - 1
                    slots(i).team = CByte(i)
                    slots(i).race = W3Slot.Races.Random
                Next i
            End If
        End Sub
#End Region

#Region "Read"
        Public Function ReadChunk(ByVal pos As Integer,
                                  Optional ByVal maxLength As Integer = 1442) As Byte()
            If pos > Me.FileSize Then Throw New InvalidOperationException("Attempted to read past end of map file.")
            If Not fileAvailable Then Throw New InvalidOperationException("Attempted to read map file data when no file available.")

            Dim buffer(0 To maxLength - 1) As Byte
            Using f = New IO.FileStream(FullPath, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read)
                f.Seek(pos, IO.SeekOrigin.Begin)
                Dim n = f.Read(buffer, 0, maxLength)
                If n < buffer.Length Then ReDim Preserve buffer(0 To n - 1)
                Return buffer
            End Using
        End Function

        Private Function OpenWar3PatchArchive(ByVal war3PatchFolder As String) As Mpq.MpqArchive
            Dim backupMPQA As Mpq.MpqArchive = Nothing
            Dim normal_path = war3PatchFolder + "War3Patch.mpq"
            Dim copy_path = war3PatchFolder + "HostBotTempCopyWar3Patch" + My.Settings.exeVersion + ".mpq"
            If IO.File.Exists(copy_path) Then
                Return New Mpq.MpqArchive(copy_path)
            ElseIf IO.File.Exists(normal_path) Then
                Try
                    Return New Mpq.MpqArchive(normal_path)
                Catch e As IO.IOException
                    IO.File.Copy(normal_path, copy_path)
                    Return New Mpq.MpqArchive(copy_path)
                End Try
            Else
                Throw New IO.IOException("Couldn't find War3Patch.mpq")
            End If
        End Function

        '''<summary>Computes one of the checksums used to uniquely identify maps.</summary>
        Private Function ComputeMapSha1Checksum(ByVal mapArchive As Mpq.MpqArchive,
                                                ByVal war3PatchArchive As Mpq.MpqArchive) As Byte()
            Dim streams As New List(Of IO.Stream)

            'Overridable map files from war3patch.mpq
            For Each filename In {"scripts\common.j",
                                  "scripts\blizzard.j"}
                Dim mpqToUse = If(mapArchive.hashTable.contains(filename),
                                  mapArchive,
                                  war3PatchArchive)
                streams.Add(mpqToUse.OpenFile(filename))
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
                Dim filenameToUse = (From filename In fileset.Split("|"c)
                                     Where mapArchive.hashTable.contains(filename)).FirstOrDefault
                If filenameToUse IsNot Nothing Then
                    streams.Add(mapArchive.OpenFile(filenameToUse))
                End If
            Next fileset

            Using f = New IO.BufferedStream(New ConcatStream(streams))
                Using sha = New Security.Cryptography.SHA1Managed()
                    Return sha.ComputeHash(f)
                End Using
            End Using
        End Function

        '''<summary>Computes parts of the Xoro checksum.</summary>
        Private Function ComputeStreamXoro(ByVal stream As IO.Stream) As ModInt32
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
        Private Function ComputeMapXoro(ByVal mapArchive As Mpq.MpqArchive,
                                        ByVal war3PatchArchive As Mpq.MpqArchive) As ModInt32
            Dim val As ModInt32 = 0

            'Overridable map files from war3patch.mpq
            For Each filename In {"scripts\common.j",
                                  "scripts\blizzard.j"}
                Dim mpqToUse = If(mapArchive.hashTable.contains(filename),
                                  mapArchive,
                                  war3PatchArchive)
                Using f = mpqToUse.OpenFile(filename)
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
                Dim filenameToUse = (From filename In fileset.Split("|"c)
                                     Where mapArchive.hashTable.contains(filename)).FirstOrDefault
                If filenameToUse IsNot Nothing Then
                    Using f = mapArchive.OpenFile(filenameToUse)
                        val = (val Xor ComputeStreamXoro(f)).ShiftRotateLeft(3)
                    End Using
                End If
            Next fileset

            Return val
        End Function

        '''<summary>Finds a string in the war3map.wts file.</summary>
        Public Shared Function GetMapString(ByVal mapArchive As Mpq.MpqArchive, ByVal key As String) As String
            Using sr As New IO.StreamReader(New IO.BufferedStream(mapArchive.OpenFile("war3map.wts")))
                Do Until sr.EndOfStream
                    Dim cur_key = sr.ReadLine()
                    If sr.ReadLine <> "{" Then Continue Do
                    Dim cur_val As New System.Text.StringBuilder()
                    Do
                        If sr.EndOfStream Then Throw New IO.IOException("Invalid strings file")
                        Dim line = sr.ReadLine()
                        If line = "}" Then Exit Do
                        If cur_val.Length > 0 Then cur_val.Append(Environment.NewLine)
                        cur_val.Append(line)
                    Loop
                    If cur_key = key Then
                        Return cur_val.ToString
                    End If
                Loop
                Throw New KeyNotFoundException("String not found")
            End Using
        End Function

        '''<summary>Reads the map information from war3map.w3i</summary>
        '''<source>war3map.w3i format found at http://www.wc3campaigns.net/tools/specs/index.html by Zepir/PitzerMike</source>
        Public Sub ReadMapInfo(ByVal mapArchive As Mpq.MpqArchive)
            Using br = New IO.BinaryReader(New IO.BufferedStream(mapArchive.OpenFile("war3map.w3i")))
                Dim fileFormat = br.ReadInt32()
                If fileFormat <> 18 And fileFormat <> 25 Then Throw New IO.IOException("Unrecognized war3map.w3i format.")

                br.ReadInt32() 'number of saves (map version)
                br.ReadInt32() 'editor version (little endian)
                Dim name_key = ""
                Do 'map name
                    Dim b = br.ReadByte()
                    If b = 0 Then Exit Do
                    name_key += Chr(b)
                Loop
                Dim key = name_key
                Try
                    If key Like "TRIGSTR_#*" Then
                        Dim key_id As UInteger
                        If UInt32.TryParse(key.Substring("TRIGSTR_".Length), key_id) Then
                            key = "STRING {0}".frmt(key_id)
                        End If
                    End If
                    Me.name = GetMapString(mapArchive, key)
                Catch e As KeyNotFoundException
                    Me.name = key
                Catch e As Exception
                    Me.name = "{0} (error reading strings file: {1})".frmt(name_key, e)
                End Try
                While br.ReadByte() <> 0 : End While 'map author
                While br.ReadByte() <> 0 : End While 'map description
                While br.ReadByte() <> 0 : End While 'players recommended
                For repeat = 1 To 8
                    br.ReadSingle()  '"Camera Bounds" as defined in the JASS file
                Next repeat
                For repeat = 1 To 4
                    br.ReadInt32() 'camera bounds complements
                Next repeat

                playableWidth = br.ReadInt32() 'map playable area width
                playableHeight = br.ReadInt32() 'map playable area height

                Dim flags = br.ReadInt32() 'flags
                Me.isMelee = CBool(flags And &H4)
                br.ReadByte() 'map main ground type
                If fileFormat = 18 Then br.ReadInt32() 'Campaign background number (-1 = none)
                If fileFormat = 25 Then
                    br.ReadInt32() 'Loading screen background number which is its index in the preset list (-1 = none or custom imported file)
                    While br.ReadByte() <> 0 : End While 'path of custom loading screen model (empty string if none or preset)
                End If
                While br.ReadByte() <> 0 : End While 'Map loading screen text
                While br.ReadByte() <> 0 : End While 'Map loading screen title
                While br.ReadByte() <> 0 : End While 'Map loading screen subtitle
                If fileFormat = 18 Then br.ReadInt32() 'Map loading screen number (-1 = none)
                If fileFormat = 25 Then
                    br.ReadInt32() 'used game data set (index in the preset list, 0 = standard)
                    While br.ReadByte() <> 0 : End While 'Prologue screen path
                End If
                While br.ReadByte() <> 0 : End While 'Prologue screen text
                While br.ReadByte() <> 0 : End While 'Prologue screen title
                While br.ReadByte() <> 0 : End While 'Prologue screen subtitle
                If fileFormat = 25 Then
                    br.ReadInt32() 'uses terrain fog (0 = not used, greater 0 = index of terrain fog style dropdown box)
                    br.ReadSingle() 'fog start z height
                    br.ReadSingle() 'fog end z height
                    br.ReadSingle() 'fog density
                    br.ReadByte() 'fog red value
                    br.ReadByte() 'fog green value
                    br.ReadByte() 'fog blue value
                    br.ReadByte() 'fog alpha value
                    br.ReadInt32() 'global weather id (0 = none, else it's set to the 4-letter-id of the desired weather found in TerrainArt\Weather.slk)
                    While br.ReadByte() <> 0 : End While 'custom sound environment (set to the desired sound label)
                    br.ReadByte() 'tileset id of the used custom light environment
                    br.ReadByte() 'custom water tinting red value
                    br.ReadByte() 'custom water tinting green value
                    br.ReadByte() 'custom water tinting blue value
                    br.ReadByte() 'custom water tinting alpha value
                End If

                Dim numSlots = br.ReadInt32()
                For i = 1 To numSlots
                    Dim slot = New W3Slot(Nothing, CByte(slots.Count + 1))
                    'color
                    slot.color = CType(br.ReadInt32(), W3Slot.PlayerColor)
                    If Not slot.color.EnumValueIsDefined Then Throw New IO.IOException("Unrecognized map slot color.")
                    'type
                    Select Case br.ReadInt32()
                        Case 1
                            slot.contents = New W3SlotContentsOpen(slot)
                            slots.Add(slot)
                            _numPlayerSlots += 1
                        Case 2
                            slot.contents = New W3SlotContentsComputer(slot, W3Slot.ComputerLevel.Normal)
                            slots.Add(slot)
                            _numPlayerSlots += 1
                        Case 3
                            slot.contents = New W3SlotContentsClosed(slot)
                        Case Else
                            Throw New IO.IOException("Unrecognized map slot type.")
                    End Select
                    'race
                    Dim race = W3Slot.Races.Random
                    Select Case br.ReadInt32() 'race
                        Case 1 : race = W3Slot.Races.Human
                        Case 2 : race = W3Slot.Races.Orc
                        Case 3 : race = W3Slot.Races.Undead
                        Case 4 : race = W3Slot.Races.NightElf
                    End Select
                    slot.race = race
                    'player
                    br.ReadInt32() 'fixed start position
                    While br.ReadByte() <> 0 : End While 'slot player name
                    br.ReadSingle() 'start position x
                    br.ReadSingle() 'start position y
                    br.ReadInt32() 'ally low priorities
                    br.ReadInt32() 'ally high priorities
                Next i

                numForces = br.ReadInt32()
                For i = 0 To numForces - 1
                    br.ReadInt32() 'flags
                    Dim playerMask = br.ReadUInt32()
                    For j = 0 To 11
                        If (playerMask And &H1) <> 0 Then
                            For k = 0 To slots.Count - 1
                                If slots(k).color = j Then
                                    slots(k).team = CByte(i)
                                End If
                            Next k
                        End If
                        playerMask >>= 1
                    Next j
                    While br.ReadByte() <> 0 : End While 'force name
                Next i

                '... more data in the file but it isn't needed ...
            End Using
        End Sub
#End Region
    End Class
End Namespace
