Imports System.Net.NetworkInformation
Imports System.Runtime.CompilerServices
Imports System.IO.Path
Imports System.Text
Imports System.Net

'''<summary>A smattering of functions and other stuff that hasn't been placed in more reasonable groups yet.</summary>
Public Module Common
#Region "UIntegers"
    '''<summary>Unsafe CUInt. Ignores overflows and underflows.</summary>
    Public Function uCUInt(ByVal n As Long) As UInteger
        Return CUInt(n And UInteger.MaxValue)
    End Function
    '''<summary>Unsafe CUInt. Ignores overflows and underflows.</summary>
    Public Function uCUInt(ByVal n As ULong) As UInteger
        Return CUInt(n And UInteger.MaxValue)
    End Function
    Public Function uCInt(ByVal u As UInteger) As Integer
        If u <= Integer.MaxValue Then Return CInt(u)
        Return CInt(u - &H100000000)
    End Function
    Public Function uCByte(ByVal n As Integer) As Byte
        Return CByte(n And Byte.MaxValue)
    End Function
    Public Function ShiftRotateLeft(ByVal i As Integer, ByVal s As Integer) As Integer
        Return uCInt(ShiftRotateLeft(uCUInt(i), s))
    End Function
    Public Function ShiftRotateRight(ByVal i As Integer, ByVal s As Integer) As Integer
        Return ShiftRotateLeft(i, -s)
    End Function
    Public Function ShiftRotateLeft(ByVal u As UInteger, ByVal s As Integer) As UInteger
        s = s And &H1F
        Return (u << s) Or (u >> (32 - s))
    End Function
    Public Function ShiftRotateRight(ByVal u As UInteger, ByVal s As Integer) As UInteger
        Return ShiftRotateLeft(u, -s)
    End Function
    Public Function ReverseByteEndian(ByVal val As UInteger) As UInteger
        Dim u2 As UInteger
        For i = 0 To 3
            u2 <<= 8
            u2 = u2 Or (val And CUInt(&HFF))
            val >>= 8
        Next i
        Return u2
    End Function
    Public Function ReverseByteEndian(ByVal i As Integer) As Integer
        Dim u = CLng(ReverseByteEndian(uCUInt(i)))
        If u > Integer.MaxValue Then u -= &H100000000
        Return CInt(u)
    End Function
    Public Function ReverseByteEndian(ByVal u As ULong) As ULong
        Dim u2 As ULong
        For i = 0 To 7
            u2 <<= 8
            u2 = u2 Or (u And CULng(&HFF))
            u >>= 8
        Next i
        Return u2
    End Function

    Public Function uSum(ByVal ParamArray nn() As Integer) As Integer
        Dim n = 0
        For Each u In nn
            n = uAdd(n, u)
        Next u
        Return n
    End Function
    Public Function uAdd(ByVal N1 As Integer, ByVal N2 As Integer) As Integer
        Dim N = CLng(N1) + CLng(N2)
        If N > Integer.MaxValue Then N -= &H100000000
        If N < Integer.MinValue Then N += &H100000000
        Return CInt(N)
    End Function
    Public Function uSum(ByVal ParamArray nn() As UInteger) As UInteger
        Dim n = CUInt(0)
        For Each u In nn
            n = uAdd(n, u)
        Next u
        Return n
    End Function
    Public Function uAdd(ByVal N1 As UInteger, ByVal N2 As UInteger) As UInteger
        Dim N = CULng(N1) + CULng(N2)
        If N > UInteger.MaxValue Then N -= CULng(&H100000000)
        Return CUInt(N)
    End Function

    Public Function uCUShort(ByVal s As Short) As UShort
        Return CUShort(If(s >= 0, s, s + &H10000))
    End Function
    Public Function uCShort(ByVal s As UShort) As Short
        Return CShort(If(s > Short.MaxValue, s, s - &H10000))
    End Function
#End Region

#Region "Numbers"
    '''<summary>Returns the smallest multiple of n that is not less than i. Formally: min {x in Z | x = 0 (mod n), x >= i}</summary>
    Public Function modCeiling(ByVal i As Integer, ByVal n As Integer) As Integer
        If i Mod n = 0 Then Return i
        If i < 0 Then Return (i \ n) * n
        Return (i \ n + 1) * n
    End Function

    Public Function alignedReadCount(ByVal count As Integer, ByVal numBufferedIn As Integer, ByVal numBufferedOut As Integer, ByVal alignModulo As Integer) As Integer
        Return Math.Max(0, modCeiling(count - numBufferedOut, alignModulo) - numBufferedIn)
    End Function

    Public Function TickCountDelta(ByVal t_now As Long, ByVal t_then As Long) As UInteger
        Return uCUInt(t_now - t_then)
    End Function

    <Extension()> Public Function between(Of N As IComparable)(ByVal v1 As N, ByVal v2 As N, ByVal v3 As N) As N
        'recursive sort
        If v1.CompareTo(v2) < 0 Then Return between(v2, v1, v3)
        If v2.CompareTo(v3) < 0 Then Return between(v1, v3, v2)
        'median
        Return v2
    End Function
#End Region

#Region "Strings"
    Public Function padded(ByVal s As String, ByVal min_chars As Integer, Optional ByVal pad_char As Char = " "c) As String
        If s.Length < min_chars Then
            s += New String(pad_char, min_chars - s.Length)
        End If
        Return s
    End Function
    Public Function indent(ByVal paragraph As String, Optional ByVal prefix As String = vbTab) As String
        If Not (paragraph IsNot Nothing) Then Throw New ArgumentException()
        If Not (prefix IsNot Nothing) Then Throw New ArgumentException()

        Return prefix + paragraph.Replace(Environment.NewLine, Environment.NewLine + prefix)
    End Function
    <Extension()> Public Function frmt(ByVal s As String, ByVal ParamArray args() As Object) As String
        If Not (s IsNot Nothing) Then Throw New ArgumentException()
        If Not (args IsNot Nothing) Then Throw New ArgumentException()

        Return String.Format(s, args)
    End Function
    Public Function Assign(Of T)(ByRef x As T, ByVal v As T) As Boolean
        x = v
        Return True
    End Function
#End Region

    Public Function MakePair(Of T1, T2)(ByVal arg1 As T1, ByVal arg2 As T2) As Pair(Of T1, T2)
        Return New Pair(Of T1, T2)(arg1, arg2)
    End Function

    Public Function FutureConnectTo(ByVal host As String, ByVal port As UShort) As IFuture(Of Outcome(Of Sockets.TcpClient))
        Dim f = New Future(Of Outcome(Of Sockets.TcpClient))
        Try
            Dim client = New Sockets.TcpClient
            client.BeginConnect(host,
                                port,
                                Sub(ar)
                                    Try
                                        client.EndConnect(ar)
                                        f.setValue(successVal(client, "Connected"))
                                    Catch e As Exception
                                        f.setValue(failure("Failed to connect: {0}".frmt(e.Message)))
                                    End Try
                                End Sub,
                                Nothing)
        Catch e As Exception
            f.setValue(failure("Failed to connect: {0}".frmt(e.Message)))
        End Try
        Return f
    End Function

#Region "Strings Extra"
    Public Function breakQuotedWords(ByVal text As String) As List(Of String)
        Dim quoted_words As New List(Of String)
        If text = "" Then Return quoted_words
        Dim cur_quoted_word As String = Nothing
        For Each word As String In text.Split(" "c)
            If word = "" Then Continue For
            If cur_quoted_word Is Nothing Then
                If word(0) = """"c Then
                    If word(word.Length - 1) = """"c Then '[start and end of quoted word]
                        quoted_words.Add(word.Substring(1, word.Length - 2))
                    Else '[start of quoted word]
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
    Public Function mendQuotedWords(ByVal words As IList(Of String), Optional ByVal skip As Integer = 0) As String
        Dim sentence As String = ""
        For Each word As String In words
            If skip > 0 Then skip -= 1 : Continue For
            sentence += """" + word + """ "
        Next word
        Return sentence.Trim()
    End Function
    Public Function DictStrUInt(ByVal text As String, Optional ByVal pair_divider As String = ";"c, Optional ByVal value_divider As String = "="c) As Dictionary(Of String, UInteger)
        Return DictStrT(text, Function(x) UInteger.Parse(x), pair_divider, value_divider)
    End Function
    Public Function DictStrStr(ByVal text As String, Optional ByVal pair_divider As String = ";"c, Optional ByVal value_divider As String = "="c) As Dictionary(Of String, String)
        Return DictStrT(text, Function(x) x, pair_divider, value_divider)
    End Function
    Private Function DictStrT(Of T)(ByVal text As String, ByVal f As Func(Of String, T), Optional ByVal pair_divider As String = ";"c, Optional ByVal value_divider As String = "="c) As Dictionary(Of String, T)
        Dim d As New Dictionary(Of String, T)
        Dim pd = New String() {pair_divider}
        Dim vd = New String() {value_divider}
        For Each pair In text.Split(pd, StringSplitOptions.RemoveEmptyEntries)
            Dim args = pair.Split(vd, StringSplitOptions.None)
            If args.Count < 2 Then Continue For
            d(args(0)) = f(pair.Substring(args(0).Length + 1))
        Next pair
        Return d
    End Function
#End Region

#Region "Enums"
    Public Function EnumValues(Of T)() As IEnumerable(Of T)

        Dim out = CType([Enum].GetValues(GetType(T)), IEnumerable(Of T))
        Return out
    End Function
    Public Function EnumNames(Of T)() As IEnumerable(Of String)

        Dim out = From x In EnumValues(Of T)() Select (x.ToString())
        Return out
    End Function
    Public Function EnumTryParse(Of T)(ByVal value As String, ByVal ignore_case As Boolean, ByRef ret As T) As Boolean
        For Each e In EnumValues(Of T)()
            If String.Compare(value, e.ToString(), ignore_case) = 0 Then
                ret = e
                Return True
            End If
        Next e
        Return False
    End Function
    Public Function IsEnumValid(Of T)(ByVal val As T) As Boolean
        Return EnumValues(Of T).Contains(val)
    End Function
#End Region

#Region "Arrays"
    Public Function ArraysEqual(Of T As IComparable(Of T))(ByVal array1() As T, ByVal array2() As T) As Boolean
        If Not (array1 IsNot Nothing) Then Throw New ArgumentException()
        If Not (array2 IsNot Nothing) Then Throw New ArgumentException()
        If array1.Length <> array2.Length Then Return False
        For i = 0 To array1.Length - 1
            If array1(i).CompareTo(array2(i)) <> 0 Then Return False
        Next i
        Return True
    End Function
    <Extension()> Public Function subArray(Of T)(ByVal baseArray As T(), ByVal startIndex As Integer, Optional ByVal max_length As Integer = Integer.MaxValue) As T()
        If Not (baseArray IsNot Nothing) Then Throw New ArgumentException()
        If Not (startIndex >= 0) Then Throw New ArgumentException()
        If Not (max_length >= 0) Then Throw New ArgumentException()


        Dim subArrayData() As T = New T() {}

        If max_length > baseArray.Length - startIndex Then max_length = baseArray.Length - startIndex
        If max_length = 0 Then Return subArrayData

        ReDim subArrayData(0 To max_length - 1)
        For i As Integer = 0 To subArrayData.Length - 1
            subArrayData(i) = baseArray(i + startIndex)
        Next i
        Return subArrayData
    End Function
    Public Function concat(Of T)(ByVal arrays As IEnumerable(Of T())) As T()
        If arrays Is Nothing Then Throw New ArgumentNullException("arrays")

        Dim n = 0
        For Each array In arrays
            If array Is Nothing Then Continue For
            n += array.Length
        Next array

        Dim flattened_array(0 To n - 1) As T
        Dim i = 0
        For Each array In arrays
            If array Is Nothing Then Continue For
            System.Array.Copy(array, 0, flattened_array, i, array.Length)
            i += array.Length
        Next array

        Return flattened_array
    End Function
    Public Function concat(Of T)(ByVal ParamArray baseArrays As T()()) As T()
        Return concat(CType(baseArrays, IEnumerable(Of T())))
    End Function
    <Extension()> Public Function reversed(Of T)(ByVal baseArray As T()) As T()
        If Not (baseArray IsNot Nothing) Then Throw New ArgumentException()

        Return CType(baseArray, IEnumerable(Of T)).Reverse().ToArray()
    End Function

    Public Function cut(Of T)(ByVal baseArray As T(), ByVal ParamArray cutLengths As Integer()) As T()()
        'Integrate
        For i As Integer = 1 To cutLengths.Length - 1
            cutLengths(i) += cutLengths(i - 1)
        Next i
        'Delegate
        Return divide(baseArray, cutLengths)
    End Function

    Public Function divide(Of T)(ByVal baseArray As T(), ByVal ParamArray partitionIndices As Integer()) As T()()
        Dim subArrays As T()()
        Dim n As Integer = 0
        Dim lastIndex As Integer = 0

        ReDim subArrays(0 To partitionIndices.Length)
        For i As Integer = 0 To partitionIndices.Length
            'Move to next partition
            lastIndex += n
            If i < partitionIndices.Length Then
                n = partitionIndices(i) - lastIndex
            Else
                n = baseArray.Length - lastIndex
            End If
            If lastIndex + n > baseArray.Length Then Throw New ArgumentOutOfRangeException("Partition index past end of array.")
            If n < 0 Then Throw New ArgumentOutOfRangeException("Negative partition length.")
            If n = 0 Then subArrays(i) = New T() {} : Continue For

            'Populate partition
            ReDim subArrays(i)(0 To n - 1)
            For j As Integer = 0 To n - 1
                subArrays(i)(j) = baseArray(lastIndex + j)
            Next j
        Next i

        Return subArrays
    End Function
#End Region

#Region "Filepaths"
    Public Function getFileNameSlash(ByVal s As String) As String
        Dim ss() As String = s.Split("\"c, "/"c)
        Return ss(ss.Length - 1)
    End Function
    Public Function findFileMatching(ByVal search_pattern As String, ByVal like_pattern As String, ByVal directory As String) As Outcome(Of String)
        Dim out = findFilesMatching(search_pattern, like_pattern, directory, 1)
        If out.val.Count = 0 Then
            Return failure(out.message)
        End If
        Return successVal(out.val(0), "{0} matched {1}".frmt(search_pattern, out.val(0)))
    End Function
    Public Function findFilesMatching(ByVal search_pattern As String, ByVal like_pattern As String, ByVal directory As String, ByVal max_results As Integer) As Outcome(Of List(Of String))
        Dim matches As New List(Of String)

        Try
            'Normalize input
            directory = directory.Replace(AltDirectorySeparatorChar, DirectorySeparatorChar)
            If directory(directory.Length - 1) <> DirectorySeparatorChar Then
                directory += DirectorySeparatorChar
            End If

            'Separate directory and filename patterns
            search_pattern = search_pattern.Replace(AltDirectorySeparatorChar, DirectorySeparatorChar)
            Dim dir_pattern = "*"
            If search_pattern.Contains(DirectorySeparatorChar) Then
                Dim words = search_pattern.Split(DirectorySeparatorChar)
                Dim file_pattern = words(words.Length - 1)
                dir_pattern = search_pattern.Substring(0, search_pattern.Length - file_pattern.Length) + "*"
                search_pattern = "*" + file_pattern
            End If

            'patterns are not case-sensitive
            dir_pattern = dir_pattern.ToLower()
            like_pattern = like_pattern.ToLower

            'Check files in folder
            For Each filename In IO.Directory.GetFiles(directory, search_pattern, IO.SearchOption.AllDirectories)
                filename = filename.Substring(directory.Length)
                If filename.ToLower() Like like_pattern AndAlso filename.ToLower Like dir_pattern Then
                    matches.Add(filename)
                    If matches.Count >= max_results Then Exit For
                End If
            Next filename

            Return successVal(matches, "Matched {0} files.".frmt(matches.Count))
        Catch e As Exception
            Return failureVal(matches, "Matched {0} files before hitting error: {1}.".frmt(matches.Count, e.Message))
        End Try
    End Function
#End Region

#Region "Linq"
    '''<summary>Determines if a sequence has no elements.</summary>
    <Extension()> Public Function None(Of T)(ByVal e As IEnumerable(Of T)) As Boolean
        Return Not e.Any()
    End Function
#End Region

#Region "Contracts"
    Public Sub ContractPositive(ByVal i As Integer, Optional ByVal param_name As String = Nothing)
        If i <= 0 Then
            Throw New ArgumentOutOfRangeException(param_name, "Value must be positive.")
        End If
    End Sub
    Public Sub ContractNonNegative(ByVal i As Integer, Optional ByVal param_name As String = Nothing)
        If i < 0 Then
            Throw New ArgumentOutOfRangeException(param_name, "Value must be non-negative.")
        End If
    End Sub
    Public Function ContractNotNull(Of T)(ByVal e As T, Optional ByVal name As String = Nothing) As T
        If e Is Nothing Then Throw New ArgumentNullException(name)
        Return e
    End Function
#End Region


    Public Sub Swap(Of T)(ByRef v1 As T, ByRef v2 As T)
        Dim vt = v1
        v1 = v2
        v2 = vt
    End Sub

    <Extension()> Public Function readBlock(ByVal this As IO.Stream, ByRef length As Integer) As Byte()
        Dim buffer(0 To length - 1) As Byte
        length = this.Read(buffer, 0, length)
        Return buffer
    End Function

    <Extension()> Public Function list_range(Of T)(ByVal L As IList(Of T), Optional ByVal start As Integer = 0, Optional ByVal [step] As Integer = 1) As IEnumerable(Of Integer)
        Return int_range(start, L.Count - 1, [step])
    End Function
    Public Function int_range(ByVal start As Integer, ByVal [end] As Integer, ByVal [step] As Integer) As IEnumerable(Of Integer)
        If [step] < 0 Then Return From x In int_range(-start, -[end], -[step]) Select -x
        Return From x In Enumerable.Range(0, ([end] - start) \ [step]) Select start + x * [step]
    End Function

    Private cached_external_ip As Byte() = Nothing
    Private cached_internal_ip As Byte() = Nothing
    Public Sub CacheIPAddresses()
        'Internal IP
        Dim addr = (From nic In NetworkInterface.GetAllNetworkInterfaces
                         Where nic.Supports(NetworkInterfaceComponent.IPv4)
                         Select a = (From address In nic.GetIPProperties.UnicastAddresses
                                     Where address.Address.AddressFamily = Net.Sockets.AddressFamily.InterNetwork
                                     Where address.Address.ToString <> "127.0.0.1").FirstOrDefault()
                         Where a IsNot Nothing).FirstOrDefault()
        If addr IsNot Nothing Then
            cached_internal_ip = addr.Address.GetAddressBytes
        Else
            cached_internal_ip = New Byte() {127, 0, 0, 1}
        End If

        'External IP
        ThreadedAction(
            Sub()
                Dim utf8 = New UTF8Encoding()
                Dim webClient = New WebClient()
                Dim externalIp = utf8.GetString(webClient.DownloadData("http://whatismyip.com/automation/n09230945.asp"))
                If externalIp.Length < 7 OrElse externalIp.Length > 15 Then  Return  'not correct length for style (#.#.#.# to ###.###.###.###)
                Dim words = externalIp.Split("."c)
                If words.Length <> 4 OrElse (From word In words Where Not Byte.TryParse(word, 0)).Any Then  Return
                cached_external_ip = (From word In words Select Byte.Parse(word)).ToArray()
            End Sub,
            "CacheExternalIP"
        )
    End Sub

    Public Function GetIpAddressBytes(ByVal external As Boolean) As Byte()
        If external AndAlso cached_external_ip IsNot Nothing Then
            Return cached_external_ip
        Else
            Return cached_internal_ip
        End If
    End Function

    Public Function streamBytes(ByVal s As IO.Stream) As Byte()
        Dim m = 1024
        Dim bb(0 To m - 1) As Byte
        Dim c = 0
        Do
            Dim n = s.Read(bb, c, m - c)
            c += n
            If c <> m Then Exit Do
            m *= 2
            ReDim Preserve bb(0 To m - 1)
        Loop
        ReDim Preserve bb(0 To c - 1)
        Return bb
    End Function
    Public Function reduce(Of T)(ByVal L As IEnumerable(Of T), ByVal reduction As Func(Of T, T, T)) As T
        Dim acc As T = Nothing
        For Each e In L
            acc = reduction(acc, e)
        Next e
        Return acc
    End Function
    Public Function setting_war3path() As String
        Return My.Settings.war3path
    End Function
    Public Function setting_mappath() As String
        Return My.Settings.mapPath
    End Function

    Public Function GetDataFolderPath(ByVal sub_folder As String) As String
        Dim folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        Try
            folder += IO.Path.DirectorySeparatorChar + "HostBot"
            If Not IO.Directory.Exists(folder) Then IO.Directory.CreateDirectory(folder)
            folder += IO.Path.DirectorySeparatorChar + sub_folder
            If Not IO.Directory.Exists(folder) Then IO.Directory.CreateDirectory(folder)
            folder += IO.Path.DirectorySeparatorChar
            Return folder
        Catch e As Exception
            Logging.logUnexpectedException("Error getting folder My Documents\HostBot\{0}.".frmt(sub_folder), e)
            Throw
        End Try
    End Function

    <Extension()> Public Function MaxPair(Of T, C As IComparable)(ByVal sequence As IEnumerable(Of T),
                                                                  ByVal transformation As Func(Of T, C),
                                                                  ByRef out_element As T,
                                                                  ByRef out_transformation As C) As Boolean
        Dim any = False
        Dim max_element = out_element
        Dim max_transformation = out_transformation

        For Each e In sequence
            Dim f = transformation(e)
            If Not any OrElse (f.CompareTo(max_transformation) > 0) Then
                max_element = e
                max_transformation = f
            End If
            any = True
        Next e

        If any Then
            out_element = max_element
            out_transformation = max_transformation
        End If
        Return any
    End Function
    <Extension()> Public Function Max(Of T)(ByVal sequence As IEnumerable(Of T), ByVal comparator As Func(Of T, T, Integer)) As T
        Dim any = False
        Dim max_element As T = Nothing

        For Each e In sequence
            If Not any OrElse comparator(max_element, e) < 0 Then
                max_element = e
            End If
        Next e

        Return max_element
    End Function
End Module

Public Class OperationFailedException
    Inherits Exception
    Public Sub New(Optional ByVal message As String = Nothing, Optional ByVal inner_exception As Exception = Nothing)
        MyBase.New(message, inner_exception)
    End Sub
End Class
Public Class UnreachableStateException
    Inherits Exception
    Public Sub New(Optional ByVal message As String = Nothing, Optional ByVal inner_exception As Exception = Nothing)
        MyBase.New(message, inner_exception)
    End Sub
End Class
Public Class Pair(Of T1, T2)
    Public ReadOnly v1 As T1
    Public ReadOnly v2 As T2
    Public Sub New(ByVal v1 As T1, ByVal v2 As T2)
        Me.v1 = v1
        Me.v2 = v2
    End Sub
    Public Overrides Function Equals(ByVal obj As Object) As Boolean
        If TypeOf obj Is Pair(Of T1, T2) Then
            Dim t = CType(obj, Pair(Of T1, T2))
            Return t.v1.Equals(v1) AndAlso t.v2.Equals(v2)
        End If
        Return False
    End Function
    Public Overrides Function GetHashCode() As Integer
        Return v1.GetHashCode Xor v2.GetHashCode
    End Function
End Class
Public Class Triplet(Of T1, T2, T3)
    Public ReadOnly v1 As T1
    Public ReadOnly v2 As T2
    Public ReadOnly v3 As T3
    Public Sub New(ByVal v1 As T1, ByVal v2 As T2, ByVal v3 As T3)
        Me.v1 = v1
        Me.v2 = v2
        Me.v3 = v3
    End Sub
    Public Overrides Function Equals(ByVal obj As Object) As Boolean
        If TypeOf obj Is Triplet(Of T1, T2, T3) Then
            Dim t = CType(obj, Triplet(Of T1, T2, T3))
            Return t.v1.Equals(v1) AndAlso t.v2.Equals(v2) AndAlso t.v3.equals(v3)
        End If
        Return False
    End Function
    Public Overrides Function GetHashCode() As Integer
        Return v1.GetHashCode Xor v2.GetHashCode Xor v3.GetHashCode
    End Function
End Class

'Public Class DeadManSwitch(Of T)
'    Implements IDisposable

'    Private WithEvents timer As Timers.Timer
'    Private ReadOnly lock As New Object()
'    Public ReadOnly arg As T
'    Public Event Fired(ByVal sender As DeadManSwitch(Of T))

'    Public Sub New(ByVal period As TimeSpan, ByVal arg As T)
'        Me.timer = New Timers.Timer(period.TotalMilliseconds)
'        Me.arg = arg
'    End Sub

'    Public Sub Reset()
'        SyncLock lock
'            timer.Stop()
'            timer.Start()
'        End SyncLock
'    End Sub
'    Public Sub Disarm()
'        SyncLock lock
'            timer.Stop()
'        End SyncLock
'    End Sub
'    Private Sub Fire() Handles timer.Elapsed
'        Disarm()
'        RaiseEvent Fired(Me)
'    End Sub

'    Public Sub Dispose() Implements IDisposable.Dispose
'        timer.Dispose()
'        GC.SuppressFinalize(Me)
'    End Sub
'End Class
