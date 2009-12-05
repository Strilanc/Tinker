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
                       Optional ByVal takeRest As Boolean = False)
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

        Protected Overridable Function DescribeValue(ByVal value As Byte()) As String
            Contract.Requires(value IsNot Nothing)
            Return "[{0}]".Frmt(value.ToHexString)
        End Function

        Public Overrides Function Pack(Of TValue As Byte())(ByVal value As TValue) As IPickle(Of TValue)
            Dim val = CType(value, Byte()).AssumeNotNull
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
                Dim ds = CUInt(size).Bytes(size:=sizePrefixSize)
                If ds.Length <> sizePrefixSize Then Throw New PicklingException("Unable to fit size into prefix.")
                For i = 0 To sizePrefixSize - 1
                    data(i) = ds(i)
                Next i
            End If
            For i = 0 To size - 1
                data(i + offset) = val(i)
            Next i

            Return New Pickle(Of TValue)(Me.Name, value, data.ToView(), Function() DescribeValue(val))
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
                outputSize = CInt(data.SubView(pos, sizePrefixSize).ToUInt32())
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
        Private ReadOnly maximumContentSize As Integer
        Private ReadOnly expectedSize As Integer
        Private ReadOnly reversed As Boolean

        Public Sub New(ByVal name As String,
                       Optional ByVal nullTerminated As Boolean = True,
                       Optional ByVal reversed As Boolean = False,
                       Optional ByVal expectedSize As Integer = 0,
                       Optional ByVal maximumContentSize As Integer = 0,
                       Optional ByVal info As String = "No Info")
            MyBase.New(name)
            Contract.Requires(name IsNot Nothing)
            Contract.Requires(info IsNot Nothing)
            Contract.Requires(maximumContentSize >= 0)
            If expectedSize < 0 Then Throw New ArgumentOutOfRangeException("expectedSize")
            If expectedSize = 0 And Not nullTerminated Then Throw New ArgumentException(Me.GetType.Name + " must be either nullTerminated or have an expectedSize")
            Me.nullTerminated = nullTerminated
            Me.expectedSize = expectedSize
            Me.maximumContentSize = maximumContentSize
            Me.reversed = reversed
        End Sub

        Public Overrides Function Pack(Of TValue As String)(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            Dim size = value.Length + If(nullTerminated, 1, 0)
            If expectedSize <> 0 And size <> expectedSize Then Throw New PicklingException("Size doesn't match expected size")
            If maximumContentSize <> 0 AndAlso value.Length > maximumContentSize Then Throw New PicklingException("Size exceeds maximum size.")
            'Pack
            Dim data(0 To size - 1) As Byte
            If nullTerminated Then size -= 1
            Dim i = 0
            While size > 0
                size -= 1
                data(If(reversed, size, i)) = CByte(Asc(value(i)))
                i += 1
            End While

            Return New Pickle(Of TValue)(Me.Name, value, data.ToView(), Function() """{0}""".Frmt(value))
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
                    Return New Pickle(Of String)(Me.Name, "", data, Function() """")
                End If
            End If
            Dim outputSize = inputSize - If(nullTerminated, 1, 0)
            'Validate
            If data.Length < inputSize Then Throw New PicklingException("Not enough data")
            If maximumContentSize <> 0 AndAlso outputSize > maximumContentSize Then Throw New PicklingException("Size exceeds maximum size.")

            'Parse string data
            Dim cc(0 To outputSize - 1) As Char
            Dim i = 0
            While outputSize > 0
                outputSize -= 1
                Dim j = If(reversed, outputSize, i)
                Contract.Assume(i < data.Length)
                cc(j) = Chr(data(i))
                i += 1
            End While

            Return New Pickle(Of String)(Me.Name, cc, data.SubView(0, inputSize), Function() """{0}""".Frmt(CStr(cc)))
        End Function
    End Class
    Public Class FusionJar(Of T)
        Inherits Jar(Of T)
        Public ReadOnly packer As IPackJar(Of T)
        Public ReadOnly parser As IParseJar(Of T)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(parser IsNot Nothing)
            Contract.Invariant(packer IsNot Nothing)
        End Sub

        Public Sub New(ByVal packer As IPackJar(Of T), ByVal parser As IParseJar(Of T))
            MyBase.New(packer.Name)
            Contract.Requires(packer IsNot Nothing)
            Contract.Requires(parser IsNot Nothing)
            If packer.Name <> parser.Name Then Throw New ArgumentException("Parser must match packer.", "parser")
            Me.packer = packer
            Me.parser = parser
        End Sub

        Public Overrides Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue)
            Return packer.Pack(value)
        End Function

        Public Overrides Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of T)
            Return parser.Parse(data)
        End Function
    End Class

    '''<summary>Combines jars to pickle ordered tuples of objects</summary>
    Public Class TuplePackJar
        Inherits PackJar(Of Dictionary(Of InvariantString, Object))
        Private ReadOnly subJars() As IPackJar(Of Object)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(subJars IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As String)
            MyBase.New(name)
            Contract.Requires(name IsNot Nothing)
            Me.subJars = New IPackJar(Of Object)() {}
        End Sub
        Public Sub New(ByVal name As String, ByVal ParamArray subJars() As IPackJar(Of Object))
            MyBase.New(name)
            Contract.Requires(name IsNot Nothing)
            Contract.Requires(subJars IsNot Nothing)
            Me.subJars = subJars
        End Sub
        Public Function Extend(Of T)(ByVal jar As IPackJar(Of T)) As TuplePackJar
            Contract.Requires(jar IsNot Nothing)
            Return New TuplePackJar(Name, Concat(subJars, New IPackJar(Of Object)() {jar.Weaken}))
        End Function

        Public Overrides Function Pack(Of TValue As Dictionary(Of InvariantString, Object))(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            If value.Keys.Count > subJars.Count Then Throw New PicklingException("Too many keys in dictionary")

            'Pack
            Dim pickles = New List(Of IPickle(Of Object))
            For Each subJar In subJars
                Contract.Assume(subJar IsNot Nothing)
                If Not value.ContainsKey(subJar.Name) Then Throw New PicklingException("Key '{0}' missing from tuple dictionary.".Frmt(subJar.Name))
                Contract.Assume(value(subJar.Name) IsNot Nothing)
                pickles.Add(subJar.Pack(value(subJar.Name)))
            Next subJar
            Return New Pickle(Of TValue)(Me.Name, value, Concat(From p In pickles Select p.Data.ToArray).ToView(), Function() Pickle(Of Object).MakeListDescription(pickles))
        End Function
    End Class
    Public Class TupleParseJar
        Inherits ParseJar(Of Dictionary(Of InvariantString, Object))
        Private ReadOnly subJars() As IParseJar(Of Object)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(subJars IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As String, ByVal ParamArray subJars() As IParseJar(Of Object))
            MyBase.New(name)
            Contract.Requires(name IsNot Nothing)
            Contract.Requires(subJars IsNot Nothing)
            Me.subJars = subJars
        End Sub
        Public Function Extend(ByVal jar As IParseJar(Of Object)) As TupleParseJar
            Contract.Requires(jar IsNot Nothing)
            Return New TupleParseJar(Name, Concat(subJars, {jar}))
        End Function
        Public Function ExWeak(Of T)(ByVal jar As IParseJar(Of T)) As TupleParseJar
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(jar IsNot Nothing)
            Return New TupleParseJar(Name, Concat(subJars, {jar.Weaken}))
        End Function

        Public Overrides Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of Dictionary(Of InvariantString, Object))
            'Parse
            Dim vals = New Dictionary(Of InvariantString, Object)
            Dim pickles = New List(Of IPickle(Of Object))
            Dim curCount = data.Length
            Dim curOffset = 0
            For Each j In subJars
                Contract.Assume(j IsNot Nothing)
                'Value
                Dim p = j.Parse(data.SubView(curOffset, curCount))
                vals(j.Name) = p.Value
                pickles.Add(p)
                'Size
                Dim n = p.Data.Length
                curCount -= n
                curOffset += n
                If curCount < 0 Then Throw New InvalidStateException("subJar lied about data used.")
            Next j

            Return New Pickle(Of Dictionary(Of InvariantString, Object))(Me.Name, vals, data.SubView(0, curOffset), Function() Pickle(Of Object).MakeListDescription(pickles))
        End Function
    End Class

    Public Class TupleJar
        Inherits FusionJar(Of Dictionary(Of InvariantString, Object))
        Private ReadOnly subJars() As IJar(Of Object)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(subJars IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As String, ByVal ParamArray subJars() As IJar(Of Object))
            MyBase.New(New TuplePackJar(name, subJars), New TupleParseJar(name, subJars))
            Contract.Requires(name IsNot Nothing)
            Contract.Requires(subJars IsNot Nothing)
            Me.subJars = subJars
        End Sub
        Public Function Extend(Of T)(ByVal jar As IJar(Of T)) As TupleJar
            Contract.Requires(jar IsNot Nothing)
            Return New TupleJar(Name, Concat(subJars, {jar.Weaken}))
        End Function

        Public Overrides Function Pack(Of TValue As Dictionary(Of InvariantString, Object))(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            If value.Keys.Count > subJars.Count Then Throw New PicklingException("Too many keys in dictionary")

            'Pack
            Dim pickles = New List(Of IPickle(Of Object))
            For Each subjar In subJars
                Contract.Assume(subJar IsNot Nothing)
                If Not value.ContainsKey(subjar.Name) Then Throw New PicklingException("Key '{0}' missing from tuple dictionary.".Frmt(subjar.Name))
                Contract.Assume(value(subjar.Name) IsNot Nothing)
                pickles.Add(subjar.Pack(value(subjar.Name)))
            Next subjar
            Return New Pickle(Of TValue)(Me.Name, value, Concat(From p In pickles Select p.Data.ToArray).ToView(), Function() Pickle(Of Object).MakeListDescription(pickles))
        End Function

        Public Overrides Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of Dictionary(Of InvariantString, Object))
            'Parse
            Dim vals = New Dictionary(Of InvariantString, Object)
            Dim pickles = New List(Of IPickle(Of Object))
            Dim curCount = data.Length
            Dim curOffset = 0
            For Each subJar In subJars
                Contract.Assume(subJar IsNot Nothing)
                'Value
                Dim p = subJar.Parse(data.SubView(curOffset, curCount))
                vals(subJar.Name) = p.Value
                pickles.Add(p)
                'Size
                Dim n = p.Data.Length
                curCount -= n
                curOffset += n
                If curCount < 0 Then Throw New InvalidStateException("subJar reported incorrect data length")
            Next subJar

            Return New Pickle(Of Dictionary(Of InvariantString, Object))(Me.Name, vals, data.SubView(0, curOffset), Function() Pickle(Of Object).MakeListDescription(pickles))
        End Function
    End Class

    Public Class ListParseJar(Of T)
        Inherits ParseJar(Of List(Of T))
        Private ReadOnly sizeJar As ValueJar
        Private ReadOnly subJar As IParseJar(Of T)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(sizeJar IsNot Nothing)
            Contract.Invariant(subJar IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As String,
                       ByVal subJar As IParseJar(Of T),
                       Optional ByVal numSizePrefixBytes As Integer = 1)
            MyBase.New(name)
            Contract.Requires(name IsNot Nothing)
            Contract.Requires(subJar IsNot Nothing)
            Contract.Requires(numSizePrefixBytes > 0)
            Contract.Requires(numSizePrefixBytes <= 8)
            Me.subJar = subJar
            Me.sizeJar = New ValueJar("size prefix", numSizePrefixBytes)
        End Sub

        Public Overrides Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of List(Of T))
            'Parse
            Dim vals As New List(Of T)
            Dim pickles As New List(Of IPickle(Of Object))
            Dim curOffset = 0
            'List Size
            Dim sz = sizeJar.Parse(data)
            Dim numElements = sz.Value
            curOffset += sz.Data.Length
            'List Elements
            For repeat = 1UL To numElements
                Contract.Assume(curOffset <= data.Length)
                'Value
                Dim p = subJar.Parse(data.SubView(curOffset, data.Length - curOffset))
                vals.Add(p.Value)
                pickles.Add(New Pickle(Of Object)(p.Value, p.Data, p.Description))
                'Size
                Dim n = p.Data.Length
                curOffset += n
            Next repeat

            Return New Pickle(Of List(Of T))(Me.Name, vals, data.SubView(0, curOffset), Function() Pickle(Of Object).MakeListDescription(pickles))
        End Function
    End Class
    Public NotInheritable Class ListPackJar(Of T)
        Inherits PackJar(Of List(Of T))
        Private ReadOnly subJar As IPackJar(Of T)
        Private ReadOnly prefixSize As Integer

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(prefixSize > 0)
            Contract.Invariant(subJar IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As String,
                       ByVal subJar As IPackJar(Of T),
                       Optional ByVal prefixSize As Integer = 1)
            MyBase.New(name)
            Contract.Requires(name IsNot Nothing)
            Contract.Requires(subJar IsNot Nothing)
            Contract.Requires(prefixSize > 0)
            Me.subJar = subJar
            Me.prefixSize = prefixSize
        End Sub

        Public Overrides Function Pack(Of TValue As List(Of T))(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            Dim pickles = (From e In value Select CType(subJar.Pack(e), IPickle(Of T))).ToList()
            Dim data = Concat(CUInt(value.Count).Bytes(size:=prefixSize), Concat(From p In pickles Select p.Data.ToArray))
            Return New Pickle(Of TValue)(Me.Name, value, data.ToView(), Function() Pickle(Of T).MakeListDescription(pickles))
        End Function
    End Class
    Public NotInheritable Class ListJar(Of T)
        Inherits FusionJar(Of List(Of T))
        Public Sub New(ByVal name As String,
                       ByVal subJar As IJar(Of T),
                       Optional ByVal prefixSize As Integer = 1)
            MyBase.New(New ListPackJar(Of T)(name, subJar, prefixSize), New ListParseJar(Of T)(name, subJar, prefixSize))
            Contract.Requires(name IsNot Nothing)
            Contract.Requires(subJar IsNot Nothing)
            Contract.Requires(prefixSize > 0)
            Contract.Requires(prefixSize <= 8)
        End Sub
    End Class

    Public NotInheritable Class RepeatingParseJar(Of T)
        Inherits ParseJar(Of List(Of T))
        Private ReadOnly subJar As IParseJar(Of T)

        Public Sub New(ByVal name As String,
                       ByVal subJar As IParseJar(Of T))
            MyBase.New(name)
            Contract.Requires(name IsNot Nothing)
            Contract.Requires(subJar IsNot Nothing)
            Me.subJar = subJar
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
                Dim p = subJar.Parse(data.SubView(curOffset, curCount))
                vals.Add(p.Value)
                pickles.Add(New Pickle(Of Object)(p.Value, p.Data, p.Description))
                'Size
                Dim n = p.Data.Length
                curCount -= n
                curOffset += n
            End While

            Return New Pickle(Of List(Of T))(Me.Name, vals, data.SubView(0, curOffset), Function() Pickle(Of Object).MakeListDescription(pickles))
        End Function
    End Class
    Public NotInheritable Class RepeatingPackJar(Of T)
        Inherits PackJar(Of List(Of T))
        Private ReadOnly subJar As IPackJar(Of T)

        Public Sub New(ByVal name As String,
                       ByVal subJar As IPackJar(Of T))
            MyBase.New(name)
            Contract.Requires(name IsNot Nothing)
            Contract.Requires(subJar IsNot Nothing)
            Me.subJar = subJar
        End Sub

        Public Overrides Function Pack(Of TValue As List(Of T))(ByVal value As TValue) As IPickle(Of TValue)
            Dim pickles = (From e In value Select CType(subJar.Pack(e), IPickle(Of T))).ToList()
            Dim data = Concat(From p In pickles Select p.Data.ToArray)
            Return New Pickle(Of TValue)(Me.Name, value, data.ToView(), Function() Pickle(Of T).MakeListDescription(pickles))
        End Function
    End Class
    Public NotInheritable Class RepeatingJar(Of T)
        Inherits FusionJar(Of List(Of T))
        Public Sub New(ByVal name As String,
                       ByVal subJar As IJar(Of T))
            MyBase.New(New RepeatingPackJar(Of T)(name, subJar), New RepeatingParseJar(Of T)(name, subJar))
            Contract.Requires(name IsNot Nothing)
            Contract.Requires(subJar IsNot Nothing)
        End Sub
    End Class

    Public NotInheritable Class InteriorSwitchJar(Of T)
        Inherits Jar(Of T)
        Private ReadOnly packers(0 To 255) As IPackJar(Of T)
        Private ReadOnly parsers(0 To 255) As IParseJar(Of T)
        Private ReadOnly valueIndexExtractor As Func(Of T, Byte)
        Private ReadOnly dataIndexExtractor As Func(Of ViewableList(Of Byte), Byte)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(packers IsNot Nothing)
            Contract.Invariant(parsers IsNot Nothing)
            Contract.Invariant(valueIndexExtractor IsNot Nothing)
            Contract.Invariant(dataIndexExtractor IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As String,
                       ByVal valueIndexExtractor As Func(Of T, Byte),
                       ByVal dataIndexExtractor As Func(Of ViewableList(Of Byte), Byte))
            MyBase.new(name)
            Contract.Requires(name IsNot Nothing)
            Contract.Requires(valueIndexExtractor IsNot Nothing)
            Contract.Requires(dataIndexExtractor IsNot Nothing)
            Me.valueIndexExtractor = valueIndexExtractor
            Me.dataIndexExtractor = dataIndexExtractor
        End Sub

        Public Overrides Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of T)
            Dim index = dataIndexExtractor(data)
            If parsers(index) Is Nothing Then Throw New PicklingException("No parser registered to {0}.".Frmt(index))
            Return parsers(index).Parse(data)
        End Function
        Public Overrides Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue)
            Dim index = valueIndexExtractor(value)
            If packers(index) Is Nothing Then Throw New PicklingException("No packer registered to {0}.".Frmt(index))
            Return packers(index).Pack(value)
        End Function

        Public Sub AddPackerParser(ByVal index As Byte, ByVal jar As IJar(Of T))
            Contract.Requires(jar IsNot Nothing)
            If parsers(index) IsNot Nothing Then Throw New InvalidOperationException("Parser already registered to index " + index.ToString(CultureInfo.InvariantCulture))
            If packers(index) IsNot Nothing Then Throw New InvalidOperationException("Packer already registered to index " + index.ToString(CultureInfo.InvariantCulture))
            parsers(index) = jar
            packers(index) = jar
        End Sub
        Public Sub AddParser(ByVal index As Byte, ByVal parser As IParseJar(Of T))
            Contract.Requires(parser IsNot Nothing)
            If parsers(index) IsNot Nothing Then Throw New InvalidOperationException("Parser already registered to index " + index.ToString(CultureInfo.InvariantCulture))
            parsers(index) = parser
        End Sub
        Public Sub AddPacker(ByVal index As Byte, ByVal packer As IPackJar(Of T))
            Contract.Requires(packer IsNot Nothing)
            If packers(index) IsNot Nothing Then Throw New InvalidOperationException("Packer already registered to index " + index.ToString(CultureInfo.InvariantCulture))
            packers(index) = packer
        End Sub
    End Class
    Public NotInheritable Class PrefixPickle(Of T)
        Public ReadOnly index As T
        Public ReadOnly payload As IPickle(Of Object)
        Public Sub New(ByVal index As T, ByVal payload As IPickle(Of Object))
            Contract.Requires(payload IsNot Nothing)
            Me.index = index
            Me.payload = payload
        End Sub
    End Class
    Public NotInheritable Class PrefixSwitchJar(Of T)
        Inherits Jar(Of PrefixPickle(Of T))
        Private ReadOnly packers(0 To 255) As IPackJar(Of Object)
        Private ReadOnly parsers(0 To 255) As IParseJar(Of Object)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(packers IsNot Nothing)
            Contract.Invariant(parsers IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As String)
            MyBase.new(name)
        End Sub

        Public Overrides Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of PrefixPickle(Of T))
            Dim index = CByte(data(0))
            Dim vindex = CType(CType(index, Object), T)
            If parsers(index) Is Nothing Then Throw New PicklingException("No parser registered to " + vindex.ToString())
            Dim payload = New PrefixPickle(Of T)(vindex, parsers(index).Parse(data.SubView(1)))
            Return New Pickle(Of PrefixPickle(Of T))(Name, payload, data.SubView(0, payload.payload.Data.Length + 1))
        End Function
        Public Overrides Function Pack(Of TValue As PrefixPickle(Of T))(ByVal value As TValue) As IPickle(Of TValue)
            Dim index = CByte(CType(value.index, Object))
            If packers(index) Is Nothing Then Throw New PicklingException("No packer registered to " + value.index.ToString())
            Return New Pickle(Of TValue)(Name, value, Concat({index}, packers(index).Pack(value.payload.Value).Data.ToArray).ToView)
        End Function

        Public Sub AddPackerParser(ByVal index As Byte, ByVal jar As IJar(Of Object))
            Contract.Requires(jar IsNot Nothing)
            If parsers(index) IsNot Nothing Then Throw New InvalidOperationException("Parser already registered to index {0}.".Frmt(index))
            If packers(index) IsNot Nothing Then Throw New InvalidOperationException("Packer already registered to index {0}.".Frmt(index))
            parsers(index) = jar
            packers(index) = jar
        End Sub
        Public Sub AddParser(ByVal index As Byte, ByVal parser As IParseJar(Of Object))
            Contract.Requires(parser IsNot Nothing)
            If parsers(index) IsNot Nothing Then Throw New InvalidOperationException("Parser already registered to index {0}.".Frmt(index))
            parsers(index) = parser
        End Sub
        Public Sub AddPacker(ByVal index As Byte, ByVal packer As IPackJar(Of Object))
            Contract.Requires(packer IsNot Nothing)
            If packers(index) IsNot Nothing Then Throw New InvalidOperationException("Packer already registered to index {0}.".Frmt(index))
            packers(index) = packer
        End Sub
    End Class
    Public NotInheritable Class ManualSwitchJar
        Private ReadOnly packers(0 To 255) As IPackJar(Of Object)
        Private ReadOnly parsers(0 To 255) As IParseJar(Of Object)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(packers IsNot Nothing)
            Contract.Invariant(parsers IsNot Nothing)
        End Sub

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
            Contract.Requires(parsers IsNot Nothing)
            Contract.Requires(packers IsNot Nothing)
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
            If parsers(index) Is Nothing Then Throw New PicklingException("No parser registered to {0}.".Frmt(index))
            Return parsers(index).Parse(data)
        End Function
        Public Overloads Function Pack(Of TValue)(ByVal index As Byte, ByVal value As TValue) As IPickle(Of TValue)
            Contract.Requires(value IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IPickle(Of TValue))() IsNot Nothing)
            If packers(index) Is Nothing Then Throw New PicklingException("No packer registered to {0}.".Frmt(index))
            Return packers(index).Pack(value)
        End Function

        Public Sub AddPackerParser(ByVal index As Byte, ByVal jar As IJar(Of Object))
            Contract.Requires(jar IsNot Nothing)
            If parsers(index) IsNot Nothing Then Throw New InvalidOperationException("Parser already registered to index " + index.ToString(CultureInfo.InvariantCulture))
            If packers(index) IsNot Nothing Then Throw New InvalidOperationException("Packer already registered to index " + index.ToString(CultureInfo.InvariantCulture))
            parsers(index) = jar
            packers(index) = jar
        End Sub
        Public Sub AddParser(ByVal index As Byte, ByVal parser As IParseJar(Of Object))
            Contract.Requires(parser IsNot Nothing)
            If parsers(index) IsNot Nothing Then Throw New InvalidOperationException("Parser already registered to index " + index.ToString(CultureInfo.InvariantCulture))
            parsers(index) = parser
        End Sub
        Public Sub AddPacker(ByVal index As Byte, ByVal packer As IPackJar(Of Object))
            Contract.Requires(packer IsNot Nothing)
            If packers(index) IsNot Nothing Then Throw New InvalidOperationException("Packer already registered to index " + index.ToString(CultureInfo.InvariantCulture))
            packers(index) = packer
        End Sub
    End Class
    Public NotInheritable Class EmptyJar
        Inherits Jar(Of Object)
        Public Sub New(ByVal name As String)
            MyBase.New(name)
            Contract.Requires(name IsNot Nothing)
        End Sub
        Public Overrides Function Pack(Of TValue As Object)(ByVal value As TValue) As IPickle(Of TValue)
            Return New Pickle(Of TValue)(Me.Name, Nothing, New Byte() {}.ToView(), Function() "[Field Skipped]")
        End Function
        Public Overrides Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of Object)
            Return New Pickle(Of Object)(Me.Name, Nothing, New Byte() {}.ToView(), Function() "[Field Skipped]")
        End Function
    End Class
End Namespace