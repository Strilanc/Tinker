Imports System.Net.Sockets
Imports System.Net

'''<summary>A thread-safe class which accepts connections on multiple ports, and returns accepted TcpClients using events.</summary>
Public NotInheritable Class ConnectionAccepter
    Private ReadOnly listeners As New HashSet(Of TcpListener)
    Private ReadOnly lock As New Object

    Public Event AcceptedConnection(sender As ConnectionAccepter, acceptedClient As TcpClient)

    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(listeners IsNot Nothing)
        Contract.Invariant(lock IsNot Nothing)
    End Sub

    '''<summary>Tries to start listening for connections on the given port.</summary>
    Public Async Sub OpenPort(port As UShort)
        Dim listener As TcpListener
        SyncLock lock
            'already open?
            If TryGetListenerOnPort(port) IsNot Nothing Then
                Return
            End If

            'listen and accept connections
            listener = New TcpListener(IPAddress.Any, port)
            listener.Start()
            listeners.Add(listener)
        End SyncLock

        'Async accept clients
        Try
            Do
                Dim client = Await listener.AcceptTcpClientAsync()
                RaiseEvent AcceptedConnection(Me, client)
                SyncLock lock
                    If Not listeners.Contains(listener) Then Exit Do
                End SyncLock
            Loop
        Catch ex As Exception
            listener.Stop()
            SyncLock lock
                listeners.Remove(listener)
            End SyncLock
        End Try
    End Sub

    '''<summary>Returns a list of all ports being listened on.</summary>
    Public Function EnumPorts() As IEnumerable(Of UShort)
        SyncLock lock
            Return (From listener In listeners
                    Select CUShort(DirectCast(listener.LocalEndpoint, Net.IPEndPoint).Port)
                    ).Cache
        End SyncLock
    End Function

    '''<summary>Stops listening for connections on the given port.</summary>
    Public Sub ClosePort(port As UShort)
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
    Private Function TryGetListenerOnPort(port As UShort) As TcpListener
        SyncLock lock
            Return (From listener In listeners
                    Where DirectCast(listener.LocalEndpoint, Net.IPEndPoint).Port = port).
                    FirstOrDefault
        End SyncLock
    End Function
End Class
