Imports System.Net.Sockets
Imports System.Net

'''<summary>A thread-safe class which accepts connections on multiple ports, and returns accepted TcpClients using events.</summary>
Public NotInheritable Class ConnectionAccepter
    Private ReadOnly listeners As New HashSet(Of TcpListener)
    Private ReadOnly lock As New Object

    Public Event AcceptedConnection(ByVal sender As ConnectionAccepter, ByVal acceptedClient As TcpClient)

    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(listeners IsNot Nothing)
        Contract.Invariant(lock IsNot Nothing)
    End Sub

    '''<summary>Tries to start listening for connections on the given port.</summary>
    Public Sub OpenPort(ByVal port As UShort)
        SyncLock lock
            'already open?
            If TryGetListenerOnPort(port) IsNot Nothing Then
                Return
            End If

            'listen and accept connections
            Dim listener = New TcpListener(IPAddress.Any, port)
            listener.Start()
            listeners.Add(listener)
            FutureIterate(AddressOf listener.FutureAcceptConnection,
                Function(client, clientException)
                    SyncLock lock
                        'succeeded?
                        If clientException IsNot Nothing Then
                            listener.Stop()
                            listeners.Remove(listener)
                            Return False.Futurized
                        End If

                        'still supposed to be listening?
                        If Not listeners.Contains(listener) Then
                            client.Close()
                            Return False.Futurized
                        End If
                    End SyncLock

                    'report
                    RaiseEvent AcceptedConnection(Me, client)
                    Return True.Futurized
                End Function
            )
        End SyncLock
    End Sub

    '''<summary>Returns a list of all ports being listened on.</summary>
    Public Function EnumPorts() As IEnumerable(Of UShort)
        Dim ports = New List(Of UShort)
        SyncLock lock
            For Each listener In listeners
                Contract.Assume(listener IsNot Nothing)
                Contract.Assume(listener.LocalEndpoint IsNot Nothing)
                ports.Add(CUShort(CType(listener.LocalEndpoint, Net.IPEndPoint).Port))
            Next listener
        End SyncLock
        Return ports
    End Function

    '''<summary>Stops listening for connections on the given port.</summary>
    Public Sub ClosePort(ByVal port As UShort)
        SyncLock lock
            'already not listening?
            Dim listener = TryGetListenerOnPort(port)
            If listener Is Nothing Then
                Return 'already closed
            End If

            'stop listening
            listener.Stop()
            listeners.Remove(listener)
        End SyncLock
    End Sub

    '''<summary>Stops listening for connections on all ports.</summary>
    Public Sub CloseAllPorts()
        SyncLock lock
            For Each listener In listeners
                Contract.Assume(listener IsNot Nothing)
                listener.Stop()
            Next listener
            listeners.Clear()
        End SyncLock
    End Sub

    '''<summary>Returns the listener on the given port.</summary>
    Private Function TryGetListenerOnPort(ByVal port As UShort) As TcpListener
        SyncLock lock
            Return (From listener In listeners
                    Where CType(listener.LocalEndpoint, Net.IPEndPoint).Port = port).
                    FirstOrDefault
        End SyncLock
    End Function
End Class
