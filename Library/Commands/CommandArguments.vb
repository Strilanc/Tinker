Namespace Commands
    Public Class CommandArguments
        Private Shared ReadOnly Delimiters As New Dictionary(Of Char, Char) From {
            {"("c, ")"c},
            {"{"c, "}"c},
            {"<"c, ">"c},
            {"["c, "]"c}}

        Private ReadOnly raw As New List(Of String)
        Private ReadOnly rawOptional As New List(Of String)
        Private ReadOnly named As New Dictionary(Of String, String)
        Private ReadOnly namedOptional As New Dictionary(Of String, String)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(raw IsNot Nothing)
            Contract.Invariant(rawOptional IsNot Nothing)
            Contract.Invariant(named IsNot Nothing)
            Contract.Invariant(namedOptional IsNot Nothing)
        End Sub

        Public ReadOnly Property RawArguments As IEnumerable(Of String)
            Get
                Contract.Ensures(Contract.Result(Of IEnumerable(Of String))() IsNot Nothing)
                Return raw
            End Get
        End Property
        Public ReadOnly Property OptionalArguments As IEnumerable(Of String)
            Get
                Contract.Ensures(Contract.Result(Of IEnumerable(Of String))() IsNot Nothing)
                Return rawOptional
            End Get
        End Property
        Public ReadOnly Property NamedArguments As IDictionary(Of String, String)
            Get
                Contract.Ensures(Contract.Result(Of IDictionary(Of String, String))() IsNot Nothing)
                Return named
            End Get
        End Property
        Public ReadOnly Property NamedOptionalArguments As IDictionary(Of String, String)
            Get
                Contract.Ensures(Contract.Result(Of IDictionary(Of String, String))() IsNot Nothing)
                Return namedOptional
            End Get
        End Property

        Public Sub New(ByVal text As String)
            Contract.Requires(text IsNot Nothing)

            Dim words = text.Split(" "c)
            For i = 0 To words.Length - 1
                If words(i) = "" Then Continue For
                Dim j = words(i).IndexOf("="c)
                If j >= 0 Then
                    Dim name = words(i).Substring(0, j)
                    Dim value = words(i).Substring(j + 1)
                    'Check for a delimeted value
                    If value.Length > 0 Then
                        If Delimiters.ContainsKey(value(0)) Then
                            'append until ending delimeter is found
                            Dim endVal = Delimiters(value(0))
                            Do Until words(i).EndsWith(endVal)
                                i += 1
                                If i >= words.Length Then
                                    Throw New ArgumentException("The named argument '{0}' starts with a '{1}', but no ending '{2}' was found.".Frmt(name, value(0), endVal))
                                End If
                                value += " " + words(i)
                            Loop
                            'remove delimeters
                            value = value.Substring(1, value.Length - 2)
                        End If
                    End If
                    'Add
                    If name.StartsWith("-") Then
                        name = name.Substring(1)
                        If namedOptional.ContainsKey(name) Then Throw New ArgumentException("The optional named argument '{0}' is specified twice.".Frmt(name))
                        namedOptional.Add(name, value)
                    Else
                        If named.ContainsKey(name) Then Throw New ArgumentException("The named argument '{0}' is specified twice.".Frmt(name))
                        named.Add(name, value)
                    End If
                Else
                    'Add
                    If words(i).StartsWith("-") Then
                        rawOptional.Add(words(i).Substring(1))
                    Else
                        raw.Add(words(i))
                    End If
                End If
            Next i
        End Sub
    End Class
End Namespace
