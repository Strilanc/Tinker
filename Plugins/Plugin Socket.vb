Imports System.Reflection

Namespace Plugins
    Friend Class Socket
        Inherits FutureDisposable

        Private ReadOnly _plugin As IPlugin
        Private ReadOnly _name As InvariantString

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_plugin IsNot Nothing)
        End Sub

        <CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId:="System.Reflection.Assembly.LoadFrom")>
        Public Sub New(ByVal name As InvariantString,
                       ByVal bot As Bot.MainBot,
                       ByVal assemblyPath As String)
            Contract.Requires(bot IsNot Nothing)
            Contract.Requires(assemblyPath IsNot Nothing)
            Me._name = name
            Try
                Dim asm = Assembly.LoadFrom(assemblyPath)
                Contract.Assume(asm IsNot Nothing)
                Dim classType = asm.GetType("TinkerPluginFactory")
                If classType Is Nothing Then Throw New OperationFailedException("The target assembly doesn't contain a TinkerPluginFactory.")
                Me._plugin = CType(Activator.CreateInstance(classType), IPluginFactory).AssumeNotNull.CreatePlugin(bot)
            Catch ex As Exception
                Throw New PluginException("Error loading plugin assembly from '{0}': {1}.".Frmt(assemblyPath, ex), ex)
            End Try
        End Sub

        Public ReadOnly Property Name As InvariantString
            Get
                Return _name
            End Get
        End Property
        Public ReadOnly Property Plugin As IPlugin
            Get
                Contract.Ensures(Contract.Result(Of IPlugin)() IsNot Nothing)
                If _plugin Is Nothing Then Throw New InvalidStateException("Used a PluginSocket whose constructor threw an exception.")
                Return _plugin
            End Get
        End Property

        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As IFuture
            _plugin.Dispose()
            Return Nothing
        End Function
    End Class
End Namespace
