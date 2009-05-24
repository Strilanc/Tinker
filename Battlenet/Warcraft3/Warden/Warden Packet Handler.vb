Namespace Warcraft3.Warden
    Public Class WardenPacketHandler
        Private ReadOnly original_seed As UInteger
        Private ReadOnly module_folder As String
        Private ReadOnly module_handler As New Warden_Module_Lib.ModuleHandler
        Private ReadOnly iniA As New Dictionary(Of String, Byte())
        Private ReadOnly iniB As New Dictionary(Of String, Byte())
        Private ReadOnly logger As MultiLogger
        Private ReadOnly ref As ICallQueue

        Private state As states = states.idle
        Private cur_module As ModuleMetaData
        Private rc4_out As RC4Converter
        Private rc4_in As RC4Converter

        Public Event Send(ByVal data As Byte())
        Public Event Fail(ByVal e As Exception)

        Private Enum states As Byte
            idle = 0
            downloading = 1
            module_loaded = 2
        End Enum

        Private Class ModuleMetaData
            Public ReadOnly path As String
            Public ReadOnly dl_size As UInteger
            Public dl_pos As UInteger
            Public dl_stream As IO.Stream
            Public Sub New(ByVal size As UInteger, ByVal path As String)
                Me.dl_size = size
                Me.path = path
            End Sub
        End Class

#Region "Life"
        Private Sub load_ini_data()
            Dim iniPath = GetDataFolderPath("Warden") & "W3XP_Warden.ini"
            If IO.File.Exists(iniPath) Then
                Using sr As New IO.StreamReader(New IO.FileStream(iniPath, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read))
                    Dim d As Dictionary(Of String, Byte()) = Nothing
                    While Not sr.EndOfStream()
                        Dim line = sr.ReadLine()
                        Select Case line
                            Case ""
                            Case "[MEMORY]"
                                d = iniA
                            Case "[PAGEA]"
                                d = iniB
                            Case Else
                                If line Like "*?=?*" Then
                                    Dim p = line.IndexOf("="c)
                                    d(line.Substring(0, p)) = packHexString(line.Substring(p + 1))
                                Else
                                    Throw New IO.IOException("Invalid INI file.")
                                End If
                        End Select
                    End While
                End Using
            Else
                iniA("game.dll&H3A1DCE_7") = packHexString("E8 5D D6 C6 FF 8B D0")
                iniA("game.dll&H285B3A_8") = packHexString("E8 81 FA 22 00 8B 40 10")
                iniA("game.dll&H743576_8") = packHexString("C1 E0 08 03 E8 8B 84 AE")
                iniA("game.dll&H361DD3_7") = packHexString("E8 78 F4 1C 00 85 C0")
                iniA("game.dll&HF453_9") = packHexString("8B 41 14 8B 49 10 BA 02 00")
                iniA("game.dll&H3C1354_8") = packHexString("F6 D0 8A C8 8B 44 24 1C")
                iniA("game.dll&H3F92CA_6") = packHexString("75 0A 83 7B 14 00")
                iniA("game.dll&H3A1E8E_7") = packHexString("8B 54 24 20 0F B7 32")
                iniA("game.dll&H285B33_7") = packHexString("B9 0D 00 00 00 8B E8")
                iniA("game.dll&H283444_7") = packHexString("8B C8 BA 01 00 00 00")
                iniA("game.dll&H39A39B_6") = packHexString("8B 97 98 01 00 00")
                iniA("game.dll&H39A458_6") = packHexString("74 27 39 6C 24 44")
                iniA("game.dll&HF490_6") = packHexString("74 08 8B 00 83 C4")
                iniA("game.dll&H73DFFC_7") = packHexString("E8 DF 3D FF FF 85 C0")
                iniA("game.dll&H361DF9_7") = packHexString("33 C9 B8 01 00 00 00")
                iniA("game.dll&H431569_6") = packHexString("85 C0 0F 84 AD 00")
                iniA("game.dll&H356F1C_8") = packHexString("3B 86 18 02 00 00 89 44")
                iniA("game.dll&H3A1DE3_4") = packHexString("75 04 A8 02")
                iniA("game.dll&H36040A_6") = packHexString("EB 08 C7 44 24 18")
                iniA("game.dll&H285BA2_5") = packHexString("75 29 53 8B CF")
                iniA("game.dll&H3A1DE9_7") = packHexString("8B 44 24 24 66 09 18")
                iniA("game.dll&H39A3B1_10") = packHexString("55 50 56 E8 37 7B 00 00 23 D8")
                iniA("game.dll&H356C67_8") = packHexString("85 DB 8A 8E E8 07 00 00")
                iniA("game.dll&H361DFC_6") = packHexString("01 00 00 00 D3 E8")
                iniA("game.dll&H39A465_13") = packHexString("66 85 87 F4 01 00 00 74 1D 8B 8F 98 01")
                iniA("game.dll&H285B8C_6") = packHexString("74 2A 8B 44 24 20")
                iniA("game.dll&H28345C_4") = packHexString("C3 CC CC CC")
                iniA("game.dll&H3A1E64_6") = packHexString("8B 0C 41 66 8B 04")
                iniA("game.dll&H356E7E_5") = packHexString("66 85 C0 76 04")
                iniA("game.dll&H73DEC9_6") = packHexString("8A 90 6C 68 AA 6F")
                iniA("game.dll&H3C135C_10") = packHexString("3D FF 00 00 00 76 05 C1 F8 1F")
                iniA("game.dll&H362211_10") = packHexString("85 C0 0F 84 30 04 00 00 8B 03")
                iniA("game.dll&H431556_6") = packHexString("85 C0 0F 84 C0 00")
                iniA("game.dll&H3A1E9B_4") = packHexString("23 CA 75 32")
                iniA("game.dll&H3C5C22_12") = packHexString("74 0B 81 88 7C 02 00 00 00 02 00 00")
                iniA("game.dll&H73DEB7_10") = packHexString("0F B7 0C 4A 81 C9 00 F0 00 00")

                iniB("&HD0000E8") = packHexString("00")
                iniB("&HE000622") = packHexString("00")
                iniB("&H300006D4") = packHexString("00")
                iniB("&H19000059") = packHexString("00")
                iniB("&H300006D7") = packHexString("00")
                iniB("&H23000048") = packHexString("00")
                iniB("&H2A0000F1") = packHexString("00")
                iniB("&H24000032") = packHexString("00")
                iniB("&HE0001FD") = packHexString("00")
                iniB("&H20000049") = packHexString("00")
                iniB("&H300007A8") = packHexString("00")
                iniB("&H1700007C") = packHexString("00")
                iniB("&H1F000234") = packHexString("00")
                iniB("&H100000A1") = packHexString("00")
                iniB("&H10000050") = packHexString("00")
                iniB("&HD000160") = packHexString("00")
                iniB("&H10000070") = packHexString("00")
                iniB("&H1A0000C3") = packHexString("00")
                iniB("&H24000030") = packHexString("00")
                iniB("&H3700008E") = packHexString("00")
                iniB("&H3000069C") = packHexString("00")
                iniB("&H1F000219") = packHexString("00")
                iniB("&H2A0000E1") = packHexString("00")
                iniB("&H28000091") = packHexString("00")
            End If
        End Sub

        Public Sub New(ByVal seed As UInteger, _
                       ByVal ref As ICallQueue, _
                       Optional ByVal logger As MultiLogger = Nothing, _
                       Optional ByVal module_folder As String = Nothing)
            load_ini_data()
            Me.ref = ContractNotNull(ref, "ref")
            Me.logger = If(logger, New MultiLogger)
            Me.original_seed = seed
            Me.module_folder = If(module_folder, GetDataFolderPath("Warden"))
            With New WardenPRNG(seed)
                Me.rc4_out = New RC4Converter(.readBlock(16))
                Me.rc4_in = New RC4Converter(.readBlock(16))
            End With
        End Sub
#End Region

#Region "Input Output"
        Public Sub ReceiveData(ByVal raw_data As Byte())
            If raw_data Is Nothing Then Throw New ArgumentNullException("raw_data")
            raw_data = raw_data.ToArray() 'clone data to avoid modifying source
            ref.enqueue(Function() eval(AddressOf _ReceiveData, raw_data))
        End Sub
        Private Sub _ReceiveData(ByVal raw_data As Byte())
            Try
                'Extract
                rc4_in.convert(raw_data, raw_data, raw_data.Length, raw_data.Length)
                Dim id = CType(raw_data(0), WardenPacketId)
                Dim data = New ImmutableArrayView(Of Byte)(raw_data, 1, raw_data.Length - 1)
                logger.log(Function() "Decrypted Warden Packet: {0}".frmt(id), LogMessageTypes.DataEvents)
                logger.log(Function() "Warden Data (after decrypt): {0}".frmt(unpackHexString(raw_data)), LogMessageTypes.RawData)

                'Parse
                Dim p = WardenPacket.FromData(id, data)
                If p.payload.getData.length <> data.length Then Throw New Pickling.PicklingException("Didn't parse entire Warden packet.")
                Dim d = CType(p.payload.getVal(), Dictionary(Of String, Object))
                logger.log(Function() p.payload.toString(), LogMessageTypes.ParsedData)
                Select Case id
                    Case WardenPacketId.LoadModule
                        Call parse_0_CheckModule(d)
                    Case WardenPacketId.DownloadModule
                        Call parse_1_DownloadModule(d)
                    Case WardenPacketId.MemCheck
                        Call parse_2_MemCheck(d)
                    Case WardenPacketId.RunModule
                        Call parse_5_ExecuteModule(d)
                    Case Else
                        Throw New Pickling.PicklingException("Unrecognized warden packet ID.")
                End Select
            Catch e As Exception
                RaiseEvent Fail(e)
            End Try
        End Sub

        Private Sub SendData(ByVal data() As Byte)
            If data Is Nothing Then Throw New ArgumentNullException()
            logger.log(Function() "Warden Response (before encrypt): {0}".frmt(unpackHexString(data)), LogMessageTypes.RawData)
            Dim data_out = data.ToArray
            rc4_out.convert(data_out, data_out, data_out.Length, data_out.Length)
            RaiseEvent Send(data_out)
        End Sub
        Private Sub SendSuccess()
            Call SendData(New Byte() {1})
        End Sub
        Private Sub SendFailure()
            Call SendData(New Byte() {0})
        End Sub
#End Region

#Region "Parse"
        Private Sub parse_0_CheckModule(ByVal vals As Dictionary(Of String, Object))
            Dim rc4_seed = CType(vals("module rc4 seed"), Byte())
            Dim module_id = CType(vals("module id"), Byte())
            Dim dl_size = CUInt(vals("dl size"))
            cur_module = New ModuleMetaData(dl_size, module_folder & unpackHexString(module_id, , "") & ".warden")

            If Not IO.File.Exists(cur_module.path) Then
                If cur_module.dl_size < 50 Or cur_module.dl_size > 5000000 Then Throw New IO.IOException("Unrealistic module size.")
                cur_module.dl_stream = New RC4Converter(rc4_seed).streamThroughTo( _
                                            New IO.BufferedStream( _
                                            New IO.FileStream(cur_module.path & ".dl", IO.FileMode.OpenOrCreate, IO.FileAccess.Write, IO.FileShare.None)))
                state = states.downloading
                SendFailure()
            Else
                If Not module_handler.LoadModule(cur_module.path) Then
                    Throw New OperationFailedException("Unable to load module.")
                End If
                state = states.module_loaded
                SendSuccess()
            End If
        End Sub
        Private Sub parse_1_DownloadModule(ByVal vals As Dictionary(Of String, Object))
            If state <> states.downloading Then Throw New InvalidOperationException("Incorrect state for downloading a module.")
            Dim dl_data = CType(vals("dl data"), Byte())
            cur_module.dl_stream.Write(dl_data, 0, dl_data.Length)
            cur_module.dl_pos += CUInt(dl_data.Length)

            If cur_module.dl_pos >= cur_module.dl_size Then
                state = states.module_loaded
                cur_module.dl_stream.Close()
                cur_module.dl_stream = Nothing
                Using rs As New IO.BufferedStream( _
                                New IO.FileStream(cur_module.path & ".dl", IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read))

                    Dim br = New IO.BinaryReader(rs)
                    Dim decompressed_size = br.ReadUInt32()
                    If decompressed_size < &H120 Or decompressed_size > 5000000 Then Throw New IO.IOException("Unrealistic module size.")
                    rs.ReadByte()
                    rs.ReadByte()
                    Using ds As New IO.Compression.DeflateStream(rs, IO.Compression.CompressionMode.Decompress)
                        Using f As New IO.BufferedStream( _
                                    New IO.FileStream(cur_module.path, IO.FileMode.Create, IO.FileAccess.Write, IO.FileShare.None))
                            For j = 0 To decompressed_size - 1
                                f.WriteByte(CByte(ds.ReadByte()))
                            Next j
                        End Using
                    End Using
                End Using
                IO.File.Delete(cur_module.path & ".dl")

                If Not module_handler.LoadModule(cur_module.path) Then
                    Throw New OperationFailedException("Unable to load module.")
                End If
                state = states.module_loaded
                SendSuccess()
            End If
        End Sub
        Private Sub parse_2_MemCheck(ByVal vals As Dictionary(Of String, Object))
            Dim payload As ImmutableArrayView(Of Byte) = CType(vals("unknown0"), Byte())
            If state <> states.module_loaded Then Throw New InvalidOperationException("Incorrect state for MemCheck")
            If payload.length <= 0 Then Throw New IO.IOException("No data.")

            'Parse list of filenames
            Dim filenames As New List(Of String)
            filenames.Add("")
            While payload.length > 0
                Dim str_len = payload(0)
                payload = payload.SubView(1)
                If str_len <= 0 Then Exit While
                filenames.Add(payload.SubView(0, str_len).toChrString())
                payload = payload.SubView(str_len)
            End While

            'Iteratively process list of commands by building up knowledge
            Dim out() As Byte = Nothing
            Dim x_iniA = -1
            Dim x_iniB = -1
            Dim x_end = payload(payload.length - 1)
            Const MAX_TRIES As Integer = 4
            For tries = 1 To MAX_TRIES
                Dim data = payload
                out = New Byte() {}

                Try
                    'Process list of commands
                    Do
                        If data.length <= 0 Then Throw New IO.IOException("Ran out of data.")
                        Dim x As Integer = data(0)
                        data = data.SubView(1)
                        Select Case x
                            Case x_iniA
                                If data(0) >= filenames.Count Then Throw New IO.IOException("Invalid filename index.")
                                Dim key = filenames(data(0)) & "&H" & Hex(data.SubView(1, 4).ToUInteger()) & "_" & data(5)
                                If Not iniA.ContainsKey(key) Then Throw New IO.IOException("Unrecognized MemCheck A: {0}.".frmt(key))
                                out = concat(out, _
                                             New Byte() {0}, _
                                             iniA(key))
                                data = data.SubView(6)

                            Case x_iniB
                                Dim key = "&H" & Hex(data.SubView(25, 4).ToUInteger())
                                If Not iniB.ContainsKey(key) Then Throw New IO.IOException("Unrecognized MemCheck B: {0}.".frmt(key))
                                out = concat(out, _
                                             New Byte() {0})
                                data = data.SubView(29)

                            Case x_end
                                If data.length > 0 Then Throw New IO.IOException("Leftover data.")
                                Exit Do

                            Case Else
                                If data.length = 0 Then
                                    Throw New IO.IOException("Empty MemCheck chunk.")
                                ElseIf x_iniA = -1 AndAlso data(0) < filenames.Count Then
                                    x_iniA = x
                                ElseIf x_iniB = -1 Then
                                    x_iniB = x
                                ElseIf x_iniA = -1 Then
                                    x_iniA = x
                                Else
                                    Throw New IO.IOException("Unrecognized MemCheck chunk type.")
                                End If
                                Continue For
                        End Select
                    Loop

                    'Respond
                    Dim bHeader(0 To 2) As Byte
                    bHeader(0) = &H2
                    bHeader(1) = CByte(out.Length Mod 256)
                    bHeader(2) = CByte(out.Length \ 256)
                    Call SendData(concat(bHeader, WardenChecksum(out), out))
                    Exit For

                Catch e As Exception
                    If tries >= MAX_TRIES Then Throw
                    Swap(x_iniA, x_iniB)
                End Try
            Next tries
        End Sub
        Private Sub parse_5_ExecuteModule(ByVal vals As Dictionary(Of String, Object))
            'Get cypher state from warden
            Dim out1 = module_handler.ExecuteModule1(uCInt(original_seed))
            If out1 Is Nothing Then
                Throw New OperationFailedException("Failed to execute module step 1.")
            End If

            'Decrypt module input
            Dim module_input_data = concat(New Byte() {WardenPacketId.RunModule}, CType(vals("module input data"), Byte()))
            Call New RC4Converter(out1.rc4_in).convert(module_input_data, module_input_data, module_input_data.Length, module_input_data.Length)
            logger.log(Function() "Warden Module Input (after decrypt): {0}".frmt(unpackHexString(module_input_data)), LogMessageTypes.RawData)

            'Pass control to warden
            Dim out2 = module_handler.ExecuteModule2((module_input_data))
            If out2 Is Nothing Then
                Throw New OperationFailedException("Failed to execute module step 2.")
            ElseIf out2.packet_length = 0 Then
                logger.log("Warden response contained no data. Probably means bnet will force disc withinin 2 minutes.", LogMessageTypes.Problem)
            End If
            Dim packet_data(0 To out2.packet_length - 1) As Byte
            For i = 0 To packet_data.Length - 1
                packet_data(i) = out2.packet_data(i)
            Next i

            'Encrypt module output
            logger.log(Function() "Warden Module Output (before encrypt): {0}".frmt(unpackHexString(packet_data)), LogMessageTypes.RawData)
            Call New RC4Converter(out1.rc4_out).convert(packet_data, packet_data, packet_data.Length, packet_data.Length)
            Call rc4_out.convert(packet_data, packet_data, packet_data.Length, packet_data.Length)

            'Update cypher state
            rc4_in = New RC4Converter(out2.rc4_in)
            rc4_out = New RC4Converter(out2.rc4_out)

            RaiseEvent Send(packet_data)
        End Sub
#End Region

#Region "Misc"
        Private Shared Function WardenChecksum(ByVal data() As Byte) As Byte()
            Dim hash = BSha1Processor.process(New IO.MemoryStream(data))
            Dim out(0 To 3) As Byte
            For i = 0 To 19
                Dim j = i Mod out.Length
                out(j) = out(j) Xor hash(i)
            Next i
            Return out
        End Function
#End Region
    End Class
End Namespace