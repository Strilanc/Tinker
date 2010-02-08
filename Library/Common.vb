Imports System.Numerics

'''<summary>A smattering of functions and other stuff that hasn't been placed in more reasonable groups yet.</summary>
Public Module PoorlyCategorizedFunctions
#Region "Strings Extra"
    'verification disabled due to stupid verifier (1.2.30118.5)
    <ContractVerification(False)>
    <Pure()>
    Public Function SplitText(ByVal body As String, ByVal maxLineLength As Integer) As IList(Of String)
        Contract.Requires(body IsNot Nothing)
        Contract.Requires(maxLineLength > 0)
        Contract.Ensures(Contract.Result(Of IList(Of String))() IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IList(Of String))().Count > 0)
        'Contract.Ensures(Contract.ForAll(Contract.Result(Of IList(Of String)), Function(item) item IsNot Nothing))
        'Contract.Ensures(Contract.ForAll(Contract.Result(Of IList(Of String)), Function(item) item.Length <= maxLineLength))

        'Recurse on actual lines, if there are multiple
        If body.Contains(Environment.NewLine) Then
            Return (From line In Microsoft.VisualBasic.Split(body, Delimiter:=Environment.NewLine)
                    Select SplitText(line, maxLineLength)
                   ).Fold.ToList
        End If

        'Separate body into lines, respecting the maximum line length and trying to divide along word boundaries
        Dim result = New List(Of String)()
        Dim ws = 0 'word start
        Dim ls = 0 'line start
        For we = 0 To body.Length 'iterate for word endings
            Contract.Assert(ls <= ws)
            Contract.Assert(ws <= ls + maxLineLength + 1)
            Contract.Assert(ws <= we)
            If we < body.Length AndAlso body(we) <> " "c Then Continue For 'not a word ending position

            If ws + maxLineLength < we Then 'word will not fit on a single line
                'Output current line, shoving as much of the word at the end of the line as possible
                If body(ls + maxLineLength - 1) = " "c Then
                    'There is a word boundary at the end of the current line, don't include it
                    result.Add(body.Substring(ls, maxLineLength - 1))
                    ls += maxLineLength
                Else
                    result.Add(body.Substring(ls, maxLineLength))
                    ls += maxLineLength
                    'If there is a word boundary at the start of the new line, skip it
                    If ls < body.Length AndAlso body(ls) = " "c Then ls += 1
                End If

                'Output lines until the word fits on a line, starting a new line with the remainder of the word
                While ls + maxLineLength < we
                    result.Add(body.Substring(ls, maxLineLength))
                    ls += maxLineLength
                End While
                ws = ls

            ElseIf ls + maxLineLength < we Then 'word will not fit on current line
                'Output current line, starting a new line with the current word
                Contract.Assert(ls < ws)
                result.Add(body.Substring(ls, ws - ls - 1))
                ls = ws
            End If

            'Start new word
            ws = we + 1
        Next we

        'Output last line
        Contract.Assert(ls = 0 OrElse result.Count > 0)
        If result.Count = 0 OrElse ls <= body.Length Then
            result.Add(body.Substring(ls))
        End If
        Return result
    End Function

    'verification disabled due to stupid verifier (1.2.30118.5)
    <ContractVerification(False)>
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
            Contract.Assume(p >= 0)
            Contract.Assume(p <= pair.Length)
            Dim key = pair.Substring(0, p)
            Contract.Assume(p + valueDivider.Length <= pair.Length)
            Dim value = pair.Substring(p + valueDivider.Length)
            result(key) = parser(value)
        Next pair
        Return result
    End Function
#End Region

    <Extension()>
    Public Function AsyncRepeat(ByVal clock As IClock, ByVal period As TimeSpan, ByVal action As action) As IDisposable
        Contract.Requires(clock IsNot Nothing)
        Contract.Requires(action IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)

        Dim stopFlag As Boolean
        Dim callback As Action
        callback = Sub()
                       If stopFlag Then Return
                       Call action()
                       clock.AsyncWait(period).CallOnSuccess(callback)
                   End Sub
        clock.AsyncWait(period).CallOnSuccess(callback)
        Return New DelegatedDisposable(Sub() stopFlag = True)
    End Function

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
                                      ByVal directory As InvariantString,
                                      ByVal maxResults As Integer) As IList(Of String)
        Contract.Requires(fileQuery IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IList(Of String))() IsNot Nothing)

        If Not directory.EndsWith(IO.Path.DirectorySeparatorChar) AndAlso Not directory.EndsWith(IO.Path.AltDirectorySeparatorChar) Then
            directory += IO.Path.DirectorySeparatorChar
        End If

        'Separate directory and filename patterns
        fileQuery = fileQuery.Replace(IO.Path.AltDirectorySeparatorChar, IO.Path.DirectorySeparatorChar)
        Dim dirQuery As InvariantString = "*"
        If fileQuery.Contains(IO.Path.DirectorySeparatorChar) Then
            Dim words = fileQuery.Split(IO.Path.DirectorySeparatorChar)
            Dim filePattern = words(words.Length - 1)
            Contract.Assume(filePattern IsNot Nothing)
            Contract.Assume(fileQuery.Length > filePattern.Length)
            dirQuery = fileQuery.Substring(0, fileQuery.Length - filePattern.Length) + "*"
            fileQuery = "*" + filePattern
        End If

        'Check files in folder
        Dim matches = New List(Of String)
        For Each filepath In IO.Directory.GetFiles(directory, fileQuery, IO.SearchOption.AllDirectories)
            Contract.Assume(filepath IsNot Nothing)
            Contract.Assume(filepath.Length > directory.Length)
            Dim relativePath = filepath.Substring(directory.Length)
            If relativePath Like likeQuery AndAlso relativePath Like dirQuery Then
                matches.Add(relativePath)
                If matches.Count >= maxResults Then Exit For
            End If
        Next filepath
        Return matches
    End Function
    Public Function GetDataFolderPath(ByVal subfolder As String) As String
        Dim path = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                                   Application.ProductName,
                                   subfolder)
        Contract.Assume(path IsNot Nothing)
        Contract.Assume(path.Length > 0)
        Try
            If Not IO.Directory.Exists(path) Then IO.Directory.CreateDirectory(path)
            Return path
        Catch e As Exception
            e.RaiseAsUnexpected("Error creating folder: {0}.".Frmt(path))
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
                                   ByVal minimumLength As Integer,
                                   Optional ByVal paddingValue As T = Nothing) As IList(Of T)
        Contract.Requires(this IsNot Nothing)
        Contract.Requires(minimumLength >= 0)
        Contract.Ensures(Contract.Result(Of IList(Of T))() IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IList(Of T))().Count = Math.Max(this.Count, minimumLength))

        Dim length = Math.Max(minimumLength, this.Count)
        Dim result = New List(Of T)(capacity:=length)
        result.AddRange(this)
        While result.Count < length
            result.Add(paddingValue)
        End While
        Contract.Assume(result.Count = Math.Max(this.Count, minimumLength))
        Return result
    End Function

    <Pure()> <Extension()>
    Public Function ToUnsignedBigInteger(ByVal digits As IEnumerable(Of Byte)) As BigInteger
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
            Dim result = New BigInteger(digits)
            Contract.Assume(result >= 0)
            Return result
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

    <Pure()>
    Public Function Tuple(Of T1, T2)(ByVal arg1 As T1, ByVal arg2 As T2) As Tuple(Of T1, T2)
        Contract.Ensures(Contract.Result(Of Tuple(Of T1, T2))() IsNot Nothing)
        Return New Tuple(Of T1, T2)(arg1, arg2)
    End Function
    <Pure()>
    Public Function Tuple(Of T1, T2, T3)(ByVal arg1 As T1, ByVal arg2 As T2, ByVal arg3 As T3) As Tuple(Of T1, T2, T3)
        Contract.Ensures(Contract.Result(Of Tuple(Of T1, T2))() IsNot Nothing)
        Return New Tuple(Of T1, T2, T3)(arg1, arg2, arg3)
    End Function

    <Pure()> <Extension()>
    Public Function AssumeNotNull(Of T)(ByVal arg As T) As T
        Contract.Ensures(Contract.Result(Of T)() IsNot Nothing)
        Contract.Assume(arg IsNot Nothing)
        Return arg
    End Function

    <Extension()>
    Public Sub WriteAt(ByVal stream As IRandomWritableStream, ByVal position As Long, ByVal data As IReadableList(Of Byte))
        Contract.Requires(stream IsNot Nothing)
        Contract.Requires(data IsNot Nothing)
        Contract.Requires(position >= 0)
        Contract.Requires(position <= stream.Length)
        Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))() IsNot Nothing)
        Contract.Ensures(stream.Position = position + data.count)
        stream.Position = position
        stream.Write(data)
    End Sub

    <Extension()>
    Public Function ReadExactAt(ByVal stream As IRandomReadableStream, ByVal position As Long, ByVal exactCount As Integer) As IReadableList(Of Byte)
        Contract.Requires(stream IsNot Nothing)
        Contract.Requires(position >= 0)
        Contract.Requires(exactCount > 0)
        Contract.Requires(position + exactCount <= stream.Length)
        Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))() IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))().Count = exactCount)
        Contract.Ensures(stream.Position = position + exactCount)
        stream.Position = position
        Return stream.ReadExact(exactCount)
    End Function
    <Extension()>
    Public Sub WriteNullTerminatedString(ByVal bw As IWritableStream, ByVal data As String)
        Contract.Requires(bw IsNot Nothing)
        Contract.Requires(data IsNot Nothing)
        bw.Write(data.ToAscBytes(True).AsReadableList)
    End Sub
    <Extension()>
    <ContractVerification(False)>
    Public Function ReadNullTerminatedData(ByVal reader As IReadableStream) As IReadableList(Of Byte)
        Contract.Requires(reader IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))() IsNot Nothing)
        Dim result = New List(Of Byte)
        Do
            Dim b = reader.ReadByte()
            If b = 0 Then Exit Do
            result.Add(b)
        Loop
        Return result.AsReadableList
    End Function
    <Extension()>
    Public Function ReadNullTerminatedString(ByVal reader As IReadableStream) As String
        Contract.Requires(reader IsNot Nothing)
        Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
        Return reader.ReadNullTerminatedData.ParseChrString(nullTerminated:=False)
    End Function
    'verification disabled due to stupid verifier (1.2.30118.5)
    <ContractVerification(False)>
    <Extension()>
    Public Function ReadNullTerminatedString(ByVal reader As IReadableStream,
                                             ByVal maxLength As Integer) As String
        Contract.Requires(reader IsNot Nothing)
        Contract.Requires(maxLength >= 0)
        Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
        Contract.Ensures(Contract.Result(Of String)().Length <= maxLength)

        Dim data = New List(Of Byte)(capacity:=maxLength)
        Do
            Contract.Assert(data.Count <= maxLength)
            Dim b = reader.ReadByte()
            If b = 0 Then
                Return data.ParseChrString(nullTerminated:=False)
            ElseIf data.Count < maxLength Then
                data.Add(b)
            Else
                Throw New IO.InvalidDataException("Null-terminated string exceeded maximum length.")
            End If
        Loop
    End Function

    <Extension()>
    Public Function ReadSingle(ByVal stream As IReadableStream) As Single
        Contract.Requires(stream IsNot Nothing)
        Return BitConverter.ToSingle(stream.ReadExact(4).ToArray, 0)
    End Function

    '''<summary>Determines the SHA-1 hash of a sequence of bytes.</summary>
    <Extension()> <Pure()>
    Public Function SHA1(ByVal data As IEnumerable(Of Byte)) As IReadableList(Of Byte)
        Contract.Requires(data IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))() IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))().Count = 20)
        Using sha = New System.Security.Cryptography.SHA1Managed()
            Dim hash = sha.ComputeHash(data.ToStream)
            Contract.Assume(hash IsNot Nothing)
            Dim result = hash.AsReadableList()
            Contract.Assume(result.Count = 20)
            Return result
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
