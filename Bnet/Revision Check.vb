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

Imports System.Linq.Expressions
Imports System.Reflection

Namespace Bnet
    ''' <summary>
    ''' A challenge/response hash function used by Blizzard to determine if a client has valid game files.
    ''' The operations and initial state of the hash are specified in the challenge.
    ''' </summary>
    Public Module RevisionCheck
#Region "Data"
        Private Const SeedVar As Char = "A"c
        Private Const ResultVar As Char = "C"c
        Private Const InputVar As Char = "S"c

        '''<summary>The files fed into the hashing process.</summary>
        Private ReadOnly HashFiles As String() = {
                "War3.exe",
                "Storm.dll",
                "Game.dll"
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

        '''<summary>Selects a seed based on the index string.</summary>
        <Pure()>
        Private Function ParseChallengeSeed(ByVal challengeSeed As String) As UInt32
            Contract.Requires(challengeSeed IsNot Nothing)

            Dim invIndexString As InvariantString = challengeSeed
            If Not invIndexString Like "ver-ix86-#.mpq" AndAlso Not invIndexString Like "ix86ver#.mpq" Then
                Throw New ArgumentException("Unrecognized Index String: {0}".Frmt(challengeSeed), "challengeSeed")
            End If
            Contract.Assume(challengeSeed.Length >= 5)

            Dim index = Byte.Parse(challengeSeed(challengeSeed.Length - 5), CultureInfo.InvariantCulture)
            If index >= IndexStringSeeds.Length Then Throw New ArgumentOutOfRangeException("challengeSeed", "Extracted index is larger than the hash seed table.")
            Return IndexStringSeeds(index)
        End Function

        ''' <summary>
        ''' Fills a buffer with data from a stream.
        ''' Any remaining space in the buffer is filled with generated data.
        ''' Returns false if there was no data in the stream.
        ''' </summary>
        Private Function PaddedRead(ByVal stream As IO.Stream, ByVal buffer As Byte()) As Boolean
            Contract.Requires(stream IsNot Nothing)
            Contract.Requires(buffer IsNot Nothing)
            Dim n = stream.Read(buffer, 0, buffer.Length)
            If n = 0 Then Return False
            For i = 0 To buffer.Length - n - 1
                buffer(i + n) = CByte(&HFF - (i And &HFF))
            Next i
            Return True
        End Function

        '''<summary>Combines two expressions into a binary expression, using the given operator character to determine the operation.</summary>
        Private Function ParseOperation(ByVal varLeft As Expression,
                                        ByVal [operator] As Char,
                                        ByVal varRight As Expression) As BinaryExpression
            Contract.Requires(varLeft IsNot Nothing)
            Contract.Requires(varRight IsNot Nothing)
            Contract.Ensures(Contract.Result(Of BinaryExpression)() IsNot Nothing)

            Select Case [operator]
                Case "+"c : Return Expression.Add(varLeft, varRight)
                Case "-"c : Return Expression.Subtract(varLeft, varRight)
                Case "*"c : Return Expression.Multiply(varLeft, varRight)
                Case "&"c : Return Expression.And(varLeft, varRight)
                Case "|"c : Return Expression.Or(varLeft, varRight)
                Case "^"c : Return Expression.ExclusiveOr(varLeft, varRight)
                Case Else : Throw New IO.InvalidDataException("Unrecognized revision check operator: '{0}'.".Frmt([operator]))
            End Select
        End Function
        '''<summary>Transforms a sequence of simple text statements, such as "A=A+B", into a block of binary expressions.</summary>
        <ContractVerification(False)>
        Private Function ParseStatements(ByVal statements As IEnumerable(Of String),
                                         ByVal locals As Dictionary(Of Char, ParameterExpression)) As BlockExpression
            Contract.Requires(statements IsNot Nothing)
            Contract.Requires(locals IsNot Nothing)
            Contract.Ensures(Contract.Result(Of BlockExpression)() IsNot Nothing)

            Return Expression.Block(From statement In statements
                                    Let varResult = locals(statement(0))
                                    Let varLeft = locals(statement(2))
                                    Let varRight = locals(statement(4))
                                    Let operation = ParseOperation(varLeft, statement(3), varRight)
                                    Select Expression.Assign(varResult, operation))
        End Function
        '''<summary>Transforms a sequence of simple text declarations, such as "A=52", into a block of assignment expressions.</summary>
        <ContractVerification(False)>
        Private Function ParseDeclarations(ByVal declarations As IEnumerable(Of String),
                                           ByVal locals As Dictionary(Of Char, ParameterExpression)) As BlockExpression
            Contract.Requires(declarations IsNot Nothing)
            Contract.Requires(locals IsNot Nothing)
            Contract.Ensures(Contract.Result(Of BlockExpression)() IsNot Nothing)

            Return Expression.Block(From declaration In declarations
                                    Let name = declaration(0)
                                    Let value = UInt32.Parse(declaration.Substring(2))
                                    Select Expression.Assign(locals(name), Expression.Constant(value)))
        End Function
        ''' <summary>
        ''' Extracts all variable names from simple text declarations and statements.
        ''' Also includes some default important variables.
        ''' </summary>
        Private Function ParseVariables(ByVal declarations As IEnumerable(Of String),
                                        ByVal statements As IEnumerable(Of String)) As Dictionary(Of Char, ParameterExpression)
            Contract.Requires(declarations IsNot Nothing)
            Contract.Requires(statements IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Dictionary(Of Char, ParameterExpression))() IsNot Nothing)

            Dim defaultVariables = {SeedVar, ResultVar, InputVar}
            Dim declarationVariables = From declaration In declarations Select declaration(0)
            Dim statementVariables = (From statement In statements Select {statement(0), statement(2), statement(4)}).Fold

            Dim variables = New Dictionary(Of Char, ParameterExpression)
            For Each name In {defaultVariables, declarationVariables, statementVariables}.Fold.Distinct
                variables.Add(name, Expression.Variable(GetType(UInt32), name))
            Next name
            Return variables
        End Function

        '''<summary>Generates a dynamically compiled function specialized to hashing data using the given instructions.</summary>
        <ContractVerification(False)>
        Private Function CompileInstructions(ByVal challengeInstructions As String,
                                             ByVal challengeSeed As String) As Func(Of IEnumerator(Of IO.Stream), UInt32)
            Contract.Requires(challengeInstructions IsNot Nothing)
            Contract.Requires(challengeSeed IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Func(Of IEnumerator(Of IO.Stream), UInt32))() IsNot Nothing)

            'Identify instructions
            Dim lines = challengeInstructions.Split(" "c)
            Dim declarations = From line In lines Where line Like "?=#*"
            Dim statements = From line In lines Where line Like "?=???"
            Dim statementCount = UInt32.Parse((From line In lines Where line Like "#").First)

            'Check
            If statements.Count <> statementCount Then Throw New ArgumentException("Statement count didn't match number of statements.")
            If statements.Count + declarations.Count + 1 <> lines.Count Then Throw New ArgumentException("Unrecognized instructions.")

            'Build method parts
            Dim dataParameter = Expression.Parameter(GetType(IEnumerator(Of IO.Stream)), "data")
            Dim varStream = Expression.Variable(GetType(IO.Stream), "stream")
            Dim varPos = Expression.Variable(GetType(Int32), "pos")
            Dim varBuffer = Expression.Variable(GetType(Byte()), "buffer")
            Dim hashVariables = ParseVariables(declarations, statements)

            'Method Initialization
            Dim initialization = Expression.Block(
                    ParseDeclarations(declarations, hashVariables),
                    Expression.Assign(varBuffer, Expression.NewArrayBounds(GetType(Byte), Expression.Constant(1024))),
                    expression.ExclusiveOrAssign(hashVariables(SeedVar), Expression.Constant(ParseChallengeSeed(challengeSeed))))

            'Method Main Loop
            '[Applies hashing instructions to each dword in buffered data]
            Dim breakTarget3 = Expression.Label("break3")
            Dim workLoop = Expression.Loop(Expression.Block(
                    Expression.IfThen(Expression.GreaterThanOrEqual(varPos, Expression.Constant(1024)),
                                      Expression.Break(breakTarget3)),
                    Expression.Assign(hashVariables(InputVar), Expression.Call(GetType(BitConverter).GetMethod("ToUInt32"), varBuffer, varPos)),
                    Expression.AddAssign(varPos, Expression.Constant(4)),
                    ParseStatements(statements, hashVariables)
                ), breakTarget3)
            '[Buffers data from 'varStream' until no more, running 'workLoop' on the buffered data]
            Dim breakTarget2 = Expression.Label("break2")
            Dim readLoop = Expression.Loop(Expression.Block(
                    Expression.IfThen(Expression.Not(Expression.Call(GetType(RevisionCheck).GetMethod("PaddedRead", BindingFlags.NonPublic Or BindingFlags.Static),
                                                                     varStream, varBuffer)),
                                      Expression.Break(breakTarget2)),
                    Expression.Assign(varPos, Expression.Constant(0)),
                    workLoop
                ), breakTarget2)
            '[Enumerates input streams, running 'readLoop' on the stream then disposing]
            Dim breakTarget1 = Expression.Label("break")
            Dim mainLoop = Expression.Loop(Expression.Block(
                    Expression.IfThen(Expression.Not(Expression.Call(dataParameter, GetType(Collections.IEnumerator).GetMethod("MoveNext"))),
                                      Expression.Break(breakTarget1)),
                    Expression.Assign(varStream, Expression.Call(dataParameter, GetType(IEnumerator(Of IO.Stream)).GetMethod("get_Current"))),
                    readLoop,
                    Expression.Call(varStream, GetType(IDisposable).GetMethod("Dispose"))
                ), breakTarget1)

            'Method Result
            Dim result = hashVariables(ResultVar)

            'Compile method
            Dim variables = hashVariables.Values.Concat({varBuffer, varPos, varStream})
            Dim methodBody = Expression.Block(variables, {initialization, mainLoop, result})
            Dim method = Expression.Lambda(Of Func(Of IEnumerator(Of IO.Stream), UInt32))(methodBody, dataParameter)
            Return method.Compile()
        End Function

        ''' <summary>
        ''' Determines the revision check value used when connecting to bnet.
        ''' </summary>
        ''' <param name="folder">The folder containing the hash files: War3.exe, Storm.dll, Game.dll.</param>
        ''' <param name="challengeSeed">Seeds the initial hash state.</param>
        ''' <param name="challengeInstructions">Specifies initial hash state as well how the hash state is updated.</param>
        ''' <remarks>Example challenge: A=443747131 B=3328179921 C=1040998290 4 A=A^S B=B-C C=C^A A=A+B</remarks>
        Public Function GenerateRevisionCheck(ByVal folder As String,
                                              ByVal challengeSeed As String,
                                              ByVal challengeInstructions As String) As UInteger
            Contract.Requires(folder IsNot Nothing)
            Contract.Requires(challengeSeed IsNot Nothing)
            Contract.Requires(challengeInstructions IsNot Nothing)
            Dim files = From filename In HashFiles
                        Select path = IO.Path.Combine(folder, filename)
                        Select New IO.FileStream(path, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read)
            Return CompileInstructions(challengeInstructions, challengeSeed).Invoke(files.GetEnumerator)
        End Function
    End Module
End Namespace
