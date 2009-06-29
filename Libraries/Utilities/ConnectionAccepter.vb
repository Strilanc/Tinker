Imports System.Net.Sockets

'''<summary>A thread-safe class which accepts connections on multiple ports, and returns accepted TcpClients using events.</summary>
Public Class ConnectionAccepter
    Private ReadOnly listeners As New HashSet(Of TcpListener)
    Private ReadOnly lock As New Object

    Public Event AcceptedConnection(ByVal sender As ConnectionAccepter, ByVal acceptedClient As TcpClient)

    '''<summary>Tries to start listening for connections on the given port.</summary>
    Public Function OpenPort(ByVal port As UShort) As Outcome
        Try
            SyncLock lock
                'already listening?
                Dim out = GetListenerOnPort(port)
                If out.succeeded Then Return success("Already listening on port {0}.".frmt(port))

                'listen and accept connections
                Dim listener = New TcpListener(Net.IPAddress.Any, port)
                listener.Start()
                listeners.Add(listener)
                listener.FutureAcceptConnection().CallWhenValueReady(YCombinator(Of Outcome(Of TcpClient))(
                    Function(self) Sub(acceptedClient)
                                       Dim client = acceptedClient.val

                                       SyncLock lock
                                           'succeeded?
                                           If Not acceptedClient.succeeded Then
                                               listener.Stop()
                                               listeners.Remove(listener)
                                               Return
                                           End If

                                           'still supposed to be listening?
                                           If Not listeners.Contains(listener) Then
                                               client.Close()
                                               Return
                                           End If

                                           'continue listening
                                           listener.FutureAcceptConnection().CallWhenValueReady(self)
                                       End SyncLock

                                       'report
                                       RaiseEvent AcceptedConnection(Me, client)
                                   End Sub))
            End SyncLock

            Return success("Started listening for connections on port {0}.".frmt(port))
        Catch e As Exception
            Return failure("Failed to listen for connections on port {0}: {1}".frmt(port, e.Message))
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
            Dim out = GetListenerOnPort(port)
            If Not out.succeeded Then Return success("Already not listening on port {0}.".frmt(port))

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
    Private Function GetListenerOnPort(ByVal port As UShort) As Outcome(Of TcpListener)
        SyncLock lock
            For Each listener In listeners
                If CType(listener.LocalEndpoint, Net.IPEndPoint).Port = port Then
                    Return successVal(listener, "Found listener on port {0}.".frmt(port))
                End If
            Next listener
        End SyncLock
        Return failure("No listener on port {0}.".frmt(port))
    End Function
End Class
