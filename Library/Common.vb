Imports System.Net.NetworkInformation
Imports System.Runtime.CompilerServices
Imports System.IO.Path
Imports System.Text
Imports System.Net
Imports System.IO
Imports System.Numerics

'''<summary>A smattering of functions and other stuff that hasn't been placed in more reasonable groups yet.</summary>
Public Module PoorlyCategorizedFunctions
#Region "Strings Extra"
    <Extension()> <Pure()>
    Public Function Linefy(ByVal text As String) As String
        Contract.Requires(text IsNot Nothing)
        Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
        Return text.Replace("\n", Environment.NewLine)
    End Function

    <Pure()>
    Public Function BreakQuotedWords(ByVal text As String) As List(Of String)
        Contract.Requires(text IsNot Nothing)
        Contract.Ensures(Contract.Result(Of List(Of String))() IsNot Nothing)

        Dim quoted_words As New List(Of String)
        If text = "" Then Return quoted_words
        Dim cur_quoted_word As String = Nothing
        For Each word In text.Split(" "c)
            If word = "" Then Continue For
            If cur_quoted_word Is Nothing Then
                Contract.Assume(word.Length > 0)
                If word(0) = """"c Then
                    If word(word.Length - 1) = """"c Then '[start and end of quoted word]
                        Contract.Assume(word.Length >= 2)
                        quoted_words.Add(word.Substring(1, word.Length - 2))
                    Else '[start of quoted word]
                        Contract.Assume(word.Length >= 1)
                        cur_quoted_word = word.Substring(1) + " "
                    End If
                Else '[normal word]
                    quoted_words.Add(word)
                End If
            ElseIf word(word.Length - 1) = """"c Then '[end of quoted word]
                quoted_words.Add(cur_quoted_word + word.Substring(0, word.Length - 1))
                cur_quoted_word = Nothing
            Else '[middle of quoted word]
                cur_quoted_word += word + " "
            End If
        Next word
        Return quoted_words
    End Function
    <Pure()>
    Public Function BuildDictionaryFromString(Of T)(ByVal text As String,
                                                    ByVal parser As Func(Of String, T),
                                                    Optional ByVal pairDivider As String = ";"c,
                                                    Optional ByVal valueDivider As String = "="c) As Dictionary(Of String, T)
        Contract.Requires(text IsNot Nothing)
        Contract.Requires(parser IsNot Nothing)
        Contract.Requires(pairDivider IsNot Nothing)
        Contract.Requires(valueDivider IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Dictionary(Of String, T))() IsNot Nothing)
        Dim d As New Dictionary(Of String, T)
        Dim pd = New String() {pairDivider}
        Dim vd = New String() {valueDivider}
        For Each pair In text.Split(pd, StringSplitOptions.RemoveEmptyEntries)
            Dim args = pair.Split(vd, StringSplitOptions.None)
            Contract.Assume(args IsNot Nothing)
            If args.Count < 2 Then Continue For
            d(args(0)) = parser(pair.Substring(args(0).Length + 1))
        Next pair
        Return d
    End Function
#End Region

#Region "Filepaths"
    Public Function FindFileMatching(ByVal fileQuery As String, ByVal likeQuery As String, ByVal directory As String) As String
        Contract.Requires(fileQuery IsNot Nothing)
        Contract.Requires(likeQuery IsNot Nothing)
        Contract.Requires(directory IsNot Nothing)
        Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
        Dim result = FindFilesMatching(fileQuery, likeQuery, directory, 1).FirstOrDefault
        If result Is Nothing Then Throw New OperationFailedException("No matches.")
        Return result
    End Function

    Public Function FindFilesMatching(ByVal fileQuery As String,
                                      ByVal likeQuery As String,
                                      ByVal directory As String,
                                      ByVal maxResults As Integer) As IList(Of String)
        Contract.Requires(fileQuery IsNot Nothing)
        Contract.Requires(likeQuery IsNot Nothing)
        Contract.Requires(directory IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IList(Of String))() IsNot Nothing)
        Dim matches As New List(Of String)

        'Normalize input
        directory = directory.Replace(AltDirectorySeparatorChar, DirectorySeparatorChar)
        Contract.Assume(directory.Length > 0)
        If directory(directory.Length - 1) <> DirectorySeparatorChar Then
            directory += DirectorySeparatorChar
        End If

        'Separate directory and filename patterns
        fileQuery = fileQuery.Replace(AltDirectorySeparatorChar, DirectorySeparatorChar)
        Dim dirQuery = "*"
        If fileQuery.Contains(DirectorySeparatorChar) Then
            Dim words = fileQuery.Split(DirectorySeparatorChar)
            Dim file_pattern = words(words.Length - 1)
            Contract.Assume(fileQuery.Length > file_pattern.Length)
            dirQuery = fileQuery.Substring(0, fileQuery.Length - file_pattern.Length) + "*"
            fileQuery = "*" + file_pattern
        End If

        'patterns are not case-sensitive
        dirQuery = dirQuery.ToUpperInvariant
        likeQuery = likeQuery.ToUpperInvariant

        'Check files in folder
        For Each filename In IO.Directory.GetFiles(directory, fileQuery, IO.SearchOption.AllDirectories)
            Contract.Assume(filename.Length < directory.Length)
            filename = filename.Substring(directory.Length)
            If filename.ToUpperInvariant Like likeQuery AndAlso filename.ToUpperInvariant Like dirQuery Then
                matches.Add(filename)
                If matches.Count >= maxResults Then Exit For
            End If
        Next filename

        Return matches
    End Function
    Public Function GetDataFolderPath(ByVal subfolder As String) As String
        Dim folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        Try
            folder += IO.Path.DirectorySeparatorChar + "HostBot"
            If Not IO.Directory.Exists(folder) Then IO.Directory.CreateDirectory(folder)
            folder += IO.Path.DirectorySeparatorChar + subfolder
            If Not IO.Directory.Exists(folder) Then IO.Directory.CreateDirectory(folder)
            folder += IO.Path.DirectorySeparatorChar
            Return folder
        Catch e As Exception
            e.RaiseAsUnexpected("Error getting folder My Documents\HostBot\{0}.".Frmt(subfolder))
            Throw
        End Try
    End Function
#End Region

    ''' <summary>
    ''' Converts little-endian digits in one base to little-endian digits in another base.
    ''' </summary>
    <Pure()> <Extension()>
    Public Function ConvertFromBaseToBase(ByVal digits As IList(Of Byte),
                                          ByVal inputBase As UInteger,
                                          ByVal outputBase As UInteger,
                                          Optional ByVal minOutputLength As Integer = 0) As IList(Of Byte)
        Contract.Requires(digits IsNot Nothing)
        Contract.Requires(inputBase >= 2)
        Contract.Requires(inputBase <= 256)
        Contract.Requires(outputBase >= 2)
        Contract.Requires(outputBase <= 256)
        Contract.Ensures(Contract.Result(Of IList(Of Byte))() IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IList(Of Byte))().Count >= minOutputLength)

        'Convert from digits in input base to BigInteger
        Dim value = New BigInteger
        For i = digits.Count - 1 To 0 Step -1
            value *= inputBase
            value += digits(i)
        Next i

        'Convert from BigInteger to digits in output base
        Dim result = New List(Of Byte)
        Do Until result.Count >= minOutputLength AndAlso value = 0
            Dim remainder As BigInteger = Nothing
            value = BigInteger.DivRem(value, outputBase, remainder)
            result.Add(CByte(remainder))
        Loop

        Return result
    End Function
    <Pure()> <Extension()>
    Public Function ToUnsignedBigInteger(ByVal digits As IEnumerable(Of Byte)) As BigInteger
        Contract.Requires(digits IsNot Nothing)
        Contract.Ensures(Contract.Result(Of BigInteger)() >= 0)
        Return New BigInteger(digits.Concat({0}).ToArray)
    End Function
    <Pure()> <Extension()>
    Public Function ToUnsignedBigInteger(ByVal digits As Byte()) As BigInteger
        Contract.Requires(digits IsNot Nothing)
        Contract.Ensures(Contract.Result(Of BigInteger)() >= 0)
        If (digits(digits.Count - 1) And &H80) = 0 Then
            Return New BigInteger(digits)
        Else
            Return New BigInteger(Concat(digits, {0}))
        End If
    End Function
    <Pure()> <Extension()>
    Public Function ToUnsignedByteArray(ByVal value As BigInteger) As Byte()
        Contract.Requires(value >= 0)
        Contract.Ensures(Contract.Result(Of Byte())() IsNot Nothing)
        Dim result = value.ToByteArray()
        If result(result.Length - 1) = 0 Then
            result = result.SubArray(0, result.Length - 1)
        End If
        Return result
    End Function

    <Extension()>
    Public Sub WriteNullTerminatedString(ByVal bw As BinaryWriter, ByVal data As String)
        Contract.Requires(bw IsNot Nothing)
        Contract.Requires(data IsNot Nothing)
        bw.Write(data.ToAscBytes(True))
    End Sub
    <Extension()>
    Public Function ReadNullTerminatedString(ByVal reader As BinaryReader,
                                             ByVal maxLength As Integer) As String
        Contract.Requires(reader IsNot Nothing)
        Contract.Requires(maxLength >= 0)
        Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
        Dim result As String = Nothing
        If Not TryReadNullTerminatedString(reader, maxLength, result) Then
            Throw New IOException("Null-terminated string exceeded maximum length.")
        End If
        Contract.Assume(result IsNot Nothing)
        Return result
    End Function
    <Extension()>
    Public Function TryReadNullTerminatedString(ByVal reader As BinaryReader,
                                                ByVal maxLength As Integer,
                                                ByRef result As String) As Boolean
        Contract.Requires(reader IsNot Nothing)
        Dim data(0 To maxLength - 1) As Byte
        Dim n = 0
        Do
            Dim b = reader.ReadByte()
            If b = 0 Then
                Dim x = data.Take(n)
                Contract.Assume(x IsNot Nothing)
                result = x.ParseChrString(False)
                Return True
            End If
            If n >= maxLength Then Return False

            data(n) = b
            n += 1
        Loop
    End Function
End Module

Public NotInheritable Class KeyPair
    Private ReadOnly _value1 As ViewableList(Of Byte)
    Private ReadOnly _value2 As ViewableList(Of Byte)
    Public ReadOnly Property Value1 As ViewableList(Of Byte)
        Get
            Contract.Ensures(Contract.Result(Of ViewableList(Of Byte))() IsNot Nothing)
            Return _value1
        End Get
    End Property
    Public ReadOnly Property Value2 As ViewableList(Of Byte)
        Get
            Contract.Ensures(Contract.Result(Of ViewableList(Of Byte))() IsNot Nothing)
            Return _value2
        End Get
    End Property

    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(_value1 IsNot Nothing)
        Contract.Invariant(_value2 IsNot Nothing)
    End Sub

    Public Sub New(ByVal value1 As ViewableList(Of Byte), ByVal value2 As ViewableList(Of Byte))
        Contract.Requires(value1 IsNot Nothing)
        Contract.Requires(value2 IsNot Nothing)
        Me._value1 = value1
        Me._value2 = value2
    End Sub
End Class
