Imports System.Reflection

Namespace Plugins
    Friend Class PluginSocket
        Inherits FutureDisposable
        Implements IPluginSocket

        Private ReadOnly _plugin As IPlugin
        Private ReadOnly _bot As MainBot
        Private ReadOnly _name As InvariantString

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_plugin IsNot Nothing)
            Contract.Invariant(_bot IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As InvariantString,
                       ByVal bot As MainBot,
                       ByVal assemblyPath As String)
            Contract.Requires(bot IsNot Nothing)
            Contract.Requires(assemblyPath IsNot Nothing)
            Me._name = name
            Me._bot = bot
            Try
                Dim asm = Assembly.LoadFrom(assemblyPath)
                Contract.Assume(asm IsNot Nothing)
                Dim classType = asm.GetType("HostBotPlugin")
                Contract.Assume(classType IsNot Nothing)
                Me._plugin = CType(Activator.CreateInstance(classType), IPlugin)
                Contract.Assume(_plugin IsNot Nothing)
                Me._plugin.Init(Me)
            Catch e As Exception
                Throw New PluginException("Error loading plugin assembly from '{1}'.".Frmt(assemblyPath), e)
            End Try
        End Sub

        Public ReadOnly Property Name As InvariantString
            Get
                Return _name
            End Get
        End Property
        Public ReadOnly Property Bot As MainBot Implements IPluginSocket.Bot
            Get
                Return _bot
            End Get
        End Property
        Public ReadOnly Property Plugin As IPlugin
            Get
                Return _plugin
            End Get
        End Property

        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As IFuture
            _plugin.Dispose()
            Return Nothing
        End Function
    End Class
End Namespace
