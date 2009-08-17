Namespace Warcraft3.Warden
    Public Class WardenPacketHandler
        Private ReadOnly originalSeed As ModInt32
        Private ReadOnly moduleFolder As String
        Private ReadOnly moduleHandler As New Warden_Module_Lib.ModuleHandler
        Private ReadOnly logger As Logger
        Private ReadOnly ref As ICallQueue
        Private ReadOnly eref As ICallQueue = New ThreadPooledCallQueue
        Private ReadOnly memCheckCache As New Dictionary(Of String, Byte())

        Private moduleState As ModuleStates = ModuleStates.None
        Private curModule As ModuleMetaData
        Private rc4OutXor As IEnumerator(Of Byte)
        Private rc4InXor As IEnumerator(Of Byte)

        Public Event Send(ByVal data As Byte())
        Public Event Fail(ByVal e As Exception)

        Private Enum ModuleStates As Byte
            None
            Downloading
            Loaded
        End Enum

        Private Class ModuleMetaData
            Public ReadOnly path As String
            Public ReadOnly dlSize As UInteger
            Public dlPosition As UInteger
            Public dlStream As IO.Stream
            Public Sub New(ByVal size As UInteger, ByVal path As String)
                Me.dlSize = size
                Me.path = path
            End Sub
        End Class

        Public Sub New(ByVal seed As ModInt32,
                       ByVal ref As ICallQueue,
                       Optional ByVal logger As Logger = Nothing,
                       Optional ByVal moduleFolder As String = Nothing)
            'contract bug wrt interface event implementation requires this:
            'Contract.Requires(ref IsNot Nothing)
            Contract.Assume(ref IsNot Nothing)
            Me.ref = ref
            Me.logger = If(logger, New Logger)
            Me.originalSeed = seed
            Me.moduleFolder = If(moduleFolder, GetDataFolderPath("Warden"))
            With New WardenPseudoRandomNumberStream(seed)
                Me.rc4OutXor = RC4Converter.XorSequence(.ReadBytes(16))
                Me.rc4InXor = RC4Converter.XorSequence(.ReadBytes(16))
            End With
        End Sub

#Region "Input Output"
        Private Sub CypherBlock(ByVal xs As IEnumerator(Of Byte), ByVal data() As Byte)
            For i = 0 To data.Length - 1
                data(i) = data(i) Xor xs.MoveNextAndReturn()
            Next i
        End Sub
        Public Sub ReceiveData(ByVal raw_data As Byte())
            If raw_data Is Nothing Then Throw New ArgumentNullException("raw_data")
            raw_data = raw_data.ToArray() 'clone data to avoid modifying source
            ref.QueueAction(
                Sub()
                    Try
                        'Extract
                        CypherBlock(rc4InXor, raw_data)
                        Dim id = CType(raw_data(0), WardenPacketId)
                        Dim data = raw_data.ToView().SubView(1, raw_data.Length - 1)
                        logger.log(Function() "Decrypted Warden Packet: {0}".frmt(id), LogMessageTypes.DataEvent)
                        logger.log(Function() "Warden Data (after decrypt): {0}".frmt(raw_data.ToHexString), LogMessageTypes.DataRaw)

                        'Parse
                        Dim p = WardenPacket.FromData(id, data)
                        If p.payload.Data.Length <> data.Length Then  Throw New Pickling.PicklingException("Didn't parse entire Warden packet.")
                        Dim d = CType(p.payload.Value, Dictionary(Of String, Object))
                        logger.log(p.payload.Description, LogMessageTypes.DataParsed)
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
            logger.log(Function() "Warden Response (before encrypt): {0}".frmt(data.ToHexString), LogMessageTypes.DataRaw)
            Dim data_out = data.ToArray
            CypherBlock(rc4OutXor, data_out)
            eref.QueueAction(
                Sub()
                    RaiseEvent Send(data_out)
                End Sub)
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
            Dim rc4Seed = CType(vals("module rc4 seed"), Byte())
            Dim module_id = CType(vals("module id"), Byte())
            Dim dl_size = CUInt(vals("dl size"))
            curModule = New ModuleMetaData(dl_size, moduleFolder & module_id.ToHexString(separator:="") & ".warden")

            If Not IO.File.Exists(curModule.path) Then
                If curModule.dlSize < 50 Or curModule.dlSize > 5000000 Then Throw New IO.IOException("Unrealistic module size.")
                curModule.dlStream = New RC4Converter(rc4Seed).ConvertWriteOnlyStream(
                                            New IO.BufferedStream(
                                            New IO.FileStream(curModule.path & ".dl", IO.FileMode.OpenOrCreate, IO.FileAccess.Write, IO.FileShare.None)))
                moduleState = ModuleStates.Downloading
                SendFailure()
            Else
                If Not moduleHandler.LoadModule(curModule.path) Then
                    Throw New OperationFailedException("Unable to load module.")
                End If
                moduleState = ModuleStates.Loaded
                SendSuccess()
            End If
        End Sub
        Private Sub ReceiveDownloadModule(ByVal vals As Dictionary(Of String, Object))
            If moduleState <> ModuleStates.Downloading Then Throw New InvalidOperationException("Incorrect state for downloading a module.")
            Dim dl_data = CType(vals("dl data"), Byte())
            curModule.dlStream.Write(dl_data, 0, dl_data.Length)
            curModule.dlPosition += CUInt(dl_data.Length)

            If curModule.dlPosition >= curModule.dlSize Then
                moduleState = ModuleStates.Loaded
                curModule.dlStream.Close()
                curModule.dlStream = Nothing
                Using rs As New IO.BufferedStream(New IO.FileStream(curModule.path & ".dl", IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read))
                    Dim br = New IO.BinaryReader(rs)
                    Dim decompressed_size = br.ReadInt32()
                    If decompressed_size < 288 Or decompressed_size > 5000000 Then Throw New IO.IOException("Unrealistic module size.")
                    Using ds As New ZLibStream(rs, IO.Compression.CompressionMode.Decompress)
                        Using f As New IO.BufferedStream(New IO.FileStream(curModule.path, IO.FileMode.Create, IO.FileAccess.Write, IO.FileShare.None))
                            For j = 0 To decompressed_size - 1
                                f.WriteByte(CByte(ds.ReadByte()))
                            Next j
                        End Using
                    End Using
                End Using
                IO.File.Delete(curModule.path & ".dl")

                If Not moduleHandler.LoadModule(curModule.path) Then
                    Throw New OperationFailedException("Unable to load module.")
                End If
                moduleState = ModuleStates.Loaded
                SendSuccess()
            End If
        End Sub
        Private Sub ReceiveExecuteModule(ByVal vals As Dictionary(Of String, Object))
            'Get cypher state from warden
            Dim out1 = moduleHandler.ExecuteModule1(originalSeed)
            If out1 Is Nothing Then
                Throw New OperationFailedException("Failed to execute module step 1.")
            End If

            'Decrypt module input
            Dim inputData = Concat({WardenPacketId.RunModule}, CType(vals("module input data"), Byte()))
            Call CypherBlock(RC4Converter.XorSequence(out1.rc4_in), inputData)
            logger.log(Function() "Warden Module Input (after decrypt): {0}".frmt(inputData.ToHexString), LogMessageTypes.DataRaw)

            'Pass control to warden
            Dim out2 = moduleHandler.ExecuteModule2((inputData))
            If out2 Is Nothing Then
                Throw New OperationFailedException("Failed to execute module step 2.")
            ElseIf out2.packet_length = 0 Then
                logger.log("Warden response contained no data. Probably means bnet will force disc withinin 2 minutes.", LogMessageTypes.Problem)
            End If

            'Encrypt module output
            Dim decryptedOutput = (From i In Enumerable.Range(0, out2.packet_length) Select out2.packet_data(i))
            logger.log(Function() "Warden Module Output (before encrypt): {0}".frmt(decryptedOutput.ToHexString), LogMessageTypes.DataRaw)
            Dim outputData = decryptedOutput.ToArray()
            Call CypherBlock(RC4Converter.XorSequence(out1.rc4_out), outputData)
            Call CypherBlock(rc4OutXor, outputData)

            'Update cypher state
            rc4InXor = RC4Converter.XorSequence(out2.rc4_in)
            rc4OutXor = RC4Converter.XorSequence(out2.rc4_out)

            RaiseEvent Send(outputData)
        End Sub

        Private Sub ReceivePerformCheck(ByVal vals As Dictionary(Of String, Object))
            Dim payload = CType(vals("unknown0"), Byte()).ToView()
            If moduleState <> ModuleStates.Loaded Then Throw New InvalidOperationException("Incorrect state for MemCheck")
            If payload.Length <= 0 Then Throw New IO.IOException("No data.")

            'Parse list of filenames
            Dim filenames As New List(Of String)
            filenames.Add("")
            While payload.Length > 0
                Dim str_len = payload(0)
                payload = payload.SubView(1)
                If str_len <= 0 Then Exit While
                filenames.Add(payload.SubView(0, str_len).ParseChrString(nullTerminated:=True))
                payload = payload.SubView(str_len)
            End While

            'Either the first id byte is a MemCheck id or a PageCheck id. Try both.
            Dim out() As Byte = Nothing
            For tries = 1 To 2
                Try
                    Dim data = payload
                    out = New Byte() {}

                    'Assign IDs
                    Dim id_End = data(data.Length - 1)
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
            Call SendData(Concat({WardenPacketId.PerformCheck},
                                 CUShort(out.Length).bytes(ByteOrder.LittleEndian),
                                 WardenChecksum(out),
                                 out))
        End Sub
        Private Function ProcessCheckCommand(ByVal data As ViewableList(Of Byte),
                                             ByVal filenames As IList(Of String),
                                             ByRef id_MemCheck As Integer,
                                             ByRef id_PageCheck As Integer,
                                             ByVal id_end As Integer,
                                             ByRef out As Byte()) As Integer
            If data.Length <= 0 Then Throw New IO.IOException("Ran out of data.")
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
                    If data.Length < 29 Then Throw New IO.IOException("Not enough pagecheck data.")
                    out = Concat(out, {0})
                    Return 30

                Case id_end
                    If data.Length > 0 Then Throw New IO.IOException("Leftover data.")
                    Return 0

                Case Else
                    Throw New IO.IOException("Unrecognized check id.")
            End Select
        End Function
        Private Function PerformMemoryCheck(ByVal data As ViewableList(Of Byte),
                                            ByVal filenames As IList(Of String),
                                            ByRef out As Byte()) As Integer
            If data.Length < 6 Then Throw New IO.IOException("Not enough memcheck data.")
            If data(0) > filenames.Count Then Throw New IO.IOException("Invalid memcheck file index: {0}.".frmt(data(0)))

            Dim filename = filenames(data(0))
            Dim position = data.SubView(1, 4).ToUInt32(ByteOrder.LittleEndian)
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
            out = Concat(out, {0}, memCheckCache(key))

            Return 7
        End Function
#End Region

        Private Shared Function WardenChecksum(ByVal data() As Byte) As Byte()
            Dim hash = BSha1Processor.process(New IO.MemoryStream(data))
            Dim out(0 To 3) As Byte
            For i = 0 To 19
                Dim j = i Mod out.Length
                out(j) = out(j) Xor hash(i)
            Next i
            Return out
        End Function
    End Class
End Namespace