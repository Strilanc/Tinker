Imports Tinker.Pickling

Public Module StreamExtensions
    <Extension()>
    Public Sub WriteNullTerminatedString(bw As IWritableStream, data As String)
        Contract.Requires(bw IsNot Nothing)
        Contract.Requires(data IsNot Nothing)
        bw.Write(data.ToAsciiBytes.Append(0).ToRist)
    End Sub
    <Extension()>
    Public Function ReadNullTerminatedString(reader As IReadableStream,
                                             maxLength As Integer) As String
        Contract.Requires(reader IsNot Nothing)
        Contract.Requires(maxLength >= 0)
        Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
        Contract.Ensures(Contract.Result(Of String)().Length <= maxLength)

        Dim data = New List(Of Byte)(capacity:=maxLength)
        Do
            Contract.Assert(data.Count <= maxLength)
            Dim b = reader.ReadByte()
            If b = 0 Then
                Dim result = data.ToAsciiChars.AsString
                Contract.Assume(result.Length <= maxLength)
                Return result
            ElseIf data.Count < maxLength Then
                data.Add(b)
            Else
                Throw New IO.InvalidDataException("Null-terminated string exceeded maximum length.")
            End If
        Loop
    End Function

    <DebuggerDisplay("{ToString()}")>
    Private Class StreamAsList
        Inherits DisposableWithTask
        Implements IRist(Of Byte)

        Private ReadOnly _stream As IRandomReadableStream
        Private ReadOnly _offset As Long
        Private ReadOnly _takeOwnershipofStream As Boolean

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_stream IsNot Nothing)
            Contract.Invariant(_offset >= 0)
            Contract.Invariant(_offset <= _stream.Length)
        End Sub

        Public Sub New(stream As IRandomReadableStream,
                       offset As Long,
                       takeOwnershipOfStream As Boolean)
            Contract.Requires(stream IsNot Nothing)
            Contract.Requires(offset >= 0)
            Contract.Requires(offset <= stream.Length)
            Me._stream = stream
            Me._offset = offset
            Me._takeOwnershipofStream = takeOwnershipOfStream
        End Sub

        Public ReadOnly Property Count As Integer Implements IRist(Of Byte).Count
            Get
                Return CInt(_stream.Length - _offset)
            End Get
        End Property

        Default Public ReadOnly Property Item(index As Integer) As Byte Implements IRist(Of Byte).Item
            Get
                If Me.IsDisposed Then Throw New ObjectDisposedException(Me.GetType.FullName)
                Contract.Assume(_offset + index + 1 <= _stream.Length)
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
                Return "[{0}, ...".Frmt(Me.TakeExact(10).StringJoin(", "))
            Else
                Return "[{0}]".Frmt(Me.StringJoin(", "))
            End If
        End Function

        <SuppressMessage("Microsoft.Contracts", "Invariant-57-23")>
        Protected Overrides Function PerformDispose(finalizing As Boolean) As Task
            If Not finalizing AndAlso _takeOwnershipofStream Then _stream.Dispose()
            Return Nothing
        End Function
    End Class

    <Extension()>
    <SuppressMessage("Microsoft.Contracts", "Ensures-76-188")>
    Public Function ReadPickle(stream As IRandomReadableStream, jar As IJar(Of Object)) As IPickle(Of Object)
        Contract.Requires(stream IsNot Nothing)
        Contract.Requires(jar IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IPickle(Of Object))() IsNot Nothing)
        Contract.Ensures(stream.Position = Contract.OldValue(stream.Position) + Contract.Result(Of IPickle(Of Object)).Data.Count)
        Try
            Dim oldPosition = stream.Position
            Using view = New StreamAsList(stream, oldPosition, takeOwnershipOfStream:=False)
                Dim parsed = jar.Parse(view)
                Contract.Assume(oldPosition + parsed.UsedDataCount <= stream.Length)
                Return jar.ParsePickle(stream.ReadExactAt(oldPosition, parsed.UsedDataCount))
            End Using
        Catch ex As PicklingException
            Throw New IO.InvalidDataException(ex.Summarize, ex)
        End Try
    End Function
    <Extension()>
    <SuppressMessage("Microsoft.Contracts", "Ensures-76-188")>
    Public Function ReadPickle(Of T)(stream As IRandomReadableStream, jar As IJar(Of T)) As IPickle(Of T)
        Contract.Requires(stream IsNot Nothing)
        Contract.Requires(jar IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IPickle(Of T))() IsNot Nothing)
        Contract.Ensures(stream.Position = Contract.OldValue(stream.Position) + Contract.Result(Of IPickle(Of T)).Data.Count)
        Try
            Dim oldPosition = stream.Position
            Using view = New StreamAsList(stream, oldPosition, takeOwnershipOfStream:=False)
                Dim parsed = jar.Parse(view)
                Contract.Assume(oldPosition + parsed.UsedDataCount <= stream.Length)
                Return jar.ParsePickle(stream.ReadExactAt(oldPosition, parsed.UsedDataCount))
            End Using
        Catch ex As PicklingException
            Throw New IO.InvalidDataException(ex.Summarize, ex)
        End Try
    End Function

    <Extension()>
    Public Function WritePickle(Of T)(stream As IWritableStream, jar As IJar(Of T), value As T) As IPickle(Of T)
        Contract.Requires(stream IsNot Nothing)
        Contract.Requires(jar IsNot Nothing)
        Contract.Requires(value IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IPickle(Of T))() IsNot Nothing)
        Dim result = jar.PackPickle(value)
        stream.Write(result.Data)
        Return result
    End Function
End Module
