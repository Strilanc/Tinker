Imports HostBot.Pickling.Jars
Imports HostBot.Pickling.Pickles
Imports System.Runtime.CompilerServices

'Library for packing and parsing object data
Namespace Pickling
#Region "Interfaces"
    '''<summary>An exception caused by pickling.</summary>
    Public Class PicklingException
        Inherits Exception
        Public Sub New(ByVal message As String, Optional ByVal innerException As Exception = Nothing)
            MyBase.New(message, innerException)
        End Sub
        Public Sub New(ByVal innerException As Exception)
            MyBase.New(innerException.Message, innerException)
        End Sub
    End Class

    '''<summary>Provides both packed and unpacked object data.</summary>
    Public Interface IPickle
        Function getData() As ImmutableArrayView(Of Byte)
        Function getName() As String
        Function getVal() As Object
        Function toString() As String
    End Interface

    '''<summary>Parses and packs data and objects to create pickles</summary>
    Public Interface IJar
        Function getName() As String
        Function getInfo() As String
        Function relOffset() As Integer
        Function parse(ByVal view As ImmutableArrayView(Of Byte)) As IPickle
        Function pack(ByVal o As Object) As IPickle
    End Interface
#End Region

#Region "Implementations"
    Namespace Pickles
        '''<summary>Stores the packed and unpacked versions of an object.</summary>
        Public Class Pickle
            Implements IPickle
            Private ReadOnly view As ImmutableArrayView(Of Byte)
            Private ReadOnly val As Object
            Private ReadOnly parent As IJar
            Private ReadOnly toStringFunc As Func(Of IJar, Object, String)
            Public Sub New(ByVal parent As IJar, ByVal val As Object, ByVal view As ImmutableArrayView(Of Byte), Optional ByVal toStringFunc As Func(Of IJar, Object, String) = Nothing)
                Me.parent = parent
                Me.view = view
                Me.val = val
                Me.toStringFunc = toStringFunc
            End Sub

            Public Function getVal() As Object Implements IPickle.getVal
                Return val
            End Function
            Public Function getData() As ImmutableArrayView(Of Byte) Implements IPickle.getData
                Return view
            End Function
            Public Overrides Function toString() As String Implements IPickle.toString
                If toStringFunc IsNot Nothing Then Return toStringFunc(parent, val)
                Return val.ToString
            End Function
            Public Function getName() As String Implements IPickle.getName
                Return parent.getName()
            End Function
        End Class

        '''<summary>Stores a list of packed and unpacked objects.</summary>
        Public Class ListPickle
            Inherits Pickle
            Private L As List(Of IPickle)
            Public Sub New(ByVal parent As IJar, ByVal val As Object, ByVal view As ImmutableArrayView(Of Byte), ByVal L As List(Of IPickle))
                MyBase.New(parent, val, view)
                Me.L = L
            End Sub
            Public Overrides Function toString() As String
                Dim s As String = ""
                For Each e As IPickle In L
                    If e IsNot L(0) Then s += Environment.NewLine
                    s += e.getName() + " = " + e.toString()
                Next e
                s = "{" + Environment.NewLine + indent(s) + Environment.NewLine + "}"
                Return s
            End Function
        End Class
    End Namespace

    Namespace Jars
        '''<summary>Creates pickles.</summary>
        Public MustInherit Class Jar
            Implements IJar
            Protected ReadOnly name As String
            Protected ReadOnly info As String
            Public Sub New(ByVal name As String, Optional ByVal info As String = "No Info")
                Me.name = name
                Me.info = info
            End Sub
            Public Function getName() As String Implements IJar.getName
                Return name
            End Function
            Public MustOverride Function pack(ByVal o As Object) As IPickle Implements IJar.pack
            Public MustOverride Function parse(ByVal view As ImmutableArrayView(Of Byte)) As IPickle Implements IJar.parse
            Public Overridable Function getInfo() As String Implements IJar.getInfo
                Return info
            End Function
            Public Overridable Function relOffset() As Integer Implements IJar.relOffset
                Return 0
            End Function
        End Class

        '''<summary>Pickles fixed-size unsigned integers</summary>
        Public Class ValueJar
            Inherits Jar
            Private ReadOnly numBytes As Integer
            Private ReadOnly little_endian As Boolean

            Public Sub New(ByVal name As String, ByVal numBytes As Integer, Optional ByVal info As String = "No Info", Optional ByVal little_endian As Boolean = True)
                MyBase.New(name, info)
                If numBytes <= 0 Then Throw New ArgumentOutOfRangeException("numBytes", "Number of bytes must be positive")
                If numBytes > 8 Then Throw New ArgumentOutOfRangeException("numBytes", "Number of bytes can't exceed the size of a ULong (8 bytes)")
                Me.numBytes = numBytes
                Me.little_endian = little_endian
            End Sub

            Public Overrides Function pack(ByVal o As Object) As IPickle
                Dim val = CULng(o)
                Dim data(0 To numBytes - 1) As Byte
                If little_endian Then
                    For i = 0 To numBytes - 1
                        data(i) = CByte(val And CULng(&HFF))
                        val >>= 8
                    Next i
                Else
                    For i = numBytes - 1 To 0 Step -1
                        data(i) = CByte(val And CULng(&HFF))
                        val >>= 8
                    Next i
                End If
                If val > 0 Then Throw New PicklingException("Value too large to fit in {0} bytes.".frmt(numBytes))
                Return New Pickle(Me, o, data)
            End Function

            Public Overrides Function parse(ByVal view As ImmutableArrayView(Of Byte)) As IPickle
                If numBytes > view.Count Then Throw New PicklingException("Not enough data")

                'Parse
                Dim u As ULong = 0
                If little_endian Then
                    For i = numBytes - 1 To 0 Step -1
                        u <<= 8
                        u += view(i)
                    Next i
                Else
                    For i = 0 To numBytes - 1
                        u <<= 8
                        u += view(i)
                    Next i
                End If

                Return New Pickle(Me, u, view.SubView(0, numBytes))
            End Function
        End Class

        '''<summary>Pickles byte arrays [can be size-prefixed, fixed-size, or full-sized]</summary>
        Public Class ArrayJar
            Inherits Jar
            Private ReadOnly expectedSize As Integer
            Private ReadOnly sizePrefixSize As Integer
            Private ReadOnly takeRest As Boolean

            Public Sub New(ByVal name As String, Optional ByVal expectedSize As Integer = 0, Optional ByVal sizePrefixSize As Integer = 0, Optional ByVal takeRest As Boolean = False, Optional ByVal info As String = "No Info")
                MyBase.New(name, info)
                If expectedSize < 0 Then Throw New ArgumentOutOfRangeException("expectedSize")
                If takeRest Then
                    If expectedSize <> 0 Or sizePrefixSize > 0 Then
                        Throw New ArgumentException(Me.GetType.Name + " can't combine takeRest with hasSizePrefix or expectedSize")
                    End If
                ElseIf expectedSize = 0 And sizePrefixSize = 0 Then
                    Throw New ArgumentException(Me.GetType.Name + " must be either size prefixed or have an expectedSize")
                End If
                Me.expectedSize = expectedSize
                Me.sizePrefixSize = sizePrefixSize
                Me.takeRest = takeRest
            End Sub

            Private Shared Function valToString(ByVal parent As IJar, ByVal val As Object) As String
                Dim hasSizePrefix As Boolean = CType(parent, ArrayJar).sizePrefixSize > 0
                Return If(hasSizePrefix, "<", "[") + unpackHexString(CType(val, Byte())).Trim() + If(hasSizePrefix, ">", "]")
            End Function

            Public Overrides Function pack(ByVal o As Object) As IPickle
                Dim val = CType(o, Byte())
                Dim offset = 0
                Dim size = val.Length
                If sizePrefixSize > 0 Then
                    size += sizePrefixSize
                    offset = sizePrefixSize
                End If
                If expectedSize <> 0 And size <> expectedSize Then Throw New PicklingException("Array size doesn't match expected size.")

                'Pack
                Dim data(0 To size - 1) As Byte
                If sizePrefixSize > 0 Then
                    size -= sizePrefixSize
                    Dim ds = CUInt(size).bytes(min_size:=sizePrefixSize)
                    If ds.Length <> sizePrefixSize Then Throw New PicklingException("Unable to fit size into prefix.")
                    For i = 0 To sizePrefixSize - 1
                        data(i) = ds(i)
                    Next i
                End If
                For i = 0 To size - 1
                    data(i + offset) = val(i)
                Next i

                Return New Pickle(Me, val, data, AddressOf valToString)
            End Function
            Public Overrides Function parse(ByVal view As ImmutableArrayView(Of Byte)) As IPickle
                'Sizes
                Dim inputSize = expectedSize
                Dim outputSize = expectedSize
                Dim pos = 0
                If takeRest Then
                    inputSize = view.length
                    outputSize = view.length
                ElseIf sizePrefixSize > 0 Then
                    'Validate
                    If view.length < sizePrefixSize Then
                        Throw New PicklingException("Not enough data to parse array. Data ended before size prefix could be read.")
                    End If
                    'Read size prefix
                    outputSize = CInt(ToUInteger(view.SubView(pos, sizePrefixSize).ToArray()))
                    inputSize = outputSize + sizePrefixSize
                    If expectedSize <> 0 And expectedSize <> inputSize Then
                        Throw New PicklingException("Array size doesn't match expected size")
                    End If
                    pos += sizePrefixSize
                End If
                'Validate
                If inputSize > view.length Then
                    Throw New PicklingException("Not enough data to parse array. Need {0} more bytes but only have {1}.".frmt(view.length, inputSize))
                End If

                'Parse
                Dim val(0 To outputSize - 1) As Byte
                For i = 0 To outputSize - 1
                    val(i) = view(pos + i)
                Next i

                Return New Pickle(Me, val, view.SubView(0, inputSize), AddressOf valToString)
            End Function
        End Class

        '''<summary>Pickles strings [can be null-terminated or fixed-size, and reversed]</summary>
        Public Class StringJar
            Inherits Jar
            Private ReadOnly nullTerminated As Boolean
            Private ReadOnly expectedSize As Integer
            Private ReadOnly reversed As Boolean

            Private Shared Function valToString(ByVal parent As IJar, ByVal val As Object) As String
                Dim quote As String = If(CType(parent, StringJar).nullTerminated, """", "'")
                Dim suffix As String = If(CType(parent, StringJar).reversed, " (reversed)", "")
                Return quote + CType(val, String) + quote + suffix
            End Function

            Public Sub New(ByVal name As String, Optional ByVal nullTerminated As Boolean = True, Optional ByVal reversed As Boolean = False, Optional ByVal expectedSize As Integer = 0, Optional ByVal info As String = "No Info")
                MyBase.New(name, info)
                If expectedSize < 0 Then Throw New ArgumentOutOfRangeException("expectedSize")
                If expectedSize = 0 And Not nullTerminated Then Throw New ArgumentException(Me.GetType.Name + " must be either nullTerminated or have an expectedSize")
                Me.nullTerminated = nullTerminated
                Me.expectedSize = expectedSize
                Me.reversed = reversed
            End Sub

            Public Overrides Function pack(ByVal o As Object) As IPickle
                Dim val = CStr(o)
                Dim size = val.Length + If(nullTerminated, 1, 0)
                If expectedSize <> 0 And size <> expectedSize Then Throw New PicklingException("Size doesn't match expected size")

                'Pack
                Dim data(0 To size - 1) As Byte
                If nullTerminated Then size -= 1
                Dim i = 0
                While size > 0
                    size -= 1
                    data(If(reversed, size, i)) = CByte(Asc(val(i)))
                    i += 1
                End While

                Return New Pickle(Me, val, data, AddressOf valToString)
            End Function

            Public Overrides Function parse(ByVal view As ImmutableArrayView(Of Byte)) As IPickle
                'Get sizes
                Dim inputSize = expectedSize
                If nullTerminated Then
                    If view.length > 0 Then
                        For j = 0 To view.length - 1
                            If view(j) = 0 Then
                                If inputSize <> 0 And inputSize <> j + 1 Then
                                    Throw New PicklingException("String size doesn't match expected size")
                                End If
                                inputSize = j + 1
                                Exit For
                            End If
                        Next j
                    Else 'empty strings at the end of data are sometimes simply omitted
                        Return New Pickle(Me, Nothing, view, AddressOf valToString)
                    End If
                End If
                Dim outputSize = inputSize - If(nullTerminated, 1, 0)
                'Validate
                If view.length < inputSize Then Throw New PicklingException("Not enough data")

                'Parse string data
                Dim cc(0 To outputSize - 1) As Char
                Dim i = 0
                While outputSize > 0
                    outputSize -= 1
                    cc(If(reversed, outputSize, i)) = Chr(view(i))
                    i += 1
                End While

                Return New Pickle(Me, cc, view.SubView(0, inputSize), AddressOf valToString)
            End Function
        End Class

        '''<summary>Combines jars to pickle ordered tuples of objects</summary>
        Public Class TupleJar
            Inherits Jar
            Private ReadOnly subjars As New List(Of IJar)

            Public Sub New(ByVal name As String, ByVal info As String, ByVal ParamArray subjars() As IJar)
                MyBase.New(name, info)
                For Each j As IJar In subjars
                    Me.subjars.Add(j)
                Next j
            End Sub
            Public Sub New(ByVal name As String, ByVal ParamArray subjars() As IJar)
                Me.New(name, "No Info", subjars)
            End Sub

            Public Overrides Function pack(ByVal o As Object) As IPickle
                Dim vals = CType(o, Dictionary(Of String, Object))
                If vals.Keys.Count > subjars.Count Then Throw New PicklingException("Too many keys in dictionary")

                'Pack
                Dim pickles = New List(Of IPickle)
                For Each j In subjars
                    If Not vals.ContainsKey(j.getName()) Then Throw New PicklingException("Key '{0}' missing from tuple dictionary.".frmt(j.getName()))
                    pickles.Add(j.pack(vals(j.getName())))
                Next j
                Return New ListPickle(Me, vals, concat(From p In pickles Select p.getData.ToArray), pickles)
            End Function

            Public Overrides Function parse(ByVal view As ImmutableArrayView(Of Byte)) As IPickle
                'Parse
                Dim vals = New Dictionary(Of String, Object)
                Dim pickles = New List(Of IPickle)
                Dim curCount = view.length
                Dim curOffset = 0
                For Each j In subjars
                    'Value
                    Dim p = j.parse(view.SubView(curOffset + j.relOffset, curCount - j.relOffset))
                    vals(j.getName()) = p.getVal()
                    pickles.Add(p)
                    'Size
                    Dim n = p.getData.length
                    curCount -= n
                    curOffset += n
                Next j

                Return New ListPickle(Me, vals, view.SubView(0, curOffset), pickles)
            End Function
        End Class

        '''<summary>Pickles lists of the same type of element [size-prefixed]</summary>
        Public Class ListJar
            Inherits Jar
            Private ReadOnly subjar As IJar

            Public Sub New(ByVal name As String, ByVal subjar As IJar, Optional ByVal info As String = "No Info")
                MyBase.New(name, info)
                If Not (subjar IsNot Nothing) Then Throw New ArgumentException()
                Me.subjar = subjar
            End Sub

            Public Overrides Function pack(ByVal o As Object) As IPickle
                Dim vals = CType(o, List(Of Object))
                Dim pickles = (From e In vals Select subjar.pack(e)).ToList()
                Dim data = concat(New Byte() {CByte(vals.Count)}, concat(From p In pickles Select p.getData.ToArray))
                Return New ListPickle(Me, vals, data, pickles)
            End Function

            Public Overrides Function parse(ByVal view As ImmutableArrayView(Of Byte)) As IPickle
                'Validate
                If view.length < 1 Then Throw New PicklingException("Not enough data")

                'Parse
                Dim vals As New List(Of Object)
                Dim pickles As New List(Of IPickle)
                Dim curCount = view.length
                Dim curOffset = 0
                'List Size
                Dim numElements = CInt(view(curOffset))
                curOffset += 1
                curCount -= 1
                'List Elements
                For i = 1 To numElements
                    'Value
                    Dim p = subjar.parse(view.SubView(curOffset + subjar.relOffset, curCount - subjar.relOffset))
                    vals.Add(p.getVal())
                    pickles.Add(p)
                    'Size
                    Dim n = p.getData.length
                    curCount -= n
                    curOffset += n
                Next i

                Return New ListPickle(Me, vals, view.SubView(0, curOffset), pickles)
            End Function
        End Class

        Public Class ManualSwitchJar
            Private ReadOnly packers(0 To 255) As IJar
            Private ReadOnly parsers(0 To 255) As IJar

            Public Function parse(ByVal index As Byte, ByVal view As ImmutableArrayView(Of Byte)) As IPickle
                If Not (view IsNot Nothing) Then Throw New ArgumentException()


                Dim p = parsers(index)
                If p Is Nothing Then Throw New PicklingException("No parser registered for packet index {0}.".frmt(index.ToString()))
                Dim offset = p.relOffset
                Return p.parse(view.SubView(offset, view.length - offset))
            End Function
            Public Function pack(ByVal index As Byte, ByVal o As Object) As IPickle
                If Not (o IsNot Nothing) Then Throw New ArgumentException()


                If packers(index) Is Nothing Then Throw New PicklingException("No packer registered to index {0}.".frmt(index.ToString()))
                Return packers(index).pack(o)
            End Function

            Public Sub reg(ByVal index As Byte, ByVal parser_packer As IJar)
                If Not (parser_packer IsNot Nothing) Then Throw New ArgumentException()

                If parsers(index) IsNot Nothing Then Throw New InvalidOperationException("Parser already registered to index " + index.ToString())
                If packers(index) IsNot Nothing Then Throw New InvalidOperationException("Packer already registered to index " + index.ToString())
                parsers(index) = parser_packer
                packers(index) = parser_packer
            End Sub
            Public Sub regParser(ByVal index As Byte, ByVal parser As IJar)
                If Not (parser IsNot Nothing) Then Throw New ArgumentException()

                If parsers(index) IsNot Nothing Then Throw New InvalidOperationException("Parser already registered to index " + index.ToString())
                parsers(index) = parser
            End Sub
            Public Sub regPacker(ByVal index As Byte, ByVal packer As IJar)
                If Not (packer IsNot Nothing) Then Throw New ArgumentException()

                If packers(index) IsNot Nothing Then Throw New InvalidOperationException("Packer already registered to index " + index.ToString())
                packers(index) = packer
            End Sub
        End Class

        Public Class AutoSwitchJar
            Inherits Jar
            Private packers(0 To 255) As IJar
            Private parsers(0 To 255) As IJar
            Private ReadOnly defaultParser As IJar
            Public ReadOnly switchByteOffset As Integer

            Public Sub New(ByVal name As String, ByVal switchByteOffset As Integer, ByVal defaultParser As IJar, Optional ByVal info As String = "No Info")
                MyBase.New(name, info)
                Me.switchByteOffset = switchByteOffset
                Me.defaultParser = defaultParser
            End Sub

            Public Overrides Function relOffset() As Integer
                Return Math.Min(switchByteOffset, 0)
            End Function
            Public Overrides Function parse(ByVal view As ImmutableArrayView(Of Byte)) As IPickle
                'Validate
                If view.length < switchByteOffset Then Throw New PicklingException("Not enough data")
                Dim index = 0
                If switchByteOffset < 0 Then
                    index = view(0)
                    view = view.SubView(-switchByteOffset, view.length + switchByteOffset)
                Else
                    index = view(switchByteOffset)
                End If

                'Pick parser
                Dim p = parsers(index)
                If p Is Nothing Then p = defaultParser
                If p Is Nothing Then Throw New PicklingException("No parser registered to index " + index.ToString())

                'Delegate
                Return p.parse(view.SubView(p.relOffset, view.length - p.relOffset))
            End Function

            Public Overrides Function pack(ByVal o As Object) As IPickle
                Dim index As Byte
                With CType(o, Pair(Of Byte, Object))
                    index = .v1
                    o = .v2
                End With
                If packers(index) Is Nothing Then Throw New PicklingException("No packer registered to index " + index.ToString())
                Return packers(index).pack(o)
            End Function

            Public Sub regParser(ByVal index As Byte, ByVal parser As IJar)
                If Not (parser IsNot Nothing) Then Throw New ArgumentException()
                If parsers(index) IsNot Nothing Then Throw New InvalidOperationException("Parser already registered to index " + index.ToString())
                parsers(index) = parser
            End Sub
            Public Sub regPacker(ByVal index As Byte, ByVal packer As IJar)
                If Not (packer IsNot Nothing) Then Throw New ArgumentException()
                If packers(index) IsNot Nothing Then Throw New InvalidOperationException("Packer already registered to index " + index.ToString())
                packers(index) = packer
            End Sub
        End Class
    End Namespace
#End Region
End Namespace