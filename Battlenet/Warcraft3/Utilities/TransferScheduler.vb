'''<summary>Schedules transfers for sharing a copyable resource among multiple clients.</summary>
Public Class TransferScheduler(Of K)
    Private ReadOnly typical_rate As Double
    Private ReadOnly clients As New Dictionary(Of K, ClientState)
    Private ReadOnly ref As ICallQueue = New ThreadpooledCallQueue

    Public Event actions(ByVal started As List(Of TransferEndPoint), ByVal stopped As List(Of TransferEndPoint))

    Public Class TransferEndPoint
        Public ReadOnly src As K
        Public ReadOnly dst As K
        Public Sub New(ByVal src As K, ByVal dst As K)
            Me.src = src
            Me.dst = dst
        End Sub
    End Class

    Public Sub New(ByVal typical_rate As Double)
        Me.typical_rate = typical_rate
    End Sub

    '''<summary>Adds a new client to the pool.</summary>
    Public Function add(ByVal client_key As K, ByVal completed As Boolean, Optional ByVal expected_rate As Double = 0) As IFuture(Of Outcome)
        Return ref.QueueFunc(
            Function()
                If clients.ContainsKey(client_key) Then  Return failure("client key already exists")
                clients(client_key) = New ClientState(client_key, completed, If(expected_rate = 0, typical_rate, expected_rate))
                Return success("added")
            End Function
        )
    End Function

    '''<summary>Adds a link between two clients.</summary>
    Public Function set_link(ByVal client_key_1 As K, ByVal client_key_2 As K, ByVal linked As Boolean) As IFuture(Of Outcome)
        Return ref.QueueFunc(
            Function()
                If Not clients.ContainsKey(client_key_1) Then  Return failure("No such client key")
                If Not clients.ContainsKey(client_key_2) Then  Return failure("No such client key")
                Dim client1 = clients(client_key_1)
                Dim client2 = clients(client_key_2)
                If client1.links.Contains(client2) = linked Then  Return success("Already set.")
                If linked Then
                    client1.links.Add(client2)
                    client2.links.Add(client1)
                Else
                    client1.links.Remove(client2)
                    client2.links.Remove(client1)
                    If client1.busy AndAlso client1.other Is client2 Then
                        stop_transfer(client_key_1, False)
                    End If
                End If
                Return success("Set.")
            End Function
        )
    End Function

    '''<summary>Removes a client from the pool.</summary>
    Public Function remove(ByVal client_key As K) As IFuture(Of Outcome)
        Return ref.QueueFunc(
            Function()
                If Not clients.ContainsKey(client_key) Then  Return failure("No such client.")
                stop_transfer(client_key, False)
                Dim client = clients(client_key)
                clients.Remove(client_key)
                For Each linked_client In client.links
                    linked_client.links.Remove(client)
                Next linked_client
                Return success("removed")
            End Function
        )
    End Function

    Public ReadOnly Property rate_estimate_string(ByVal client_key As K) As String
        Get
            If Not clients.ContainsKey(client_key) Then Return "?"
            With clients(client_key)
                If Not .measured AndAlso Not .busy Then Return "?"

                Dim d = .get_cur_rate_estimate * 1000
                Dim f = 1
                For Each s In New String() {"B", "KiB", "MiB", "GiB", "TiB", "PiB"}
                    Dim f2 = f * 1024
                    If d < f2 Then
                        Return "{0:0.0} {1}/s".frmt(d / f, s)
                    End If
                    f = f2
                Next s

                Return "!"
            End With
        End Get
    End Property

    '''<summary>Updates the progress of a transfer to or from the given client.</summary>
    Public Function update_progress(ByVal client_key As K, ByVal progress As Double) As IFuture(Of Outcome)
        Return ref.QueueFunc(
            Function()
                If Not clients.ContainsKey(client_key) Then  Return failure("No such client key.")
                Dim client = clients(client_key)
                If Not client.busy Then  Return failure("Client isn't transfering.")
                client.update_progress(progress)
                client.other.update_progress(progress)
                Return success("Updated")
            End Function
        )
    End Function

    '''<summary>Stops any transfers to or from the given client.</summary>
    Public Sub stop_transfer(ByVal client_key As K, ByVal complete As Boolean)
        ref.QueueAction(
            Sub()
                If Not clients.ContainsKey(client_key) Then  Return

                Dim client = clients(client_key)
                If Not client.busy Then
                    client.set_not_transfering(complete)
                    Return
                End If

                Dim other = client.other
                client.set_not_transfering(complete)
                other.set_not_transfering(complete)
            End Sub
        )
    End Sub

    Public Sub update()
        ref.QueueAction(
            Sub()
                Dim transfers = New List(Of TransferEndPoint)
                Dim breaks = New List(Of TransferEndPoint)

                'Match downloaders to uploaders
                For Each downloader In (From client In clients.Values Where Not client.completed)
                    Dim available_uploaders = From e In downloader.links Where e.completed AndAlso Not e.busy
                    If available_uploaders.None Then  Continue For

                    Dim cur_uploader = downloader.other
                    Dim best_uploader = available_uploaders.Max(Function(e1, e2)
                                                                    Dim n = Math.Sign(e1.get_max_rate_estimate - e2.get_max_rate_estimate)
                                                                    If n <> 0 Then  Return n
                                                                    Return Math.Sign(e2.links.Count - e1.links.Count)
                                                                End Function)

                    If cur_uploader Is Nothing Then
                        'Start transfer
                        best_uploader.set_transfering(downloader.last_progress, downloader)
                        downloader.set_transfering(downloader.last_progress, best_uploader)
                        transfers.Add(New TransferEndPoint(best_uploader.client, downloader.client))
                    ElseIf Math.Min(downloader.transfering_time, cur_uploader.transfering_time) > ClientState.MIN_SWITCH_PERIOD Then
                        'Stop transfer if a significantly better uploader is available
                        If cur_uploader.is_probably_frozen OrElse best_uploader.get_max_rate_estimate > cur_uploader.get_max_rate_estimate * 2 + 1 Then
                            breaks.Add(New TransferEndPoint(cur_uploader.client, downloader.client))
                        End If
                    End If
                Next downloader

                'Stop transfers seen as improvable
                '[Done here instead of in the loop to avoid potentially giving two commands to clients at the same time]
                For Each e In breaks
                    set_link(e.src, e.dst, False)
                Next e

                'Report actions to the outside
                If transfers.Any Or breaks.Any Then
                    RaiseEvent actions(transfers, breaks)
                End If
            End Sub
        )
    End Sub

    Private Class ClientState
        Private max_rate_estimate As Double
        Private cur_rate_estimate As Double

        Private last_update_time As Integer
        Private last_update_progress As Double
        Private last_start_progress As Double
        Private last_start_time As Integer
        Private num_measures As Integer = 0

        Public ReadOnly client As K
        Public other As ClientState = Nothing
        Public links As New HashSet(Of ClientState)

        Public Const PROBABLY_FROZEN_PERIOD As Integer = 10000
        Public Const MIN_SWITCH_PERIOD As Integer = 3000
        Private Const CUR_ESTIMATE_CONVERGENCE_PER_SECOND As Double = 0.6

#Region "Properties"
        Public ReadOnly Property transfering_time() As Double
            Get
                If Not busy Then Return 0
                Return TickCountDelta(Environment.TickCount, last_start_time)
            End Get
        End Property
        Public ReadOnly Property get_max_rate_estimate() As Double
            Get
                If num_measures = 0 Then Return cur_rate_estimate
                Return max_rate_estimate * (1 + 1 / num_measures)
            End Get
        End Property
        Public ReadOnly Property get_cur_rate_estimate() As Double
            Get
                Return cur_rate_estimate
            End Get
        End Property
        Public ReadOnly Property measured() As Boolean
            Get
                Return num_measures <> 0
            End Get
        End Property

        Public ReadOnly Property last_progress() As Double
            Get
                Return last_update_progress
            End Get
        End Property
        Public ReadOnly Property is_probably_frozen() As Boolean
            Get
                Return TickCountDelta(Environment.TickCount, last_update_time) > PROBABLY_FROZEN_PERIOD
            End Get
        End Property

        Private prop_busy As Boolean = False
        Public Property busy() As Boolean
            Get
                Return prop_busy
            End Get
            Private Set(ByVal value As Boolean)
                prop_busy = value
            End Set
        End Property
        Private prop_completed As Boolean = False
        Public Property completed() As Boolean
            Get
                Return prop_completed
            End Get
            Private Set(ByVal value As Boolean)
                prop_completed = value
            End Set
        End Property
#End Region

#Region "New"
        Public Sub New(ByVal client As K, ByVal completed As Boolean, ByVal typical_rate_per_ms As Double)
            Me.client = client
            Me.completed = completed
            Me.max_rate_estimate = typical_rate_per_ms
            Me.cur_rate_estimate = typical_rate_per_ms
        End Sub
#End Region

#Region "State"
        Public Sub set_transfering(ByVal progress As Double, ByVal other As ClientState)
            If busy Then Return

            'Update state
            busy = True
            Me.other = other

            'Prep measure
            last_start_progress = progress
            last_start_time = Environment.TickCount()
            last_update_progress = last_start_progress
            last_update_time = last_start_time
        End Sub

        Public Sub update_progress(ByVal progress As Double)
            If Not busy Then Return

            'Measure cur rate
            Dim time = Environment.TickCount()
            Dim dp = progress - last_update_progress
            Dim dt = TickCountDelta(time, last_update_time)
            If dt > 0 AndAlso dp > 0 Then
                Dim r = dp / dt
                Dim c = CUR_ESTIMATE_CONVERGENCE_PER_SECOND ^ (dt / 1000)
                cur_rate_estimate *= c
                cur_rate_estimate += r * (1 - c)
            End If

            'Update state
            last_update_time = time
            last_update_progress = progress
        End Sub

        Public Sub set_not_transfering(ByVal now_finished As Boolean)
            If Not busy Then
                Me.completed = Me.completed Or now_finished
                Return
            End If

            'Measure max rate
            Dim dp = last_update_progress - last_start_progress
            Dim dt = transfering_time()
            If dt > 0 And dp > 0 Then
                Dim r = dp / dt
                Dim c = If(num_measures = 0 OrElse r > max_rate_estimate, 0, 0.99)
                max_rate_estimate *= c
                max_rate_estimate += r * (1 - c)
                num_measures += 1
            End If

            'Update state
            Me.busy = False
            Me.completed = Me.completed Or now_finished
            Me.other = Nothing
            Me.cur_rate_estimate = Me.max_rate_estimate
        End Sub
#End Region
    End Class
End Class
