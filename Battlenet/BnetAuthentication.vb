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

Imports System.Numerics
Imports System.Security

Namespace Bnet
    Public Class ClientCredentials
        Private Shared ReadOnly G As BigInteger = 47
        Private Shared ReadOnly N As BigInteger = BigInteger.Parse("112624315653284427036559548610503669920632123929604336254260115573677366691719")

        Private ReadOnly _username As String
        Private ReadOnly _password As String
        Private ReadOnly _privateKey As BigInteger
        Private ReadOnly _publicKey As BigInteger

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_username IsNot Nothing)
            Contract.Invariant(_password IsNot Nothing)
            Contract.Invariant(_privateKey >= 0)
            Contract.Invariant(_publicKey >= 0)
        End Sub

        Public Sub New(ByVal username As String,
                       ByVal password As String,
                       ByVal privateKey As BigInteger)
            Contract.Requires(username IsNot Nothing)
            Contract.Requires(password IsNot Nothing)
            Contract.Requires(privateKey >= 0)
            Contract.Ensures(Me.UserName = username)
            Me._username = username
            Me._password = password
            Me._privateKey = privateKey
            Me._publicKey = BigInteger.ModPow(G, _privateKey, N)
            Contract.Assume(Me._publicKey >= 0)
        End Sub
        Public Sub New(ByVal username As String,
                       ByVal password As String,
                       Optional ByVal rng As Cryptography.RandomNumberGenerator = Nothing)
            Me.New(username, password, GeneratePrivateKey(rng))
            Contract.Requires(username IsNot Nothing)
            Contract.Requires(password IsNot Nothing)
            Contract.Ensures(Me.UserName = username)
        End Sub

        Public ReadOnly Property UserName As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Return _username
            End Get
        End Property
        Public ReadOnly Property PublicKey As BigInteger
            Get
                Contract.Ensures(Contract.Result(Of BigInteger)() >= 0)
                Return _publicKey
            End Get
        End Property
        Public ReadOnly Property PublicKeyBytes As IList(Of Byte)
            Get
                Contract.Ensures(Contract.Result(Of IList(Of Byte))() IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IList(Of Byte))().Count = 32)
                Return _publicKey.ToUnsignedByteArray.PaddedTo(minimumLength:=32)
            End Get
        End Property

        ''' <summary>
        ''' Generates a new private crypto key.
        ''' </summary>
        ''' <remarks>I'm not a cryptographer, but this is probably way more than safe enough given the application.</remarks>
        Private Shared Function GeneratePrivateKey(Optional ByVal rng As Cryptography.RandomNumberGenerator = Nothing) As BigInteger
            Contract.Ensures(Contract.Result(Of BigInteger)() >= 0)
            Contract.Assume(N >= 0)
            Dim privateKeyDataBuffer = N.ToUnsignedByteArray
            If rng Is Nothing Then
                Using r = New Cryptography.RNGCryptoServiceProvider()
                    r.GetBytes(privateKeyDataBuffer)
                End Using
            Else
                rng.GetBytes(privateKeyDataBuffer)
            End If
            Dim key = (privateKeyDataBuffer.ToUnsignedBigInteger Mod (N - 1)) + 1
            Contract.Assume(key >= 0)
            Return key
        End Function
        ''' <summary>
        ''' Derives a fixed salt value from the shared crypto constants.
        ''' </summary>
        Private Shared ReadOnly Property FixedSalt() As Byte()
            Get
                Contract.Ensures(Contract.Result(Of Byte())() IsNot Nothing)
                Contract.Ensures(Contract.Result(Of Byte())().Length = 20)

                Contract.Assume(G >= 0)
                Contract.Assume(N >= 0)
                Dim hash1 = G.ToUnsignedByteArray.SHA1
                Dim hash2 = N.ToUnsignedByteArray.SHA1
                Return (From i In Enumerable.Range(0, 20)
                        Select hash1(i) Xor hash2(i)
                        ).ToArray
            End Get
        End Property

        ''' <summary>
        ''' Determines credentials for the same user, but with a new crypto key pair.
        ''' </summary>
        Public Function Regenerate(Optional ByVal rng As Cryptography.RandomNumberGenerator = Nothing) As ClientCredentials
            Contract.Ensures(Contract.Result(Of ClientCredentials)() IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ClientCredentials)().UserName = Me.UserName)
            Return New ClientCredentials(Me._username, Me._password, rng)
        End Function

        ''' <summary>
        ''' Determines the shared secret value, which can be computed by both the client and server, using the client-side data.
        ''' </summary>
        Private ReadOnly Property SharedSecret(ByVal accountSalt As IList(Of Byte),
                                               ByVal serverPublicKeyBytes As IList(Of Byte)) As Byte()
            Get
                Contract.Requires(serverPublicKeyBytes IsNot Nothing)
                Contract.Requires(accountSalt IsNot Nothing)
                Contract.Ensures(Contract.Result(Of Byte())() IsNot Nothing)
                Contract.Ensures(Contract.Result(Of Byte())().Length = 40)

                Dim userIdAuthData = "{0}:{1}".Frmt(Me._username.ToUpperInvariant, Me._password.ToUpperInvariant).ToAscBytes
                Dim passwordKey = Concat(accountSalt.ToArray, userIdAuthData.SHA1).SHA1.ToUnsignedBigInteger
                Dim verifier = BigInteger.ModPow(G, passwordKey, N)
                Dim serverKey = serverPublicKeyBytes.SHA1.SubArray(0, 4).Reverse.ToUnsignedBigInteger

                'Shared value
                Dim serverPublicKey = serverPublicKeyBytes.ToUnsignedBigInteger
                Dim sharedValue = BigInteger.ModPow(serverPublicKey - verifier + N, Me._privateKey + serverKey * passwordKey, N)
                Contract.Assume(sharedValue >= 0)

                'Hash odd and even bytes of the shared value
                Dim sharedValueBytes = sharedValue.ToUnsignedByteArray.PaddedTo(32)
                Dim sharedHashEven = (From i In Enumerable.Range(0, 16)
                                      Select sharedValueBytes(i * 2)
                                      ).SHA1
                Dim sharedHashOdd = (From i In Enumerable.Range(0, 16)
                                     Select sharedValueBytes(i * 2 + 1)
                                     ).SHA1

                'Interleave odd and even hashes
                Return (From i In Enumerable.Range(0, 40)
                        Select If(i Mod 2 = 0, sharedHashEven(i \ 2), sharedHashOdd(i \ 2))
                        ).ToArray
            End Get
        End Property
        ''' <summary>
        ''' Determines a proof for the server that the client knows the password.
        ''' </summary>
        Public ReadOnly Property ClientPasswordProof(ByVal accountSalt As IList(Of Byte),
                                                     ByVal serverPublicKeyBytes As IList(Of Byte)) As IList(Of Byte)
            Get
                Contract.Requires(serverPublicKeyBytes IsNot Nothing)
                Contract.Requires(accountSalt IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IList(Of Byte))() IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IList(Of Byte))().Count = 20)

                Return Concat(FixedSalt,
                              UserName.ToUpperInvariant.ToAscBytes.SHA1,
                              accountSalt.ToArray,
                              Me.PublicKeyBytes.ToArray,
                              serverPublicKeyBytes.ToArray,
                              SharedSecret(accountSalt, serverPublicKeyBytes)).SHA1
            End Get
        End Property
        ''' <summary>
        ''' Determines the expected proof from the server that it knew the shared secret.
        ''' </summary>
        Public ReadOnly Property ServerPasswordProof(ByVal accountSalt As IList(Of Byte),
                                                     ByVal serverPublicKeyBytes As IList(Of Byte)) As IList(Of Byte)
            Get
                Contract.Requires(serverPublicKeyBytes IsNot Nothing)
                Contract.Requires(accountSalt IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IList(Of Byte))() IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IList(Of Byte))().Count = 20)

                Return Concat(Me.PublicKeyBytes.ToArray,
                              ClientPasswordProof(accountSalt, serverPublicKeyBytes).ToArray,
                              SharedSecret(accountSalt, serverPublicKeyBytes)).SHA1
            End Get
        End Property
    End Class

    Public Module ProgramAuthentication
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
            variables("A"c) = 0
            variables("C"c) = 0
            variables("S"c) = 0
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
                Using f = New IO.FileStream(folder + filename, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read)
                    Dim n = CInt(f.Length).ModCeiling(1024)
                    Dim br = New IO.BinaryReader(New IO.BufferedStream(New ConcatStream({f, tail_stream})))

                    'Apply operations using each dword in stream
                    For repeat = 0 To n - 1 Step 4
                        RevisionCheckApplyOperation(br.ReadUInt32(), variables, operations)
                    Next repeat
                End Using
            Next filename

            'Return value of Variable C
            Return variables("C"c)
        End Function
    End Module
End Namespace
