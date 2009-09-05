Namespace Pickling.Jars
    '''<summary>Pickles byte arrays [can be size-prefixed, fixed-size, or full-sized]</summary>
    Public Class ArrayJar
        Inherits Jar(Of Byte())
        Private ReadOnly expectedSize As Integer
        Private ReadOnly sizePrefixSize As Integer
        Private ReadOnly takeRest As Boolean

        Public Sub New(ByVal name As String,
                       Optional ByVal expectedSize As Integer = 0,
                       Optional ByVal sizePrefixSize As Integer = 0,
                       Optional ByVal takeRest As Boolean = False,
                       Optional ByVal info As String = Nothing)
            MyBase.New(name)
            Contract.Requires(name IsNot Nothing)
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

        Protected Overridable Function DescribeValue(ByVal val As Byte()) As String
            Return "[{0}]".Frmt(val.ToHexString)
        End Function

        Public Overrides Function Pack(Of R As Byte())(ByVal value As R) As IPickle(Of R)
            Dim val = CType(value, Byte())
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
                Dim ds = CUInt(size).Bytes(ByteOrder.LittleEndian, size:=sizePrefixSize)
                If ds.Length <> sizePrefixSize Then Throw New PicklingException("Unable to fit size into prefix.")
                For i = 0 To sizePrefixSize - 1
                    data(i) = ds(i)
                Next i
            End If
            For i = 0 To size - 1
                data(i + offset) = val(i)
            Next i

            Return New Pickle(Of R)(Me.Name, value, data.ToView(), Function() DescribeValue(val))
        End Function
        Public Overrides Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of Byte())
            'Sizes
            Dim inputSize = expectedSize
            Dim outputSize = expectedSize
            Dim pos = 0
            If takeRest Then
                inputSize = data.Length
                outputSize = data.Length
            ElseIf sizePrefixSize > 0 Then
                'Validate
                If data.Length < sizePrefixSize Then
                    Throw New PicklingException("Not enough data to parse array. Data ended before size prefix could be read.")
                End If
                'Read size prefix
                outputSize = CInt(data.SubView(pos, sizePrefixSize).ToUInt32(ByteOrder.LittleEndian))
                inputSize = outputSize + sizePrefixSize
                If expectedSize <> 0 And expectedSize <> inputSize Then
                    Throw New PicklingException("Array size doesn't match expected size")
                End If
                pos += sizePrefixSize
            End If
            'Validate
            If inputSize > data.Length Then
                Throw New PicklingException("Not enough data to parse array. Need {0} more bytes but only have {1}.".Frmt(inputSize, data.Length))
            End If

            'Parse
            Dim val(0 To outputSize - 1) As Byte
            For i = 0 To outputSize - 1
                val(i) = data(pos + i)
            Next i

            Return New Pickle(Of Byte())(Me.Name, val, data.SubView(0, inputSize), Function() DescribeValue(val))
        End Function
    End Class

    '''<summary>Pickles strings [can be null-terminated or fixed-size, and reversed]</summary>
    Public Class StringJar
        Inherits Jar(Of String)
        Private ReadOnly nullTerminated As Boolean
        Private ReadOnly expectedSize As Integer
        Private ReadOnly reversed As Boolean

        Public Sub New(ByVal name As String,
                       Optional ByVal nullTerminated As Boolean = True,
                       Optional ByVal reversed As Boolean = False,
                       Optional ByVal expectedSize As Integer = 0,
                       Optional ByVal info As String = "No Info")
            MyBase.New(name)
            Contract.Requires(name IsNot Nothing)
            Contract.Requires(info IsNot Nothing)
            If expectedSize < 0 Then Throw New ArgumentOutOfRangeException("expectedSize")
            If expectedSize = 0 And Not nullTerminated Then Throw New ArgumentException(Me.GetType.Name + " must be either nullTerminated or have an expectedSize")
            Me.nullTerminated = nullTerminated
            Me.expectedSize = expectedSize
            Me.reversed = reversed
        End Sub

        Public Overrides Function Pack(Of R As String)(ByVal value As R) As IPickle(Of R)
            Dim size = value.Length + If(nullTerminated, 1, 0)
            If expectedSize <> 0 And size <> expectedSize Then Throw New PicklingException("Size doesn't match expected size")

            'Pack
            Dim data(0 To size - 1) As Byte
            If nullTerminated Then size -= 1
            Dim i = 0
            While size > 0
                size -= 1
                data(If(reversed, size, i)) = CByte(Asc(value(i)))
                i += 1
            End While

            Return New Pickle(Of R)(Me.Name, value, data.ToView(), Function() """{0}""".Frmt(value))
        End Function

        Public Overrides Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of String)
            'Get sizes
            Dim inputSize = expectedSize
            If nullTerminated Then
                If data.Length > 0 Then
                    For j = 0 To data.Length - 1
                        If data(j) = 0 Then
                            If inputSize <> 0 And inputSize <> j + 1 Then
                                Throw New PicklingException("String size doesn't match expected size")
                            End If
                            inputSize = j + 1
                            Exit For
                        End If
                    Next j
                Else 'empty strings at the end of data are sometimes simply omitted
                    Return New Pickle(Of String)(Me.Name, Nothing, data, Function() """")
                End If
            End If
            Dim outputSize = inputSize - If(nullTerminated, 1, 0)
            'Validate
            If data.Length < inputSize Then Throw New PicklingException("Not enough data")

            'Parse string data
            Dim cc(0 To outputSize - 1) As Char
            Dim i = 0
            While outputSize > 0
                outputSize -= 1
                cc(If(reversed, outputSize, i)) = Chr(data(i))
                i += 1
            End While

            Return New Pickle(Of String)(Me.Name, cc, data.SubView(0, inputSize), Function() """{0}""".Frmt(CStr(cc)))
        End Function
    End Class
    Public Class FusionJar(Of T)
        Inherits Jar(Of T)
        Public ReadOnly packer As IPackJar(Of T)
        Public ReadOnly parser As IParseJar(Of T)
        Public Sub New(ByVal packer As IPackJar(Of T), ByVal parser As IParseJar(Of T))
            MyBase.New(packer.Name)
            If packer.Name <> parser.Name Then Throw New ArgumentException()
            Me.packer = packer
            Me.parser = parser
        End Sub

        Public Overrides Function Pack(Of R As T)(ByVal value As R) As IPickle(Of R)
            Return packer.Pack(value)
        End Function

        Public Overrides Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of T)
            Return parser.Parse(data)
        End Function
    End Class

    '''<summary>Combines jars to pickle ordered tuples of objects</summary>
    Public Class TuplePackJar
        Inherits PackJar(Of Dictionary(Of String, Object))
        Private ReadOnly subjars() As IPackJar(Of Object)

        Public Sub New(ByVal name As String)
            MyBase.New(name)
            Contract.Requires(name IsNot Nothing)
            Me.subjars = New IPackJar(Of Object)() {}
        End Sub
        Public Sub New(ByVal name As String, ByVal ParamArray subjars() As IPackJar(Of Object))
            MyBase.New(name)
            Contract.Requires(name IsNot Nothing)
            Me.subjars = subjars
        End Sub
        Public Function Extend(Of R)(ByVal jar As IPackJar(Of R)) As TuplePackJar
            Return New TuplePackJar(Name, Concat(subjars, New IPackJar(Of Object)() {jar.Weaken}))
        End Function

        Public Overrides Function Pack(Of R As Dictionary(Of String, Object))(ByVal value As R) As IPickle(Of R)
            If value.Keys.Count > subjars.Count Then Throw New PicklingException("Too many keys in dictionary")

            'Pack
            Dim pickles = New List(Of IPickle(Of Object))
            For Each j In subjars
                If Not value.ContainsKey(j.Name) Then Throw New PicklingException("Key '{0}' missing from tuple dictionary.".Frmt(j.Name))
                pickles.Add(j.Pack(value(j.Name)))
            Next j
            Return New Pickle(Of R)(Me.Name, value, Concat(From p In pickles Select p.Data.ToArray).ToView(), Function() Pickle(Of R).MakeListDescription(pickles))
        End Function
    End Class
    Public Class TupleParseJar
        Inherits ParseJar(Of Dictionary(Of String, Object))
        Private ReadOnly subjars() As IParseJar(Of Object)

        Public Sub New(ByVal name As String, ByVal ParamArray subjars() As IParseJar(Of Object))
            MyBase.New(name)
            Contract.Requires(name IsNot Nothing)
            Me.subjars = subjars
        End Sub
        Public Function Extend(ByVal jar As IParseJar(Of Object)) As TupleParseJar
            Return New TupleParseJar(Name, Concat(subjars, {jar}))
        End Function
        Public Function ExWeak(Of R)(ByVal jar As IParseJar(Of R)) As TupleParseJar
            Return New TupleParseJar(Name, Concat(subjars, {jar.Weaken}))
        End Function

        Public Overrides Function parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of Dictionary(Of String, Object))
            'Parse
            Dim vals = New Dictionary(Of String, Object)
            Dim pickles = New List(Of IPickle(Of Object))
            Dim curCount = data.Length
            Dim curOffset = 0
            For Each j In subjars
                'Value
                Dim p = j.Parse(data.SubView(curOffset, curCount))
                vals(j.Name) = p.Value
                pickles.Add(p)
                'Size
                Dim n = p.Data.Length
                curCount -= n
                curOffset += n
            Next j

            Return New Pickle(Of Dictionary(Of String, Object))(Me.Name, vals, data.SubView(0, curOffset), Function() Pickle(Of Dictionary(Of String, Object)).MakeListDescription(pickles))
        End Function
    End Class

    Public Class TupleJar
        Inherits FusionJar(Of Dictionary(Of String, Object))
        Private ReadOnly subjars() As IJar(Of Object)

        Public Sub New(ByVal name As String, ByVal ParamArray subjars() As IJar(Of Object))
            MyBase.New(New TuplePackJar(name, subjars), New TupleParseJar(name, subjars))
            Contract.Requires(name IsNot Nothing)
            Me.subjars = subjars
        End Sub
        Public Function Extend(Of R)(ByVal jar As IJar(Of R)) As TupleJar
            Return New TupleJar(Name, Concat(subjars, {jar.Weaken}))
        End Function

        Public Overrides Function Pack(Of R As Dictionary(Of String, Object))(ByVal value As R) As IPickle(Of R)
            If value.Keys.Count > subjars.Count Then Throw New PicklingException("Too many keys in dictionary")

            'Pack
            Dim pickles = New List(Of IPickle(Of Object))
            For Each j In subjars
                If Not value.ContainsKey(j.Name) Then Throw New PicklingException("Key '{0}' missing from tuple dictionary.".Frmt(j.Name))
                pickles.Add(j.Pack(value(j.Name)))
            Next j
            Return New Pickle(Of R)(Me.Name, value, Concat(From p In pickles Select p.Data.ToArray).ToView(), Function() Pickle(Of R).MakeListDescription(pickles))
        End Function

        Public Overrides Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of Dictionary(Of String, Object))
            'Parse
            Dim vals = New Dictionary(Of String, Object)
            Dim pickles = New List(Of IPickle(Of Object))
            Dim curCount = data.Length
            Dim curOffset = 0
            For Each j In subjars
                'Value
                Dim p = j.Parse(data.SubView(curOffset, curCount))
                vals(j.Name) = p.Value
                pickles.Add(p)
                'Size
                Dim n = p.Data.Length
                curCount -= n
                curOffset += n
            Next j

            Return New Pickle(Of Dictionary(Of String, Object))(Me.Name, vals, data.SubView(0, curOffset), Function() Pickle(Of Dictionary(Of String, Object)).MakeListDescription(pickles))
        End Function
    End Class

    Public Class ListParseJar(Of T)
        Inherits ParseJar(Of List(Of T))
        Private ReadOnly sizeJar As ValueJar
        Private ReadOnly subjar As IParseJar(Of T)

        Public Sub New(ByVal name As String,
                       ByVal subjar As IParseJar(Of T),
                       Optional ByVal numSizePrefixBytes As Integer = 1)
            MyBase.New(name)
            Contract.Requires(name IsNot Nothing)
            Contract.Requires(subjar IsNot Nothing)
            Contract.Requires(numSizePrefixBytes > 0)
            Contract.Requires(numSizePrefixBytes <= 8)
            Me.subjar = subjar
            Me.sizeJar = New ValueJar("size prefix", numSizePrefixBytes)
        End Sub

        Public Overrides Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of List(Of T))
            'Parse
            Dim vals As New List(Of T)
            Dim pickles As New List(Of IPickle(Of Object))
            Dim curCount = data.Length
            Dim curOffset = 0
            'List Size
            Dim sz = sizeJar.Parse(data)
            Dim numElements = sz.Value
            curOffset += sz.Data.Length
            curCount -= sz.Data.Length
            'List Elements
            For i = 1UL To numElements
                'Value
                Dim p = subjar.Parse(data.SubView(curOffset, curCount))
                vals.Add(p.Value)
                pickles.Add(New Pickle(Of Object)(p.Value, p.Data, p.Description))
                'Size
                Dim n = p.Data.Length
                curCount -= n
                curOffset += n
            Next i

            Return New Pickle(Of List(Of T))(Me.Name, vals, data.SubView(0, curOffset), Function() Pickle(Of List(Of T)).MakeListDescription(pickles))
        End Function
    End Class
    Public Class ListPackJar(Of T)
        Inherits PackJar(Of List(Of T))
        Private ReadOnly subjar As IPackJar(Of T)
        Private ReadOnly prefixSize As Integer

        Public Sub New(ByVal name As String,
                       ByVal subjar As IPackJar(Of T),
                       Optional ByVal prefixSize As Integer = 1)
            MyBase.New(name)
            Contract.Requires(name IsNot Nothing)
            Contract.Requires(subjar IsNot Nothing)
            Me.subjar = subjar
            Me.prefixSize = prefixSize
        End Sub

        Public Overrides Function Pack(Of R As List(Of T))(ByVal value As R) As IPickle(Of R)
            Dim pickles = (From e In value Select CType(subjar.Pack(e), IPickle(Of T))).ToList()
            Dim data = Concat(CUInt(value.Count).Bytes(ByteOrder.LittleEndian, size:=prefixSize), Concat(From p In pickles Select p.Data.ToArray))
            Return New Pickle(Of R)(Me.Name, value, data.ToView(), Function() Pickle(Of R).MakeListDescription(pickles))
        End Function
    End Class
    Public Class ListJar(Of T)
        Inherits FusionJar(Of List(Of T))
        Private ReadOnly sizeJar As ValueJar
        Private ReadOnly subjar As IJar(Of T)

        Public Sub New(ByVal name As String,
                       ByVal subjar As IJar(Of T),
                       Optional ByVal prefixSize As Integer = 1)
            MyBase.New(New ListPackJar(Of T)(name, subjar, prefixSize), New ListParseJar(Of T)(name, subjar, prefixSize))
            Contract.Requires(name IsNot Nothing)
            Contract.Requires(subjar IsNot Nothing)
        End Sub
    End Class

    Public Class RepeatingParseJar(Of T)
        Inherits ParseJar(Of List(Of T))
        Private ReadOnly subjar As IParseJar(Of T)

        Public Sub New(ByVal name As String,
                       ByVal subjar As IParseJar(Of T))
            MyBase.New(name)
            Contract.Requires(name IsNot Nothing)
            Contract.Requires(subjar IsNot Nothing)
            Me.subjar = subjar
        End Sub

        Public Overrides Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of List(Of T))
            'Parse
            Dim vals As New List(Of T)
            Dim pickles As New List(Of IPickle(Of Object))
            Dim curCount = data.Length
            Dim curOffset = 0
            'List Size
            'List Elements
            While curOffset < data.Length
                'Value
                Dim p = subjar.Parse(data.SubView(curOffset, curCount))
                vals.Add(p.Value)
                pickles.Add(New Pickle(Of Object)(p.Value, p.Data, p.Description))
                'Size
                Dim n = p.Data.Length
                curCount -= n
                curOffset += n
            End While

            Return New Pickle(Of List(Of T))(Me.Name, vals, data.SubView(0, curOffset), Function() Pickle(Of List(Of T)).MakeListDescription(pickles))
        End Function
    End Class
    Public Class RepeatingPackJar(Of T)
        Inherits PackJar(Of List(Of T))
        Private ReadOnly subjar As IPackJar(Of T)

        Public Sub New(ByVal name As String,
                       ByVal subjar As IPackJar(Of T))
            MyBase.New(name)
            Contract.Requires(name IsNot Nothing)
            Contract.Requires(subjar IsNot Nothing)
            Me.subjar = subjar
        End Sub

        Public Overrides Function Pack(Of R As List(Of T))(ByVal value As R) As IPickle(Of R)
            Dim pickles = (From e In value Select CType(subjar.Pack(e), IPickle(Of T))).ToList()
            Dim data = Concat(From p In pickles Select p.Data.ToArray)
            Return New Pickle(Of R)(Me.Name, value, data.ToView(), Function() Pickle(Of R).MakeListDescription(pickles))
        End Function
    End Class
    Public Class RepeatingJar(Of T)
        Inherits FusionJar(Of List(Of T))
        Private ReadOnly subjar As IJar(Of T)

        Public Sub New(ByVal name As String,
                       ByVal subjar As IJar(Of T))
            MyBase.New(New RepeatingPackJar(Of T)(name, subjar), New RepeatingParseJar(Of T)(name, subjar))
            Contract.Requires(name IsNot Nothing)
            Contract.Requires(subjar IsNot Nothing)
        End Sub
    End Class

    Public Class InteriorSwitchJar(Of T)
        Inherits Jar(Of T)
        Private ReadOnly packers(0 To 255) As IPackJar(Of T)
        Private ReadOnly parsers(0 To 255) As IParseJar(Of T)
        Private ReadOnly f1 As Func(Of T, Byte)
        Private ReadOnly f2 As Func(Of ViewableList(Of Byte), Byte)
        Public Sub New(ByVal name As String, ByVal f1 As Func(Of T, Byte), ByVal f2 As Func(Of ViewableList(Of Byte), Byte))
            MyBase.new(name)
            Me.f1 = f1
            Me.f2 = f2
        End Sub

        Public Overrides Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of T)
            Dim index = f2(data)
            If parsers(index) Is Nothing Then Throw New PicklingException("No parser registered to " + index.ToString())
            Return parsers(index).Parse(data)
        End Function
        Public Overrides Function Pack(Of R As T)(ByVal value As R) As IPickle(Of R)
            Dim index = f1(value)
            If packers(index) Is Nothing Then Throw New PicklingException("No packer registered to " + index.ToString())
            Return packers(index).Pack(value)
        End Function

        Public Sub reg(ByVal index As Byte, ByVal parser_packer As IJar(Of T))
            Contract.Requires(parser_packer IsNot Nothing)
            If parsers(index) IsNot Nothing Then Throw New InvalidOperationException("Parser already registered to index " + index.ToString())
            If packers(index) IsNot Nothing Then Throw New InvalidOperationException("Packer already registered to index " + index.ToString())
            parsers(index) = parser_packer
            packers(index) = parser_packer
        End Sub
        Public Sub regParser(ByVal index As Byte, ByVal parser As IParseJar(Of T))
            If Not (parser IsNot Nothing) Then Throw New ArgumentException()
            If parsers(index) IsNot Nothing Then Throw New InvalidOperationException("Parser already registered to index " + index.ToString())
            parsers(index) = parser
        End Sub
        Public Sub regPacker(ByVal index As Byte, ByVal packer As IPackJar(Of T))
            If Not (packer IsNot Nothing) Then Throw New ArgumentException()
            If packers(index) IsNot Nothing Then Throw New InvalidOperationException("Packer already registered to index " + index.ToString())
            packers(index) = packer
        End Sub
    End Class
    Public Class PrefixSwitchJar(Of T)
        Inherits Jar(Of PrefixPickle)
        Private ReadOnly packers(0 To 255) As IPackJar(Of Object)
        Private ReadOnly parsers(0 To 255) As IParseJar(Of Object)
        Public Sub New(ByVal name As String)
            MyBase.new(name)
        End Sub

        Public Class PrefixPickle
            Public ReadOnly index As T
            Public ReadOnly payload As IPickle(Of Object)
            Public Sub New(ByVal index As T, ByVal payload As IPickle(Of Object))
                Contract.Requires(payload IsNot Nothing)
                Me.index = index
                Me.payload = payload
            End Sub
        End Class
        Public Overrides Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of PrefixPickle)
            Dim index = CByte(data(0))
            Dim vindex = CType(CType(index, Object), T)
            If parsers(index) Is Nothing Then Throw New PicklingException("No parser registered to " + vindex.ToString())
            Dim payload = New PrefixPickle(vindex, parsers(index).Parse(data.SubView(1)))
            Return New Pickle(Of PrefixPickle)(Name, payload, data.SubView(0, payload.payload.Data.Length + 1))
        End Function
        Public Overrides Function Pack(Of R As PrefixPickle)(ByVal value As R) As IPickle(Of R)
            Dim index = CByte(CType(value.index, Object))
            If packers(index) Is Nothing Then Throw New PicklingException("No packer registered to " + value.index.ToString())
            Return New Pickle(Of R)(Name, value, Concat({index}, packers(index).Pack(value.payload.Value).Data.ToArray).ToView)
        End Function

        Public Sub reg(ByVal index As Byte, ByVal parser_packer As IJar(Of Object))
            Contract.Requires(parser_packer IsNot Nothing)
            If parsers(index) IsNot Nothing Then Throw New InvalidOperationException("Parser already registered to index " + index.ToString())
            If packers(index) IsNot Nothing Then Throw New InvalidOperationException("Packer already registered to index " + index.ToString())
            parsers(index) = parser_packer
            packers(index) = parser_packer
        End Sub
        Public Sub regParser(ByVal index As Byte, ByVal parser As IParseJar(Of Object))
            If Not (parser IsNot Nothing) Then Throw New ArgumentException()
            If parsers(index) IsNot Nothing Then Throw New InvalidOperationException("Parser already registered to index " + index.ToString())
            parsers(index) = parser
        End Sub
        Public Sub regPacker(ByVal index As Byte, ByVal packer As IPackJar(Of Object))
            If Not (packer IsNot Nothing) Then Throw New ArgumentException()
            If packers(index) IsNot Nothing Then Throw New InvalidOperationException("Packer already registered to index " + index.ToString())
            packers(index) = packer
        End Sub
    End Class
    Public Class ManualSwitchJar
        Private ReadOnly packers(0 To 255) As IPackJar(Of Object)
        Private ReadOnly parsers(0 To 255) As IParseJar(Of Object)

        Public Sub New()
        End Sub
        Public Sub New(ByVal parsersPackers As IDictionary(Of Byte, IJar(Of Object)))
            If parsersPackers IsNot Nothing Then
                For Each pair In parsersPackers
                    Me.packers(pair.Key) = pair.Value
                    Me.parsers(pair.Key) = pair.Value
                Next pair
            End If
        End Sub
        Public Sub New(ByVal parsers As IDictionary(Of Byte, IParseJar(Of Object)),
                       ByVal packers As IDictionary(Of Byte, IPackJar(Of Object)))
            For Each pair In parsers
                Me.parsers(pair.Key) = pair.Value
            Next pair
            For Each pair In packers
                Me.packers(pair.Key) = pair.Value
            Next pair
        End Sub

        Public Overloads Function Parse(ByVal index As Byte, ByVal data As ViewableList(Of Byte)) As IPickle(Of Object)
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IPickle(Of Object))() IsNot Nothing)
            If parsers(index) Is Nothing Then Throw New PicklingException("No parser registered to " + index.ToString())
            Return parsers(index).Parse(data)
        End Function
        Public Overloads Function Pack(Of R)(ByVal index As Byte, ByVal value As R) As IPickle(Of R)
            Contract.Requires(value IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IPickle(Of R))() IsNot Nothing)
            If packers(index) Is Nothing Then Throw New PicklingException("No packer registered to " + index.ToString())
            Return packers(index).Pack(value)
        End Function

        Public Sub reg(ByVal index As Byte, ByVal parser_packer As IJar(Of Object))
            Contract.Requires(parser_packer IsNot Nothing)
            If parsers(index) IsNot Nothing Then Throw New InvalidOperationException("Parser already registered to index " + index.ToString())
            If packers(index) IsNot Nothing Then Throw New InvalidOperationException("Packer already registered to index " + index.ToString())
            parsers(index) = parser_packer
            packers(index) = parser_packer
        End Sub
        Public Sub regParser(ByVal index As Byte, ByVal parser As IParseJar(Of Object))
            If Not (parser IsNot Nothing) Then Throw New ArgumentException()
            If parsers(index) IsNot Nothing Then Throw New InvalidOperationException("Parser already registered to index " + index.ToString())
            parsers(index) = parser
        End Sub
        Public Sub regPacker(ByVal index As Byte, ByVal packer As IPackJar(Of Object))
            If Not (packer IsNot Nothing) Then Throw New ArgumentException()
            If packers(index) IsNot Nothing Then Throw New InvalidOperationException("Packer already registered to index " + index.ToString())
            packers(index) = packer
        End Sub
    End Class
    Public Class EmptyJar
        Inherits Jar(Of Object)
        Public Sub New(ByVal name As String)
            MyBase.New(name)
            Contract.Requires(name IsNot Nothing)
        End Sub
        Public Overrides Function Pack(Of R As Object)(ByVal value As R) As IPickle(Of R)
            Return New Pickle(Of R)(Me.Name, Nothing, New Byte() {}.ToView(), Function() "[Field Skipped]")
        End Function
        Public Overrides Function Parse(ByVal view As ViewableList(Of Byte)) As IPickle(Of Object)
            Return New Pickle(Of Object)(Me.Name, Nothing, New Byte() {}.ToView(), Function() "[Field Skipped]")
        End Function
    End Class
End Namespace