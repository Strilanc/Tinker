Imports System.Reflection

Namespace Plugins
    <Serializable()>
    Public NotInheritable Class PluginException
        Inherits Exception
        Public Sub New(ByVal message As String, Optional ByVal innerException As Exception = Nothing)
            MyBase.New(message, innerException)
        End Sub
    End Class

    <ContractClass(GetType(IPluginSocket.ContractClass))>
    Public Interface IPluginSocket
        Inherits IDisposable
        ReadOnly Property Bot() As MainBot

        <ContractClassFor(GetType(IPluginSocket))>
        NotInheritable Class ContractClass
            Implements IPluginSocket
            Public ReadOnly Property Bot As MainBot Implements IPluginSocket.Bot
                Get
                    Contract.Ensures(Contract.Result(Of MainBot)() IsNot Nothing)
                    Throw New NotSupportedException
                End Get
            End Property
            Public Sub Dispose() Implements IDisposable.Dispose
            End Sub
        End Class
    End Interface

    <ContractClass(GetType(IPlugin.ContractClass))>
    Public Interface IPlugin
        Inherits IDisposable
        Sub Init(ByVal plugout As IPluginSocket)
        ReadOnly Property Description() As String
        ReadOnly Property Logger As Logger
        ReadOnly Property HasControl As Boolean
        ReadOnly Property Control() As Control
        Function InvokeCommand(ByVal user As BotUser, ByVal argument As String) As IFuture(Of String)
        Function IsArgumentPrivate(ByVal argument As String) As Boolean

        <ContractClassFor(GetType(IPlugin))>
        NotInheritable Class ContractClass
            Implements IPlugin
            Public ReadOnly Property Description As String Implements IPlugin.Description
                Get
                    Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                    Throw New NotSupportedException
                End Get
            End Property
            Public Sub Init(ByVal plugout As IPluginSocket) Implements IPlugin.Init
                Contract.Requires(plugout IsNot Nothing)
            End Sub
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
            Public Function InvokeCommand(ByVal user As BotUser, ByVal argument As String) As IFuture(Of String) Implements IPlugin.InvokeCommand
                Contract.Requires(argument IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IFuture(Of String))() IsNot Nothing)
                Throw New NotSupportedException
            End Function
            Public Function IsArgumentPrivate(ByVal argument As String) As Boolean Implements IPlugin.IsArgumentPrivate
                Contract.Requires(argument IsNot Nothing)
                Throw New NotSupportedException
            End Function
        End Class
    End Interface
End Namespace
