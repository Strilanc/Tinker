'Imports System.Net.Sockets

'Public Class RelayServer
'    Inherits ConnectionAccepter
'    Private WithEvents accepter As ConnectionAccepter
'    Private ReadOnly forward_host As String
'    Private ReadOnly forward_Port As UShort

'    Public Sub New(ByVal listen_port As UShort, ByVal forward_host As String, ByVal forward_port As UShort)
'        Me.forward_Port = forward_port
'        Me.forward_host = forward_host
'        accepter = New ConnectionAccepter()
'        Dim out = accepter.start_listening_on_port(listen_port)
'        If out.outcome <> Functional.Operations.Outcomes.succeeded Then Throw New Exception("Accepter failed")
'    End Sub

'    Private Sub accepter_accepted_connection(ByVal sender As ConnectionAccepter, ByVal accepted_client As System.Net.Sockets.TcpClient) Handles accepter.accepted_connection
'        Dim connecting_client As New TcpClient
'        connecting_client.BeginConnect(forward_host, forward_Port, AddressOf connected, New Tuple(Of TcpClient, TcpClient)(accepted_client, connecting_client))
'    End Sub

'    Private Sub connected(ByVal ar As IAsyncResult)
'        Dim clients = CType(ar.AsyncState, Tuple(Of TcpClient, TcpClient))
'        Dim accepted_client = clients.v1
'        Dim connecting_client = clients.v2
'        Try
'            connecting_client.EndConnect(ar)
'            Dim r = New StreamInterconnect(accepted_client.GetStream, connecting_client.GetStream)
'            AddHandler r.stream_ended, AddressOf stream_ended
'        Catch e As Exception
'            accepted_client.Close()
'            connecting_client.Close()
'        End Try
'    End Sub

'    Private Sub stream_ended(ByVal sender As StreamInterconnect, ByVal relay As StreamRelay)
'        RemoveHandler sender.stream_ended, AddressOf stream_ended
'        relay.out_stream.Close()
'    End Sub
'End Class

'Public Class StreamRelay
'    Private Const BUFFER_SIZE As Integer = 4096

'    Public ReadOnly in_stream As IO.Stream
'    Public ReadOnly out_stream As IO.Stream

'    Private ReadOnly buffer(0 To BUFFER_SIZE - 1) As Byte

'    Public Event stream_ended(ByVal sender As StreamRelay)

'    Public Sub New(ByVal in_stream As IO.Stream, ByVal out_stream As IO.Stream)
'        Me.in_stream = in_stream
'        Me.out_stream = out_stream
'        in_stream.BeginRead(buffer, 0, BUFFER_SIZE, AddressOf readed, Nothing)
'    End Sub

'    Private Sub readed(ByVal ar As IAsyncResult)
'        'End read
'        If Not in_stream.CanRead Then
'            RaiseEvent stream_ended(Me)
'            Return
'        End If
'        Dim total = in_stream.EndRead(ar)
'        If total = 0 Then
'            RaiseEvent stream_ended(Me)
'            Return
'        End If

'        'write
'        out_stream.Write(buffer, 0, total)

'        'start read
'        in_stream.BeginRead(buffer, 0, BUFFER_SIZE, AddressOf readed, Nothing)
'    End Sub
'End Class

'Public Class StreamMultiplexer
'    Private Const BUFFER_SIZE As Integer = 4096

'    Private relays As New List(Of StreamRelay)
'    Public ReadOnly out_stream As IO.Stream

'    Public Event stream_ended(ByVal sender As StreamMultiplexer, ByVal stream As IO.Stream)

'    Public Sub New(ByVal out_stream As IO.Stream)
'        Me.out_stream = out_stream
'    End Sub

'    Public Sub add(ByVal in_stream As IO.Stream, ByVal index As Integer)
'        relays.Add(New StreamRelay(in_stream, Me.out_stream))
'        Dim x = New Stream2(in_stream, index)
'        in_stream.BeginRead(x.buffer, 0, BUFFER_SIZE, AddressOf readed, Nothing)
'    End Sub
'    Public Sub remove(ByVal in_stream As IO.Stream)

'    End Sub

'    Private Sub readed(ByVal ar As IAsyncResult)
'        'End read
'        Dim stream = CType(ar.AsyncState, IO.Stream)
'        If Not stream.CanRead Then
'            RaiseEvent stream_ended(Me, stream)
'            Return
'        End If
'        Dim total = stream.EndRead(ar)
'        If total = 0 Then
'            RaiseEvent stream_ended(Me, stream)
'            Return
'        End If

'        'write
'        out_stream.Write(buffer, 0, total)

'        'start read
'        in_stream.BeginRead(buffer, 0, BUFFER_SIZE, AddressOf readed, Nothing)
'    End Sub
'End Class

'Public Class StreamInterconnect
'    Private WithEvents relay1 As StreamRelay
'    Private WithEvents relay2 As StreamRelay

'    Public ReadOnly stream1 As IO.Stream
'    Public ReadOnly stream2 As IO.Stream

'    Public Event stream_ended(ByVal sender As StreamInterconnect, ByVal relay As StreamRelay)

'    Public Sub New(ByVal stream1 As IO.Stream, ByVal stream2 As IO.Stream)
'        Me.stream1 = stream1
'        Me.stream2 = stream2
'        relay1 = New StreamRelay(stream1, stream2)
'        relay2 = New StreamRelay(stream2, stream1)
'    End Sub

'    Private Sub relay_stream_ended(ByVal sender As StreamRelay) Handles relay1.stream_ended, relay2.stream_ended
'        RaiseEvent stream_ended(Me, sender)
'    End Sub
'End Class

'Public Class ProcketServer
'    Private WithEvents accepter As New ConnectionAccepter()

'    Public Sub New(ByVal listen_port As UShort)
'        Dim out = accepter.start_listening_on_port(listen_port)
'        If out.outcome <> Functional.Operations.Outcomes.succeeded Then Throw New Exception("Accepter failed")
'    End Sub

'    Private Sub accepter_accepted_connection(ByVal sender As ConnectionAccepter, ByVal accepted_client As System.Net.Sockets.TcpClient) Handles accepter.accepted_connection

'    End Sub
'End Class

'Public Class ProcketSocket
'    Private WithEvents accepter As ConnectionAccepter
'    Private client As TcpClient
'    Private WithEvents reader As BnetSocket
'    Private connection_index As UInteger = 0
'    Private amap1 As New Dictionary(Of TcpClient, UInteger)
'    Private amap2 As New Dictionary(Of UInteger, TcpClient)

'    Private Enum IDS As Byte
'        [error]
'        open_port
'        close_port
'        open_connection
'        close_connection
'        transfer_data
'    End Enum

'    Private Sub reader_disconnected() Handles reader.disconnected

'    End Sub

'    Private Sub reader_receivedPacket(ByVal flag As Byte, ByVal id As Byte, ByVal data() As Byte) Handles reader.receivedPacket
'        Dim err_msg As String = Nothing
'        Dim return_data As Byte() = Nothing

'        Select Case CType(id, IDS)
'            Case IDS.open_port
'                If data.Length <> 2 Then

'                End If
'                Dim port = CUShort(unpackUInteger(data))
'                Dim out = accepter.start_listening_on_port(port)
'                If out.outcome = Functional.Operations.Outcomes.succeeded Then
'                    err_msg = out.message
'                    return_data = concat(New Byte() {1}, packUInteger(port))
'                Else
'                    return_data = concat(New Byte() {0}, packUInteger(port))
'                End If

'            Case IDS.close_port
'                If data.Length <> 2 Then

'                End If
'                Dim port = CUShort(unpackUInteger(data))
'                Dim out = accepter.stop_listening_on_port(port)
'                Select Case out.outcome
'                    Case Functional.Operations.Outcomes.unnecessary, Functional.Operations.Outcomes.failed
'                        err_msg = "No such open port"
'                        return_data = concat(New Byte() {1}, packUInteger(port))
'                    Case Else
'                        return_data = concat(New Byte() {0}, packUInteger(port))
'                End Select

'            Case IDS.open_connection

'            Case IDS.close_connection
'                If data.Length <> 4 Then

'                End If
'                Dim index = unpackUInteger(data)
'                If amap2.ContainsKey(index) Then
'                    Dim client = amap2(index)
'                    client.Close()
'                    amap1.Remove(client)
'                    amap2.Remove(index)
'                    return_data = concat(New Byte() {1}, packUInteger(index))
'                Else
'                    return_data = concat(New Byte() {0}, packUInteger(index))
'                End If

'            Case IDS.transfer_data
'                Dim index = unpackUInteger(subArray(data, 0, 4))
'                If amap2.ContainsKey(index) Then
'                    Dim client = amap2(index)
'                    client.GetStream().Write(data, 4, data.Length - 4)
'                Else
'                    err_msg = "what?"
'                End If
'        End Select
'    End Sub

'    Private Sub accepter_accepted_connection(ByVal sender As ConnectionAccepter, ByVal accepted_client As System.Net.Sockets.TcpClient)
'        connection_index += 1ui
'        amap1(accepted_client) = connection_index
'        amap2(connection_index) = accepted_client
'        reader.writeChunk(New Byte() {0, IDS.open_connection}, packUInteger(connection_index))
'    End Sub


'End Class