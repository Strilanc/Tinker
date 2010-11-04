Namespace Commands
    <ContractClass(GetType(ICommandContractClass(Of )))>
    Public Interface ICommand(Of In T)
        ReadOnly Property Name As InvariantString
        ReadOnly Property Description As String
        ReadOnly Property Format As InvariantString
        ReadOnly Property HelpTopics As IDictionary(Of InvariantString, String)
        ReadOnly Property Permissions As String
        Function IsArgumentPrivate(ByVal argument As String) As Boolean
        Function IsUserAllowed(ByVal user As BotUser) As Boolean
        Function Invoke(ByVal target As T, ByVal user As BotUser, ByVal argument As String) As Task(Of String)
    End Interface

    <ContractClassFor(GetType(ICommand(Of )))>
    Public MustInherit Class ICommandContractClass(Of T)
        Implements ICommand(Of T)
        Public ReadOnly Property Description As String Implements ICommand(Of T).Description
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Throw New NotSupportedException
            End Get
        End Property
        Public ReadOnly Property Format As InvariantString Implements ICommand(Of T).Format
            Get
                Throw New NotSupportedException
            End Get
        End Property
        Public ReadOnly Property HelpTopics As IDictionary(Of InvariantString, String) Implements ICommand(Of T).HelpTopics
            Get
                Contract.Ensures(Contract.Result(Of IDictionary(Of InvariantString, String))() IsNot Nothing)
                Throw New NotSupportedException
            End Get
        End Property
        Public Function Invoke(ByVal target As T, ByVal user As BotUser, ByVal argument As String) As Task(Of String) Implements ICommand(Of T).Invoke
            Contract.Requires(target IsNot Nothing)
            Contract.Requires(argument IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of String))() IsNot Nothing)
            Throw New NotSupportedException
        End Function
        Public Function IsArgumentPrivate(ByVal argument As String) As Boolean Implements ICommand(Of T).IsArgumentPrivate
            Contract.Requires(argument IsNot Nothing)
            Throw New NotSupportedException
        End Function
        Public Function IsUserAllowed(ByVal user As BotUser) As Boolean Implements ICommand(Of T).IsUserAllowed
            Throw New NotSupportedException
        End Function
        Public ReadOnly Property Name As InvariantString Implements ICommand(Of T).Name
            Get
                Throw New NotSupportedException
            End Get
        End Property
        Public ReadOnly Property Permissions As String Implements ICommand(Of T).Permissions
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Throw New NotSupportedException
            End Get
        End Property
    End Class
End Namespace
