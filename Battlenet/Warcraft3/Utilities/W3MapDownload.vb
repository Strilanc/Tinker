Imports HostBot.Warcraft3.W3PacketId
Imports HostBot.Warcraft3

Namespace Warcraft3
    Public Class W3MapDownload
        Public file As IO.Stream
        Private destinationPath As String
        Private downloadPath As String
        Public ReadOnly size As UInteger
        Private crc() As Byte
        Private xoro() As Byte
        Private sha1() As Byte

        <ContractInvariantMethod()> Protected Sub Invariant()
            Contract.Invariant(downloadPath IsNot Nothing)
            Contract.Invariant(destinationPath IsNot Nothing)
            Contract.Invariant(crc IsNot Nothing)
            Contract.Invariant(xoro IsNot Nothing)
            Contract.Invariant(sha1 IsNot Nothing)
            Contract.Invariant(crc.Length = 4)
            Contract.Invariant(xoro.Length = 4)
            Contract.Invariant(sha1.Length = 20)
            Contract.Invariant(size > 0)
        End Sub

        Public Sub New(ByVal path As String,
                       ByVal size As UInteger,
                       ByVal crc() As Byte,
                       ByVal xoro As Byte(),
                       ByVal sha1 As Byte())
            Contract.Requires(path IsNot Nothing)
            Contract.Requires(size > 0)
            Contract.Requires(crc IsNot Nothing)
            Contract.Requires(xoro IsNot Nothing)
            Contract.Requires(sha1 IsNot Nothing)
            Contract.Requires(crc.Length = 4)
            Contract.Requires(xoro.Length = 4)
            Contract.Requires(sha1.Length = 20)

            If Not IO.Directory.Exists(My.Settings.mapPath + "HostBot") Then
                IO.Directory.CreateDirectory(My.Settings.mapPath + "HostBot")
            End If
            Dim filename = GetFileNameSlash(path)
            Dim filenameWithoutExtension = IO.Path.GetFileNameWithoutExtension(filename)
            Dim fileExtension = IO.Path.GetExtension(filename)
            Dim n = 1
            Do
                Me.destinationPath = "{0}{1}{2}{3}{4}{5}".frmt(My.Settings.mapPath,
                                                               "HostBot",
                                                               IO.Path.DirectorySeparatorChar,
                                                               filenameWithoutExtension,
                                                               If(n = 1, "", " " + n.ToString),
                                                               fileExtension)
                Me.downloadPath = Me.destinationPath + ".dl"
                n += 1
            Loop While IO.File.Exists(Me.destinationPath) Or IO.File.Exists(Me.downloadPath)
            Me.size = size
            Me.crc = crc
            Me.xoro = xoro
            Me.sha1 = sha1
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
                If Not ArraysEqual(map.ChecksumSha1, sha1) Then Throw New IO.IOException("Invalid data.")
                If Not ArraysEqual(map.ChecksumXoro, xoro) Then Throw New IO.IOException("Invalid data.")
                If Not ArraysEqual(map.crc32, crc) Then Throw New IO.IOException("Invalid data.")
                IO.File.Move(downloadPath, destinationPath)
                Return True
            End If

            Return False
        End Function
    End Class
End Namespace