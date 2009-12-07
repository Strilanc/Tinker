Imports System.Net.NetworkInformation
Imports System.Runtime.CompilerServices
Imports System.IO.Path
Imports System.Text
Imports System.Net
Imports System.IO
Imports System.Numerics
Imports Strilbrary.Enumeration

'''<summary>A smattering of functions and other stuff that hasn't been placed in more reasonable groups yet.</summary>
Public Module PoorlyCategorizedFunctions
#Region "Strings Extra"
    <Extension()> <Pure()>
    Public Function Linefy(ByVal text As String) As String
        Contract.Requires(text IsNot Nothing)
        Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
        Return text.Replace("\n", Environment.NewLine)
    End Function

    <Pure()> <Extension()>
    Public Function FuturizedFail(ByVal exception As Exception) As IFuture
        Contract.Requires(exception IsNot Nothing)
        Contract.Ensures(Contract.Result(Of ifuture)() IsNot Nothing)
        Dim result = New FutureAction
        result.SetFailed(exception)
        Return result
    End Function

    <Pure()>
    Public Function SplitText(ByVal body As String, ByVal maxLineLength As Integer) As IList(Of String)
        Contract.Requires(body IsNot Nothing)
        Contract.Requires(maxLineLength > 0)
        Contract.Ensures(Contract.Result(Of IList(Of String))() IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IList(Of String))().Count > 0)
        'Contract.Ensures(Contract.ForAll(Contract.Result(Of IList(Of String)), Function(item) item IsNot Nothing))
        'Contract.Ensures(Contract.ForAll(Contract.Result(Of IList(Of String)), Function(item) item.Length <= maxLineLength))

        Dim result = New List(Of String)()
        For Each line In Microsoft.VisualBasic.Split(body, Delimiter:=Environment.NewLine)
            Contract.Assume(line IsNot Nothing)
            If line.Length <= maxLineLength Then
                result.Add(line)
                Continue For
            End If

            Dim ws = 0 'word start
            Dim ls = 0 'line start
            For i = 0 To line.Length
                Contract.Assert(ls <= ws)
                Contract.Assert(ws <= ls + maxLineLength + 1)
                Contract.Assert(ws <= i)
                If i < line.Length AndAlso line(i) <> " "c Then Continue For

                If i - ws > maxLineLength Then 'word will not fit on a single line
                    'Shove as much word as possible at end of current line
                    If line(ls + maxLineLength - 1) = " "c Then
                        result.Add(line.Substring(ls, maxLineLength - 1))
                    ElseIf line.Length > ls + maxLineLength AndAlso line(ls + maxLineLength) = " "c Then
                        result.Add(line.Substring(ls, maxLineLength))
                        ls += 1
                    Else
                        result.Add(line.Substring(ls, maxLineLength))
                    End If
                    ls += maxLineLength
                    'Divide remainder of word into lines
                    While i - ls > maxLineLength
                        result.Add(line.Substring(ls, maxLineLength))
                        ls += maxLineLength
                    End While
                    ws = ls
                ElseIf i - ls > maxLineLength Then 'word will not fit on current line
                    Contract.Assert(ls < ws)
                    result.Add(line.Substring(ls, ws - ls - 1))
                    ls = ws
                End If

                ws = i + 1
            Next i

            Contract.Assert(ls = 0 OrElse result.Count > 0)
            If ls <= line.Length Then
                result.Add(line.Substring(ls))
            End If
        Next line
        Return result
    End Function

    <Pure()>
    Public Function BuildDictionaryFromString(Of T)(ByVal text As String,
                                                    ByVal parser As Func(Of String, T),
                                                    ByVal pairDivider As String,
                                                    ByVal valueDivider As String) As Dictionary(Of InvariantString, T)
        Contract.Requires(parser IsNot Nothing)
        Contract.Requires(text IsNot Nothing)
        Contract.Requires(pairDivider IsNot Nothing)
        Contract.Requires(valueDivider IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Dictionary(Of InvariantString, T))() IsNot Nothing)
        Dim result = New Dictionary(Of InvariantString, T)
        Dim pd = New String() {pairDivider}
        Dim vd = New String() {valueDivider}
        For Each pair In text.Split(pd, StringSplitOptions.RemoveEmptyEntries)
            Contract.Assume(pair IsNot Nothing)
            Dim p = pair.IndexOf(valueDivider, StringComparison.OrdinalIgnoreCase)
            If p = -1 Then Throw New ArgumentException("'{0}' didn't include a value divider ('{1}').".Frmt(pair, valueDivider))
            Dim key = pair.Substring(0, p)
            Dim value = pair.Substring(p + valueDivider.Length)
            result(key) = parser(value)
        Next pair
        Return result
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
                                      ByVal likeQuery As InvariantString,
                                      ByVal directory As String,
                                      ByVal maxResults As Integer) As IList(Of String)
        Contract.Requires(fileQuery IsNot Nothing)
        Contract.Requires(directory IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IList(Of String))() IsNot Nothing)

        'Normalize input
        directory = directory.Replace(AltDirectorySeparatorChar, DirectorySeparatorChar)
        Contract.Assume(directory.Length > 0)
        If directory(directory.Length - 1) <> DirectorySeparatorChar Then
            directory += DirectorySeparatorChar
        End If

        'Separate directory and filename patterns
        fileQuery = fileQuery.Replace(AltDirectorySeparatorChar, DirectorySeparatorChar)
        Dim dirQuery As InvariantString = "*"
        If fileQuery.Contains(DirectorySeparatorChar) Then
            Dim words = fileQuery.Split(DirectorySeparatorChar)
            Dim filePattern = words(words.Length - 1)
            Contract.Assume(filePattern IsNot Nothing)
            Contract.Assume(fileQuery.Length > filePattern.Length)
            dirQuery = fileQuery.Substring(0, fileQuery.Length - filePattern.Length) + "*"
            fileQuery = "*" + filePattern
        End If

        'Check files in folder
        Dim matches = New List(Of String)
        For Each filename In IO.Directory.GetFiles(directory, fileQuery, IO.SearchOption.AllDirectories)
            Contract.Assume(filename IsNot Nothing)
            Contract.Assume(filename.Length > directory.Length)
            filename = filename.Substring(directory.Length)
            If filename Like likeQuery AndAlso filename Like dirQuery Then
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
            e.RaiseAsUnexpected("Error getting folder Documents\HostBot\{0}.".Frmt(subfolder))
            Throw
        End Try
    End Function
#End Region

    ''' <summary>
    ''' Determines the little-endian digits in one base from the little-endian digits in another base.
    ''' </summary>
    <Pure()> <Extension()>
    Public Function ConvertFromBaseToBase(ByVal digits As IList(Of Byte),
                                          ByVal inputBase As UInteger,
                                          ByVal outputBase As UInteger) As IList(Of Byte)
        Contract.Requires(digits IsNot Nothing)
        Contract.Requires(inputBase >= 2)
        Contract.Requires(inputBase <= 256)
        Contract.Requires(outputBase >= 2)
        Contract.Requires(outputBase <= 256)
        Contract.Ensures(Contract.Result(Of IList(Of Byte))() IsNot Nothing)

        'Convert from digits in input base to BigInteger
        Dim value = New BigInteger
        For i = digits.Count - 1 To 0 Step -1
            value *= inputBase
            value += digits(i)
        Next i

        'Convert from BigInteger to digits in output base
        Dim result = New List(Of Byte)
        Do Until value = 0
            Dim remainder As BigInteger = Nothing
            value = BigInteger.DivRem(value, outputBase, remainder)
            result.Add(CByte(remainder))
        Loop

        Return result
    End Function
    ''' <summary>
    ''' Determines a list starting with the elements of the given list but padded with default values to meet a minimum length.
    ''' </summary>
    <Pure()> <Extension()>
    Public Function PaddedTo(Of T)(ByVal this As IList(Of T),
                                   ByVal minimumLength As Integer) As IList(Of T)
        Contract.Requires(this IsNot Nothing)
        Contract.Requires(minimumLength >= 0)
        Contract.Ensures(Contract.Result(Of IList(Of T))() IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IList(Of T))().Count = Math.Max(this.Count, minimumLength))

        Dim result(0 To Math.Max(minimumLength, this.Count) - 1) As T
        For i = 0 To this.Count - 1
            result(i) = this(i)
        Next i
        Return result
    End Function

    <Pure()> <Extension()>
    Public Function ToUnsignedBigInteger(ByVal digits As IList(Of Byte)) As BigInteger
        Contract.Requires(digits IsNot Nothing)
        Contract.Ensures(Contract.Result(Of BigInteger)() >= 0)
        Return digits.ToArray.ToUnsignedBigInteger
    End Function
    <Pure()> <Extension()>
    Public Function ToUnsignedBigInteger(ByVal digits As Byte()) As BigInteger
        Contract.Requires(digits IsNot Nothing)
        Contract.Ensures(Contract.Result(Of BigInteger)() >= 0)
        If digits.Length = 0 Then
            Dim result = New BigInteger(0)
            Contract.Assume(result >= 0)
            Return result
        ElseIf (digits(digits.Length - 1) And &H80) = 0 Then
            Return New BigInteger(digits)
        Else
            Dim result = New BigInteger(Concat(digits, {0}))
            Contract.Assume(result >= 0)
            Return result
        End If
    End Function
    <Pure()> <Extension()>
    Public Function ToUnsignedByteArray(ByVal value As BigInteger) As Byte()
        Contract.Requires(value >= 0)
        Contract.Ensures(Contract.Result(Of Byte())() IsNot Nothing)
        Dim result = value.ToByteArray()
        Contract.Assume(result IsNot Nothing)
        If result.Length > 0 AndAlso result(result.Length - 1) = 0 Then
            result = result.SubArray(0, result.Length - 1)
        End If
        Return result
    End Function

    <Pure()> <Extension()>
    Public Function AssumeNotNull(Of T)(ByVal arg As T) As T
        Contract.Ensures(Contract.Result(Of T)() IsNot Nothing)
        Contract.Assume(arg IsNot Nothing)
        Return arg
    End Function

    <Extension()>
    Public Sub WriteNullTerminatedString(ByVal bw As BinaryWriter, ByVal data As String)
        Contract.Requires(bw IsNot Nothing)
        Contract.Requires(data IsNot Nothing)
        bw.Write(data.ToAscBytes(True))
    End Sub
    <Extension()>
    Public Function ReadNullTerminatedData(ByVal reader As BinaryReader) As IList(Of Byte)
        Contract.Requires(reader IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IList(Of Byte))() IsNot Nothing)
        Dim data = New List(Of Byte)
        Do
            Dim b = reader.ReadByte()
            If b = 0 Then Exit Do
            data.Add(b)
        Loop
        Return data
    End Function
    <Extension()>
    Public Function ReadNullTerminatedString(ByVal reader As BinaryReader) As String
        Contract.Requires(reader IsNot Nothing)
        Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
        Return reader.ReadNullTerminatedData.ParseChrString(nullTerminated:=False)
    End Function
    <Extension()>
    Public Function ReadNullTerminatedString(ByVal reader As BinaryReader,
                                             ByVal maxLength As Integer) As String
        Contract.Requires(reader IsNot Nothing)
        Contract.Requires(maxLength >= 0)
        Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
        Contract.Ensures(Contract.Result(Of String)().Length <= maxLength)

        Dim data(0 To maxLength - 1) As Byte
        Dim numRead = 0
        Do
            Dim b = reader.ReadByte()

            If b = 0 Then Return data.Take(numRead).ParseChrString(nullTerminated:=False)
            If numRead >= maxLength Then Throw New InvalidDataException("Null-terminated string exceeded maximum length.")

            data(numRead) = b
            numRead += 1
        Loop
    End Function

    '''<summary>Determines the SHA-1 hash of a sequence of bytes.</summary>
    <Extension()>
    Public Function SHA1(ByVal data As IEnumerable(Of Byte)) As Byte()
        Contract.Requires(data IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Byte())() IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Byte())().Length = 20)
        Using sha = New System.Security.Cryptography.SHA1Managed()
            Dim hash = sha.ComputeHash(data.ToStream)
            Contract.Assume(hash IsNot Nothing)
            Contract.Assume(hash.Length = 20)
            Return hash
        End Using
    End Function

    '''<summary>Determines the crc32 checksum of a sequence of bytes.</summary>
    <Extension()> <Pure()>
    Public Function CRC32(ByVal data As IEnumerable(Of Byte),
                          Optional ByVal poly As UInteger = &H4C11DB7,
                          Optional ByVal polyAlreadyReversed As Boolean = False) As UInteger
        Contract.Requires(data IsNot Nothing)
        Return data.GetEnumerator.CRC32(poly, polyAlreadyReversed)
    End Function
    '''<summary>Determines the crc32 checksum of a sequence of bytes.</summary>
    <Extension()>
    Public Function CRC32(ByVal data As IEnumerator(Of Byte),
                          Optional ByVal poly As UInteger = &H4C11DB7,
                          Optional ByVal polyAlreadyReversed As Boolean = False) As UInteger
        Contract.Requires(data IsNot Nothing)
        Dim reg As UInteger

        'Reverse the polynomial
        If polyAlreadyReversed = False Then
            Dim polyRev As UInteger = 0
            For i = 0 To 31
                If ((poly >> i) And &H1) <> 0 Then
                    polyRev = polyRev Or (CUInt(&H1) << (31 - i))
                End If
            Next i
            poly = polyRev
        End If

        'Precompute the combined XOR masks for each byte
        Dim xorTable(0 To 255) As UInteger
        For i = 0 To 255
            reg = CUInt(i)
            For j = 0 To 7
                If (reg And CUInt(&H1)) <> 0 Then
                    reg = (reg >> 1) Xor poly
                Else
                    reg >>= 1
                End If
            Next j
            xorTable(i) = reg
        Next i

        'Direct Table Algorithm
        reg = UInteger.MaxValue
        While data.MoveNext
            reg = (reg >> 8) Xor xorTable(data.Current Xor CByte(reg And &HFF))
        End While

        Return Not reg
    End Function

    '''<summary>Converts versus strings to a list of the team sizes (eg. 1v3v2 -> {1,3,2}).</summary>
    Public Function TeamVersusStringToTeamSizes(ByVal value As String) As IList(Of Integer)
        Contract.Requires(value IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IList(Of Integer))() IsNot Nothing)

        'parse numbers between 'v's
        Dim vals = value.ToUpperInvariant.Split("V"c)
        Dim nums = New List(Of Integer)
        For Each e In vals
            Dim b As Byte
            Contract.Assume(e IsNot Nothing)
            If Not Byte.TryParse(e, b) Then
                Throw New InvalidOperationException("Non-numeric team limit '{0}'.".Frmt(e))
            End If
            nums.Add(b)
        Next e
        Return nums
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

Public NotInheritable Class DelegatedDisposable
    Implements IDisposable
    Private ReadOnly disposer As action
    Private ReadOnly disposed As New OnetimeLock

    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(disposer IsNot Nothing)
        Contract.Invariant(disposed IsNot Nothing)
    End Sub

    Public Sub New(ByVal disposer As Action)
        Contract.Requires(disposer IsNot Nothing)
        Me.disposer = disposer
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        If disposed.TryAcquire Then disposer()
    End Sub
End Class