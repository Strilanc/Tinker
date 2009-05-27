Imports HostBot.Warcraft3.W3PacketId
Imports HostBot.Warcraft3

Namespace Warcraft3
    Public Class W3MapDownload
        Public file As IO.Stream
        Private filepath As String
        Private temp_filepath As String
        Public ReadOnly size As UInteger
        Private crc() As Byte
        Private xoro() As Byte
        Private sha1() As Byte

        Public Sub New(ByVal path As String, ByVal size As UInteger, ByVal crc() As Byte, ByVal xoro As Byte(), ByVal sha1 As Byte())
            If Not IO.Directory.Exists(My.Settings.mapPath + "HostBot") Then
                IO.Directory.CreateDirectory(My.Settings.mapPath + "HostBot")
            End If
            Dim filename = getFileNameSlash(path)
            Dim filename_without_extension = IO.Path.GetFileNameWithoutExtension(filename)
            Dim file_extension = IO.Path.GetExtension(filename)
            Dim n = 1
            Do
                Me.filepath = "{0}{1}{2}{3}{4}{5}".frmt(My.Settings.mapPath,
                                                        "HostBot",
                                                        IO.Path.DirectorySeparatorChar,
                                                        filename_without_extension,
                                                        If(n = 1, "", " " + n.ToString),
                                                        file_extension)
                Me.temp_filepath = Me.filepath + ".dl"
                n += 1
            Loop While IO.File.Exists(Me.filepath) Or IO.File.Exists(Me.temp_filepath)
            Me.size = size
            Me.crc = crc
            Me.xoro = xoro
            Me.sha1 = sha1
            Me.file = New IO.FileStream(Me.temp_filepath, IO.FileMode.OpenOrCreate, IO.FileAccess.Write, IO.FileShare.None)
        End Sub

        Public Function receive_chunk(ByVal pos As Integer, ByVal data() As Byte) As Boolean
            If file Is Nothing Then Throw New InvalidOperationException("File is closed.")
            If pos <> file.Position Then Return False
            If file.Position + data.Length > size Then Throw New IO.IOException("Invalid data.")
            file.Write(data, 0, data.Length)

            If file.Position = size Then
                'Finished Download
                file.Close()
                file = Nothing
                Dim map As New W3Map(My.Settings.mapPath, temp_filepath.Substring(My.Settings.mapPath.Length), My.Settings.war3path)
                If Not ArraysEqual(map.checksum_sha1, sha1) Then Throw New IO.IOException("Invalid data.")
                If Not ArraysEqual(map.checksum_xoro, xoro) Then Throw New IO.IOException("Invalid data.")
                If Not ArraysEqual(map.crc32, crc) Then Throw New IO.IOException("Invalid data.")
                IO.File.Move(temp_filepath, filepath)
                Return True
            End If

            Return False
        End Function
    End Class
End Namespace