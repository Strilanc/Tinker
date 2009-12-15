'Tinker - Warcraft 3 game hosting bot
'Copyright (C) 2009 Craig Gidney
'
'This program is free software: you can redistribute it and/or modify
'it under the terms of the GNU General Public License as published by
'the Free Software Foundation, either version 3 of the License, or
'(at your option) any later version.
'
'This program is distributed in the hope that it will be useful,
'but WITHOUT ANY WARRANTY; without even the implied warranty of
'MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
'GNU General Public License for more details.
'You should have received a copy of the GNU General Public License
'along with this program.  If not, see http://www.gnu.org/licenses/

Namespace Bnet
    ''' <summary>
    ''' A challenge/response hash function used by Blizzard to determine if a client has valid game files.
    ''' The operations and initial state of the hash are specified in the challenge.
    ''' </summary>
    Public Module RevisionCheck
#Region "Data"
        '''<summary>The files fed into the hashing process.</summary>
        Private ReadOnly HashFiles As String() = {
                "War3.exe",
                "Storm.dll",
                "Game.dll"
            }
        '''<summary>The operations usable by the hashing process.</summary>
        Private ReadOnly Operations As New Dictionary(Of Char, Func(Of ModInt32, ModInt32, ModInt32)) From {
                {"+"c, Function(a, b) a + b},
                {"-"c, Function(a, b) a - b},
                {"*"c, Function(a, b) a * b},
                {"&"c, Function(a, b) a And b},
                {"^"c, Function(a, b) a Xor b},
                {"|"c, Function(a, b) a Or b}
            }
        '''<summary>The values which the index string chooses from to XOR into variable A's initial value.</summary>
        Private ReadOnly IndexStringSeeds As UInteger() = {
                &HE7F4CB62UI,
                &HF6A14FFCUI,
                &HAA5504AFUI,
                &H871FCDC2UI,
                &H11BF6A18UI,
                &HC57292E6UI,
                &H7927D27EUI,
                &H2FEC8733UI
            }
#End Region

        '''<summary>A parsed hashing operation.</summary>
        Private Structure Operation
            Private ReadOnly leftVar As Char
            Private ReadOnly rightVar As Char
            Private ReadOnly destVar As Char
            Private ReadOnly operation As Func(Of ModInt32, ModInt32, ModInt32)

            Public Sub New(ByVal leftVar As Char, ByVal rightVar As Char, ByVal destVar As Char, ByVal [operator] As Char)
                Me.leftVar = leftVar
                Me.rightVar = rightVar
                Me.destVar = destVar
                Me.operation = Operations([operator])
            End Sub

            Public Sub ApplyTo(ByVal vars As Dictionary(Of Char, ModInt32))
                vars(destVar) = operation(vars(leftVar), vars(rightVar))
            End Sub
        End Structure

        '''<summary>Extracts the initial variable values from the challenge.</summary>
        Private Function ReadVariablesFrom(ByVal lines As IEnumerator(Of String)) As Dictionary(Of Char, ModInt32)
            Contract.Requires(lines IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Dictionary(Of Char, ModInt32))() IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Dictionary(Of Char, ModInt32))().ContainsKey("A"c))
            Contract.Ensures(Contract.Result(Of Dictionary(Of Char, ModInt32))().ContainsKey("C"c))
            Contract.Ensures(Contract.Result(Of Dictionary(Of Char, ModInt32))().ContainsKey("S"c))

            Dim variables = New Dictionary(Of Char, ModInt32)
            variables("A"c) = 0
            variables("C"c) = 0
            variables("S"c) = 0
            Do 'Read until a non-variable declaration line is met
                If Not lines.MoveNext Then Throw New ArgumentException("Instructions ended prematurely.")
                If lines.Current Is Nothing OrElse Not lines.Current Like "?=*" Then Exit Do
                Contract.Assume(lines.Current.Length >= 2)

                'Parse declaration
                Dim u As UInteger
                If Not UInteger.TryParse(lines.Current.Substring(2), u) Then
                    Throw New ArgumentException("Invalid variable initialization line: {0}".Frmt(lines.Current))
                End If
                variables(lines.Current(0)) = u
            Loop
            Contract.Assume(variables.ContainsKey("A"c))
            Return variables
        End Function

        '''<summary>Extracts the hash operations from the challenge.</summary>
        Private Function ReadOperationsFrom(ByVal lines As IEnumerator(Of String),
                                            ByVal variables As Dictionary(Of Char, ModInt32)) As IEnumerable(Of Operation)
            Contract.Requires(lines IsNot Nothing)
            Contract.Requires(variables IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IEnumerable(Of Operation))() IsNot Nothing)

            'Read Operation Count [already loaded from reading variables; no need for MoveNext]
            Dim numOps As Byte 'number of operations
            If Not Byte.TryParse(lines.Current, numOps) Then
                Throw New ArgumentException("Instructions did not include a valid operation count: {0}".Frmt(lines.Current))
            End If

            'Read Operations
            Dim result = New List(Of Operation)(capacity:=numOps)
            For i = 0 To numOps - 1
                If Not lines.MoveNext Then
                    Throw New ArgumentException("Instructions included less operations than the {0} specified.".Frmt(numOps))
                ElseIf lines.Current Is Nothing OrElse Not lines.Current Like "?=???" Then
                    Throw New ArgumentException("Invalid operation specified: {0}".Frmt(lines.Current))
                End If

                'Parse
                Dim destVar = lines.Current(0)
                Dim leftVar = lines.Current(2)
                Dim [operator] = lines.Current(3)
                Dim rightVar = lines.Current(4)
                'Check
                If Not variables.ContainsKey(destVar) OrElse
                        Not variables.ContainsKey(leftVar) OrElse
                        Not variables.ContainsKey(rightVar) OrElse
                        Not Operations.ContainsKey([operator]) Then
                    Throw New ArgumentException("Operation involved unexpected variable or operator: {0}.".Frmt(lines.Current))
                End If
                'Store
                result.Add(New Operation(leftVar, rightVar, destVar, [operator]))
            Next i
            Return result
        End Function

        '''<summary>Selects a seed based on the index string.</summary>
        <Pure()>
        Private Function ExtractIndexStringHashSeed(ByVal indexString As String) As UInteger
            Contract.Requires(indexString IsNot Nothing)

            Dim invIndexString As InvariantString = indexString
            If Not invIndexString Like "ver-ix86-#.mpq" AndAlso Not invIndexString Like "ix86ver#.mpq" Then
                Throw New ArgumentException("Unrecognized Index String: {0}".Frmt(indexString), "indexString")
            End If
            Contract.Assume(indexString.Length >= 5)

            Dim index = Byte.Parse(indexString(indexString.Length - 5), CultureInfo.InvariantCulture)
            If index >= IndexStringSeeds.Length Then Throw New ArgumentOutOfRangeException("Extracted index is larger than the hash seed table.")
            Return IndexStringSeeds(index)
        End Function

        ''' <summary>
        ''' Determines the revision check value used when connecting to bnet.
        ''' </summary>
        ''' <param name="folder">The folder containing the hash files: War3.exe, Storm.dll, Game.dll.</param>
        ''' <param name="seedString">Seeds the initial hash state.</param>
        ''' <param name="challengeString">Specifies initial hash state as well how the hash state is updated.</param>
        ''' <remarks>Example input: A=443747131 B=3328179921 C=1040998290 4 A=A^S B=B-C C=C^A A=A+B</remarks>
        Public Function GenerateRevisionCheck(ByVal folder As String,
                                              ByVal seedString As String,
                                              ByVal challengeString As String) As UInteger
            Contract.Requires(folder IsNot Nothing)
            Contract.Requires(seedString IsNot Nothing)
            Contract.Requires(challengeString IsNot Nothing)

            'Parse
            Dim lines = CType(challengeString.Split(" "c), IEnumerable(Of String)).GetEnumerator()
            Dim variables = ReadVariablesFrom(lines)
            Dim operations = ReadOperationsFrom(lines, variables)
            If lines.MoveNext Then Throw New ArgumentException("More revision check instructions than expected.")

            'Seed variable A [the point of this? obfuscation I guess]
            variables("A"c) = variables("A"c) Xor ExtractIndexStringHashSeed(seedString)

            'Init tail buffer [used to extend file data to a multiple of 1024]
            Dim tailBuffer(0 To 1024 - 1) As Byte
            For i = 0 To tailBuffer.Length - 1
                tailBuffer(i) = CByte(255 - (i Mod 256))
            Next i

            'Process hash files
            For Each filename In HashFiles
                Using file = New IO.FileStream(folder + filename, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read)
                    'Apply operations using each dword in stream
                    Dim br = New IO.BinaryReader(New IO.BufferedStream(New ConcatStream({file, New IO.MemoryStream(tailBuffer)})))
                    For repeat = 0 To CInt(file.Length).CeilingMultiple(1024) - 1 Step 4
                        variables("S"c) = br.ReadUInt32()
                        For Each op In operations
                            op.ApplyTo(variables)
                        Next op
                    Next repeat
                End Using
            Next filename

            'Result is the final value of Variable C
            Return variables("C"c)
        End Function
    End Module
End Namespace
