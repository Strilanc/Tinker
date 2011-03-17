Imports System.Numerics

'''<summary>A smattering of functions and other stuff that hasn't been placed in more reasonable groups yet.</summary>
Public Module PoorlyCategorizedFunctions
    <Extension()> <Pure()>
    Public Function MaybeFirst(Of T)(sequence As IEnumerable(Of T)) As Maybe(Of T)
        Contract.Requires(sequence IsNot Nothing)
        Using e = sequence.GetEnumerator()
            If Not e.MoveNext() Then Return Maybe(Of T).Empty
            If e.Current Is Nothing Then Throw New NullReferenceException("sequence.First()")
            Return e.Current
        End Using
    End Function

    'verification disabled due to inadequate verifier
    <ContractVerification(False)>
    <Pure()>
    Public Function SplitText(body As String, maxLineLength As Integer) As IEnumerable(Of String)
        Contract.Requires(body IsNot Nothing)
        Contract.Requires(maxLineLength > 0)
        Contract.Ensures(Contract.Result(Of IEnumerable(Of String))() IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IEnumerable(Of String))().Count > 0)
        'Contract.Ensures(Contract.ForAll(Contract.Result(Of IList(Of String)), Function(item) item IsNot Nothing))
        'Contract.Ensures(Contract.ForAll(Contract.Result(Of IList(Of String)), Function(item) item.Length <= maxLineLength))

        'Recurse on actual lines, if there are multiple
        If body.Contains(Environment.NewLine) Then
            Return Concat(From line In body.Split({Environment.NewLine}, StringSplitOptions.None)
                          Select SplitText(line, maxLineLength))
        End If

        'Separate body into lines, respecting the maximum line length and trying to divide along word boundaries
        Dim result = New List(Of String)()
        Dim ws = 0 'word start
        Dim ls = 0 'line start
        For Each we In body.IndexesOf(" "c).Append(body.Length) 'iterate over word endings
            Contract.Assert(ls <= ws)
            Contract.Assert(ws <= ls + maxLineLength + 1)
            Contract.Assume(ws <= we)
            Contract.Assume(we <= body.Length)

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

    <Pure()> <Extension()>
    Public Function StartingAt(clock As IClock, time As TimeSpan) As IClock
        Contract.Requires(clock IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IClock)() IsNot Nothing)
        Dim offset = time - clock.ElapsedTime
        Return New RelativeClock(clock, offset)
    End Function
    <Pure()> <Extension()>
    Public Function Stopped(clock As IClock) As IClock
        Contract.Requires(clock IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IClock)() IsNot Nothing)
        Dim r = New ManualClock()
        r.Advance(clock.ElapsedTime)
        Return r
    End Function

    <Pure()> <Extension()>
    <SuppressMessage("Microsoft.Contracts", "EnsuresInMethod-Contract.Result(Of Net.IPEndPoint)().Address Is address")>
    <SuppressMessage("Microsoft.Contracts", "EnsuresInMethod-Contract.Result(Of Net.IPEndPoint)().Port = port")>
    Public Function WithPort(address As Net.IPAddress, port As UShort) As Net.IPEndPoint
        Contract.Requires(address IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Net.IPEndPoint)() IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Net.IPEndPoint)().Address Is address)
        Contract.Ensures(Contract.Result(Of Net.IPEndPoint)().Port = port)
        Return New Net.IPEndPoint(address, port)
    End Function

    <Pure()>
    Public Function BuildDictionaryFromString(Of T)(text As String,
                                                    parser As Func(Of String, T),
                                                    pairDivider As String,
                                                    valueDivider As String) As IDictionary(Of InvariantString, T)
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
                Let key = pair.Substring(0, p).ToInvariant
                Let value = parser(pair.Substring(p + valueDivider.Length))
                ).ToDictionary(keySelector:=Function(e) e.key, elementSelector:=Function(e) e.value)
    End Function

    <Pure()> <Extension()>
    Public Function Times(timeSpan As TimeSpan, factor As Double) As TimeSpan
        Return New TimeSpan(CLng(timeSpan.Ticks * factor))
    End Function
    <Pure()> <Extension()>
    Public Function ToUValue(data As IEnumerable(Of Byte),
                             Optional byteOrder As ByteOrder = ByteOrder.LittleEndian) As UInt64
        Contract.Requires(data IsNot Nothing)
        If data.LazyCount > 8 Then Throw New ArgumentException("Too many bytes.", "data")
        Contract.Assume(8 - data.Count > 0)
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
    Public Function FindFileMatching(fileQuery As String, likeQuery As String, directory As String) As String
        Contract.Requires(fileQuery IsNot Nothing)
        Contract.Requires(likeQuery IsNot Nothing)
        Contract.Requires(directory IsNot Nothing)
        Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
        Dim result = FindFilesMatching(fileQuery, likeQuery, directory, 1).FirstOrDefault
        If result Is Nothing Then Throw New OperationFailedException("No matches.")
        Return result
    End Function

    <Extension()> <Pure()>
    Public Function EnumUInt32Includes(Of T)(value As T, [option] As UInt32) As Boolean
        Return (value.DynamicDirectCastTo(Of UInt32)() And [option]) = [option]
    End Function
    <Extension()> <Pure()>
    Public Function EnumIncludes(Of TEnum)(value As TEnum, [option] As TEnum) As Boolean
        Select Case [Enum].GetUnderlyingType(GetType(TEnum))
            Case GetType(SByte) : Return (value.DynamicDirectCastTo(Of SByte)() And [option].DynamicDirectCastTo(Of SByte)()) <> 0
            Case GetType(Int16) : Return (value.DynamicDirectCastTo(Of Int16)() And [option].DynamicDirectCastTo(Of Int16)()) <> 0
            Case GetType(Int32) : Return (value.DynamicDirectCastTo(Of Int32)() And [option].DynamicDirectCastTo(Of Int32)()) <> 0
            Case GetType(Int64) : Return (value.DynamicDirectCastTo(Of Int64)() And [option].DynamicDirectCastTo(Of Int64)()) <> 0
            Case GetType(Byte) : Return (value.DynamicDirectCastTo(Of Byte)() And [option].DynamicDirectCastTo(Of Byte)()) <> 0
            Case GetType(UInt16) : Return (value.DynamicDirectCastTo(Of UInt16)() And [option].DynamicDirectCastTo(Of UInt16)()) <> 0
            Case GetType(UInt32) : Return (value.DynamicDirectCastTo(Of UInt32)() And [option].DynamicDirectCastTo(Of UInt32)()) <> 0
            Case GetType(UInt64) : Return (value.DynamicDirectCastTo(Of UInt64)() And [option].DynamicDirectCastTo(Of UInt64)()) <> 0
            Case Else
                Throw New InvalidOperationException("{0} does not have a recognized underlying enum type.".Frmt(GetType(TEnum)))
        End Select
    End Function
    <Extension()> <Pure()>
    Public Function AssumeAny(Of T)(sequence As IEnumerable(Of T)) As IEnumerable(Of T)
        Contract.Requires(sequence IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IEnumerable(Of T))().Any())
        Contract.Ensures(Contract.Result(Of IEnumerable(Of T))() IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IEnumerable(Of T))() Is sequence)
        Contract.Assume(sequence.Any())
        Return sequence
    End Function
    <Extension()> <Pure()>
    Public Function EnumUInt32WithSet(Of T)(value As T, [option] As UInt32, include As Boolean) As T
        Dim v = value.DynamicDirectCastTo(Of UInt32)()
        v = If(include, v Or [option], v And Not [option])
        Return v.DynamicDirectCastTo(Of T)()
    End Function
    <Extension()> <Pure()>
    Public Function EnumWith(Of TEnum)(value As TEnum, [option] As TEnum) As TEnum
        Select Case [Enum].GetUnderlyingType(GetType(TEnum))
            Case GetType(SByte) : Return (value.DynamicDirectCastTo(Of SByte)() Or [option].DynamicDirectCastTo(Of SByte)()).DynamicDirectCastTo(Of TEnum)()
            Case GetType(Int16) : Return (value.DynamicDirectCastTo(Of Int16)() Or [option].DynamicDirectCastTo(Of Int16)()).DynamicDirectCastTo(Of TEnum)()
            Case GetType(Int32) : Return (value.DynamicDirectCastTo(Of Int32)() Or [option].DynamicDirectCastTo(Of Int32)()).DynamicDirectCastTo(Of TEnum)()
            Case GetType(Int64) : Return (value.DynamicDirectCastTo(Of Int64)() Or [option].DynamicDirectCastTo(Of Int64)()).DynamicDirectCastTo(Of TEnum)()
            Case GetType(Byte) : Return (value.DynamicDirectCastTo(Of Byte)() Or [option].DynamicDirectCastTo(Of Byte)()).DynamicDirectCastTo(Of TEnum)()
            Case GetType(UInt16) : Return (value.DynamicDirectCastTo(Of UInt16)() Or [option].DynamicDirectCastTo(Of UInt16)()).DynamicDirectCastTo(Of TEnum)()
            Case GetType(UInt32) : Return (value.DynamicDirectCastTo(Of UInt32)() Or [option].DynamicDirectCastTo(Of UInt32)()).DynamicDirectCastTo(Of TEnum)()
            Case GetType(UInt64) : Return (value.DynamicDirectCastTo(Of UInt64)() Or [option].DynamicDirectCastTo(Of UInt64)()).DynamicDirectCastTo(Of TEnum)()
            Case Else
                Throw New InvalidOperationException("{0} does not have a recognized underlying enum type.".Frmt(GetType(TEnum)))
        End Select
    End Function

    <Extension()> <Pure()>
    Public Function Summarize(ex As Exception) As String
        Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)

        If ex Is Nothing Then Return "Null Exception"

        Dim ax = TryCast(ex, AggregateException)
        If ax IsNot Nothing Then
            Contract.Assume(ax.InnerExceptions IsNot Nothing)
            Select Case ax.InnerExceptions.Count
                Case 0 : Return "Empty AggregateException"
                Case 1 : Return ax.InnerExceptions.Single().Summarize()
            End Select
            Return "{0} Exceptions Occured: {1}".Frmt(
                ax.InnerExceptions.Count,
                Environment.NewLine + ax.InnerExceptions.StringJoin(Environment.NewLine))
        End If

        Return "({0}) {1}".Frmt(ex.GetType.Name, ex.Message)
    End Function

    Public Function FindFilesMatching(fileQuery As String,
                                      likeQuery As InvariantString,
                                      directory As InvariantString,
                                      maxResults As Integer) As IList(Of String)
        Contract.Requires(fileQuery IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IList(Of String))() IsNot Nothing)

        If Not directory.EndsWith(IO.Path.DirectorySeparatorChar) AndAlso Not directory.EndsWith(IO.Path.AltDirectorySeparatorChar) Then
            directory += IO.Path.DirectorySeparatorChar
        End If

        'Separate directory and filename patterns
        fileQuery = fileQuery.Replace(IO.Path.AltDirectorySeparatorChar, IO.Path.DirectorySeparatorChar)
        Dim dirQuery = "*".ToInvariant
        If fileQuery.Contains(IO.Path.DirectorySeparatorChar) Then
            Dim words = fileQuery.Split(IO.Path.DirectorySeparatorChar)
            Dim filePattern = words.Last
            Contract.Assume(filePattern IsNot Nothing)
            Contract.Assume(fileQuery.Length > filePattern.Length)
            dirQuery = fileQuery.Substring(0, fileQuery.Length - filePattern.Length) + "*"
            fileQuery = "*" + filePattern
        End If

        'Check files in folder
        Return (From filepath In IO.Directory.GetFiles(directory, fileQuery, IO.SearchOption.AllDirectories)
                Select relativePath = filepath.Substring(directory.Length)
                Where relativePath Like likeQuery
                Where relativePath Like dirQuery
                ).Take(maxResults).ToList
    End Function
    Public Function GetDataFolderPath(subfolder As String) As String
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

    <Pure()> <Extension()>
    Public Function ToUnsignedBigInteger(digits As IEnumerable(Of Byte),
                                         base As UInt32) As BigInteger
        Contract.Requires(digits IsNot Nothing)
        Contract.Requires(base >= 2)
        Contract.Requires(base <= 256)
        Contract.Ensures(Contract.Result(Of BigInteger)() >= 0)
        Dim result = digits.Reverse.Aggregate(New BigInteger, Function(acc, e) acc * base + e)
        Contract.Assume(result >= 0)
        Return result
    End Function
    <Pure()> <Extension()>
    Public Function UnsignedDigits(value As BigInteger,
                                   base As UInt32) As IEnumerable(Of Byte)
        Contract.Requires(value >= 0)
        Contract.Requires(base >= 2)
        Contract.Requires(base <= 256)
        Contract.Ensures(Contract.Result(Of IEnumerable(Of Byte))() IsNot Nothing)
        Return Iterator Function()
                   Dim numerator = value
                   While numerator <> 0
                       Dim remainder = [Default](Of BigInteger)()
                       numerator = BigInteger.DivRem(numerator, base, remainder)
                       Yield CByte(remainder)
                   End While
               End Function().AssumeNotNull()
    End Function
    ''' <summary>
    ''' Determines the little-endian digits in one base from the little-endian digits in another base.
    ''' </summary>
    <Pure()> <Extension()>
    Public Function ConvertFromBaseToBase(digits As IEnumerable(Of Byte),
                                          inputBase As UInteger,
                                          outputBase As UInteger) As IRist(Of Byte)
        Contract.Requires(digits IsNot Nothing)
        Contract.Requires(inputBase >= 2)
        Contract.Requires(inputBase <= 256)
        Contract.Requires(outputBase >= 2)
        Contract.Requires(outputBase <= 256)
        Contract.Ensures(Contract.Result(Of IRist(Of Byte))() IsNot Nothing)
        Return digits.ToUnsignedBigInteger(inputBase).UnsignedDigits(outputBase).ToRist
    End Function
    ''' <summary>
    ''' Determines a list starting with the elements of the given list but padded with default values to meet a minimum length.
    ''' </summary>
    <Pure()> <Extension()>
    Public Function PaddedTo(Of T)(this As IRist(Of T),
                                   minimumLength As Integer,
                                   Optional paddingValue As T = Nothing) As IRist(Of T)
        Contract.Requires(this IsNot Nothing)
        Contract.Requires(minimumLength >= 0)
        Contract.Ensures(Contract.Result(Of IRist(Of T))() IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IRist(Of T))().Count = Math.Max(this.Count, minimumLength))
        If this.Count >= minimumLength Then Return this
        Dim result = this.Concat(paddingValue.Repeated(minimumLength - this.Count)).ToRist
        Contract.Assume(result.Count = Math.Max(this.Count, minimumLength))
        Return result
    End Function

    <Pure()> <Extension()>
    Public Function ToUnsignedBigInteger(digits As IEnumerable(Of Byte)) As BigInteger
        Contract.Requires(digits IsNot Nothing)
        Contract.Ensures(Contract.Result(Of BigInteger)() >= 0)
        Return digits.ToArray.ToUnsignedBigInteger
    End Function
    <Pure()> <Extension()>
    Public Function ToUnsignedBigInteger(digits As Byte()) As BigInteger
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
    Public Function ToUnsignedBytes(value As BigInteger) As IRist(Of Byte)
        Contract.Requires(value >= 0)
        Contract.Ensures(Contract.Result(Of IRist(Of Byte))() IsNot Nothing)
        Dim result = value.ToByteArray().AssumeNotNull().AsRist()
        If result.Count > 0 AndAlso result.Last() = 0 Then
            Return result.SkipLastExact(1)
        Else
            Return result
        End If
    End Function
    <Pure()> <Extension()>
    Public Function AssumeNotNull(Of T)(arg As T) As T
        Contract.Ensures(Contract.Result(Of T)() IsNot Nothing)
        Contract.Assume(arg IsNot Nothing)
        Return arg
    End Function

    '''<summary>Determines the SHA-1 hash of a sequence of bytes.</summary>
    <Extension()> <Pure()>
    Public Function SHA1(data As IEnumerable(Of Byte)) As IRist(Of Byte)
        Contract.Requires(data IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IRist(Of Byte))() IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IRist(Of Byte))().Count = 20)
        Using sha = New System.Security.Cryptography.SHA1Managed()
            Dim result = sha.ComputeHash(data.AsReadableStream.AsStream).AsRist()
            Contract.Assume(result.Count = 20)
            Return result
        End Using
    End Function

    Public Function CRC32Table(Optional poly As UInteger = &H4C11DB7,
                               Optional polyAlreadyReversed As Boolean = False) As UInt32()
        'Reverse the polynomial
        If Not polyAlreadyReversed Then
            poly = poly.Bits.Aggregate(0UI, Function(acc, bit) (acc << 1) + If(bit, 1UI, 0UI))
        End If

        'Precompute the combined XOR masks for each byte
        Dim xorTable = From i In 256UI.Range
                       Select 8.Range.Aggregate(CUInt(i),
                                                Function(acc, e)
                                                    If CBool(acc And &H1) Then
                                                        Return (acc >> 1) Xor poly
                                                    Else
                                                        Return acc >> 1
                                                    End If
                                                End Function)

        Return xorTable.ToArray
    End Function
    '''<summary>Reads all remaining data from a stream, computing its CRC32 checksum.</summary>
    <Extension()>
    Public Function ReadCRC32(data As IReadableStream,
                              Optional poly As UInteger = &H4C11DB7,
                              Optional polyAlreadyReversed As Boolean = False) As UInteger
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
    '''<summary>Determines the CRC32 checksum of a sequence of bytes.</summary>
    <Extension()> <Pure()>
    Public Function CRC32(data As IEnumerable(Of Byte),
                          Optional poly As UInteger = &H4C11DB7,
                          Optional polyAlreadyReversed As Boolean = False) As UInteger
        Contract.Requires(data IsNot Nothing)
        Return data.AsReadableStream.ReadCRC32(poly, polyAlreadyReversed)
    End Function

    '''<summary>Converts versus strings to a list of the team sizes (eg. 1v3v2 -> {1,3,2}).</summary>
    Public Function TeamVersusStringToTeamSizes(value As String) As IRist(Of Integer)
        Contract.Requires(value IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IList(Of Integer))() IsNot Nothing)

        'parse numbers between 'v's
        Return (From e In value.ToUpperInvariant.Split("V"c)
                Select CInt(Byte.Parse(e, NumberStyles.Integer, CultureInfo.InvariantCulture))
                ).ToRist
    End Function

    Public Sub CheckIOData(clause As Boolean, message As String)
        Contract.Requires(message IsNot Nothing)
        Contract.Ensures(clause)
        Contract.EnsuresOnThrow(Of IO.InvalidDataException)(Not clause)
        If Not clause Then Throw New IO.InvalidDataException(message)
    End Sub

    <Extension()>
    Public Function PanelWithControls(controls As IEnumerable(Of Control),
                                      Optional leftToRight As Boolean = False,
                                      Optional spacing As Int32 = 3,
                                      Optional margin As Int32 = 3,
                                      Optional borderStyle As BorderStyle = BorderStyle.None) As Panel
        Contract.Requires(controls IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Panel)() IsNot Nothing)

        Dim result = New Panel()
        result.Controls.AddRange(controls.ToArray)
        LayoutPanel(result, leftToRight, spacing, margin, borderStyle)
        Return result
    End Function
    <Extension()>
    Public Sub LayoutPanel(panel As Panel,
                           Optional leftToRight As Boolean = False,
                           Optional spacing As Int32 = 3,
                           Optional margin As Int32 = 3,
                           Optional borderStyle As BorderStyle = BorderStyle.None)
        Contract.Requires(panel IsNot Nothing)

        panel.BorderStyle = borderStyle
        If panel.Controls.Count = 0 Then
            panel.Height = margin * 2
            panel.Width = margin * 2
            Return
        End If

        'Position controls
        panel.Controls(0).AssumeNotNull.Top = margin
        panel.Controls(0).AssumeNotNull.Left = margin
        For Each i In panel.Controls.Count.Range.Skip(1)
            Contract.Assume(i > 0)
            Contract.Assume(i < panel.Controls.Count)
            Dim c = panel.Controls(i).AssumeNotNull
            Dim p = panel.Controls(i - 1).AssumeNotNull
            If leftToRight Then
                c.Left = p.Right + spacing
                c.Top = margin
            Else
                c.Left = margin
                c.Top = p.Bottom + spacing
            End If
        Next i
        panel.Height = panel.Controls(panel.Controls.Count - 1).AssumeNotNull.Bottom + margin

        'Size controls
        Dim maxWidth = 0
        For Each c As Control In panel.Controls
            Contract.Assume(c IsNot Nothing)
            If leftToRight Then
                If panel.Width < c.Right + margin Then
                    panel.Width = c.Right + margin
                End If
                If panel.Height < c.Bottom + margin Then
                    panel.Height = c.Bottom + margin
                End If
            Else
                c.Width = panel.Width - margin * 2
                If c.MaximumSize.Width = 0 Then
                    c.Anchor = AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Top
                End If
            End If
            If c.Width > maxWidth Then maxWidth = c.Width
        Next c
        maxWidth += margin * 2

        'Shrink if all subcontrols are smaller
        If Not leftToRight AndAlso maxWidth < panel.Width Then
            panel.Width = maxWidth
        End If
    End Sub

    <Extension()>
    Public Function ReplaceDirectorySeparatorWith(path As String, separator As Char) As String
        Contract.Requires(path IsNot Nothing)
        Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
        Return path.ToString.
                    Replace(IO.Path.DirectorySeparatorChar, separator).
                    Replace(IO.Path.AltDirectorySeparatorChar, separator)
    End Function

    ''' <summary>
    ''' Determines the relative path to a file from a directory.
    ''' Returns null if the file is not nested within the directory.
    ''' </summary>
    Public Function FilePathRelativeToDirectoryPath(path As String, baseDirectoryPath As String) As String
        Contract.Requires(path IsNot Nothing)
        Contract.Requires(baseDirectoryPath IsNot Nothing)

        Dim baseDir = New IO.DirectoryInfo(path)
        Dim fileInfo = New IO.FileInfo(path)
        Dim result = New List(Of String) From {fileInfo.Name}

        Dim parentDir = fileInfo.Directory
        Do
            If parentDir Is Nothing Then
                Return Nothing 'not nested
            ElseIf parentDir.Equals(baseDir) Then
                Return result.StringJoin(IO.Path.DirectorySeparatorChar) 'nested
            End If
            result.Add(parentDir.Name)
            parentDir = parentDir.Parent
        Loop
    End Function

    <Pure()> <Extension()>
    <SuppressMessage("Microsoft.Contracts", "EnsuresInMethod-Contract.Result(Of Integer?)() Is Nothing OrElse Contract.Result(Of Integer?)().Value >= startIndex + substring.Length")>
    <SuppressMessage("Microsoft.Contracts", "EnsuresInMethod-Contract.Result(Of Integer?)() Is Nothing OrElse Contract.Result(Of Integer?)().Value <= text.Length")>
    Public Function IndexAfter(text As String,
                               substring As String,
                               startIndex As Integer,
                               comparisonType As StringComparison) As Integer?
        Contract.Requires(text IsNot Nothing)
        Contract.Requires(substring IsNot Nothing)
        Contract.Requires(startIndex >= 0)
        Contract.Requires(startIndex <= text.Length)
        Contract.Ensures(Contract.Result(Of Integer?)() Is Nothing OrElse Contract.Result(Of Integer?)().Value >= startIndex + substring.Length)
        Contract.Ensures(Contract.Result(Of Integer?)() Is Nothing OrElse Contract.Result(Of Integer?)().Value <= text.Length)
        Dim result = text.IndexOf(substring, startIndex, comparisonType)
        If result < 0 Then Return Nothing
        Return result + substring.Length()
    End Function
End Module
