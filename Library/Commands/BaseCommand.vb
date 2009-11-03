Namespace Commands
    '''<summary>A simple base implementation of the itfCommand(Of T) interface.</summary>
    Public MustInherit Class BaseCommand(Of T)
        Implements ICommand(Of T)
        Private ReadOnly _name As String
        Private ReadOnly _help As String
        Private ReadOnly _argumentLimit As Integer
        Private ReadOnly _argumentLimitType As ArgumentLimitType
        Public ReadOnly requiredPermissions As Dictionary(Of String, UInteger)
        Private ReadOnly _extraHelp As Dictionary(Of String, String)
        Private ReadOnly _shouldHideArguments As Boolean

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_name IsNot Nothing)
            Contract.Invariant(_help IsNot Nothing)
            Contract.Invariant(requiredPermissions IsNot Nothing)
            Contract.Invariant(_extraHelp IsNot Nothing)
        End Sub

        Protected Sub New(ByVal name As String,
                          ByVal argumentLimit As Integer,
                          ByVal argumentLimitType As ArgumentLimitType,
                          ByVal help As String,
                          ByVal requiredPermissions As String,
                          ByVal extraHelp As String,
                          Optional ByVal shouldHideArguments As Boolean = False)
            Me.New(name,
                   argumentLimit,
                   argumentLimitType,
                   help,
                   BuildDictionaryFromString(requiredPermissions, Function(x) UInteger.Parse(x, CultureInfo.InvariantCulture)),
                   BuildDictionaryFromString(extraHelp, Function(x) x, vbNewLine),
                   shouldHideArguments)
            Contract.Requires(name IsNot Nothing)
            Contract.Requires(help IsNot Nothing)
            Contract.Requires(requiredPermissions IsNot Nothing)
            Contract.Requires(extraHelp IsNot Nothing)
        End Sub
        Protected Sub New(ByVal name As String,
                          ByVal argumentLimit As Integer,
                          ByVal argumentLimitType As ArgumentLimitType,
                          ByVal help As String,
                          Optional ByVal requiredPermissions As Dictionary(Of String, UInteger) = Nothing,
                          Optional ByVal extraHelp As Dictionary(Of String, String) = Nothing,
                          Optional ByVal shouldHideArguments As Boolean = False)
            Contract.Requires(name IsNot Nothing)
            Contract.Requires(help IsNot Nothing)
            Me._name = name
            Me._help = help
            Me._extraHelp = If(extraHelp, New Dictionary(Of String, String))
            Me._argumentLimit = argumentLimit
            Me._argumentLimitType = argumentLimitType
            Me._shouldHideArguments = shouldHideArguments
            Me.requiredPermissions = If(requiredPermissions, New Dictionary(Of String, UInteger))
            If Me.requiredPermissions.Count > 0 Then
                Me._help += " {"
                For Each permission In Me.requiredPermissions.Keys
                    Me._help += " {0}={1}".Frmt(permission, Me.requiredPermissions(permission))
                Next permission
                Me._help += " }"
            End If
        End Sub

        Public ReadOnly Property Name() As String Implements ICommand(Of T).Name
            Get
                Return _name
            End Get
        End Property
        Public ReadOnly Property Help() As String Implements ICommand(Of T).Help
            Get
                Return _help
            End Get
        End Property
        Public ReadOnly Property ExtraHelp() As Dictionary(Of String, String) Implements ICommand(Of T).ExtraHelp
            Get
                Return _extraHelp
            End Get
        End Property
        Public ReadOnly Property ShouldHideArguments() As Boolean Implements ICommand(Of T).ShouldHideArguments
            Get
                Return _shouldHideArguments
            End Get
        End Property
        Public ReadOnly Property ArgumentLimit() As Integer Implements ICommand(Of T).ArgumentLimit
            Get
                Return _argumentLimit
            End Get
        End Property
        Public ReadOnly Property ArgumentLimitType() As ArgumentLimitType Implements ICommand(Of T).ArgumentLimitType
            Get
                Return _argumentLimitType
            End Get
        End Property

        Public Function IsUserAllowed(ByVal user As BotUser) As Boolean Implements ICommand(Of T).IsUserAllowed
            If user Is Nothing Then Return True
            For Each key In requiredPermissions.Keys
                Contract.Assume(key IsNot Nothing)
                If user.Permission(key) < requiredPermissions(key) Then
                    Return False
                End If
            Next key
            Return True
        End Function

        '''<summary>Checks user permissions, number of arguments, and delegates processing to child implementation.</summary>
        <System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")>
        Public Function ProcessCommand(ByVal target As T, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String) Implements ICommand(Of T).Process
            Try
                'Check permissions
                If Not IsUserAllowed(user) Then
                    Throw New InvalidOperationException("You do not have sufficient permissions to use that command.")
                End If

                'Check arguments
                Select Case ArgumentLimitType
                    Case ArgumentLimitType.Exact
                        If arguments.Count > ArgumentLimit Then
                            Throw New ArgumentException("Too many arguments, expected at most {0}. Use quotes (""like this"") to surround individual arguments containing spaces.".Frmt(ArgumentLimit))
                        ElseIf arguments.Count < ArgumentLimit Then
                            Throw New ArgumentException("Not enough arguments, expected at least {0}.".Frmt(ArgumentLimit))
                        End If
                    Case ArgumentLimitType.Max
                        If arguments.Count > ArgumentLimit Then
                            Throw New ArgumentException("Too many arguments, expected at most {0}. Use quotes (""like this"") to surround individual arguments containing spaces.".Frmt(ArgumentLimit))
                        End If
                    Case ArgumentLimitType.Min
                        If arguments.Count < ArgumentLimit Then
                            Throw New ArgumentException("Not enough arguments, expected at least {0}.".Frmt(ArgumentLimit))
                        End If
                End Select

                'Run
                Return Process(target, user, arguments)
            Catch e As Exception
                e.RaiseAsUnexpected("Processing text for command.")
                Dim result = New FutureFunction(Of String)
                result.SetFailed(e)
                Return result
            End Try
        End Function

        Public Overridable Function Process(ByVal target As T, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
            Contract.Requires(target IsNot Nothing)
            Contract.Requires(arguments IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of String))() IsNot Nothing)
            Throw New NotSupportedException()
        End Function
    End Class
End Namespace
