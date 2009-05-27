Imports System.Runtime.CompilerServices

Public Enum ByteOrder
    '''<summary>Least significant bytes first.</summary>
    LittleEndian
    '''<summary>Most significant bytes first.</summary>
    BigEndian
End Enum

Public Module Pack
    <Extension()> Public Function ToULong(ByVal data As IEnumerable(Of Byte),
                                          ByVal byte_order As ByteOrder) As ULong
        If data Is Nothing Then Throw New ArgumentNullException("data")
        Dim val As ULong
        For Each b In If(byte_order = ByteOrder.LittleEndian, data.Reverse, data)
            val <<= 8
            val = val Or b
        Next b
        Return val
    End Function
    <Extension()> Public Function ToUInteger(ByVal data As IEnumerable(Of Byte),
                                             ByVal byte_order As ByteOrder) As UInteger
        Return CUInt(ToULong(data, byte_order))
    End Function
    <Extension()> Public Function bytes(ByVal n As UShort,
                                        ByVal byte_order As ByteOrder,
                                        Optional ByVal min_size As Integer = 2) As Byte()
        Return CULng(n).bytes(byte_order, min_size)
    End Function
    <Extension()> Public Function bytes(ByVal n As UInteger,
                                        ByVal byte_order As ByteOrder,
                                        Optional ByVal min_size As Integer = 4) As Byte()
        Return CULng(n).bytes(byte_order, min_size)
    End Function
    <Extension()> Public Function bytes(ByVal n As ULong,
                                        ByVal byte_order As ByteOrder,
                                        Optional ByVal min_size As Integer = 8) As Byte()
        Dim data As New List(Of Byte)
        Do Until n = 0 And data.Count >= min_size
            data.Add(CByte(n And CULng(&HFF)))
            n >>= 8
        Loop
        Select Case byte_order
            Case ByteOrder.BigEndian
                Return data.ToArray.reversed
            Case ByteOrder.LittleEndian
                Return data.ToArray
            Case Else
                Throw New ArgumentException("Unreocgnized byte order.")
        End Select
    End Function

    Public Function packString(ByVal s As String,
                               Optional ByVal nullTerminate As Boolean = False) As Byte()
        If Not (s IsNot Nothing) Then Throw New ArgumentException()


        Dim size = s.Length + If(nullTerminate, 1, 0)
        Dim buffer(0 To size - 1) As Byte
        For i = 0 To s.Length - 1
            buffer(i) = CByte(Asc(s(i)))
        Next i
        Return buffer
    End Function
    <Extension()> Public Function toChrString(ByVal data As IEnumerable(Of Byte),
                                 Optional ByVal null_terminated As Boolean = True) As String
        If data Is Nothing Then Return ""

        Dim chars As New List(Of Char)
        For Each b In data
            If b = 0 AndAlso null_terminated Then Exit For
            chars.Add(Chr(b))
        Next b

        Return chars.ToArray
    End Function

    Public Function unpackHexString(ByVal data As IEnumerable(Of Byte),
                                    Optional ByVal minByteLength As Byte = 2,
                                    Optional ByVal separator As String = " ") As String

        If data Is Nothing Then Return ""

        Dim s As New System.Text.StringBuilder()
        For Each b In data
            s.Append(separator)
            Dim h = Hex(b)
            For i = 1 To minByteLength - h.Length
                s.Append("0"c)
            Next i
            s.Append(h)
        Next b
        If s.Length = 0 Then Return ""
        Return s.ToString(1, s.Length - 1)
    End Function
    Public Function packHexString(ByVal s As String) As Byte()
        If Not (s IsNot Nothing) Then Throw New ArgumentException()
        Dim words = s.Split(" "c)
        Dim bb(0 To words.Length - 1) As Byte
        For i = 0 To words.Length - 1
            bb(i) = CByte(dehex(words(i)))
        Next i
        Return bb
    End Function

    Public Function bin(ByVal i As UInteger, Optional ByVal minLength As Integer = 8) As String
        bin = ""
        While i > 0 Or minLength > 0
            bin = (i Mod 2).ToString() + bin
            i \= CUInt(2)
            minLength -= 1
        End While
    End Function
    Public Function dehex(ByVal s As String) As Long
        Dim c As String
        If s.Length = 0 Then Return 0
        If s.Chars(0) = "-" Then Return -dehex(s.Substring(1))
        For i As Integer = 0 To s.Length - 1
            c = s.Substring(i, 1).ToUpper()
            Select Case c
                Case "0", "1", "2", "3", "4", "5", "6", "7", "8", "9"
                    dehex = dehex * 16 + Asc(c) - Asc("0")
                Case "A", "B", "C", "D", "E", "F"
                    dehex = dehex * 16 + Asc(c) - Asc("A") + 10
            End Select
        Next i
    End Function
End Module
