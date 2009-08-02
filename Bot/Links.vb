Imports HostBot.Warcraft3
Imports HostBot.Bnet

Namespace Links
    Public Interface IGameSource
        Event AddedGame(ByVal sender As IGameSource, ByVal game As W3GameHeader, ByVal server As IW3Server)
        Event RemovedGame(ByVal sender As IGameSource, ByVal game As W3GameHeader, ByVal reason As String)
        Event DisposedLink(ByVal sender As IGameSource, ByVal partner As IGameSink)
    End Interface
    Public Interface IGameSink
        Sub AddGame(ByVal game As W3GameHeader, ByVal server As IW3Server)
        Sub RemoveGame(ByVal game As W3GameHeader, ByVal reason As String)
        Sub SetAdvertisingOptions(ByVal [private] As Boolean)
    End Interface
    Public Interface IGameSourceSink
        Inherits IGameSource
        Inherits IGameSink
    End Interface

    Public Class AdvertisingLink
        Inherits NotifyingDisposable
        Private ReadOnly master As IGameSource
        Private ReadOnly servant As IGameSink

        Public Shared Function CreateMultiWayLink(ByVal members As IEnumerable(Of IGameSourceSink)) As INotifyingDisposable
            Contract.Requires(members IsNot Nothing)
            Dim members_ = members
            Return DisposeLink.CreateMultiWayLink(From m1 In members_, m2 In members_
                                                  Where m1 IsNot m2
                                                  Select AdvertisingLink.CreateOneWayLink(m1, m2))
        End Function
        Public Shared Function CreateOneWayLink(ByVal master As IGameSource,
                                                ByVal servant As IGameSink) As INotifyingDisposable
            Return New AdvertisingLink(master, servant)
        End Function
        Private Sub New(ByVal master As IGameSource,
                        ByVal servant As IGameSink)
            'contract bug wrt interface event implementation requires this:
            'Contract.Requires(master IsNot Nothing)
            'Contract.Requires(servant IsNot Nothing)
            Contract.Assume(master IsNot Nothing)
            Contract.Assume(servant IsNot Nothing)
            Me.master = master
            Me.servant = servant
            AddHandler master.AddedGame, AddressOf c_StartedAdvertising
            AddHandler master.RemovedGame, AddressOf c_StoppedAdvertising
            AddHandler master.DisposedLink, AddressOf c_DisposedLink
        End Sub

        Protected Overrides Sub Dispose(ByVal disposing As Boolean)
            RemoveHandler master.AddedGame, AddressOf c_StartedAdvertising
            RemoveHandler master.RemovedGame, AddressOf c_StoppedAdvertising
            RemoveHandler master.DisposedLink, AddressOf c_DisposedLink
        End Sub

        Private Sub c_DisposedLink(ByVal sender As IGameSource,
                                   ByVal partner As IGameSink)
            If partner Is servant Then
                Dispose()
            End If
        End Sub
        Private Sub c_StartedAdvertising(ByVal sender As IGameSource,
                                         ByVal game As W3GameHeader,
                                         ByVal server As IW3Server)
            servant.AddGame(game, server)
        End Sub
        Private Sub c_StoppedAdvertising(ByVal sender As IGameSource,
                                         ByVal game As W3GameHeader,
                                         ByVal reason As String)
            servant.RemoveGame(game, reason)
        End Sub
    End Class

    Public Class AdvertisingDisposeNotifier
        Inherits NotifyingDisposable
        Private ReadOnly member As IGameSourceSink

        Public Sub New(ByVal member As IGameSourceSink)
            'contract bug wrt interface event implementation requires this:
            'Contract.Requires(member IsNot Nothing)
            Contract.Assume(member IsNot Nothing)
            Me.member = member
            AddHandler Me.member.RemovedGame, AddressOf CatchStoppedAdvertising
        End Sub
        Private Sub CatchStoppedAdvertising(ByVal sender As Links.IGameSource, ByVal game As W3GameHeader, ByVal reason As String)
            Dispose()
        End Sub

        Protected Overrides Sub Dispose(ByVal disposing As Boolean)
            RemoveHandler Me.member.RemovedGame, AddressOf CatchStoppedAdvertising
        End Sub
    End Class

    Public Class DisposeLink
        Inherits NotifyingDisposable
        Private ReadOnly master As INotifyingDisposable
        Private ReadOnly servant As INotifyingDisposable

        Public Shared Function CreateMultiWayLink(ByVal members As IEnumerable(Of INotifyingDisposable)) As INotifyingDisposable
            Contract.Requires(members IsNot Nothing)
            Dim center As New NotifyingDisposable
            For Each member In members
                Contract.Assume(member IsNot Nothing)
                DisposeLink.CreateOneWayLink(member, center)
                DisposeLink.CreateOneWayLink(center, member)
            Next member
            Return center
        End Function
        Public Shared Function CreateOneWayLink(ByVal master As INotifyingDisposable,
                                                ByVal servant As INotifyingDisposable) As INotifyingDisposable
            'contract bug wrt interface event implementation requires this:
            'Contract.Requires(master IsNot Nothing)
            'Contract.Requires(servant IsNot Nothing)
            Contract.Assume(master IsNot Nothing)
            Contract.Assume(servant IsNot Nothing)
            Return New DisposeLink(master, servant)
        End Function

        Private Sub New(ByVal master As INotifyingDisposable,
                        ByVal servant As INotifyingDisposable)
            'contract bug wrt interface event implementation requires this:
            'Contract.Requires(master IsNot Nothing)
            'Contract.Requires(servant IsNot Nothing)
            Contract.Assume(master IsNot Nothing)
            Contract.Assume(servant IsNot Nothing)
            Me.master = master
            Me.servant = servant
            AddHandler master.Disposed, AddressOf CatchMasterDisposed
            AddHandler servant.Disposed, AddressOf CatchServantDisposed
            If servant.IsDisposed Then
                CatchServantDisposed()
            ElseIf master.IsDisposed Then
                CatchMasterDisposed()
            End If
        End Sub

        Private Sub CatchMasterDisposed()
            Me.Dispose()
            servant.Dispose()
        End Sub
        Private Sub CatchServantDisposed()
            Me.Dispose()
        End Sub

        Protected Overrides Sub Dispose(ByVal disposing As Boolean)
            RemoveHandler master.Disposed, AddressOf CatchMasterDisposed
            RemoveHandler servant.Disposed, AddressOf CatchServantDisposed
        End Sub
    End Class
End Namespace
