Namespace WC3
    Public NotInheritable Class MapDownload
        Inherits DisposableWithTask
        Public file As IO.Stream
        Private destinationPath As String
        Private downloadPath As String
        Public ReadOnly size As UInteger
        Private ReadOnly fileChecksumCRC32 As UInt32
        Private ReadOnly mapChecksumXORO As UInt32
        Private ReadOnly mapChecksumSHA1 As IRist(Of Byte)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(downloadPath IsNot Nothing)
            Contract.Invariant(destinationPath IsNot Nothing)
            Contract.Invariant(mapChecksumSHA1 IsNot Nothing)
            Contract.Invariant(mapChecksumSHA1.Count = 20)
            Contract.Invariant(size > 0)
            Contract.Invariant(downloadPath.Length > 0)
            Contract.Invariant(destinationPath.Length > 0)
        End Sub

        <ContractVerification(False)>
        Public Sub New(ByVal path As String,
                       ByVal size As UInteger,
                       ByVal fileChecksumCRC32 As UInt32,
                       ByVal mapChecksumXORO As UInt32,
                       ByVal mapChecksumSHA1 As IRist(Of Byte))
            Contract.Requires(path IsNot Nothing)
            Contract.Requires(size > 0)
            Contract.Requires(mapChecksumSHA1 IsNot Nothing)
            Contract.Requires(mapChecksumSHA1.Count = 20)

            If Not IO.Directory.Exists(IO.Path.Combine(My.Settings.mapPath, Application.ProductName)) Then
                IO.Directory.CreateDirectory(IO.Path.Combine(My.Settings.mapPath, Application.ProductName))
            End If
            Dim filename = path.Split("\"c).Last
            Dim filenameWithoutExtension = IO.Path.GetFileNameWithoutExtension(filename)
            Dim fileExtension = IO.Path.GetExtension(filename)
            Dim n = 1
            Do
                Me.destinationPath = IO.Path.Combine(My.Settings.mapPath,
                                                     Application.ProductName,
                                                     filenameWithoutExtension + If(n = 1, "", " {0}".Frmt(n)) + fileExtension)
                Me.downloadPath = Me.destinationPath + ".dl"
                n += 1
            Loop While IO.File.Exists(Me.destinationPath) Or IO.File.Exists(Me.downloadPath)
            Contract.Assume(Me.destinationPath IsNot Nothing)
            Contract.Assume(Me.destinationPath.Length > 0)
            Me.size = size
            Me.fileChecksumCRC32 = fileChecksumCRC32
            Me.mapChecksumXORO = mapChecksumXORO
            Me.mapChecksumSHA1 = mapChecksumSHA1
            Me.file = New IO.FileStream(Me.downloadPath, IO.FileMode.OpenOrCreate, IO.FileAccess.Write, IO.FileShare.None)
        End Sub

        <ContractVerification(False)>
        Public Function ReceiveChunk(ByVal pos As Integer,
                                     ByVal data As IRist(Of Byte)) As Boolean
            Contract.Requires(pos >= 0)
            Contract.Requires(data IsNot Nothing)
            If file Is Nothing Then Throw New InvalidOperationException("File is closed.")
            If pos <> file.Position Then Return False
            If file.Position + data.Count > size Then Throw New IO.InvalidDataException("Data runs past end of file.")
            file.Write(data.ToArray, 0, data.Count)

            If file.Position = size Then
                'Finished Download
                file.Close()
                file = Nothing
                Dim map = WC3.Map.FromFile(downloadPath, My.Settings.mapPath, My.Settings.war3path)
                If Not map.MapChecksumSHA1.SequenceEqual(mapChecksumSHA1) Then Throw New IO.InvalidDataException("Completed map doesn't match reported SHA1 checksum.")
                If map.MapChecksumXORO <> mapChecksumXORO Then Throw New IO.InvalidDataException("Completed map doesn't match reported XORO checksum.")
                If map.FileChecksumCRC32 <> fileChecksumCRC32 Then Throw New IO.InvalidDataException("Completed map doesn't match reported CRC32 checksum.")
                IO.File.Move(downloadPath, destinationPath)
                Return True
            End If

            Return False
        End Function

        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As Task
            If finalizing Then Return Nothing
            If file IsNot Nothing Then file.Dispose()
            Return Nothing
        End Function
    End Class
End Namespace
