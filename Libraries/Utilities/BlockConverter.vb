Imports System.Runtime.CompilerServices

'''<summary>A stateful converter for blocks of data.</summary>
Public Interface IBlockConverter
    '''<summary>Returns an estimate of the input size required for a given output size, in bytes.</summary>
    Function needs(ByVal outputSize As Integer) As Integer
    '''<summary>
    ''' Converts data in the read buffer and writes it to the write buffer.
    ''' Returns read and write count set to 0 to indicate no more data can be accepted.
    '''</summary>
    '''<remarks>
    ''' If more input data is needed before producing any output, be sure to buffer some of the input.
    ''' Otherwise you will return read and write count set to 0, and the outside user will assume no more data can be accepted.
    '''</remarks>
    '''<param name="OutReadCount">Number of bytes consumed from start of read view.</param>
    '''<param name="OutWriteCount">Number of bytes produced from start of write view.</param>
    Sub convert(ByVal ReadView As ReadOnlyArrayView(Of Byte), _
                ByVal WriteView As ArrayView(Of Byte), _
                ByRef OutReadCount As Integer, _
                ByRef OutWriteCount As Integer)
End Interface

Public Module BlockConverterExtensions
    '''<summary>Returns a stream which feeds through the converter from the source stream.</summary>
    <Extension()> Public Function streamThroughFrom(ByVal converter As IBlockConverter, ByVal source_stream As IO.Stream) As IO.Stream
        If Not (converter IsNot Nothing) Then Throw New ArgumentException()
        Return New BlockConverterStreamReader(source_stream, converter)
    End Function
    '''<summary>Returns a stream which feeds through the converter to the destination stream.</summary>
    <Extension()> Public Function streamThroughTo(ByVal converter As IBlockConverter, ByVal destination_stream As IO.Stream) As IO.Stream
        If Not (converter IsNot Nothing) Then Throw New ArgumentException()
        Return New BlockConverterStreamWriter(destination_stream, converter)
    End Function

    '''<summary>A stream which feeds from a converter and feeds the converter from a source stream.</summary>
    Private Class BlockConverterStreamWriter
        Inherits WrappedWriteOnlyConversionStream
        Private Const BASE_BUFFER_SIZE As Integer = 512
        Private to_buffer(0 To BASE_BUFFER_SIZE - 1) As Byte
        Private ReadOnly converter As IBlockConverter

        Public Sub New(ByVal substream As IO.Stream, ByVal converter As IBlockConverter)
            MyBase.new(substream)
            If Not (converter IsNot Nothing) Then Throw New ArgumentException()
            Me.converter = converter
        End Sub

        Public NotOverridable Overrides Sub Write(ByVal from_buffer() As Byte, ByVal offset As Integer, ByVal count As Integer)
            If offset < 0 Then Throw New ArgumentException("Buffer offset is negative.")
            If offset + count > from_buffer.Length Then Throw New ArgumentException("Buffer offset + count exceeds length.")
            If to_buffer.Length < count Then ReDim to_buffer(0 To count * 2 - 1)
            While count > 0
                'convert prep
                Dim maxReadCount = count
                Dim maxWriteCount = to_buffer.Length
                Dim outReadCount = 0
                Dim outWriteCount = 0
                Dim ReadView = New ReadOnlyArrayView(Of Byte)(from_buffer, offset, maxReadCount)
                Dim WriteView = New ArrayView(Of Byte)(to_buffer, 0, maxWriteCount)

                'convert
                converter.convert(ReadView, WriteView, outReadCount, outWriteCount)
                If outReadCount < 0 Or outReadCount > maxReadCount Then Throw New InvalidOperationException("Converter returned an invalid read count.")
                If outWriteCount < 0 Or outWriteCount > maxWriteCount Then Throw New InvalidOperationException("Converter returned an invalid write count.")
                If outReadCount = 0 AndAlso outWriteCount = 0 Then Exit While

                'next
                count -= outReadCount
                offset += outReadCount
                If outWriteCount > 0 Then substream.Write(to_buffer, 0, outWriteCount)
            End While
        End Sub
    End Class

    '''<summary>A stream which feeds to a converter and feeds the converter to a destination stream.</summary>
    Private Class BlockConverterStreamReader
        Inherits WrappedReadOnlyConversionStream
        Private Const BASE_BUFFER_SIZE As Integer = 512
        Private buffer_filling() As Byte
        Private buffer_emptying() As Byte
        Private offset_filling As Integer
        Private offset_emptying As Integer
        Private counts_filling As Integer
        Private counts_emptying As Integer
        Private ReadOnly converter As IBlockConverter

        Public Sub New(ByVal substream As IO.Stream, ByVal converter As IBlockConverter)
            MyBase.new(substream)
            If Not (converter IsNot Nothing) Then Throw New ArgumentException()
            ReDim buffer_filling(0 To BASE_BUFFER_SIZE - 1)
            ReDim buffer_emptying(0 To BASE_BUFFER_SIZE - 1)
            Me.converter = converter
        End Sub

        Private Sub swapBuffers()
            Dim bb = buffer_filling
            buffer_filling = buffer_emptying
            buffer_emptying = bb
            Dim o = offset_filling
            offset_filling = offset_emptying
            offset_emptying = o
            Dim c = counts_filling
            counts_filling = counts_emptying
            counts_emptying = c
        End Sub

        Public NotOverridable Overrides Function Read(ByVal to_buffer() As Byte, ByVal offset As Integer, ByVal to_count As Integer) As Integer
            Dim totalOut = 0
            While to_count > 0
                'read
                Dim expected_required_input = Math.Max(converter.needs(to_count), 1) - counts_filling - counts_emptying
                If expected_required_input > 0 Then
                    'make sure there is room in the class buffer
                    Dim m = offset_filling + counts_filling + expected_required_input
                    If m > buffer_filling.Length And counts_emptying = 0 Then
                        swapBuffers()
                        offset_filling = 0
                        m = counts_filling + expected_required_input
                    End If
                    If m > buffer_filling.Length Then
                        ReDim Preserve buffer_filling(0 To m * 2 - 1)
                    End If
                    'read bytes into class buffer
                    counts_filling += substream.Read(buffer_filling, offset_filling + counts_filling, expected_required_input)
                End If

                'prep convert
                If to_count <= 0 OrElse counts_filling + counts_emptying <= 0 Then Exit While
                If counts_emptying = 0 Then swapBuffers() 'don't read from empty buffer
                Dim outReadCount = 0
                Dim outWriteCount = 0
                Dim ReadView = New ReadOnlyArrayView(Of Byte)(buffer_emptying, offset_emptying, counts_emptying)
                Dim WriteView = New ArrayView(Of Byte)(to_buffer, offset + totalOut, to_count)

                'convert
                converter.convert(ReadView, WriteView, outReadCount, outWriteCount)
                If outReadCount < 0 Or outReadCount > counts_emptying Then Throw New InvalidOperationException("Converter returned an invalid read count.")
                If outWriteCount < 0 Or outWriteCount > to_count Then Throw New InvalidOperationException("Converter returned an invalid write count.")

                'write
                offset_emptying += outReadCount
                counts_emptying -= outReadCount
                If counts_emptying = 0 Then offset_emptying = 0
                to_count -= outWriteCount
                totalOut += outWriteCount
                If outWriteCount = 0 AndAlso outReadCount = 0 Then Exit While
            End While

            Return totalOut
        End Function
    End Class
End Module
