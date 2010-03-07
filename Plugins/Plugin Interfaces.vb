Namespace Plugins
    <Serializable()>
    Public NotInheritable Class PluginException
        Inherits Exception
        Public Sub New(ByVal message As String, Optional ByVal innerException As Exception = Nothing)
            MyBase.New(message, innerException)
        End Sub
    End Class

    <ContractClass(GetType(IPlugin.ContractClass))>
    Public Interface IPlugin
        Inherits IDisposableWithTask
        ReadOnly Property Description() As String
        ReadOnly Property Logger As Logger
        ReadOnly Property HasControl As Boolean
        ReadOnly Property Control() As Control
        Function InvokeCommand(ByVal user As BotUser, ByVal argument As String) As Task(Of String)
        Function IsArgumentPrivate(ByVal argument As String) As Boolean

        <ContractClassFor(GetType(IPlugin))>
        NotInheritable Shadows Class ContractClass
            Implements IPlugin
            Public ReadOnly Property Description As String Implements IPlugin.Description
                Get
                    Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                    Throw New NotSupportedException
                End Get
            End Property
            Public Sub Dispose() Implements IDisposable.Dispose
            End Sub
            Public ReadOnly Property Control As Control Implements IPlugin.Control
                Get
                    Contract.Requires(Me.HasControl)
                    Contract.Ensures(Contract.Result(Of Control)() IsNot Nothing)
                    Throw New NotSupportedException
                End Get
            End Property
            Public ReadOnly Property HasControl As Boolean Implements IPlugin.HasControl
                Get
                    Throw New NotSupportedException
                End Get
            End Property
            Public ReadOnly Property Logger As Logger Implements IPlugin.Logger
                Get
                    Contract.Ensures(Contract.Result(Of Logger)() IsNot Nothing)
                    Throw New NotSupportedException
                End Get
            End Property
            Public Function InvokeCommand(ByVal user As BotUser, ByVal argument As String) As Task(Of String) Implements IPlugin.InvokeCommand
                Contract.Requires(argument IsNot Nothing)
                Contract.Ensures(Contract.Result(Of Task(Of String))() IsNot Nothing)
                Throw New NotSupportedException
            End Function
            Public Function IsArgumentPrivate(ByVal argument As String) As Boolean Implements IPlugin.IsArgumentPrivate
                Contract.Requires(argument IsNot Nothing)
                Throw New NotSupportedException
            End Function
            Public ReadOnly Property DisposalTask As Task Implements IDisposableWithTask.DisposalTask
                Get
                    Throw New NotSupportedException
                End Get
            End Property
        End Class
    End Interface

    <ContractClass(GetType(IPluginFactory.ContractClass))>
    Public Interface IPluginFactory
        Function CreatePlugin(ByVal bot As Bot.MainBot) As IPlugin

        <ContractClassFor(GetType(IPluginFactory))>
        NotInheritable Class ContractClass
            Implements IPluginFactory

            Public Function CreatePlugin(ByVal bot As Bot.MainBot) As IPlugin Implements IPluginFactory.CreatePlugin
                Contract.Requires(bot IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IPlugin)() IsNot Nothing)
                Throw New NotSupportedException()
            End Function
        End Class
    End Interface
End Namespace
