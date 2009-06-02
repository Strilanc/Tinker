Namespace Warcraft3.Warden
    Public Class WardenPacketHandler
        Private ReadOnly original_seed As UInteger
        Private ReadOnly module_folder As String
        Private ReadOnly module_handler As New Warden_Module_Lib.ModuleHandler
        Private ReadOnly logger As Logger
        Private ReadOnly ref As ICallQueue
        Private ReadOnly eref As ICallQueue = New ThreadPooledCallQueue
        Private ReadOnly memCheckCache As New Dictionary(Of String, Byte())

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
        Public Sub New(ByVal seed As UInteger,
                       ByVal ref As ICallQueue,
                       Optional ByVal logger As Logger = Nothing,
                       Optional ByVal module_folder As String = Nothing)
            Me.ref = ContractNotNull(ref, "ref")
            Me.logger = If(logger, New Logger)
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
            ref.QueueAction(
                Sub()
                    Try
                        'Extract
                        rc4_in.convert(raw_data, raw_data, raw_data.Length, raw_data.Length)
                        Dim id = CType(raw_data(0), WardenPacketId)
                        Dim data = New ImmutableArrayView(Of Byte)(raw_data, 1, raw_data.Length - 1)
                        logger.log(Function() "Decrypted Warden Packet: {0}".frmt(id), LogMessageTypes.DataEvent)
                        logger.log(Function() "Warden Data (after decrypt): {0}".frmt(unpackHexString(raw_data)), LogMessageTypes.DataRaw)

                        'Parse
                        Dim p = WardenPacket.FromData(id, data)
                        If p.payload.getData.length <> data.length Then  Throw New Pickling.PicklingException("Didn't parse entire Warden packet.")
                        Dim d = CType(p.payload.getVal(), Dictionary(Of String, Object))
                        logger.log(Function() p.payload.toString(), LogMessageTypes.DataParsed)
                        Select Case id
                            Case WardenPacketId.LoadModule
                                Call ReceiveCheckModule(d)
                            Case WardenPacketId.DownloadModule
                                Call ReceiveDownloadModule(d)
                            Case WardenPacketId.PerformCheck
                                Call ReceivePerformCheck(d)
                            Case WardenPacketId.RunModule
                                Call ReceiveExecuteModule(d)
                            Case Else
                                Throw New Pickling.PicklingException("Unrecognized warden packet ID.")
                        End Select
                    Catch e As Exception
                        RaiseEvent Fail(e)
                    End Try
                End Sub
            )
        End Sub

        Private Sub SendData(ByVal data() As Byte)
            If data Is Nothing Then Throw New ArgumentNullException()
            logger.log(Function() "Warden Response (before encrypt): {0}".frmt(unpackHexString(data)), LogMessageTypes.DataRaw)
            Dim data_out = data.ToArray
            rc4_out.convert(data_out, data_out, data_out.Length, data_out.Length)
            eref.QueueAction(
                Sub()
                    RaiseEvent Send(data_out)
                End Sub
            )
        End Sub
        Private Sub SendSuccess()
            Call SendData(New Byte() {1})
        End Sub
        Private Sub SendFailure()
            Call SendData(New Byte() {0})
        End Sub
#End Region

#Region "Parse"
        Private Sub ReceiveCheckModule(ByVal vals As Dictionary(Of String, Object))
            Dim rc4_seed = CType(vals("module rc4 seed"), Byte())
            Dim module_id = CType(vals("module id"), Byte())
            Dim dl_size = CUInt(vals("dl size"))
            cur_module = New ModuleMetaData(dl_size, module_folder & unpackHexString(module_id, separator:="") & ".warden")

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
        Private Sub ReceiveDownloadModule(ByVal vals As Dictionary(Of String, Object))
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
        Private Sub ReceiveExecuteModule(ByVal vals As Dictionary(Of String, Object))
            'Get cypher state from warden
            Dim out1 = module_handler.ExecuteModule1(uCInt(original_seed))
            If out1 Is Nothing Then
                Throw New OperationFailedException("Failed to execute module step 1.")
            End If

            'Decrypt module input
            Dim module_input_data = concat({WardenPacketId.RunModule}, CType(vals("module input data"), Byte()))
            Call New RC4Converter(out1.rc4_in).convert(module_input_data, module_input_data, module_input_data.Length, module_input_data.Length)
            logger.log(Function() "Warden Module Input (after decrypt): {0}".frmt(unpackHexString(module_input_data)), LogMessageTypes.DataRaw)

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
            logger.log(Function() "Warden Module Output (before encrypt): {0}".frmt(unpackHexString(packet_data)), LogMessageTypes.DataRaw)
            Call New RC4Converter(out1.rc4_out).convert(packet_data, packet_data, packet_data.Length, packet_data.Length)
            Call rc4_out.convert(packet_data, packet_data, packet_data.Length, packet_data.Length)

            'Update cypher state
            rc4_in = New RC4Converter(out2.rc4_in)
            rc4_out = New RC4Converter(out2.rc4_out)

            RaiseEvent Send(packet_data)
        End Sub

        Private Sub ReceivePerformCheck(ByVal vals As Dictionary(Of String, Object))
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

            'Either the first id byte is a MemCheck id or a PageCheck id. Try both.
            Dim out() As Byte = Nothing
            For tries = 1 To 2
                Try
                    Dim data = payload
                    out = New Byte() {}

                    'Assign IDs
                    Dim id_End = data(data.length - 1)
                    Dim id_MemCheck = -1
                    Dim id_PageCheck = -1
                    If tries = 1 Then
                        id_MemCheck = payload(0)
                    Else
                        id_PageCheck = payload(0)
                    End If

                    'Process commands
                    Do
                        Dim n = ProcessCheckCommand(data, filenames, id_MemCheck, id_PageCheck, id_End, out)
                        If n = 0 Then Exit Do
                        data = data.SubView(n)
                    Loop

                    Exit For
                Catch e As Exception
                    If tries >= 2 Then Throw
                End Try
            Next tries

            'Respond
            Call SendData(concat({WardenPacketId.PerformCheck},
                                 CUShort(out.Length).bytes(ByteOrder.LittleEndian),
                                 WardenChecksum(out),
                                 out))
        End Sub
        Private Function ProcessCheckCommand(ByVal data As ImmutableArrayView(Of Byte),
                                             ByVal filenames As IList(Of String),
                                             ByRef id_MemCheck As Integer,
                                             ByRef id_PageCheck As Integer,
                                             ByVal id_end As Integer,
                                             ByRef out As Byte()) As Integer
            If data.length <= 0 Then Throw New IO.IOException("Ran out of data.")
            Dim id As Integer = data(0)
            data = data.SubView(1)

            'Assign new IDs
            Select Case id
                Case id_MemCheck, id_PageCheck, id_end
                Case Else
                    If id_MemCheck = -1 Then
                        id_MemCheck = id
                    ElseIf id_PageCheck = -1 Then
                        id_PageCheck = id
                    End If
            End Select

            'Process command
            Select Case id
                Case id_MemCheck
                    Return PerformMemoryCheck(data, filenames, out)

                Case id_PageCheck
                    If data.length < 29 Then Throw New IO.IOException("Not enough pagecheck data.")
                    out = concat(out, {0})
                    Return 30

                Case id_end
                    If data.length > 0 Then Throw New IO.IOException("Leftover data.")
                    Return 0

                Case Else
                    Throw New IO.IOException("Unrecognized check id.")
            End Select
        End Function
        Private Function PerformMemoryCheck(ByVal data As ImmutableArrayView(Of Byte),
                                            ByVal filenames As IList(Of String),
                                            ByRef out As Byte()) As Integer
            If data.length < 6 Then Throw New IO.IOException("Not enough memcheck data.")
            If data(0) > filenames.Count Then Throw New IO.IOException("Invalid memcheck file index: {0}.".frmt(data(0)))

            Dim filename = filenames(data(0))
            Dim position = data.SubView(1, 4).ToUInteger(ByteOrder.LittleEndian)
            Dim length = data(5)
            Dim key = "{0}:{1}".frmt(position, length)
            If memCheckCache.Count > 100 Then
                logger.log("Warning: The number of unique warden checks has become quite large (>100), which is not expected behavior.", LogMessageTypes.Negative)
                memCheckCache.Clear()
            End If
            If Not memCheckCache.ContainsKey(key) Then
                If Not IO.File.Exists(My.Settings.war3path + filename) Then Throw New IO.IOException("Memcheck file '{0}' doesn't exist.".frmt(filename))
                Using f As New IO.FileStream(My.Settings.war3path + filename, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.ReadWrite)
                    If position + length >= f.Length Then Throw New IO.IOException("Memcheck runs past end of file.")
                    f.Seek(position, IO.SeekOrigin.Begin)
                    Using br As New IO.BinaryReader(f)
                        memCheckCache(key) = br.ReadBytes(length)
                    End Using
                End Using
            End If
            out = concat(out, {0}, memCheckCache(key))

            Return 7
        End Function
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