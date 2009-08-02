Imports System.Runtime.CompilerServices

Public Enum ByteOrder
    '''<summary>Least significant bytes first.</summary>
    LittleEndian
    '''<summary>Most significant bytes first.</summary>
    BigEndian
End Enum

Public Module Pack
    <Extension()> Public Function ToULong(ByVal data As IEnumerable(Of Byte),
                                          ByVal byteOrder As ByteOrder) As ULong
        Contract.Requires(data IsNot Nothing)
        If data.Count > 8 Then Throw New ArgumentOutOfRangeException("Data has too many bytes.")
        Dim val As ULong
        Select Case byteOrder
            Case ByteOrder.LittleEndian
                data = data.Reverse
            Case ByteOrder.BigEndian
                'no change required
            Case Else
                Throw New ArgumentException("Unrecognized byte order.")
        End Select
        For Each b In data
            val <<= 8
            val = val Or b
        Next b
        Return val
    End Function
    <Extension()> Public Function ToUInteger(ByVal data As IEnumerable(Of Byte),
                                             ByVal byteOrder As ByteOrder) As UInteger
        Contract.Requires(data IsNot Nothing)
        If data.Count > 4 Then Throw New ArgumentOutOfRangeException("Data has too many bytes.")
        Return CUInt(ToULong(data, byteOrder))
    End Function
    <Extension()> Public Function bytes(ByVal n As UShort,
                                        ByVal byteOrder As ByteOrder,
                                        Optional ByVal size As Integer = 2) As Byte()
        Contract.Requires(size >= 0)
        Contract.Ensures(Contract.Result(Of Byte())() IsNot Nothing)
        Return CULng(n).bytes(byteOrder, size)
    End Function
    <Extension()> Public Function bytes(ByVal n As UInteger,
                                        ByVal byte_order As ByteOrder,
                                        Optional ByVal size As Integer = 4) As Byte()
        Contract.Requires(size >= 0)
        Contract.Ensures(Contract.Result(Of Byte())() IsNot Nothing)
        Return CULng(n).bytes(byte_order, size)
    End Function
    <Extension()> Public Function bytes(ByVal n As ULong,
                                        ByVal byteOrder As ByteOrder,
                                        Optional ByVal size As Integer = 8) As Byte()
        Contract.Requires(size >= 0)
        Contract.Ensures(Contract.Result(Of Byte())() IsNot Nothing)
        Dim data(0 To size - 1) As Byte
        For i = 0 To size - 1
            data(i) = CByte(n And CULng(&HFF))
            n >>= 8
        Next i
        If n <> 0 Then Throw New ArgumentOutOfRangeException("The specified value won't fit in the specified number of bytes.")
        Select Case byteOrder
            Case ByteOrder.BigEndian
                Return data.Reverse.ToArray
            Case ByteOrder.LittleEndian
                Return data
            Case Else
                Throw New ArgumentException("Unrecognized byte order.")
        End Select
    End Function

    <Extension()> Public Function ToAscBytes(ByVal data As String,
                                             Optional ByVal nullTerminate As Boolean = False) As Byte()
        Contract.Requires(data IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Byte())() IsNot Nothing)
        Dim bytes As New List(Of Byte)(data.Length + 1)
        For Each c In data
            bytes.Add(CByte(Asc(c)))
        Next
        If nullTerminate Then bytes.Add(0)
        Return bytes.ToArray()
    End Function
    <Extension()> Public Function ParseChrString(ByVal data As IEnumerable(Of Byte),
                                                 ByVal nullTerminated As Boolean) As String
        Contract.Requires(data IsNot Nothing)
        Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)

        Dim s As New System.Text.StringBuilder()
        For Each b In data
            If b = 0 AndAlso nullTerminated Then Exit For
            s.Append(Chr(b))
        Next b

        Return s.ToString
    End Function

    <Extension()> Public Function ToHexString(ByVal data As IEnumerable(Of Byte),
                                              Optional ByVal minByteLength As Byte = 2,
                                              Optional ByVal separator As String = " ") As String
        Contract.Requires(data IsNot Nothing)
        Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)

        Dim s As New System.Text.StringBuilder()
        For Each b In data
            If s.Length > 0 Then s.Append(separator)
            Dim h = Hex(b)
            For i = 1 To minByteLength - h.Length
                s.Append("0"c)
            Next i
            s.Append(h)
        Next b
        Return s.ToString()
    End Function
    <Extension()> Public Function FromHexStringToBytes(ByVal data As String) As Byte()
        Contract.Requires(data IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Byte())() IsNot Nothing)

        If data Like "*[!0-9A-Fa-f ]*" Then Throw New ArgumentException("Invalid characters.")
        If data Like "*[! ][! ][! ]*" Then Throw New ArgumentException("Contains a hex value which won't fit in a byte.")
        Dim words = data.Split(" "c)
        Dim bb(0 To words.Length - 1) As Byte
        For i = 0 To words.Length - 1
            bb(i) = CByte(dehex(words(i), ByteOrder.BigEndian))
        Next i
        Return bb
    End Function

    <Extension()>
    Public Function ToBinary(ByVal i As ULong, Optional ByVal minLength As Integer = 8) As String
        Contract.Requires(minLength >= 0)
        Contract.Requires(minLength <= 64)
        Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
        Dim ret = ""
        While i > 0 Or minLength > 0
            ret = (i And CULng(&H1)).ToString() + ret
            i >>= 1
            minLength -= 1
        End While
        Return ret
    End Function
    Public Function udehex(ByVal chars As IEnumerable(Of Char), ByVal byteOrder As ByteOrder) As ULong
        Contract.Requires(chars IsNot Nothing)
        Select Case byteOrder
            Case byteOrder.LittleEndian
                chars = chars.Reverse()
            Case byteOrder.BigEndian
                'no change needed
            Case Else
                Throw New ArgumentException("Unrecognized byte order.")
        End Select

        Dim val = 0UL
        For Each c In chars
            c = Char.ToUpper(c)
            val <<= 4
            Select Case c
                Case "0"c, "1"c, "2"c, "3"c, "4"c, "5"c, "6"c, "7"c, "8"c, "9"c
                    val += CULng(Asc(c) - Asc("0"c))
                Case "A"c, "B"c, "C"c, "D"c, "E"c, "F"c
                    val += CULng(Asc(c) - Asc("A"c) + 10)
                Case Else
                    Throw New ArgumentException("Invalid character.")
            End Select
        Next c
        Return val
    End Function
    Public Function dehex(ByVal chars As IEnumerable(Of Char), ByVal byteOrder As ByteOrder) As Long
        Contract.Requires(chars IsNot Nothing)
        If chars.None Then Return 0
        If chars.First = "-" Then Return -dehex(chars.Skip(1), byteOrder)

        Select Case byteOrder
            Case byteOrder.LittleEndian
                chars = chars.Reverse()
            Case byteOrder.BigEndian
                'no change needed
            Case Else
                Throw New ArgumentException("Unrecognized byte order.")
        End Select
        Dim val As Long = 0
        For Each c In chars
            c = Char.ToUpper(c)
            val <<= 4
            Select Case c
                Case "0"c, "1"c, "2"c, "3"c, "4"c, "5"c, "6"c, "7"c, "8"c, "9"c
                    val += CLng(Asc(c) - Asc("0"))
                Case "A"c, "B"c, "C"c, "D"c, "E"c, "F"c
                    val += CLng(Asc(c) - Asc("A") + 10)
                Case Else
                    Throw New ArgumentException("Invalid character.")
            End Select
        Next c
        Return val
    End Function
End Module
