'''<summary>Schedules transfers for sharing a copyable resource among multiple clients.</summary>
Public Class TransferScheduler(Of T)
#Region "Variables"
    Public Class TransferEndPoint
        Public ReadOnly src As T
        Public ReadOnly dst As T
        Public Sub New(ByVal src As T, ByVal dst As T)
            Me.src = src
            Me.dst = dst
        End Sub
    End Class
    Public Event actions(ByVal started As List(Of TransferEndPoint), ByVal stopped As List(Of TransferEndPoint))

    Private ReadOnly typical_rate As Double
    Private ReadOnly client_states As New Dictionary(Of T, ClientState)
    Private lock As New Object()
#End Region

    Public Sub New(ByVal typical_rate As Double)
        Me.typical_rate = typical_rate
    End Sub

    '''<summary>Adds a new client to the pool.</summary>
    Public Sub add(ByVal client As T, ByVal completed As Boolean, Optional ByVal expected_rate As Double = 0)
        SyncLock lock
            If client_states.ContainsKey(client) Then Throw New ArgumentException("Same widget added twice.")
            Dim state = New ClientState(completed, If(expected_rate = 0, typical_rate, expected_rate))
            client_states(client) = state
        End SyncLock
    End Sub

    '''<summary>Adds a link between two clients.</summary>
    Public Sub set_link(ByVal client1 As T, ByVal client2 As T, ByVal linked As Boolean)
        SyncLock lock
            If Not client_states.ContainsKey(client1) Then Return
            If Not client_states.ContainsKey(client2) Then Return
            Dim state1 = client_states(client1)
            Dim state2 = client_states(client2)
            If state1.links.Contains(client2) = linked Then Return
            If linked Then
                state1.links.Add(client2)
                state2.links.Add(client1)
            Else
                state1.links.Remove(client2)
                state2.links.Remove(client1)
                If state1.busy AndAlso client_states(state1.other) Is state2 Then
                    stop_transfer(client1, False)
                End If
            End If
        End SyncLock
    End Sub

    '''<summary>Removes a client from the pool.</summary>
    Public Sub remove(ByVal client As T)
        SyncLock lock
            If Not client_states.ContainsKey(client) Then Return
            stop_transfer(client, False)
            Dim state = client_states(client)
            client_states.Remove(client)
            For Each linked_client In state.links
                If client_states.ContainsKey(linked_client) Then
                    client_states(linked_client).links.Remove(client)
                End If
            Next linked_client
        End SyncLock
    End Sub

    Public ReadOnly Property rate_estimate_string(ByVal e As T) As String
        Get
            If Not client_states.ContainsKey(e) Then Return "?"
            With client_states(e)
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
    Public Sub update_progress(ByVal widget As T, ByVal progress As Double)
        SyncLock lock
            If Not client_states.ContainsKey(widget) Then Return
            Dim state = client_states(widget)
            If Not state.busy Then Return
            state.update_progress(progress)
            client_states(state.other).update_progress(progress)
        End SyncLock
    End Sub

    '''<summary>Stops any transfers to or from the given client.</summary>
    Public Sub stop_transfer(ByVal client As T, ByVal complete As Boolean)
        SyncLock lock
            If Not client_states.ContainsKey(client) Then Return

            Dim state = client_states(client)
            If Not state.busy Then
                state.set_not_transfering(complete)
                Return
            End If

            Dim other_state = client_states(state.other)
            state.set_not_transfering(complete)
            other_state.set_not_transfering(complete)
        End SyncLock
    End Sub

    Public Sub update()
        Dim transfers = New List(Of TransferEndPoint)
        Dim breaks = New List(Of TransferEndPoint)

        'Match downloaders to uploaders
        SyncLock lock
            For Each c In client_states.Keys
                Dim cs = client_states(c)
                If cs.links.Count = 0 Then Continue For
                If cs.completed Then Continue For
                If Not cs.busy Then
                    'Find best uploader and start transfer
                    Dim b As T = Nothing
                    Dim bs As ClientState = Nothing
                    For Each e In cs.links
                        Dim es = client_states(e)
                        If Not es.completed Then Continue For 'can't upload file you don't have
                        If es.busy Then Continue For 'already uploading to someone else
                        
                        Dim better = False
                        If bs Is Nothing Then
                            better = True 'anyone is better than nothing
                        ElseIf es.get_max_rate_estimate > bs.get_max_rate_estimate Then
                            better = True 'faster is better
                        ElseIf es.get_max_rate_estimate = bs.get_max_rate_estimate AndAlso es.links.Count < bs.links.Count Then
                            better = True 'less important to others is good
                        End If
                        If better Then
                            b = e
                            bs = es
                        End If
                    Next e

                    If bs IsNot Nothing Then
                        bs.set_transfering(cs.last_progress, c)
                        cs.set_transfering(cs.last_progress, b)
                        transfers.Add(New TransferEndPoint(b, c))
                    End If
                Else
                    'Stop transfer if a significantly better uploader is available
                    '[stopped clients will not be reassigned this update to avoid forcing start/stop order dependence on outside]
                    Dim p = cs.other
                    Dim ps = client_states(p)
                    Dim breakit = False
                    If Math.Min(cs.transfering_time, ps.transfering_time) <= ClientState.MIN_SWITCH_PERIOD Then Continue For
                    If ps.is_probably_frozen Then
                        breakit = True
                    Else
                        For Each e In cs.links
                            Dim es = client_states(e)
                            If Not es.completed Then Continue For 'can't upload file you don't have
                            If es.busy Then Continue For 'already uploading to someone else

                            If es.get_max_rate_estimate > ps.get_max_rate_estimate * 2 + 1 Then
                                breakit = True
                                Exit For
                            End If
                        Next e
                    End If
                    If breakit Then
                        breaks.Add(New TransferEndPoint(p, c))
                    End If
                End If
            Next c

            'Stop transfers seen as 'improvable'
            For Each e In breaks
                set_link(e.src, e.dst, False)
            Next e
        End SyncLock

        'Report actions to the outside
        If transfers.Count + breaks.Count > 0 Then
            RaiseEvent actions(transfers, breaks)
        End If
    End Sub

    Private Class ClientState
#Region "Variables"
        Private max_rate_estimate As Double
        Private cur_rate_estimate As Double

        Private last_update_time As Integer
        Private last_update_progress As Double
        Private last_start_progress As Double
        Private last_start_time As Integer
        Private num_measures As Integer = 0

        Public other As T = Nothing
        Public links As New List(Of T)

        Public Const PROBABLY_FROZEN_PERIOD As Integer = 10000
        Public Const MIN_SWITCH_PERIOD As Integer = 3000
        Private Const CUR_ESTIMATE_CONVERGENCE_PER_SECOND As Double = 0.6
#End Region

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
        Public Sub New(ByVal completed As Boolean, ByVal typical_rate_per_ms As Double)
            Me.completed = completed
            Me.max_rate_estimate = typical_rate_per_ms
            Me.cur_rate_estimate = typical_rate_per_ms
        End Sub
#End Region

#Region "State"
        Public Sub set_transfering(ByVal progress As Double, ByVal other As T)
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
