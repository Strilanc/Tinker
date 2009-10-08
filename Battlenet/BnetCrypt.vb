Imports System.Numerics

''HostBot - Warcraft 3 game hosting bot
''Copyright (C) 2008 Craig Gidney
''
''This program is free software: you can redistribute it and/or modify
''it under the terms of the GNU General Public License as published by
''the Free Software Foundation, either version 3 of the License, or
''(at your option) any later version.
''
''This program is distributed in the hope that it will be useful,
''but WITHOUT ANY WARRANTY; without even the implied warranty of
''MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
''GNU General Public License for more details.
''You should have received a copy of the GNU General Public License
''along with this program.  If not, see http://www.gnu.org/licenses/

''''Implements the various cryptographic checks and algorithms blizzard uses
Namespace Bnet.Crypt
    Public Module CryptCommon
        Private ReadOnly G As BigInteger = 47
        Private ReadOnly N As BigInteger = BigInteger.Parse("112624315653284427036559548610503669920632123929604336254260115573677366691719")

        '''<summary>Computes the crc32 value for a stream of data.</summary>
        '''<param name="stream">The stream to read data from.</param>
        '''<param name="length">The total amount of data to read. -1 means read entire stream.</param>
        '''<param name="poly">The polynomial to be used, specified in a bit pattern. (Default is CRC-32-IEEE 802.3).</param>
        '''<param name="polyAlreadyReversed">Indicates whether or not the bit pattern of the polynomial is already reversed.</param>
        '''<returns>crc32 value</returns>
        Public Function CRC32(ByVal stream As IO.Stream,
                              Optional ByVal length As Long = -1,
                              Optional ByVal poly As UInteger = &H4C11DB7,
                              Optional ByVal polyAlreadyReversed As Boolean = False) As UInteger
            Dim r = New IO.BinaryReader(stream)
            Dim reg As UInteger

            'Reverse the polynomial
            If polyAlreadyReversed = False Then
                Dim polyRev As UInteger = 0
                For i = 0 To 31
                    If ((poly >> i) And &H1) <> 0 Then
                        polyRev = polyRev Or (CUInt(&H1) << (31 - i))
                    End If
                Next i
                poly = polyRev
            End If

            'Precompute the combined XOR masks for each byte
            Dim xorTable(0 To 255) As UInteger
            For i = 0 To 255
                reg = CUInt(i)
                For j = 0 To 7
                    If (reg And CUInt(&H1)) <> 0 Then
                        reg = (reg >> 1) Xor poly
                    Else
                        reg >>= 1
                    End If
                Next j
                xorTable(i) = reg
            Next i

            'Direct Table Algorithm
            reg = Not 0UI
            If length = -1 Then length = stream.Length - stream.Position
            For i As Long = 0 To length - 1
                reg = (reg >> 8) Xor xorTable(r.ReadByte() Xor CByte(reg And &HFF))
            Next i

            Return Not reg
        End Function

        Public Function GeneratePublicPrivateKeyPair(ByVal secureRandomNumberGenerator As System.Security.Cryptography.RandomNumberGenerator) As KeyPair
            Contract.Requires(secureRandomNumberGenerator IsNot Nothing)
            Contract.Ensures(Contract.Result(Of KeyPair)() IsNot Nothing)

            Dim privateKeyBytes = N.ToUnsignedByteArray
            secureRandomNumberGenerator.GetBytes(privateKeyBytes)
            Dim privateKey = privateKeyBytes.ToUnsignedBigInteger Mod N
            If privateKey = 0 Then privateKey = 1
            privateKeyBytes = privateKey.ToUnsignedByteArray

            Dim publicKey = BigInteger.ModPow(G, privateKey, N)
            Dim publicKeyBytes = publicKey.ToUnsignedByteArray

            ReDim Preserve privateKeyBytes(0 To 31)
            ReDim Preserve publicKeyBytes(0 To 31)
            Contract.Assume(privateKeyBytes IsNot Nothing) 'remove this once contract static verifier understands redim preserve
            Contract.Assume(publicKeyBytes IsNot Nothing) 'remove this once contract static verifier understands redim preserve

            Return New KeyPair(publicKeyBytes.ToView, privateKeyBytes.ToView)
        End Function

        Public Function GenerateClientServerPasswordProofs(ByVal userName As String,
                                                           ByVal password As String,
                                                           ByVal accountSalt As ViewableList(Of Byte),
                                                           ByVal serverOffsetPublicKeyBytes As ViewableList(Of Byte),
                                                           ByVal clientPrivateKeyBytes As ViewableList(Of Byte),
                                                           ByVal clientPublicKeyBytes As ViewableList(Of Byte)) As KeyPair
            Contract.Requires(userName IsNot Nothing)
            Contract.Requires(password IsNot Nothing)
            Contract.Requires(clientPrivateKeyBytes IsNot Nothing)
            Contract.Requires(clientPublicKeyBytes IsNot Nothing)
            Contract.Requires(serverOffsetPublicKeyBytes IsNot Nothing)
            Contract.Requires(accountSalt IsNot Nothing)
            Contract.Ensures(Contract.Result(Of KeyPair)() IsNot Nothing)

            Dim userIdAuthData = System.Text.ASCIIEncoding.ASCII.GetBytes("{0}:{1}".Frmt(userName.ToUpperInvariant, password.ToUpperInvariant))
            Dim serverOffsetPublicKey = serverOffsetPublicKeyBytes.ToUnsignedBigInteger
            Dim clientPrivateKey = clientPrivateKeyBytes.ToUnsignedBigInteger
            Dim clientPublicKey = clientPublicKeyBytes.ToUnsignedBigInteger

            'Password private key
            Dim x = Concat(accountSalt.ToArray, userIdAuthData.SHA1).SHA1.ToUnsignedBigInteger

            'Verifier
            Dim v = BigInteger.ModPow(G, x, N)
            Dim u = serverOffsetPublicKeyBytes.ToArray.SHA1.SubArray(0, 4).Reverse.ToUnsignedBigInteger

            'Shared secret
            Dim sharedSecretData = BigInteger.ModPow((N + serverOffsetPublicKey - v) Mod N, clientPrivateKey + u * x, N).ToUnsignedByteArray
            ReDim Preserve sharedSecretData(0 To 32 - 1)
            Contract.Assume(sharedSecretData IsNot Nothing)
            'Hash odd and even elements
            Dim sharedHashEven = (From i In Enumerable.Range(0, 16)
                                  Select sharedSecretData(i * 2)).ToArray.SHA1
            Dim sharedHashOdd = (From i In Enumerable.Range(0, 16)
                                 Select sharedSecretData(i * 2 + 1)).ToArray.SHA1
            'Interleave odd and even hashes
            Dim sharedHash = (From i In Enumerable.Range(0, 40)
                              Select If(i Mod 2 = 0, sharedHashEven(i \ 2), sharedHashOdd(i \ 2))).ToArray

            'Fixed salt
            Dim fixed_G = G.ToUnsignedByteArray.SHA1
            Dim fixed_N = N.ToUnsignedByteArray.SHA1
            Dim fixed_salt(0 To 20 - 1) As Byte
            For i = 0 To 20 - 1
                fixed_salt(i) = fixed_G(i) Xor fixed_N(i)
            Next i

            'Proofs
            Dim clientProof = Concat(fixed_salt,
                                     userName.ToAscBytes.SHA1,
                                     accountSalt.ToArray,
                                     clientPublicKeyBytes.ToArray,
                                     serverOffsetPublicKeyBytes.ToArray,
                                     sharedHash).SHA1
            Dim serverProof = Concat(clientPublicKeyBytes.ToArray,
                                     clientProof,
                                     sharedHash).SHA1
            Return New KeyPair(clientProof.ToView, serverProof.ToView)
        End Function

#Region "MPQ Revision Check"
        Private Structure RevisionCheckOperation
            Public ReadOnly leftOperand As Char
            Public ReadOnly rightOperand As Char
            Public ReadOnly destinationOperand As Char
            Public ReadOnly [operator] As Char
            Public Sub New(ByVal leftOperand As Char, ByVal rightOperand As Char, ByVal destinationOperand As Char, ByVal [operator] As Char)
                Me.leftOperand = leftOperand
                Me.rightOperand = rightOperand
                Me.destinationOperand = destinationOperand
                Me.operator = [operator]
            End Sub
        End Structure
        Private Function ParseRevisionCheckVariables(ByVal lines As IEnumerator(Of String)) As Dictionary(Of Char, ModInt32)
            Contract.Requires(lines IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Dictionary(Of Char, ModInt32))() IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Dictionary(Of Char, ModInt32))().ContainsKey("A"c))
            Contract.Ensures(Contract.Result(Of Dictionary(Of Char, ModInt32))().ContainsKey("C"c))
            Contract.Ensures(Contract.Result(Of Dictionary(Of Char, ModInt32))().ContainsKey("S"c))

            Dim variables = New Dictionary(Of Char, ModInt32)
            variables("S"c) = 0
            variables("A"c) = 0
            variables("C"c) = 0
            Do
                If Not lines.MoveNext Then
                    Throw New ArgumentException("Instructions did not include any operations.")
                End If

                If lines.Current Is Nothing OrElse Not lines.Current Like "?=*" Then Exit Do 'end of initialization block
                Contract.Assume(lines.Current.Length >= 2)

                Dim u As UInteger
                If Not UInteger.TryParse(lines.Current.Substring(2), u) Then
                    Throw New ArgumentException("Invalid variable initialization line: {0}".Frmt(lines.Current))
                End If
                variables(lines.Current(0)) = u
            Loop
            Contract.Assume(variables.ContainsKey("A"c))
            Contract.Assume(variables.ContainsKey("C"c))
            Contract.Assume(variables.ContainsKey("S"c))
            Return variables
        End Function
        Private Function ParseRevisionCheckOperations(ByVal lines As IEnumerator(Of String),
                                                      ByVal variables As Dictionary(Of Char, ModInt32)) As IEnumerable(Of RevisionCheckOperation)
            Contract.Requires(lines IsNot Nothing)
            Contract.Requires(variables IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IEnumerable(Of RevisionCheckOperation))() IsNot Nothing)

            'Operation Count
            Dim numOps As Byte 'number of operations
            If Not Byte.TryParse(lines.Current, numOps) Then
                Throw New ArgumentException("Instructions did not include a valid operation count: {0}".Frmt(lines.Current))
            End If

            'Operations
            Dim operations = New List(Of RevisionCheckOperation)(numOps)
            For i = 0 To numOps - 1
                If Not lines.MoveNext Then
                    Throw New ArgumentException("Instructions did not include {0} operations as specified.".Frmt(numOps))
                ElseIf lines.Current Is Nothing OrElse Not lines.Current Like "?=?[-+^|&]?" Then
                    Throw New ArgumentException("Invalid operation specified: {0}".Frmt(lines.Current))
                End If

                Dim op = New RevisionCheckOperation(destinationOperand:=lines.Current(0),
                                                    leftOperand:=lines.Current(2),
                                                    [operator]:=lines.Current(3),
                                                    rightOperand:=lines.Current(4))
                If Not variables.ContainsKey(op.leftOperand) OrElse
                   Not variables.ContainsKey(op.rightOperand) OrElse
                   Not variables.ContainsKey(op.destinationOperand) Then
                    Throw New ArgumentException("Operation involved undefined variable.")
                End If
                operations.Add(op)
            Next i

            'End of instructions
            If lines.MoveNext Then
                Throw New ArgumentException("Instructions included more than {0} operations as specified.".Frmt(numOps))
            End If
            Return operations
        End Function
        Private Sub RevisionCheckApplyOperation(ByVal value As UInteger,
                                                ByVal variables As Dictionary(Of Char, ModInt32),
                                                ByVal operations As IEnumerable(Of RevisionCheckOperation))
            Contract.Requires(variables IsNot Nothing)
            Contract.Requires(operations IsNot Nothing)

            'Variable S = file dword
            variables("S"c) = value

            'Run Operations
            For Each op In operations
                Dim vL = variables(op.leftOperand)
                Dim vR = variables(op.rightOperand)
                Dim vD As ModInt32
                Select Case op.operator
                    Case "+"c : vD = vL + vR
                    Case "-"c : vD = vL - vR
                    Case "^"c : vD = vL Xor vR
                    Case "&"c : vD = vL And vR
                    Case "|"c : vD = vL Or vR
                    Case Else
                        Throw New UnreachableException("Unrecognized operator: {0}".Frmt(op.operator))
                End Select
                variables(op.destinationOperand) = vD
            Next op
        End Sub
        Public Function GenerateRevisionCheck(ByVal folder As String,
                                              ByVal indexString As String,
                                              ByVal instructions As String) As UInteger
            Contract.Requires(folder IsNot Nothing)
            Contract.Requires(indexString IsNot Nothing)
            Contract.Requires(instructions IsNot Nothing)

            'Parse
            'Example: [newlines actually spaces in data]
            '   A=443747131
            '   B=3328179921
            '   C=1040998290
            '   4
            '   A=A^S
            '   B=B-C
            '   C=C^A
            '   A=A+B
            Dim lines = CType(instructions.Split(" "c), IEnumerable(Of String)).GetEnumerator()
            Dim variables = ParseRevisionCheckVariables(lines)
            Dim operations = ParseRevisionCheckOperations(lines, variables)

            'Adjust Variable A using mpq number string [the point of this? obfuscation I guess]
            indexString = indexString.ToUpperInvariant
            If Not indexString Like "VER-IX86-#.MPQ" AndAlso Not indexString Like "IX86VER#.MPQ" Then
                Throw New ArgumentException("Unrecognized MPQ String: {0}".Frmt(indexString), "indexString")
            End If
            Contract.Assume(indexString.Length >= 5)
            Dim table = {&HE7F4CB62UI,
                         &HF6A14FFCUI,
                         &HAA5504AFUI,
                         &H871FCDC2UI,
                         &H11BF6A18UI,
                         &HC57292E6UI,
                         &H7927D27EUI,
                         &H2FEC8733UI}
            variables("A"c) = variables("A"c) Xor table(Integer.Parse(indexString(indexString.Length - 5), CultureInfo.InvariantCulture))

            'Tail Buffer [rounds file data sizes to 1024]
            Dim tailBuffer(0 To 1023) As Byte
            For i = 0 To tailBuffer.Length - 1
                tailBuffer(i) = CByte(255 - (i Mod 256))
            Next i
            Dim tail_stream As New IO.MemoryStream(tailBuffer)

            'Parse Files
            For Each filename In {"War3.exe", "Storm.dll", "Game.dll"}
                'Open file
                Dim f = New IO.FileStream(folder + filename, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read)
                Dim n = CInt(f.Length).ModCeiling(1024)
                Dim br = New IO.BinaryReader(New IO.BufferedStream(New ConcatStream({f, tail_stream})))

                'Apply operations using each dword in stream
                For repeat = 0 To n - 1 Step 4
                    RevisionCheckApplyOperation(br.ReadUInt32(), variables, operations)
                Next repeat
            Next filename

            'Return value of Variable C
            Return variables("C"c)
        End Function
#End Region

        <Extension()>
        Public Function SHA1(ByVal data() As Byte) As Byte()
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Byte())() IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Byte())().Length = 20)
            Using sha = New Security.Cryptography.SHA1Managed()
                Dim hash = sha.ComputeHash(data)
                Contract.Assume(hash IsNot Nothing)
                Return hash
            End Using
        End Function
    End Module
End Namespace
