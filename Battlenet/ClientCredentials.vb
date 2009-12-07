''Tinker - Warcraft 3 game hosting bot
''Copyright (C) 2009 Craig Gidney
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
    ''' <summary>
    ''' Stores bnet login credentials for identification and authentication.
    ''' </summary>
    <DebuggerDisplay("{UserName}")>
    Public Class ClientCredentials
        Private Shared ReadOnly G As BigInteger = 47
        Private Shared ReadOnly N As BigInteger = BigInteger.Parse("112624315653284427036559548610503669920632123929604336254260115573677366691719",
                                                                   CultureInfo.InvariantCulture)

        Private ReadOnly _userName As String
        Private ReadOnly _password As String
        Private ReadOnly _privateKey As BigInteger
        Private ReadOnly _publicKey As BigInteger

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_userName IsNot Nothing)
            Contract.Invariant(_password IsNot Nothing)
            Contract.Invariant(_privateKey >= 0)
            Contract.Invariant(_publicKey >= 0)
        End Sub

        ''' <summary>
        ''' Constructs client credentials using the given username, password and private key.
        ''' Computes the public key from the private key.
        ''' </summary>
        ''' <param name="username">The client's username.</param>
        ''' <param name="password">The client's password.</param>
        ''' <param name="privateKey">The privateKey used for authentication.</param>
        Public Sub New(ByVal userName As String,
                       ByVal password As String,
                       ByVal privateKey As BigInteger)
            Contract.Requires(userName IsNot Nothing)
            Contract.Requires(password IsNot Nothing)
            Contract.Requires(privateKey >= 0)
            Contract.Ensures(Me.UserName = userName)
            Me._userName = userName
            Me._password = password
            Me._privateKey = privateKey
            Me._publicKey = BigInteger.ModPow(G, _privateKey, N)
            Contract.Assume(Me._publicKey >= 0)
        End Sub
        ''' <summary>
        ''' Constructs client credentials using the given username and password, and a generated a public/private key pair.
        ''' </summary>
        ''' <param name="username">The client's username.</param>
        ''' <param name="password">The client's password.</param>
        ''' <param name="rng">
        ''' An optional random number generator to generate the private key.
        ''' A System.Cryptography.RNGCryptoServiceProvider is created and used if this argument is omitted.
        ''' </param>
        Public Sub New(ByVal userName As String,
                       ByVal password As String,
                       Optional ByVal rng As Cryptography.RandomNumberGenerator = Nothing)
            Me.New(userName, password, GeneratePrivateKey(rng))
            Contract.Requires(userName IsNot Nothing)
            Contract.Requires(password IsNot Nothing)
            Contract.Ensures(Me.UserName = userName)
        End Sub

        '''<summary>The client's username used for identification.</summary>
        Public ReadOnly Property UserName As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Contract.Ensures(Contract.Result(Of String)() = Me._userName)
                Return _userName
            End Get
        End Property
        '''<summary>The public key used for authentication.</summary>
        Public ReadOnly Property PublicKey As BigInteger
            Get
                Contract.Ensures(Contract.Result(Of BigInteger)() >= 0)
                Contract.Ensures(Contract.Result(Of BigInteger)() = Me._publicKey)
                Return _publicKey
            End Get
        End Property
        '''<summary>Determines the 32 byte representation of the public key used for authentication.</summary>
        Public ReadOnly Property PublicKeyBytes As IList(Of Byte)
            Get
                Contract.Ensures(Contract.Result(Of IList(Of Byte))() IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IList(Of Byte))().Count = 32)
                Dim result = _publicKey.ToUnsignedByteArray.PaddedTo(minimumLength:=32)
                Contract.Assume(result.Count = 32)
                Return result
            End Get
        End Property

        ''' <summary>Generates a new private crypto key.</summary>
        ''' <remarks>I'm not a cryptographer but this is probably safe enough, given the application.</remarks>
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
        ''' <summary>Derives a fixed salt value from the shared crypto constants.</summary>
        Private Shared ReadOnly Property FixedSalt() As Byte()
            Get
                Contract.Ensures(Contract.Result(Of Byte())() IsNot Nothing)
                Contract.Ensures(Contract.Result(Of Byte())().Length = 20)

                Contract.Assume(G >= 0)
                Contract.Assume(N >= 0)
                Dim hash1 = G.ToUnsignedByteArray.SHA1
                Dim hash2 = N.ToUnsignedByteArray.SHA1
                Dim result = (From i In Enumerable.Range(0, 20) Select hash1(i) Xor hash2(i)).ToArray
                Contract.Assume(result.Length = 20)
                Return result
            End Get
        End Property

        ''' <summary>Determines credentials for the same client, but with a new key pair.</summary>
        ''' 
        Public Function Regenerate(Optional ByVal rng As Cryptography.RandomNumberGenerator = Nothing) As ClientCredentials
            Contract.Ensures(Contract.Result(Of ClientCredentials)() IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ClientCredentials)().UserName = Me.UserName)
            Return New ClientCredentials(Me.UserName, Me._password, rng)
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

                Dim userIdAuthData = "{0}:{1}".Frmt(Me._userName.ToUpperInvariant, Me._password.ToUpperInvariant).ToAscBytes
                Dim passwordKey = Concat(accountSalt.ToArray, userIdAuthData.SHA1).SHA1.ToUnsignedBigInteger
                Dim verifier = BigInteger.ModPow(G, passwordKey, N)
                Dim serverKey = serverPublicKeyBytes.SHA1.SubArray(0, 4).Reverse.ToUnsignedBigInteger

                'Shared value
                Dim serverPublicKey = serverPublicKeyBytes.ToUnsignedBigInteger
                Dim sharedValue = BigInteger.ModPow(serverPublicKey - verifier + N, Me._privateKey + serverKey * passwordKey, N)
                Contract.Assume(sharedValue >= 0)

                'Hash odd and even bytes of the shared value
                Dim sharedValueBytes = sharedValue.ToUnsignedByteArray.PaddedTo(32)
                Dim sharedHashEven = (From i In Enumerable.Range(0, 16) Select sharedValueBytes(i * 2) ).SHA1
                Dim sharedHashOdd = (From i In Enumerable.Range(0, 16) Select sharedValueBytes(i * 2 + 1) ).SHA1

                'Interleave odd and even hashes
                Dim result = (From i In Enumerable.Range(0, 40) Select If(i Mod 2 = 0, sharedHashEven(i \ 2), sharedHashOdd(i \ 2))).ToArray
                Contract.Assume(result.Length = 40)
                Return result
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
        ''' Determines the expected proof, from the server, that it knew the shared secret.
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
End Namespace
