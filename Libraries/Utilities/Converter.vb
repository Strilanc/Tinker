Imports System.Runtime.CompilerServices

<ContractClass(GetType(ContractClassForIConverter(Of ,)))>
Public Interface IConverter(Of In TInput, Out TOutput)
    Function Convert(ByVal sequence As IEnumerator(Of TInput)) As IEnumerator(Of TOutput)
End Interface
Public Interface IConverter(Of T)
    Inherits IConverter(Of T, T)
End Interface
<ContractClassFor(GetType(IConverter(Of ,)))>
Public Class ContractClassForIConverter(Of TInput, TOutput)
    Implements IConverter(Of TInput, TOutput)
    Public Function Convert(ByVal sequence As IEnumerator(Of TInput)) As IEnumerator(Of TOutput) Implements IConverter(Of TInput, TOutput).Convert
        Contract.Requires(sequence IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IEnumerator(Of TOutput))() IsNot Nothing)
        Throw New NotSupportedException()
    End Function
End Class

Friend Class EnumeratorStream
    Inherits IO.Stream
    Private ReadOnly sequence As IEnumerator(Of Byte)
    Public Sub New(ByVal sequence As IEnumerator(Of Byte))
        Contract.Requires(sequence IsNot Nothing)
        Me.sequence = sequence
    End Sub

    Public Overrides ReadOnly Property CanRead As Boolean
        Get
            Return True
        End Get
    End Property

    Public Shadows Function ReadByte() As Integer
        If Not sequence.MoveNext Then Return -1
        Return sequence.Current
    End Function
    Public Overrides Function Read(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer) As Integer
        For n = 0 To count - 1
            Dim r = ReadByte()
            If r = -1 Then Return n
            buffer(n + offset) = CByte(r)
        Next n
        Return count
    End Function

#Region "Not Supported"
    Public Overrides ReadOnly Property CanSeek As Boolean
        Get
            Return False
        End Get
    End Property
    Public Overrides ReadOnly Property CanWrite As Boolean
        Get
            Return False
        End Get
    End Property
    Public Overrides ReadOnly Property Length As Long
        Get
            Throw New NotSupportedException()
        End Get
    End Property

    Public Overrides Property Position As Long
        Get
            Throw New NotSupportedException()
        End Get
        Set(ByVal value As Long)
            Throw New NotSupportedException()
        End Set
    End Property
    Public Overrides Function Seek(ByVal offset As Long, ByVal origin As System.IO.SeekOrigin) As Long
        Throw New NotSupportedException()
    End Function
    Public Overrides Sub SetLength(ByVal value As Long)
        Throw New NotSupportedException()
    End Sub
    Public Overrides Sub Write(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer)
        Throw New NotSupportedException()
    End Sub
    Public Overrides Sub Flush()
    End Sub
#End Region
End Class

Friend Class PushEnumeratorStream
    Inherits IO.Stream
    Private ReadOnly pusher As PushEnumerator(Of Byte)
    Private closed As Boolean
    Public Sub New(ByVal pusher As PushEnumerator(Of Byte))
        Contract.Requires(pusher IsNot Nothing)
        Me.pusher = pusher
    End Sub

    Public Overrides ReadOnly Property CanWrite As Boolean
        Get
            Return True
        End Get
    End Property

    Public Overrides Sub Write(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer)
        If closed Then Throw New InvalidOperationException("Closed streams are not writable.")
        Dim index = 0
        pusher.Push(New Enumerator(Of Byte)(Function(controller)
                                                If index >= count Then  Return controller.Break()
                                                index += 1
                                                Return buffer(index + offset - 1)
                                            End Function))
    End Sub

    Public Overrides Sub Close()
        If closed Then Return
        pusher.PushDone()
        pusher.Dispose()
        MyBase.Close()
        closed = True
    End Sub

#Region "Not Supported"
    Public Overrides ReadOnly Property CanSeek As Boolean
        Get
            Return False
        End Get
    End Property
    Public Overrides ReadOnly Property CanRead As Boolean
        Get
            Return False
        End Get
    End Property
    Public Overrides ReadOnly Property Length As Long
        Get
            Throw New NotSupportedException()
        End Get
    End Property
    Public Overrides Property Position As Long
        Get
            Throw New NotSupportedException()
        End Get
        Set(ByVal value As Long)
            Throw New NotSupportedException()
        End Set
    End Property
    Public Overrides Function Seek(ByVal offset As Long, ByVal origin As System.IO.SeekOrigin) As Long
        Throw New NotSupportedException()
    End Function
    Public Overrides Sub SetLength(ByVal value As Long)
        Throw New NotSupportedException()
    End Sub
    Public Overrides Function Read(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer) As Integer
        Throw New NotSupportedException()
    End Function
    Public Overrides Sub Flush()
    End Sub
#End Region
End Class

Public Module ExtensionsIConverter
    <Extension()> Public Function Convert(Of T, R)(ByVal converter As IConverter(Of T, R), ByVal sequence As IEnumerable(Of T)) As IEnumerable(Of R)
        Contract.Requires(converter IsNot Nothing)
        Contract.Requires(sequence IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IEnumerable(Of R))() IsNot Nothing)
        Return sequence.Transform(AddressOf converter.Convert)
    End Function

    <Extension()> Public Function ToWriteableStream(ByVal enumerator As PushEnumerator(Of Byte)) As IO.Stream
        Contract.Requires(enumerator IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IO.Stream)() IsNot Nothing)
        Return New PushEnumeratorStream(enumerator)
    End Function
    <Extension()> Public Function ToReadableStream(ByVal enumerator As IEnumerator(Of Byte)) As IO.Stream
        Contract.Requires(enumerator IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IO.Stream)() IsNot Nothing)
        Return New EnumeratorStream(enumerator)
    End Function
    <Extension()> Public Function ToReadEnumerator(ByVal stream As IO.Stream) As IEnumerator(Of Byte)
        Contract.Requires(stream IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IEnumerator(Of Byte))() IsNot Nothing)
        Dim stream_ = stream
        Return New Enumerator(Of Byte)(Function(controller)
                                           Dim r = stream_.ReadByte()
                                           If r = -1 Then  Return controller.Break()
                                           Return CByte(r)
                                       End Function)
    End Function
    <Extension()> Public Function ToWritePushEnumerator(Of T)(ByVal stream As IO.Stream,
                                                              ByVal converter As IConverter(Of T, Byte)) As PushEnumerator(Of T)
        Contract.Requires(converter IsNot Nothing)
        Contract.Requires(stream IsNot Nothing)
        Contract.Ensures(Contract.Result(Of PushEnumerator(Of T))() IsNot Nothing)
        Dim converter_ = converter
        Dim stream_ = stream
        Return New PushEnumerator(Of T)(Sub(sequenceT)
                                            Dim sequence = converter_.Convert(sequenceT)
                                            While sequence.MoveNext
                                                stream_.WriteByte(sequence.Current)
                                            End While
                                            stream_.Close()
                                        End Sub)
    End Function

    <Extension()> Public Function ConvertReadOnlyStream(ByVal converter As IConverter(Of Byte), ByVal stream As IO.Stream) As IO.Stream
        Contract.Requires(converter IsNot Nothing)
        Contract.Requires(stream IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IO.Stream)() IsNot Nothing)
        Return converter.Convert(stream.ToReadEnumerator()).ToReadableStream()
    End Function
    <Extension()> Public Function ConvertWriteOnlyStream(ByVal converter As IConverter(Of Byte), ByVal stream As IO.Stream) As IO.Stream
        Contract.Requires(converter IsNot Nothing)
        Contract.Requires(stream IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IO.Stream)() IsNot Nothing)
        Return stream.ToWritePushEnumerator(converter).ToWriteableStream()
    End Function

    <Extension()> Public Function MoveNextAndReturn(Of T)(ByVal enumerator As IEnumerator(Of T)) As T
        Contract.Requires(enumerator IsNot Nothing)
        If Not enumerator.MoveNext Then Throw New InvalidOperationException("Ran past end of enumerator")
        Return enumerator.Current()
    End Function
End Module
