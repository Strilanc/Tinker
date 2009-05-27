Imports HostBot.Warcraft3
Imports HostBot.Bnet

Namespace Links
#Region "AdvertisingLink"
    Public Interface IAdvertisingLinkMember
        Event started_advertising(ByVal sender As IAdvertisingLinkMember,
                                  ByVal server As IW3Server,
                                  ByVal name As String,
                                  ByVal map As W3Map,
                                  ByVal options As IList(Of String))
        Event stopped_advertising(ByVal sender As IAdvertisingLinkMember,
                                  ByVal reason As String)

        Sub start_advertising(ByVal server As IW3Server,
                              ByVal name As String,
                              ByVal map As W3Map,
                              ByVal options As IList(Of String))
        Sub stop_advertising(ByVal reason As String)
        Sub set_advertising_options(ByVal [private] As Boolean)

        Event break(ByVal sender As IAdvertisingLinkMember,
                    ByVal partner As IAdvertisingLinkMember)
    End Interface

    Public Class AdvertisingLink
        Implements IDependencyLinkServant
        Private ReadOnly members(0 To 1) As IAdvertisingLinkMember
        Private ReadOnly advertising As New Dictionary(Of IAdvertisingLinkMember, Boolean)
        Private ReadOnly lock As New Object()
        Public Event closed() Implements IDependencyLinkServant.Closed

        Public Sub New(ByVal member1 As IAdvertisingLinkMember, ByVal member2 As IAdvertisingLinkMember)
            If member1 Is Nothing Then Throw New ArgumentNullException("member1")
            If member2 Is Nothing Then Throw New ArgumentNullException("member2")
            If member1 Is member2 Then Throw New ArgumentException("member1 is member2")
            Me.members(0) = member1
            Me.members(1) = member2
            For i = 0 To 1
                AddHandler members(i).started_advertising, AddressOf member_started_advertising
                AddHandler members(i).stopped_advertising, AddressOf member_stopped_advertising
                AddHandler members(i).break, AddressOf member_break
                advertising(members(i)) = False
            Next i
        End Sub

        Public Sub break() Implements IDependencyLinkServant.close
            SyncLock lock
                If members(0) Is Nothing Then Return
                For i = 0 To 1
                    RemoveHandler members(i).started_advertising, AddressOf member_started_advertising
                    RemoveHandler members(i).stopped_advertising, AddressOf member_stopped_advertising
                    RemoveHandler members(i).break, AddressOf member_break
                    advertising.Clear()
                    members(i) = Nothing
                Next i
            End SyncLock
            RaiseEvent closed()
        End Sub

        Private Function opposite(ByVal m As IAdvertisingLinkMember,
                                  ByVal desired_advertising_state As Boolean) As IAdvertisingLinkMember
            SyncLock lock
                Dim i = -1
                If m Is members(0) Then i = 1
                If m Is members(1) Then i = 0
                If i = -1 Then Return Nothing
                If members(i) Is Nothing Then Return Nothing
                If advertising(members(i)) <> desired_advertising_state Then Return Nothing
                Return members(i)
            End SyncLock
        End Function
        Private Sub member_break(ByVal sender As IAdvertisingLinkMember,
                                 ByVal partner As IAdvertisingLinkMember)
            SyncLock lock
                If partner IsNot Nothing AndAlso Not members.Contains(partner) Then Return
                break()
            End SyncLock
        End Sub
        Private Sub member_started_advertising(ByVal sender As IAdvertisingLinkMember,
                                               ByVal server As IW3Server,
                                               ByVal name As String,
                                               ByVal map As Warcraft3.W3Map,
                                               ByVal options As IList(Of String))
            SyncLock lock
                advertising(sender) = True
            End SyncLock
            Dim receiver = opposite(sender, False)
            If receiver Is Nothing Then Return
            receiver.start_advertising(server, name, map, options)
        End Sub
        Private Sub member_stopped_advertising(ByVal sender As IAdvertisingLinkMember,
                                               ByVal reason As String)
            SyncLock lock
                advertising(sender) = False
            End SyncLock
            Dim receiver = opposite(sender, True)
            If receiver Is Nothing Then Return
            receiver.stop_advertising(reason)
        End Sub
    End Class
#End Region

#Region "Dependency Link"
    Public Interface IDependencyLinkMaster
        Event Closed()
    End Interface
    Public Interface IDependencyLinkServant
        Inherits IDependencyLinkMaster
        Sub close()
    End Interface

    Public Class DependencyLink
        Private ReadOnly master As IDependencyLinkMaster
        Private ReadOnly servant As IDependencyLinkServant
        Private broken As Boolean = False
        Private ReadOnly ref As New ThreadedCallQueue(Me.GetType.Name)
        Private Sub New(ByVal master As IDependencyLinkMaster, ByVal servant As IDependencyLinkServant)
            If Not (master IsNot Nothing) Then Throw New ArgumentException()
            If Not (servant IsNot Nothing) Then Throw New ArgumentException()
            Me.master = master
            Me.servant = servant
            AddHandler master.Closed, AddressOf master_closed_R
            AddHandler servant.Closed, AddressOf servant_closed_R
        End Sub
        Public Shared Function link(ByVal master As IDependencyLinkMaster, ByVal servant As IDependencyLinkServant) As DependencyLink
            Return New DependencyLink(master, servant)
        End Function

        Private Sub master_closed_R()
            break_R(True)
        End Sub
        Private Sub servant_closed_R()
            break_R(False)
        End Sub

        Public Function break_R(ByVal kill_servant As Boolean) As IFuture(Of Boolean)
            Return ref.enqueueFunc(Function() break_L(kill_servant))
        End Function
        Private Function break_L(ByVal kill_servant As Boolean) As Boolean
            If broken Then Return False
            broken = True
            RemoveHandler master.Closed, AddressOf master_closed_R
            RemoveHandler servant.Closed, AddressOf servant_closed_R
            If kill_servant Then servant.close()
            Return True
        End Function
    End Class
#End Region
End Namespace
