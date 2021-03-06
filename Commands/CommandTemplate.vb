Namespace Commands
    ''' <summary>
    ''' A template which text command arguments can be matched against.
    ''' </summary>
    <DebuggerDisplay("{ToString()}")>
    Public NotInheritable Class CommandTemplate
        Private ReadOnly template As CommandArgument
        Private ReadOnly rawMinCount As Integer
        Private ReadOnly rawMaxCount As Integer

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(template IsNot Nothing)
            Contract.Invariant(rawMinCount >= 0)
            Contract.Invariant(rawMaxCount >= 0)
        End Sub

        ''' <summary>
        ''' Constructs a command template using a format string.
        ''' </summary>
        Public Sub New(format As InvariantString)
            Me.New(New CommandArgument(format))
        End Sub
        ''' <summary>
        ''' Constructs a command template using a representative command argument.
        ''' </summary>
        Public Sub New(representativeArgument As CommandArgument)
            Contract.Requires(representativeArgument IsNot Nothing)
            Me.template = representativeArgument
            Me.rawMaxCount = Me.template.RawValueCount
            Me.rawMinCount = (From arg In Me.template.RawValues Where Not arg.StartsWith("?"c, StringComparison.Ordinal)).Count
            If (From arg In Me.template.RawValues.Take(Me.rawMinCount) Where arg.StartsWith("?"c, StringComparison.Ordinal)).Any Then
                Throw New ArgumentException("Optional raw values must come after required raw values.")
            End If
        End Sub

        ''' <summary>
        ''' Determines an exception for any mismatch between an argument and the template.
        ''' Returns nothing if the argument matches the template.
        ''' </summary>
        Public Function TryFindMismatch(text As String) As ArgumentException
            Contract.Requires(text IsNot Nothing)
            Return TryFindMismatch(New CommandArgument(text))
        End Function
        ''' <summary>
        ''' Determines an exception for any mismatch between an argument and the template.
        ''' Returns nothing if the argument matches the template.
        ''' </summary>
        Public Function TryFindMismatch(argument As CommandArgument) As ArgumentException
            Contract.Requires(argument IsNot Nothing)

            'Optional named argument keys must be understood
            For Each key In argument.OptionalNames
                If template.TryGetOptionalNamedValue(key) Is Nothing Then
                    If template.TryGetNamedValue(key) IsNot Nothing Then
                        Return New ArgumentException(
                            "The argument '-{0}={1}' should be required, not optional. Remove the '-' prefix.".
                                Frmt(key, argument.TryGetOptionalNamedValue(key)))
                    Else
                        Return New ArgumentException(
                            "The named optional argument '-{0}={1}' is not recognized. Remove it or perhaps fix any typos in it.".
                                Frmt(key, argument.TryGetOptionalNamedValue(key)))
                    End If
                End If
            Next key

            'Optional arguments must be understood
            For Each arg In argument.Switches
                If Not template.HasOptionalSwitch(arg) Then
                    Return New ArgumentException(
                        "The optional switch '-{0}' is not recognized. Remove it or perhaps fix any typos in it.".
                            Frmt(arg))
                End If
            Next arg

            'Named argument keys must match
            For Each key In argument.Names
                If template.TryGetNamedValue(key) Is Nothing Then
                    If template.TryGetOptionalNamedValue(key) IsNot Nothing Then
                        Return New ArgumentException(
                            "The argument '{0}={1}' should be optional, not required. Prefix it with a '-'.".
                                Frmt(key, argument.NamedValue(key)))
                    Else
                        Return New ArgumentException(
                            "The named argument '{0}={1}' is not recognized. Remove it or perhaps fix any typos in it.".
                                Frmt(key, argument.NamedValue(key)))
                    End If
                End If
            Next key
            For Each key In template.Names
                If argument.TryGetNamedValue(key) Is Nothing Then
                    Return New ArgumentException("Missing a named argument ({0}={1}).".Frmt(key, template.NamedValue(key)))
                End If
            Next key

            'Correct number of raw arguments must be included
            If argument.RawValueCount > rawMaxCount Then
                Return New ArgumentException(
                    "Expected only {0} raw arguments ({1}), but received {2}: '{3}'.".
                        Frmt(rawMaxCount, template.RawValues.StringJoin(", "), argument.RawValueCount, argument.RawValues.StringJoin(" ")))
            ElseIf argument.RawValueCount < rawMinCount Then
                Return New ArgumentException(
                    "Expected {0} raw arguments ({1}), but only received {2}: '{3}'.".
                        Frmt(rawMinCount, template.RawValues.StringJoin(", "), argument.RawValueCount, argument.RawValues.StringJoin(" ")))
            End If

            Return Nothing
        End Function

        Public Overrides Function ToString() As String
            Return template.ToString
        End Function
    End Class
End Namespace
