Imports Tinker.Pickling

Public Module StreamExtensions
    <Extension()>
    Public Sub WriteNullTerminatedString(ByVal bw As IWritableStream, ByVal data As String)
        Contract.Requires(bw IsNot Nothing)
        Contract.Requires(data IsNot Nothing)
        bw.Write(data.ToAscBytes(True).AsReadableList)
    End Sub
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

    <DebuggerDisplay("{ToString}")>
    Private Class StreamAsList
        Inherits DisposableWithTask
        Implements IReadableList(Of Byte)

        Private ReadOnly _stream As IRandomReadableStream
        Private ReadOnly _offset As Long
        Private ReadOnly _takeOwnershipofStream As Boolean

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_stream IsNot Nothing)
            Contract.Invariant(_offset >= 0)
            Contract.Invariant(_offset <= _stream.Length)
        End Sub

        Public Sub New(ByVal stream As IRandomReadableStream,
                       ByVal offset As Long,
                       ByVal takeOwnershipOfStream As Boolean)
            Contract.Requires(stream IsNot Nothing)
            Contract.Requires(offset >= 0)
            Contract.Requires(offset <= stream.Length)
            Me._stream = stream
            Me._offset = offset
            Me._takeOwnershipofStream = takeOwnershipOfStream
        End Sub

        <ContractVerification(False)>
        Public Function Contains(ByVal item As Byte) As Boolean Implements IReadableCollection(Of Byte).Contains
            Return (From e In Me Where item = e).Any
        End Function

        Public ReadOnly Property Count As Integer Implements IReadableCollection(Of Byte).Count
            Get
                Return CInt(_stream.Length - _offset)
            End Get
        End Property

        <ContractVerification(False)>
        Public Function IndexOf(ByVal item As Byte) As Integer Implements IReadableList(Of Byte).IndexOf
            Return (From i In Count.Range Where Me.Item(i) = item).OffsetBy(1).FirstOrDefault - 1
        End Function
        Default Public ReadOnly Property Item(ByVal index As Integer) As Byte Implements IReadableList(Of Byte).Item
            <ContractVerification(False)>
            Get
                If Me.IsDisposed Then Throw New ObjectDisposedException(Me.GetType.FullName)
                Return _stream.ReadExactAt(position:=_offset + index, exactCount:=1)(0)
            End Get
        End Property

        Public Function GetEnumerator() As IEnumerator(Of Byte) Implements IEnumerable(Of Byte).GetEnumerator
            Return (From i In Count.Range Select Me.Item(i)).GetEnumerator
        End Function
        Private Function GetEnumeratorObj() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
            Return GetEnumerator()
        End Function

        Public Overrides Function ToString() As String
            If Me.Count > 10 Then
                Return "[{0}, ...".Frmt(Me.Take(10).StringJoin(", "))
            Else
                Return "[{0}]".Frmt(Me.StringJoin(", "))
            End If
        End Function

        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As Task
            If Not finalizing AndAlso _takeOwnershipofStream Then _stream.Dispose()
            Return Nothing
        End Function
    End Class

    <Extension()>
    Public Function ReadPickle(Of T)(ByVal stream As IRandomReadableStream, ByVal jar As IParseJar(Of T)) As IPickle(Of T)
        Contract.Requires(stream IsNot Nothing)
        Contract.Requires(jar IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IPickle(Of T))() IsNot Nothing)
        Contract.Ensures(stream.Position = Contract.OldValue(stream.Position) + Contract.Result(Of IPickle(Of T)).Data.Count)
        Return DirectCast(stream.ReadPickle(DirectCast(jar, ISimpleParseJar)), IPickle(Of T))
    End Function
    <Extension()>
    <ContractVerification(False)>
    Public Function ReadPickle(ByVal stream As IRandomReadableStream, ByVal jar As ISimpleParseJar) As ISimplePickle
        Contract.Requires(stream IsNot Nothing)
        Contract.Requires(jar IsNot Nothing)
        Contract.Ensures(Contract.Result(Of ISimplePickle)() IsNot Nothing)
        Contract.Ensures(stream.Position = Contract.OldValue(stream.Position) + Contract.Result(Of ISimplePickle).Data.Count)
        Try
            Dim oldPosition = stream.Position
            Using view = New StreamAsList(stream, oldPosition, takeOwnershipOfStream:=False)
                '[first parse: learn needed data]
                Dim result = jar.Parse(view)
                '[second parse: work with copied data, to avoid accessing the disposed StreamAsList later]
                result = jar.Parse(result.Data.Cache.ToReadableList)
                'Place position after used data, as if it had been read normally
                stream.Position = oldPosition + result.Data.Count
                Return result
            End Using
        Catch ex As PicklingException
            Throw New IO.InvalidDataException(ex.Summarize, ex)
        End Try
    End Function

    <Extension()>
    <ContractVerification(False)>
    Public Function WritePickle(Of T)(ByVal stream As IWritableStream, ByVal jar As IPackJar(Of T), ByVal value As T) As IPickle(Of T)
        Contract.Requires(stream IsNot Nothing)
        Contract.Requires(jar IsNot Nothing)
        Contract.Requires(value IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IPickle(Of T))() IsNot Nothing)
        Dim result = jar.Pack(value)
        stream.Write(result.Data)
        Return result
    End Function
End Module
