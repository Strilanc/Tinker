Namespace Pickling
    '''<summary>Pickles values with data of a specified size.</summary>
    Public NotInheritable Class FixedSizeFramingJar(Of T)
        Inherits BaseJar(Of T)

        Private ReadOnly _subJar As IJar(Of T)
        Private ReadOnly _dataSize As Integer

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_subJar IsNot Nothing)
            Contract.Invariant(_dataSize >= 0)
        End Sub

        Public Sub New(ByVal subJar As IJar(Of T),
                       ByVal dataSize As Integer)
            Contract.Requires(subJar IsNot Nothing)
            Contract.Requires(dataSize >= 0)
            Me._subJar = subJar
            Me._dataSize = dataSize
        End Sub

        Public Overrides Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            Dim pickle = _subJar.Pack(value)
            If pickle.Data.Count <> _dataSize Then Throw New PicklingException("Packed data did not take exactly {0} bytes.".Frmt(_dataSize))
            Return pickle
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T)
            If data.Count < _dataSize Then Throw New PicklingNotEnoughDataException()
            Dim result As IPickle(Of T)
            Try
                result = _subJar.Parse(data.SubView(0, _dataSize))
            Catch ex As PicklingException
                '[Only wrap the exception as 'too limited data' if allowing all data causes it to go away]
                Try
                    Dim pickle = _subJar.Parse(data)
                    Throw New PicklingException("Pickled value could not be parsed from limited data.", ex)
                Catch exIgnored As PicklingException
                End Try
                Throw
            End Try
            If result.Data.Count <> _dataSize Then Throw New PicklingException("Parsed value did not use exactly {0} bytes.".Frmt(_dataSize))
            Return result
        End Function
    End Class

    '''<summary>Pickles values with data up to a maximum size.</summary>
    Public Class LimitedSizeFramingJar(Of T)
        Inherits BaseJar(Of T)

        Private ReadOnly _subJar As IJar(Of T)
        Private ReadOnly _maxDataCount As Integer

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_subJar IsNot Nothing)
            Contract.Invariant(_maxDataCount >= 0)
        End Sub

        Public Sub New(ByVal subJar As IJar(Of T),
                       ByVal maxDataCount As Integer)
            Contract.Requires(subJar IsNot Nothing)
            Contract.Requires(maxDataCount >= 0)
            Me._subJar = subJar
            Me._maxDataCount = maxDataCount
        End Sub

        Public Overrides Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            Dim pickle = _subJar.Pack(value)
            If pickle.Data.Count > _maxDataCount Then Throw New PicklingException("Packed data did not fit in {0} bytes.".Frmt(_maxDataCount))
            Return pickle
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T)
            Try
                Return _subJar.Parse(data.SubView(0, Math.Min(data.Count, _maxDataCount)))
            Catch ex As PicklingException
                '[Only wrap the exception as 'too limited data' if allowing all data causes it to go away]
                Try
                    Dim pickle = _subJar.Parse(data)
                    Throw New PicklingException("Pickled value could not be parsed from limited data.", ex)
                Catch exIgnored As PicklingException
                End Try
                Throw
            End Try
        End Function
    End Class

    '''<summary>Pickles values with data prefixed by a count of the number of bytes (not counting the prefix).</summary>
    Public NotInheritable Class SizePrefixedFramingJar(Of T)
        Inherits BaseJar(Of T)

        Private ReadOnly _subJar As IJar(Of T)
        Private ReadOnly _prefixSize As Integer

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_subJar IsNot Nothing)
            Contract.Invariant(_prefixSize > 0)
            Contract.Invariant(_prefixSize <= 8)
        End Sub

        Public Sub New(ByVal subJar As IJar(Of T),
                       ByVal prefixSize As Integer)
            Contract.Requires(prefixSize > 0)
            Contract.Requires(subJar IsNot Nothing)
            If prefixSize > 8 Then Throw New ArgumentOutOfRangeException("prefixSize", "prefixSize must be less than or equal to 8.")
            Me._subJar = subJar
            Me._prefixSize = prefixSize
        End Sub

        Public Overrides Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue)
            Dim pickle = _subJar.Pack(value)
            Dim sizeBytes = CULng(pickle.Data.Count).Bytes.Take(_prefixSize)
            If sizeBytes.Take(_prefixSize).ToUValue <> pickle.Data.Count Then Throw New PicklingException("Unable to fit byte count into size prefix.")
            Dim data = sizeBytes.Concat(pickle.Data).ToReadableList
            Return value.Pickled(data, pickle.Description)
        End Function

        <ContractVerification(False)>
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T)
            If data.Count < _prefixSize Then Throw New PicklingNotEnoughDataException()
            Dim dataSize = data.SubView(0, _prefixSize).ToUValue
            If data.Count < _prefixSize + dataSize Then Throw New PicklingNotEnoughDataException()

            Dim datum = data.SubView(0, CInt(_prefixSize + dataSize))
            Dim pickle = _subJar.Parse(datum.SubView(_prefixSize))
            If pickle.Data.Count < dataSize Then Throw New PicklingException("Fragmented data.")
            Return pickle.Value.Pickled(datum, pickle.Description)
        End Function
    End Class

    '''<summary>Pickles values with data followed by a null terminator.</summary>
    Public Class NullTerminatedFramingJar(Of T)
        Inherits BaseJar(Of T)

        Private ReadOnly _subJar As IJar(Of T)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_subJar IsNot Nothing)
        End Sub

        Public Sub New(ByVal subJar As IJar(Of T))
            Contract.Requires(subJar IsNot Nothing)
            Me._subJar = subJar
        End Sub

        Public Overrides Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            Dim pickle = _subJar.Pack(value)
            Return pickle.Value.Pickled(pickle.Data.Append(0).ToReadableList, pickle.Description)
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T)
            'Find terminator
            Dim p = data.IndexOf(0)
            If p < 0 Then Throw New PicklingException("No null terminator found.")
            Contract.Assume(p < data.Count)
            'Parse
            Dim pickle = _subJar.Parse(data.SubView(0, p))
            If pickle.Data.Count <> p Then Throw New PicklingException("Leftover data before null terminator.")
            Return pickle.Value.Pickled(data.SubView(0, p + 1), pickle.Description)
        End Function
    End Class

    '''<summary>Pickles values which may or may not be included in the data.</summary>
    Public NotInheritable Class OptionalFramingJar(Of T)
        Inherits BaseJar(Of Tuple(Of Boolean, T))

        Private ReadOnly _subJar As IJar(Of T)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_subJar IsNot Nothing)
        End Sub

        Public Sub New(ByVal subJar As IJar(Of T))
            Contract.Requires(subJar IsNot Nothing)
            Me._subJar = subJar
        End Sub

        Public Overrides Function Pack(Of TValue As Tuple(Of Boolean, T))(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            If value.Item1 Then
                Contract.Assume(value.Item2 IsNot Nothing)
                Dim pickle = _subJar.Pack(value.Item2)
                Return value.Pickled(pickle.Data, pickle.Description)
            Else
                Return value.Pickled(New Byte() {}.AsReadableList, Function() "[Not Included]")
            End If
        End Function

        <ContractVerification(False)>
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of Tuple(Of Boolean, T))
            If data.Count > 0 Then
                Dim pickle = _subJar.Parse(data)
                Return Tuple.Create(True, pickle.Value).Pickled(pickle.Data, pickle.Description)
            Else
                Return Tuple.Create(False, CType(Nothing, T)).Pickled(data, Function() "[Not Included]")
            End If
        End Function
    End Class

    '''<summary>Pickles values which may be included side-by-side in the data multiple times (including 0 times).</summary>
    Public NotInheritable Class RepeatedFramingJar(Of T)
        Inherits BaseJar(Of IReadableList(Of T))
        Private ReadOnly _subJar As IJar(Of T)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_subJar IsNot Nothing)
        End Sub

        Public Sub New(ByVal subJar As IJar(Of T))
            Contract.Requires(subJar IsNot Nothing)
            Me._subJar = subJar
        End Sub

        Public Overrides Function Pack(Of TValue As IReadableList(Of T))(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            Dim pickles = (From e In value Select CType(_subJar.Pack(e), IPickle(Of T))).Cache
            Dim data = Concat(From p In pickles Select (p.Data)).ToReadableList
            Return value.Pickled(data, Function() pickles.MakeListDescription())
        End Function

        'verification disabled due to stupid verifier (1.2.30118.5)
        <ContractVerification(False)>
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of IReadableList(Of T))
            'Parse
            Dim pickles = New List(Of IPickle(Of T))
            Dim curCount = data.Count
            Dim curOffset = 0
            'List Elements
            While curOffset < data.Count
                'Value
                Dim p = _subJar.Parse(data.SubView(curOffset, curCount))
                pickles.Add(p)
                'Size
                Dim n = p.Data.Count
                curCount -= n
                curOffset += n
            End While

            Dim datum = data.SubView(0, curOffset)
            Dim value = (From p In pickles Select (p.Value)).ToReadableList
            Return value.Pickled(datum, Function() pickles.MakeListDescription())
        End Function
    End Class

    '''<summary>Pickles values with data prefixed by a checksum.</summary>
    Public NotInheritable Class ChecksumPrefixedFramingJar(Of T)
        Inherits BaseJar(Of T)

        Private ReadOnly _subJar As IJar(Of T)
        Private ReadOnly _checksumFunction As Func(Of IReadableList(Of Byte), IReadableList(Of Byte))
        Private ReadOnly _checksumSize As Integer

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_subJar IsNot Nothing)
            Contract.Invariant(_checksumFunction IsNot Nothing)
            Contract.Invariant(_checksumSize > 0)
        End Sub

        Public Sub New(ByVal subJar As IJar(Of T),
                       ByVal checksumSize As Integer,
                       ByVal checksumFunction As Func(Of IReadableList(Of Byte), IReadableList(Of Byte)))
            Contract.Requires(checksumSize > 0)
            Contract.Requires(subJar IsNot Nothing)
            Contract.Requires(checksumFunction IsNot Nothing)
            Me._subJar = subJar
            Me._checksumSize = checksumSize
            Me._checksumFunction = checksumFunction
        End Sub

        Public Overrides Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue)
            Dim pickle = _subJar.Pack(value)
            Dim checksum = _checksumFunction(pickle.Data)
            Contract.Assume(checksum IsNot Nothing)
            Contract.Assume(checksum.Count = _checksumSize)
            Dim data = checksum.Concat(pickle.Data).ToReadableList
            Return value.Pickled(data, pickle.Description)
        End Function

        <ContractVerification(False)>
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T)
            If data.Count < _checksumSize Then Throw New PicklingNotEnoughDataException()
            Dim checksum = data.SubView(0, _checksumSize)
            Dim pickle = _subJar.Parse(data.SubView(_checksumSize))
            If Not _checksumFunction(pickle.Data).SequenceEqual(checksum) Then Throw New PicklingException("Checksum didn't match.")
            Dim datum = data.SubView(0, _checksumSize + pickle.Data.Count)
            Return pickle.Value.Pickled(datum, pickle.Description)
        End Function
    End Class
End Namespace
