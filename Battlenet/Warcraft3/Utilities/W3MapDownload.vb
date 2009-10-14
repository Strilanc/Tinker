Imports HostBot.Warcraft3.W3PacketId
Imports HostBot.Warcraft3

Namespace Warcraft3
    Public NotInheritable Class W3MapDownload
        Public file As IO.Stream
        Private destinationPath As String
        Private downloadPath As String
        Public ReadOnly size As UInteger
        Private ReadOnly fileChecksumCRC32 As ViewableList(Of Byte)
        Private ReadOnly contentChecksumXORO As ViewableList(Of Byte)
        Private ReadOnly contentChecksumSHA1 As ViewableList(Of Byte)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(downloadPath IsNot Nothing)
            Contract.Invariant(destinationPath IsNot Nothing)
            Contract.Invariant(fileChecksumCRC32 IsNot Nothing)
            Contract.Invariant(contentChecksumXORO IsNot Nothing)
            Contract.Invariant(contentChecksumSHA1 IsNot Nothing)
            Contract.Invariant(fileChecksumCRC32.Length = 4)
            Contract.Invariant(contentChecksumXORO.Length = 4)
            Contract.Invariant(contentChecksumSHA1.Length = 20)
            Contract.Invariant(size > 0)
        End Sub

        Public Sub New(ByVal path As String,
                       ByVal size As UInteger,
                       ByVal fileChecksumCRC32 As ViewableList(Of Byte),
                       ByVal contentChecksumXORO As ViewableList(Of Byte),
                       ByVal contentChecksumSHA1 As ViewableList(Of Byte))
            Contract.Requires(path IsNot Nothing)
            Contract.Requires(size > 0)
            Contract.Requires(fileChecksumCRC32 IsNot Nothing)
            Contract.Requires(contentChecksumXORO IsNot Nothing)
            Contract.Requires(contentChecksumSHA1 IsNot Nothing)
            Contract.Requires(fileChecksumCRC32.Length = 4)
            Contract.Requires(contentChecksumXORO.Length = 4)
            Contract.Requires(contentChecksumSHA1.Length = 20)

            If Not IO.Directory.Exists(My.Settings.mapPath + "HostBot") Then
                IO.Directory.CreateDirectory(My.Settings.mapPath + "HostBot")
            End If
            Dim filename = path.Split("\"c).Last
            Dim filenameWithoutExtension = IO.Path.GetFileNameWithoutExtension(filename)
            Dim fileExtension = IO.Path.GetExtension(filename)
            Dim n = 1
            Do
                Me.destinationPath = "{0}{1}{2}{3}{4}{5}".Frmt(My.Settings.mapPath,
                                                               "HostBot",
                                                               IO.Path.DirectorySeparatorChar,
                                                               filenameWithoutExtension,
                                                               If(n = 1, "", " " + n.ToString(CultureInfo.InvariantCulture)),
                                                               fileExtension)
                Me.downloadPath = Me.destinationPath + ".dl"
                n += 1
            Loop While IO.File.Exists(Me.destinationPath) Or IO.File.Exists(Me.downloadPath)
            Me.size = size
            Me.fileChecksumCRC32 = fileChecksumCRC32
            Me.contentChecksumXORO = contentChecksumXORO
            Me.contentChecksumSHA1 = contentChecksumSHA1
            Me.file = New IO.FileStream(Me.downloadPath, IO.FileMode.OpenOrCreate, IO.FileAccess.Write, IO.FileShare.None)
        End Sub

        Public Function ReceiveChunk(ByVal pos As Integer,
                                     ByVal data() As Byte) As Boolean
            Contract.Requires(pos >= 0)
            Contract.Requires(data IsNot Nothing)
            If file Is Nothing Then Throw New InvalidOperationException("File is closed.")
            If pos <> file.Position Then Return False
            If file.Position + data.Length > size Then Throw New IO.IOException("Invalid data.")
            file.Write(data, 0, data.Length)

            If file.Position = size Then
                'Finished Download
                file.Close()
                file = Nothing
                Dim map As New W3Map(My.Settings.mapPath, downloadPath.Substring(My.Settings.mapPath.Length), My.Settings.war3path)
                If Not map.ChecksumSHA1.HasSameItemsAs(contentChecksumSHA1) Then Throw New IO.IOException("Invalid data.")
                If Not map.ChecksumXORO.HasSameItemsAs(contentChecksumXORO) Then Throw New IO.IOException("Invalid data.")
                If Not map.ChecksumCRC32.HasSameItemsAs(fileChecksumCRC32) Then Throw New IO.IOException("Invalid data.")
                IO.File.Move(downloadPath, destinationPath)
                Return True
            End If

            Return False
        End Function
    End Class
End Namespace