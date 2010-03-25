Imports System.Numerics

'''<summary>A smattering of functions and other stuff that hasn't been placed in more reasonable groups yet.</summary>
Public Module PoorlyCategorizedFunctions
#Region "Strings Extra"
    'verification disabled due to stupid verifier (1.2.30118.5)
    <ContractVerification(False)>
    <Pure()>
    Public Function SplitText(ByVal body As String, ByVal maxLineLength As Integer) As IEnumerable(Of String)
        Contract.Requires(body IsNot Nothing)
        Contract.Requires(maxLineLength > 0)
        Contract.Ensures(Contract.Result(Of IEnumerable(Of String))() IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IEnumerable(Of String))().Count > 0)
        'Contract.Ensures(Contract.ForAll(Contract.Result(Of IList(Of String)), Function(item) item IsNot Nothing))
        'Contract.Ensures(Contract.ForAll(Contract.Result(Of IList(Of String)), Function(item) item.Length <= maxLineLength))

        'Recurse on actual lines, if there are multiple
        If body.Contains(Environment.NewLine) Then
            Return Concat(From line In Microsoft.VisualBasic.Split(body, Delimiter:=Environment.NewLine)
                          Select SplitText(line, maxLineLength))
        End If

        'Separate body into lines, respecting the maximum line length and trying to divide along word boundaries
        Dim result = New List(Of String)()
        Dim ws = 0 'word start
        Dim ls = 0 'line start
        For Each we In body.IndexesOf(" "c).Append(body.Length) 'iterate over word endings
            Contract.Assert(ls <= ws)
            Contract.Assert(ws <= ls + maxLineLength + 1)
            Contract.Assert(ws <= we)

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
                                                    ByVal valueDivider As String) As IDictionary(Of InvariantString, T)
        Contract.Requires(parser IsNot Nothing)
        Contract.Requires(text IsNot Nothing)
        Contract.Requires(pairDivider IsNot Nothing)
        Contract.Requires(valueDivider IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IDictionary(Of InvariantString, T))() IsNot Nothing)
        Dim pairs = text.Split({pairDivider}, StringSplitOptions.RemoveEmptyEntries)
        If (From pair In pairs Where Not pair.Contains(valueDivider)).Any Then
            Throw New ArgumentException("Missing value divider '{0}' in '{1}'.".Frmt(valueDivider, text))
        End If

        Return (From pair In pairs
                Let p = pair.IndexOf(valueDivider, StringComparison.Ordinal)
                Let key As InvariantString = pair.Substring(0, p)
                Let value = parser(pair.Substring(p + valueDivider.Length))
                ).ToDictionary(keySelector:=Function(e) e.key, elementSelector:=Function(e) e.value)
    End Function
#End Region

    <Pure()> <Extension()>
    Public Function ToUValue(ByVal data As IEnumerable(Of Byte),
                             Optional ByVal byteOrder As ByteOrder = ByteOrder.LittleEndian) As UInt64
        Contract.Requires(data IsNot Nothing)
        If data.Count > 8 Then Throw New ArgumentException("Too many bytes.", "data")
        Dim padding = CByte(0).Repeated(8 - data.Count)
        Select Case byteOrder
            Case Strilbrary.Values.ByteOrder.LittleEndian
                Return data.Concat(padding).ToUInt64(byteOrder)
            Case Strilbrary.Values.ByteOrder.BigEndian
                Return padding.Concat(data).ToUInt64(byteOrder)
            Case Else
                Throw byteOrder.MakeArgumentValueException("byteOrder")
        End Select
    End Function
    Public Function FindFileMatching(ByVal fileQuery As String, ByVal likeQuery As String, ByVal directory As String) As String
        Contract.Requires(fileQuery IsNot Nothing)
        Contract.Requires(likeQuery IsNot Nothing)
        Contract.Requires(directory IsNot Nothing)
        Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
        Dim result = FindFilesMatching(fileQuery, likeQuery, directory, 1).FirstOrDefault
        If result Is Nothing Then Throw New OperationFailedException("No matches.")
        Return result
    End Function

    <Extension()> <Pure()>
    Public Function HasBitSet(ByVal value As Byte, ByVal bitPosition As Integer) As Boolean
        Return ((value >> bitPosition) And CByte(1)) <> 0
    End Function
    <Extension()> <Pure()>
    Public Function HasBitSet(ByVal value As UInt16, ByVal bitPosition As Integer) As Boolean
        Return ((value >> bitPosition) And 1US) <> 0
    End Function
    <Extension()> <Pure()>
    Public Function HasBitSet(ByVal value As UInt32, ByVal bitPosition As Integer) As Boolean
        Return ((value >> bitPosition) And 1UI) <> 0
    End Function
    <Extension()> <Pure()>
    Public Function HasBitSet(ByVal value As UInt64, ByVal bitPosition As Integer) As Boolean
        Return ((value >> bitPosition) And 1UL) <> 0
    End Function

    <Extension()> <Pure()>
    Public Function WithBitSetTo(ByVal value As Byte, ByVal bitPosition As Integer, ByVal bitValue As Boolean) As Byte
        Dim bit = CByte(1) << bitPosition
        Return If(bitValue, value Or bit, value And Not bit)
    End Function
    <Extension()> <Pure()>
    Public Function WithBitSetTo(ByVal value As UInt16, ByVal bitPosition As Integer, ByVal bitValue As Boolean) As UInt16
        Dim bit = 1US << bitPosition
        Return If(bitValue, value Or bit, value And Not bit)
    End Function
    <Extension()> <Pure()>
    Public Function WithBitSetTo(ByVal value As UInt32, ByVal bitPosition As Integer, ByVal bitValue As Boolean) As UInt32
        Dim bit = 1UI << bitPosition
        Return If(bitValue, value Or bit, value And Not bit)
    End Function
    <Extension()> <Pure()>
    Public Function WithBitSetTo(ByVal value As UInt64, ByVal bitPosition As Integer, ByVal bitValue As Boolean) As UInt64
        Dim bit = 1UL << bitPosition
        Return If(bitValue, value Or bit, value And Not bit)
    End Function

    <Extension()> <Pure()>
    Public Function EnumIncludes(Of T)(ByVal value As T, ByVal [option] As UInt32) As Boolean
        Return (value.DynamicDirectCastTo(Of UInt32)() And [option]) = [option]
    End Function
    <Extension()> <Pure()>
    Public Function EnumWithSet(Of T)(ByVal value As T, ByVal [option] As UInt32, ByVal include As Boolean) As T
        Dim v = value.DynamicDirectCastTo(Of UInt32)()
        v = If(include, v Or [option], v And Not [option])
        Return v.DynamicDirectCastTo(Of T)()
    End Function

    <Extension()> <Pure()>
    <ContractVerification(False)>
    Public Function Bits(ByVal value As UInt64) As IEnumerable(Of Boolean)
        Contract.Ensures(Contract.Result(Of IEnumerable(Of Boolean))() IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IEnumerable(Of Boolean))().Count = 64)
        Return From i In 64.Range Select value.HasBitSet(i)
    End Function
    <Extension()> <Pure()>
    <ContractVerification(False)>
    Public Function Bits(ByVal value As UInt32) As IEnumerable(Of Boolean)
        Contract.Ensures(Contract.Result(Of IEnumerable(Of Boolean))() IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IEnumerable(Of Boolean))().Count = 32)
        Return From i In 32.Range Select value.HasBitSet(i)
    End Function
    <Extension()> <Pure()>
    <ContractVerification(False)>
    Public Function Bits(ByVal value As UInt16) As IEnumerable(Of Boolean)
        Contract.Ensures(Contract.Result(Of IEnumerable(Of Boolean))() IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IEnumerable(Of Boolean))().Count = 16)
        Return From i In 16.Range Select value.HasBitSet(i)
    End Function
    <Extension()> <Pure()>
    <ContractVerification(False)>
    Public Function Bits(ByVal value As Byte) As IEnumerable(Of Boolean)
        Contract.Ensures(Contract.Result(Of IEnumerable(Of Boolean))() IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IEnumerable(Of Boolean))().Count = 8)
        Return From i In 8.Range Select value.HasBitSet(i)
    End Function

    <CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")>
    <Extension()> <Pure()>
    Public Function Summarize(ByVal ex As Exception) As String
        Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
        If ex Is Nothing Then
            Return "Null Exception"
        ElseIf TypeOf ex Is AggregateException Then
            Dim ax = DirectCast(ex, AggregateException)
            Contract.Assume(ax.InnerExceptions IsNot Nothing)
            Select Case ax.InnerExceptions.Count
                Case 0 : Return "Empty AggregateException"
                Case 1 : Return ax.InnerExceptions.Single.Summarize()
                Case Else : Return "{0} Exceptions Occured: {1}".Frmt(ax.InnerExceptions.Count,
                                                                      Environment.NewLine + ax.InnerExceptions.StringJoin(Environment.NewLine))
            End Select
        Else
            Return "({0}) {1}".Frmt(ex.GetType.Name, ex.Message)
        End If
    End Function

    <Extension()>
    Public Function IgnoreExceptions(Of T)(ByVal task As TaskCompletionSource(Of T)) As task
        Contract.Requires(task IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
        Contract.Assume(task.Task IsNot Nothing)
        Return task.Task.IgnoreExceptions()
    End Function
    <Extension()>
    Public Function IgnoreExceptions(ByVal task As Task) As task
        Contract.Requires(task IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
        Return task.Catch(Sub()
                          End Sub)
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
            Dim filePattern = words.Last
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
        Contract.Requires(subfolder IsNot Nothing)
        Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
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

    ''' <summary>
    ''' Determines the little-endian digits in one base from the little-endian digits in another base.
    ''' </summary>
    <Pure()> <Extension()>
    Public Function ConvertFromBaseToBase(ByVal digits As IEnumerable(Of Byte),
                                          ByVal inputBase As UInteger,
                                          ByVal outputBase As UInteger) As IReadableList(Of Byte)
        Contract.Requires(digits IsNot Nothing)
        Contract.Requires(inputBase >= 2)
        Contract.Requires(inputBase <= 256)
        Contract.Requires(outputBase >= 2)
        Contract.Requires(outputBase <= 256)
        Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))() IsNot Nothing)

        'Convert from digits in input base to BigInteger
        Dim value = New BigInteger
        For Each digit In digits.Reverse
            value *= inputBase
            value += digit
        Next digit

        'Convert from BigInteger to digits in output base
        Dim result = New List(Of Byte)
        Do Until value = 0
            Dim remainder As BigInteger = Nothing
            value = BigInteger.DivRem(value, outputBase, remainder)
            result.Add(CByte(remainder))
        Loop

        Return result.ToReadableList
    End Function
    ''' <summary>
    ''' Determines a list starting with the elements of the given list but padded with default values to meet a minimum length.
    ''' </summary>
    <Pure()> <Extension()>
    Public Function PaddedTo(Of T)(ByVal this As IReadableList(Of T),
                                   ByVal minimumLength As Integer,
                                   Optional ByVal paddingValue As T = Nothing) As IReadableList(Of T)
        Contract.Requires(this IsNot Nothing)
        Contract.Requires(minimumLength >= 0)
        Contract.Ensures(Contract.Result(Of IReadableList(Of T))() IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IReadableList(Of T))().Count = Math.Max(this.Count, minimumLength))
        If this.Count >= minimumLength Then Return this
        Dim result = this.Concat(paddingValue.Repeated(minimumLength - this.Count)).ToReadableList
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
        ElseIf digits.Last.Bits.Last Then 'BigInteger will misinterpret the last bit as a negative sign, so append 0 first
            Dim result = New BigInteger(digits.Append(0).ToArray)
            Contract.Assume(result >= 0)
            Return result
        Else
            Dim result = New BigInteger(digits)
            Contract.Assume(result >= 0)
            Return result
        End If
    End Function
    <Pure()> <Extension()>
    <ContractVerification(False)>
    Public Function ToUnsignedBytes(ByVal value As BigInteger) As IReadableList(Of Byte)
        Contract.Requires(value >= 0)
        Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))() IsNot Nothing)
        Dim result = value.ToByteArray.ToReadableList
        If result.Count > 0 AndAlso result.Last = 0 Then
            Return result.SubView(0, result.Count - 1)
        Else
            Return result
        End If
    End Function
    <Pure()> <Extension()>
    Public Function AssumeNotNull(Of T)(ByVal arg As T) As T
        Contract.Ensures(Contract.Result(Of T)() IsNot Nothing)
        Contract.Assume(arg IsNot Nothing)
        Return arg
    End Function

    '''<summary>Determines the SHA-1 hash of a sequence of bytes.</summary>
    <Extension()> <Pure()>
    Public Function SHA1(ByVal data As IEnumerable(Of Byte)) As IReadableList(Of Byte)
        Contract.Requires(data IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))() IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))().Count = 20)
        Using sha = New System.Security.Cryptography.SHA1Managed()
            Dim result = sha.ComputeHash(data.AsReadableStream.AsStream).AsReadableList
            Contract.Assume(result.Count = 20)
            Return result
        End Using
    End Function

    Public Function CRC32Table(Optional ByVal poly As UInteger = &H4C11DB7,
                               Optional ByVal polyAlreadyReversed As Boolean = False) As UInt32()
        'Reverse the polynomial
        If Not polyAlreadyReversed Then
            Dim polyRev = 0UI
            For Each bit In poly.Bits
                polyRev <<= 1
                If bit Then polyRev += 1UI
            Next bit
            poly = polyRev
        End If

        'Precompute the combined XOR masks for each byte
        Dim xorTable(0 To 256 - 1) As UInteger
        For Each i In xorTable.Count.Range
            Dim accumulator = CUInt(i)
            For Each repeat In 8.Range
                Dim useXor = accumulator.Bits.First
                accumulator >>= 1
                If useXor Then accumulator = accumulator Xor poly
            Next repeat
            xorTable(i) = accumulator
        Next i

        Return xorTable
    End Function
    <Extension()>
    Public Function CRC32(ByVal data As IReadableStream,
                          Optional ByVal poly As UInteger = &H4C11DB7,
                          Optional ByVal polyAlreadyReversed As Boolean = False) As UInteger
        Contract.Requires(data IsNot Nothing)
        Dim xorTable = CRC32Table(poly, polyAlreadyReversed)

        'Direct Table Algorithm
        Dim result = UInteger.MaxValue
        Do
            Dim block = data.Read(1024)
            If block.Count = 0 Then Exit Do
            For Each e In block
                result = (result >> 8) Xor xorTable(e Xor CByte(result And &HFFUI))
            Next e
        Loop

        Return Not result
    End Function
    <Extension()> <Pure()>
    Public Function CRC32(ByVal data As IEnumerable(Of Byte),
                          Optional ByVal poly As UInteger = &H4C11DB7,
                          Optional ByVal polyAlreadyReversed As Boolean = False) As UInteger
        Contract.Requires(data IsNot Nothing)
        Return data.AsReadableStream.CRC32(poly, polyAlreadyReversed)
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

    Public Sub CheckIOData(ByVal clause As Boolean, ByVal message As String)
        Contract.Requires(message IsNot Nothing)
        Contract.Ensures(clause)
        Contract.EnsuresOnThrow(Of IO.InvalidDataException)(Not clause)
        If Not clause Then Throw New IO.InvalidDataException(message)
    End Sub

    <Extension()>
    Public Function Max(Of T As IComparable(Of T))(ByVal val1 As T, ByVal val2 As T) As T
        Return If(val1.CompareTo(val2) >= 0, val1, val2)
    End Function
End Module
