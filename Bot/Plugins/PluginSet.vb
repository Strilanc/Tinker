Namespace Plugins
    Friend Class PluginSet
        Inherits FutureDisposable

        Private ReadOnly inQueue As ICallQueue = New TaskedCallQueue()
        Private ReadOnly _sockets As New Dictionary(Of InvariantString, PluginSocket)
        Public Event DisposedPlugin(ByVal sender As PluginSet, ByVal socket As PluginSocket)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_sockets IsNot Nothing)
        End Sub

        Private Function LoadPlugin(ByVal name As InvariantString,
                                    ByVal assemblyPath As String,
                                    ByVal bot As MainBot) As PluginSocket
            Contract.Requires(assemblyPath IsNot Nothing)
            Contract.Ensures(Contract.Result(Of PluginSocket)() IsNot Nothing)

            If _sockets.ContainsKey(name) Then Throw New InvalidOperationException("Plugin already loaded.")
            Dim socket = New PluginSocket(name, bot, assemblyPath)
            _sockets(name) = socket
            socket.FutureDisposed.QueueCallWhenReady(inQueue,
                Sub()
                    _sockets.Remove(name)
                    RaiseEvent DisposedPlugin(Me, socket)
                End Sub)
            Return socket
        End Function
        Public Function QueueLoadPlugin(ByVal name As InvariantString, ByVal assemblyPath As String, ByVal bot As MainBot) As IFuture(Of PluginSocket)
            Contract.Requires(assemblyPath IsNot Nothing)
            Contract.Requires(bot IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of PluginSocket))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() LoadPlugin(name, assemblyPath, bot))
        End Function

        Public Function TryGetSocket(ByVal name As InvariantString) As IFuture(Of PluginSocket)
            Return inQueue.QueueFunc(Function() If(_sockets.ContainsKey(name), _sockets(name), Nothing))
        End Function

        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As Strilbrary.Threading.IFuture
            Return inQueue.QueueAction(
                Sub()
                    For Each socket In _sockets.Values
                        socket.Dispose()
                    Next socket
                End Sub)
        End Function
    End Class
End Namespace
