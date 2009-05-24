Imports System.Net.Sockets

'''<summary>A thread-safe class which accepts connections on multiple ports, and returns accepted TcpClients using events.</summary>
Public Class ConnectionAccepter
    Private ReadOnly listeners As New List(Of TcpListener)
    Private ReadOnly lock As New Object()

    Public Event accepted_connection(ByVal sender As ConnectionAccepter, ByVal accepted_client As TcpClient)

    '''<summary>Tries to start listening for connections on the given port.</summary>
    Public Function OpenPort(ByVal port As UShort) As Outcome
        Try
            SyncLock lock
                'already listening?
                Dim out = get_listener_on_port(port)
                If out.outcome = Outcomes.succeeded Then Return success("Already listening on port {0}.".frmt(port))

                'listen
                Dim listener = New TcpListener(Net.IPAddress.Any, port)
                listener.Start()
                listeners.Add(listener)
                listener.BeginAcceptTcpClient(AddressOf finish_accept_connection, listener)
            End SyncLock

            Return success("Started listening for connections on port {0}.".frmt(port))
        Catch e As Exception
            Return failure("Failed to listen for connections on port {0}. {1}".frmt(port, e.Message))
        End Try
    End Function

    '''<summary>Returns a list of all ports being listened on.</summary>
    Public Function EnumPorts() As IEnumerable(Of UShort)
        Dim ports = New List(Of UShort)
        SyncLock lock
            For Each listener In listeners
                ports.Add(CUShort(CType(listener.LocalEndpoint, Net.IPEndPoint).Port))
            Next listener
        End SyncLock
        Return ports
    End Function

    '''<summary>Stops listening for connections on the given port.</summary>
    Public Function ClosePort(ByVal port As UShort) As Outcome
        SyncLock lock
            'already not listening?
            Dim out = get_listener_on_port(port)
            If out.outcome <> Outcomes.succeeded Then Return success("Already not listening on port {0}.".frmt(port))

            'stop listening
            Dim listener = out.val
            listener.Stop()
            listeners.Remove(listener)
        End SyncLock

        Return success("Stopped listening on port {0}.".frmt(port.ToString()))
    End Function

    '''<summary>Stops listening for connections on all ports.</summary>
    Public Function CloseAllPorts() As Outcome
        SyncLock lock
            'already not listening?
            If listeners.Count = 0 Then
                Return success("Already wasn't listening on any ports.")
            End If

            'stop listening
            For Each listener In listeners
                listener.Stop()
            Next listener
            listeners.Clear()
        End SyncLock

        Return success("Stopped listening on all ports.")
    End Function

    '''<summary>Returns the listener on the given port.</summary>
    Private Function get_listener_on_port(ByVal port As UShort) As Outcome(Of TcpListener)
        SyncLock lock
            For Each listener In listeners
                If CType(listener.LocalEndpoint, Net.IPEndPoint).Port = port Then
                    Return successVal(listener, "Found listener on port {0}.".frmt(port))
                End If
            Next listener
        End SyncLock
        Return failure("No listener on port {0}.".frmt(port))
    End Function

    '''<summary>Finishes accepting a client, and continues listening.</summary>
    Private Sub finish_accept_connection(ByVal ar As System.IAsyncResult)
        Dim client As TcpClient
        Dim listener = CType(ar.AsyncState, TcpListener)
        SyncLock lock
            'still supposed to be listening?
            If Not listeners.Contains(listener) Then Return
            'accept
            Try
                client = listener.EndAcceptTcpClient(ar)
            Catch e As Exception
                client = Nothing
            End Try
            'keep listening
            listener.BeginAcceptTcpClient(AddressOf finish_accept_connection, listener)
        End SyncLock

        'report
        If client IsNot Nothing Then
            RaiseEvent accepted_connection(Me, client)
        End If
    End Sub
End Class
