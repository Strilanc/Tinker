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
    ''' <remarks>
    ''' I am not a cryptographer. I do not know how to write/verify code against things like timing attacks.
    ''' I do consider this implementation secure enough, given that the thing at risk is the bnet user account of a bot.
    ''' </remarks>
    <DebuggerDisplay("{UserName}")>
    Public Class ClientCredentials
        Private Shared ReadOnly G As BigInteger = 47
        Private Shared ReadOnly N As BigInteger = BigInteger.Parse("112624315653284427036559548610503669920632123929604336254260115573677366691719",
                                                                   CultureInfo.InvariantCulture)
        '''<summary>A value derived from the shared crypto constants.</summary>
        Private Shared ReadOnly Property FixedSalt() As IEnumerable(Of Byte)
            Get
                Contract.Ensures(Contract.Result(Of IEnumerable(Of Byte))() IsNot Nothing)
                Contract.Assume(G >= 0)
                Contract.Assume(N >= 0)
                Dim hash1 = G.ToUnsignedBytes.SHA1
                Dim hash2 = N.ToUnsignedBytes.SHA1
                Return hash1.Zip(hash2, Function(e1, e2) e1 xor e2)
            End Get
        End Property

        Private ReadOnly _userName As String
        Private ReadOnly _password As String
        Private ReadOnly _privateKey As BigInteger
        Private ReadOnly _publicKey As BigInteger

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_userName IsNot Nothing)
            Contract.Invariant(_password IsNot Nothing)
            Contract.Invariant(_privateKey > 0)
            Contract.Invariant(_publicKey >= 0)
        End Sub

        ''' <summary>
        ''' Constructs client credentials using the given username, password and private key.
        ''' Computes the public key from the private key.
        ''' </summary>
        ''' <param name="username">The client's username.</param>
        ''' <param name="password">The client's password.</param>
        ''' <param name="privateKey">The privateKey used for authentication.</param>
        Public Sub New(userName As String,
                       password As String,
                       privateKey As BigInteger)
            Contract.Requires(userName IsNot Nothing)
            Contract.Requires(password IsNot Nothing)
            Contract.Requires(privateKey > 0)
            Contract.Ensures(Me.UserName = userName)
            Me._userName = userName
            Me._password = password
            Me._privateKey = privateKey
            Me._publicKey = BigInteger.ModPow(G, _privateKey, N)
            Contract.Assume(_publicKey >= 0)
        End Sub
        ''' <summary>Creates client credentials for the given username and password, with a public/private key pair generated using the given random number generator.</summary>
        ''' <param name="username">The client's username.</param>
        ''' <param name="password">The client's password.</param>
        ''' <param name="randomNumberGenerator">The random number generator used to create the private key.</param>
        Public Shared Function GeneratedFrom(userName As String,
                                             password As String,
                                             randomNumberGenerator As Cryptography.RandomNumberGenerator) As ClientCredentials
            Contract.Requires(userName IsNot Nothing)
            Contract.Requires(password IsNot Nothing)
            Contract.Requires(randomNumberGenerator IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ClientCredentials)() IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ClientCredentials)().UserName = userName)
            Return New ClientCredentials(userName, password, GeneratePrivateKey(randomNumberGenerator))
        End Function

        '''<summary>The client's username used for identification.</summary>
        Public ReadOnly Property UserName As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Return _userName
            End Get
        End Property
        '''<summary>The public key used for authentication.</summary>
        Public ReadOnly Property PublicKey As BigInteger
            Get
                Contract.Ensures(Contract.Result(Of BigInteger)() >= 0)
                Return _publicKey
            End Get
        End Property
        '''<summary>The 32 byte representation of the public key used for authentication.</summary>
        Public ReadOnly Property PublicKeyBytes As IRist(Of Byte)
            Get
                Contract.Ensures(Contract.Result(Of IRist(Of Byte))() IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IRist(Of Byte))().Count = 32)
                Dim result = _publicKey.ToUnsignedBytes.PaddedTo(minimumLength:=32)
                Contract.Assume(result.Count = 32)
                Return result
            End Get
        End Property

        '''<summary>Generates a new private crypto key.</summary>
        '''<remarks>1 bit of entropy is lost in the Mod operation, due to the bias towards lower values.</remarks>
        Private Shared Function GeneratePrivateKey(rng As Cryptography.RandomNumberGenerator) As BigInteger
            Contract.Requires(rng IsNot Nothing)
            Contract.Ensures(Contract.Result(Of BigInteger)() > 0)
            Contract.Assume(N > 0)
            Contract.Assume(N >= 0)
            Dim privateKeyDataBuffer = N.ToUnsignedBytes.ToArray
            rng.GetBytes(privateKeyDataBuffer)
            Return privateKeyDataBuffer.ToUnsignedBigInteger.PositiveMod(N)
        End Function

        '''<summary>Creates credentials for the same client, but with a new key pair generated using the given random number generator.</summary>
        Public Function WithNewGeneratedKeys(randomNumberGenerator As Cryptography.RandomNumberGenerator) As ClientCredentials
            Contract.Requires(randomNumberGenerator IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ClientCredentials)() IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ClientCredentials)().UserName = Me.UserName)
            Return GeneratedFrom(Me.UserName, Me._password, randomNumberGenerator)
        End Function

        '''<summary>A shared secret value, which can be computed by both the client and server.</summary>
        Private ReadOnly Property SharedSecret(accountSalt As IEnumerable(Of Byte),
                                               serverPublicKeyBytes As IEnumerable(Of Byte)) As IRist(Of Byte)
            Get
                Contract.Requires(serverPublicKeyBytes IsNot Nothing)
                Contract.Requires(accountSalt IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IRist(Of Byte))() IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IRist(Of Byte))().Count = 40)

                Dim userIdAuthData = "{0}:{1}".Frmt(Me._userName.ToUpperInvariant, Me._password.ToUpperInvariant).ToAsciiBytes
                Dim passwordKey = Concat(accountSalt, userIdAuthData.SHA1).SHA1.ToUnsignedBigInteger
                Dim verifier = BigInteger.ModPow(G, passwordKey, N)
                Dim serverKey = serverPublicKeyBytes.SHA1.TakeExact(4).Reverse.ToUnsignedBigInteger

                'Shared value
                Dim serverPublicKey = serverPublicKeyBytes.ToUnsignedBigInteger
                Dim sharedValue = BigInteger.ModPow(serverPublicKey - verifier + N, Me._privateKey + serverKey * passwordKey, N)

                'Interleave hashes of odd and even bytes of the shared value
                Contract.Assume(sharedValue >= 0)
                Dim result = sharedValue.ToUnsignedBytes().PaddedTo(32).
                             Deinterleaved(2).Select(Function(slice) slice.SHA1()).Interleaved().
                             ToRist()
                Contract.Assume(result.Count = 40)
                Return result
            End Get
        End Property
        '''<summary>A proof for the server that the client knows the password.</summary>
        Public ReadOnly Property ClientPasswordProof(accountSalt As IEnumerable(Of Byte),
                                                     serverPublicKeyBytes As IEnumerable(Of Byte)) As IRist(Of Byte)
            Get
                Contract.Requires(serverPublicKeyBytes IsNot Nothing)
                Contract.Requires(accountSalt IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IRist(Of Byte))() IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IRist(Of Byte))().Count = 20)

                Return Concat(FixedSalt,
                              UserName.ToUpperInvariant.ToAsciiBytes.SHA1,
                              accountSalt,
                              PublicKeyBytes,
                              serverPublicKeyBytes,
                              SharedSecret(accountSalt, serverPublicKeyBytes)
                              ).SHA1
            End Get
        End Property
        '''<summary>An expected proof of knowing the shared secret from the server.</summary>
        Public ReadOnly Property ServerPasswordProof(accountSalt As IEnumerable(Of Byte),
                                                     serverPublicKeyBytes As IEnumerable(Of Byte)) As IRist(Of Byte)
            Get
                Contract.Requires(serverPublicKeyBytes IsNot Nothing)
                Contract.Requires(accountSalt IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IRist(Of Byte))() IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IRist(Of Byte))().Count = 20)

                Return Concat(PublicKeyBytes,
                              ClientPasswordProof(accountSalt, serverPublicKeyBytes),
                              SharedSecret(accountSalt, serverPublicKeyBytes)
                              ).SHA1
            End Get
        End Property
    End Class
End Namespace
