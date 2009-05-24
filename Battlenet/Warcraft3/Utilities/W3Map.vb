Imports HostBot.Pickling.Jars

Namespace Warcraft3
    Public Class W3Map
#Region "Inner"
        Public Enum SPD
            SLOW
            MEDIUM
            FAST
        End Enum
        Public Enum OBS
            NO_OBSERVERS
            OBSERVERS_ON_DEFEAT
            FULL_OBSERVERS
            REFEREES
        End Enum
        Public Enum VIS
            MAP_DEFAULT
            ALWAYS_VISIBLE
            EXPLORED
            HIDE_TERRAIN
        End Enum
        Public Class MapSettings
            Public ReadOnly randomHero As Boolean = False
            Public ReadOnly randomRace As Boolean = False
            Public ReadOnly allowFullSharedControl As Boolean = False
            Public ReadOnly lockTeams As Boolean = True
            Public ReadOnly teamsTogether As Boolean = True
            Public ReadOnly observers As W3Map.OBS = W3Map.OBS.NO_OBSERVERS
            Public ReadOnly visibility As W3Map.VIS = W3Map.VIS.MAP_DEFAULT
            Public ReadOnly speed As W3Map.SPD = W3Map.SPD.FAST
            Public Sub New(ByVal arguments As IList(Of String))
                For Each arg In arguments
                    Dim arg2 = ""
                    If arg.Contains("="c) Then
                        Dim n = arg.IndexOf("="c)
                        arg2 = arg.Substring(n + 1)
                        arg = arg.Substring(0, n + 1)
                    End If
                    arg = arg.ToLower.Trim()
                    arg2 = arg2.ToLower.Trim()

                    Select Case arg
                        Case "-obs", "-multiobs", "-mo", "-o"
                            observers = W3Map.OBS.FULL_OBSERVERS
                        Case "-referees", "-ref"
                            observers = W3Map.OBS.REFEREES
                        Case "-obsondefeat", "-od"
                            observers = W3Map.OBS.OBSERVERS_ON_DEFEAT
                        Case "-rh", "-randomhero"
                            randomHero = True
                        Case "-rr", "-randomrace"
                            randomRace = True
                        Case "-unlockteams"
                            lockTeams = False
                        Case "-fullshared", "-fullshare", "-allowfullshared", "-allowfullshare", "-fullsharedcontrol", "-allowfullsharedcontrol"
                            allowFullSharedControl = True
                        Case "-teamsapart"
                            teamsTogether = False
                        Case "-speed="
                            Select Case arg2
                                Case "medium"
                                    speed = W3Map.SPD.MEDIUM
                                Case "slow"
                                    speed = W3Map.SPD.SLOW
                            End Select
                        Case "-visibility=", "-vis="
                            Select Case arg2
                                Case "all", "always visible", "visible", "alwaysvisible"
                                    visibility = W3Map.VIS.ALWAYS_VISIBLE
                                Case "explored"
                                    visibility = W3Map.VIS.EXPLORED
                                Case "none", "hide", "hideterrain"
                                    visibility = W3Map.VIS.HIDE_TERRAIN
                            End Select
                    End Select
                Next arg
            End Sub
        End Class
#End Region

#Region "Members"
        Public playableWidth As Integer
        Public playableHeight As Integer
        Public isMelee As Boolean
        Public name As String
        Public numForces As Integer
        Public numPlayerSlots As Integer
        Public ReadOnly crc32 As Byte()
        Public ReadOnly fileSize As UInteger
        Public ReadOnly checksum_sha1 As Byte() = Nothing
        Public ReadOnly checksum_xoro As Byte() = Nothing
        Public ReadOnly folder As String
        Public ReadOnly relative_path As String
        Public ReadOnly full_path As String
        Public downloaded_size As Integer
        Public file_available As Boolean
        Public slots As New List(Of W3Slot)

        Public ReadOnly Property numPlayerAndObsSlots(ByVal map_settings As W3Map.MapSettings) As Integer
            Get
                Select Case map_settings.observers
                    Case OBS.FULL_OBSERVERS, OBS.REFEREES
                        Return 12
                    Case Else
                        Return numPlayerSlots
                End Select
            End Get
        End Property
#End Region

#Region "New"
        Public Shared Function FromArgument(ByVal arg As String) As Outcome(Of W3Map)
            If arg(0) = "-"c Then
                Return failure("Map argument begins with '-', is probably an option. (did you forget an argument?)")
            ElseIf arg Like "0[xX]*" Then
                Dim out_fail = failure("Invalid map meta data. [0x prefix should be followed by hex MAP_INFO packet data].")
                If arg Like "0x*[!0-9a-fA-F]" Then Return out_fail
                If arg.Length Mod 2 <> 0 Then Return out_fail

                Dim vals As Dictionary(Of String, Object)
                Try
                    Dim packet_data(0 To arg.Length \ 2 - 1 - 1) As Byte
                    For i = 0 To packet_data.Length - 1
                        packet_data(i) = CByte(dehex(arg.Substring(i * 2 + 2, 2)))
                    Next i
                    Dim packet = W3Packet.FromData(W3PacketId.MAP_INFO, packet_data)
                    vals = CType(packet.payload.getVal, Dictionary(Of String, Object))
                Catch e As Exception
                    Return out_fail
                End Try

                Dim path = CStr(vals("path"))
                Dim size = CUInt(vals("size"))
                Dim crc32 = CType(vals("crc32"), Byte())
                Dim xoro = CType(vals("xoro checksum"), Byte())
                Dim sha1 = CType(vals("sha1 checksum"), Byte())
                Return successVal(New W3Map(My.Settings.mapPath, path, size, crc32, sha1, xoro, 3), "Loaded map meta data.")
            Else
                Dim out = findFileMatching("*" + arg + "*", "*.[wW]3[mxMX]", My.Settings.mapPath)
                If out.outcome <> Outcomes.succeeded Then Return CType(out, Outcome)
                Return successVal(New W3Map(My.Settings.mapPath, out.val, My.Settings.war3path), "Loaded map file.")
            End If
        End Function
        Public Sub New(ByVal folder As String, _
                       ByVal rel_path As String, _
                       ByVal fileSize As UInteger, _
                       ByVal crc32 As Byte(), _
                       ByVal sha1_checksum As Byte(), _
                       ByVal xoro_checksum As Byte(), _
                       ByVal num_slots As Integer)
            Me.file_available = False
            Me.full_path = folder + rel_path.Substring(5)
            Me.relative_path = rel_path
            Me.folder = folder
            Me.playableHeight = 256
            Me.playableWidth = 256
            Me.isMelee = True
            Me.name = getFileNameSlash(rel_path)
            Me.numPlayerSlots = num_slots
            Me.fileSize = fileSize
            Me.crc32 = crc32
            Me.checksum_sha1 = sha1_checksum
            Me.checksum_xoro = xoro_checksum
            For i = 1 To num_slots
                Dim slot = New W3Slot(Nothing, CByte(i))
                slot.color = CType(i - 1, W3Slot.PlayerColor)
                slot.contents = New W3SlotContentsOpen(slot)
                slots.Add(slot)
            Next i
        End Sub
        Public Sub New(ByVal folder As String, _
                       ByVal rel_path As String, _
                       ByVal w3patch_folder As String)
            Me.file_available = True
            Me.relative_path = rel_path
            Me.full_path = folder + rel_path
            Me.folder = folder
            Using f = New IO.BufferedStream(New IO.FileStream(full_path, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read))
                Me.fileSize = CUInt(f.Length)
                Me.crc32 = Bnet.Crypt.crc32(f).bytes()
            End Using
            Dim mpqa = New MPQ.MPQArchive(full_path)
            Dim mpq_war3path = OpenWar3PatchMpq(w3patch_folder)
            Me.checksum_sha1 = computeMapSha1Checksum(mpqa, mpq_war3path)
            Me.checksum_xoro = computeMapXoro(mpqa, mpq_war3path).bytes()

            readFileInfo(mpqa)

            If isMelee Then
                For i = 0 To slots.Count - 1
                    slots(i).team = CByte(i)
                    slots(i).race = W3Slot.RaceFlags.Random
                Next i
            End If
        End Sub
#End Region

#Region "Read"
        Public Function getChunk(ByVal pos As Integer, Optional ByVal maxLength As Integer = 1442) As Byte()
            If Not file_available Then Throw New InvalidOperationException("Attempted to read map file data when no file available.")
            Dim buffer(0 To maxLength - 1) As Byte
            Using f = New IO.FileStream(full_path, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read)
                If pos < f.Length Then
                    f.Seek(pos, IO.SeekOrigin.Begin)
                    Dim n = f.Read(buffer, 0, maxLength)
                    If n < buffer.Length Then ReDim Preserve buffer(0 To n - 1)
                    Return buffer
                Else
                    Return Nothing
                End If
            End Using
        End Function

        Private Function OpenWar3PatchMpq(ByVal w3patch_folder As String) As MPQ.MPQArchive
            Dim backupMPQA As MPQ.MPQArchive = Nothing
            Dim normal_path = w3patch_folder + "War3Patch.mpq"
            Dim copy_path = w3patch_folder + "HostBotTempCopyWar3Patch" + My.Settings.exeVersion + ".mpq"
            If IO.File.Exists(copy_path) Then
                Return New MPQ.MPQArchive(copy_path)
            ElseIf IO.File.Exists(normal_path) Then
                Try
                    Return New MPQ.MPQArchive(normal_path)
                Catch e As IO.IOException
                    IO.File.Copy(normal_path, copy_path)
                    Return New MPQ.MPQArchive(copy_path)
                End Try
            Else
                Throw New IO.IOException("Couldn't find War3Patch.mpq")
            End If
        End Function

        '''<summary>Computes one of the checksums used to uniquely identify maps.</summary>
        Private Function computeMapSha1Checksum(ByVal mpqa As MPQ.MPQArchive, ByVal mpq_war3path As MPQ.MPQArchive) As Byte()
            Dim streams As New List(Of IO.Stream)

            'Overridable map files from war3patch.mpq
            For Each filename In New String() { _
                        "scripts\common.j", _
                        "scripts\blizzard.j"}
                Dim mpq_to_use = If(mpqa.hashTable.contains(filename), mpqa, mpq_war3path)
                streams.Add(New MPQ.MPQFileStream(mpq_to_use, filename))
            Next filename

            'Magic value
            streams.Add(New IO.MemoryStream(New Byte() {&H9E, &H37, &HF1, &H3}))

            'Important map files
            For Each fileset In New String() { _
                        "war3map.j|scripts\war3map.j", _
                        "war3map.w3e", _
                        "war3map.wpm", _
                        "war3map.doo", _
                        "war3map.w3u", _
                        "war3map.w3b", _
                        "war3map.w3d", _
                        "war3map.w3a", _
                        "war3map.w3q"}
                For Each filename In fileset.Split("|"c)
                    If mpqa.hashTable.contains(filename) Then
                        streams.Add(New MPQ.MPQFileStream(mpqa, filename))
                        Exit For
                    End If
                Next filename
            Next fileset

            Using f = New IO.BufferedStream(New ConcatStream(streams))
                Return New Security.Cryptography.SHA1Managed().ComputeHash(f)
            End Using
        End Function

        '''<summary>Computes parts of the Xoro checksum.</summary>
        Private Function computeStreamXoro(ByVal stream As IO.Stream) As UInteger
            Dim val As UInteger = 0

            With New IO.BinaryReader(New IO.BufferedStream(stream))
                'Process complete dwords
                For repeat = 1 To stream.Length \ 4
                    val = ShiftRotateLeft(val Xor .ReadUInt32(), 3)
                Next repeat

                'Process bytes not in a complete dword
                For repeat = 1 To stream.Length Mod 4
                    val = ShiftRotateLeft(val Xor CUInt(.ReadByte()), 3)
                Next repeat
            End With

            Return val
        End Function

        '''<summary>Computes one of the checksums used to uniquely identify maps.</summary>
        Private Function computeMapXoro(ByVal mpqa As MPQ.MPQArchive, ByVal mpq_war3path As MPQ.MPQArchive) As UInteger
            Dim val = CUInt(0)

            'Overridable map files from war3patch.mpq
            For Each filename In New String() { _
                        "Scripts\common.j", _
                        "Scripts\blizzard.j"}
                Dim mpq_to_use = If(mpqa.hashTable.contains(filename), mpqa, mpq_war3path)
                Using f = New MPQ.MPQFileStream(mpq_to_use, filename)
                    val = val Xor computeStreamXoro(f)
                End Using
            Next filename

            'Magic value
            val = ShiftRotateLeft(val, 3)
            val = ShiftRotateLeft(val Xor CUInt(&H3F1379E), 3)

            'Important map files
            For Each fileset In New String() { _
                        "war3map.j|scripts\war3map.j", _
                        "war3map.w3e", _
                        "war3map.wpm", _
                        "war3map.doo", _
                        "war3map.w3u", _
                        "war3map.w3b", _
                        "war3map.w3d", _
                        "war3map.w3a", _
                        "war3map.w3q"}
                For Each filename In fileset.Split("|"c)
                    If mpqa.hashTable.contains(filename) Then
                        Using f = New MPQ.MPQFileStream(mpqa, filename)
                            val = ShiftRotateLeft(val Xor computeStreamXoro(f), 3)
                        End Using
                        Exit For
                    End If
                Next filename
            Next fileset

            Return val
        End Function

        '''<summary>Finds a string in the war3map.wts file.</summary>
        Public Shared Function GetMapString(ByVal mpqa As MPQ.MPQArchive, ByVal key As String) As String
            Using sr As New IO.StreamReader(New IO.BufferedStream(New MPQ.MPQFileStream(mpqa, "war3map.wts")))
                Do Until sr.EndOfStream
                    Dim cur_key = sr.ReadLine()
                    If sr.ReadLine <> "{" Then Throw New IO.IOException("Invalid strings file")
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
                Throw New IO.IOException("String not found")
            End Using
        End Function

        '''<summary>Reads the map information from war3map.w3i</summary>
        '''<source>war3map.w3i format found at http://www.wc3campaigns.net/tools/specs/index.html by Zepir/PitzerMike</source>
        Public Sub readFileInfo(ByVal mpqa As MPQ.MPQArchive)
            Using br = New IO.BinaryReader(New IO.BufferedStream(New MPQ.MPQFileStream(mpqa, "war3map.w3i")))
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
                Try
                    Dim key = name_key
                    If key Like "TRIGSTR_#*" Then
                        Dim key_id As UInteger
                        If UInt32.TryParse(key.Substring("TRIGSTR_".Length), key_id) Then
                            key = "STRING {0}".frmt(key_id)
                        End If
                    End If
                    Me.name = GetMapString(mpqa, key)
                Catch e As Exception
                    Me.name = "{0} (error reading strings file: {1})".frmt(name_key, e.Message)
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
                For i = 0 To numSlots - 1
                    Dim slot = New W3Slot(Nothing, CByte(slots.Count + 1))
                    'color
                    slot.color = CType(br.ReadInt32(), W3Slot.PlayerColor)
                    If Not IsEnumValid(slot.color) Then Throw New IO.IOException("Unrecognized map slot color.")
                    'type
                    Select Case br.ReadInt32()
                        Case 1
                            slot.contents = New W3SlotContentsOpen(slot)
                            slots.Add(slot)
                            numPlayerSlots += 1
                        Case 2
                            slot.contents = New W3SlotContentsComputer(slot, W3Slot.ComputerLevel.Normal)
                            slots.Add(slot)
                            numPlayerSlots += 1
                        Case 3
                            slot.contents = New W3SlotContentsClosed(slot)
                        Case Else
                            Throw New IO.IOException("Unrecognized map slot type.")
                    End Select
                    'race
                    Dim race = W3Slot.RaceFlags.Random
                    Select Case br.ReadInt32() 'race
                        Case 1 : race = W3Slot.RaceFlags.Human
                        Case 2 : race = W3Slot.RaceFlags.Orc
                        Case 3 : race = W3Slot.RaceFlags.Undead
                        Case 4 : race = W3Slot.RaceFlags.NightElf
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

#Region "StatString"
        Public Shared Function makeStatStringParser() As TupleJar
            Return New TupleJar("statstring", _
                New PrecodeJar("encoded", New TupleJar("encoded", _
                    New ArrayJar("settings", 4), _
                    New ValueJar("unknown1", 1), _
                    New ValueJar("playable width", 2), _
                    New ValueJar("playable height", 2), _
                    New ArrayJar("xoro checksum", 4), _
                    New StringJar("relative path"), _
                    New StringJar("username"), _
                    New StringJar("unknown2"))), _
                New ValueJar("terminator", 1))
        End Function

        ''' <summary>Encodes game parameters, properties, etc.</summary>
        ''' <returns>The encoded string</returns>
        ''' <remarks>
        '''   0 BYTE #slots-1 (text-hex)
        '''   1 BYTE[8] host counter (text-hex)
        '''   encoded:
        '''      2 DWORD settings
        '''      3 BYTE[5] map size and other?
        '''      4 DWORD xoro key
        '''      5 STRING map name
        '''      6 STRING user name
        '''      7 STRING unknown("")
        ''' </remarks>
        Public Function generateStatStringVals(ByVal username As String, ByVal map_settings As W3Map.MapSettings) As Dictionary(Of String, Object)
            Dim vals As New Dictionary(Of String, Object)
            Dim valsEncoded As New Dictionary(Of String, Object)

            'settings
            Dim b As Byte
            Dim bbSettings(0 To 3) As Byte
            b = 0
            'If False Then b = b Or CByte(&H80) 'unknown
            'If False Then b = b Or CByte(&H40) 'unknown
            'If False Then b = b Or CByte(&H20) 'unknown (set for the melee map "divide and conquer")
            'If False Then b = b Or CByte(&H10) 'unknown
            'If False Then b = b Or CByte(&H8) 'unknown
            'If False Then b = b Or CByte(&H4) 'unknown
            If map_settings.speed = SPD.FAST Then b = b Or CByte(&H2)
            If map_settings.speed = SPD.MEDIUM Then b = b Or CByte(&H1)
            bbSettings(0) = b
            b = 0
            'If False Then b = b Or CByte(&H80) 'unknown
            If map_settings.teamsTogether Then b = b Or CByte(&H40)
            If map_settings.observers = OBS.FULL_OBSERVERS Or map_settings.observers = OBS.OBSERVERS_ON_DEFEAT Then b = b Or CByte(&H20)
            If map_settings.observers = OBS.FULL_OBSERVERS Then b = b Or CByte(&H10)
            If map_settings.visibility = VIS.MAP_DEFAULT Then b = b Or CByte(&H8)
            If map_settings.visibility = VIS.ALWAYS_VISIBLE Then b = b Or CByte(&H4)
            If map_settings.visibility = VIS.EXPLORED Then b = b Or CByte(&H2)
            If map_settings.visibility = VIS.HIDE_TERRAIN Then b = b Or CByte(&H1)
            bbSettings(1) = b
            b = 0
            'If False Then b = b Or CByte(&H80) 'unknown
            'If False Then b = b Or CByte(&H40) 'unknown
            'If False Then b = b Or CByte(&H20) 'unknown
            'If False Then b = b Or CByte(&H10) 'unknown
            'If False Then b = b Or CByte(&H8) 'unknown
            If map_settings.lockTeams Then b = b Or CByte(&H4)
            If map_settings.lockTeams Then b = b Or CByte(&H2) 'why lock teams again?
            'If False Then b = b Or CByte(&H1) 'unknown
            bbSettings(2) = b
            b = 0
            'If False Then b = b Or CByte(&H80) 'unknown
            If map_settings.observers = OBS.REFEREES Then b = b Or CByte(&H40)
            'If False Then b = b Or CByte(&H20) 'unknown
            'If False Then b = b Or CByte(&H10) 'unknown
            'If False Then b = b Or CByte(&H8) 'unknown
            If map_settings.randomHero Then b = b Or CByte(&H4)
            If map_settings.randomRace Then b = b Or CByte(&H2)
            If map_settings.allowFullSharedControl Then b = b Or CByte(&H1)
            bbSettings(3) = b

            'values
            valsEncoded("playable width") = playableWidth
            valsEncoded("playable height") = playableHeight
            valsEncoded("settings") = bbSettings
            valsEncoded("xoro checksum") = checksum_xoro
            valsEncoded("relative path") = "Maps\" + relative_path
            valsEncoded("username") = username
            valsEncoded("unknown1") = CUInt(0)
            valsEncoded("unknown2") = ""
            vals("encoded") = valsEncoded
            vals("terminator") = 0
            Return vals
        End Function

        Public Class PrecodeJar
            Inherits Pickling.Jars.Jar
            Private ReadOnly subjar As Pickling.IJar

            Public Sub New(ByVal name As String, ByVal subjar As Pickling.IJar)
                MyBase.New(name)
                Me.subjar = subjar
            End Sub

            Private Class EncodedPickle
                Inherits Pickling.Pickles.Pickle
                Private subpickle As Pickling.IPickle
                Public Sub New(ByVal parent As Pickling.IJar, ByVal val As Object, ByVal view As ImmutableArrayView(Of Byte), ByVal subpickle As Pickling.IPickle)
                    MyBase.New(parent, val, view)
                    Me.subpickle = subpickle
                End Sub
                Public Overrides Function toString() As String
                    Return "{" + Environment.NewLine + indent(subpickle.toString()) + Environment.NewLine + "}"
                End Function
            End Class

            Public Overrides Function pack(ByVal o As Object) As Pickling.IPickle
                Dim p = subjar.pack(o)
                Dim bb = Bnet.Crypt.encodePreMaskedByteArray(p.getData)
                Return New EncodedPickle(Me, o, bb, p)
            End Function

            Public Overrides Function parse(ByVal view As ImmutableArrayView(Of Byte)) As Pickling.IPickle
                Throw New Pickling.PicklingException("Parsing not supported for precode jar")
            End Function
        End Class
#End Region
    End Class
End Namespace
