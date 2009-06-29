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
            Return DisposeLink.CreateMultiWayLink(From m1 In members, m2 In members
                                                  Where m1 IsNot m2
                                                  Select AdvertisingLink.CreateOneWayLink(m1, m2))
        End Function
        Public Shared Function CreateOneWayLink(ByVal master As IGameSource,
                                                ByVal servant As IGameSink) As INotifyingDisposable
            Return New AdvertisingLink(master, servant)
        End Function
        Private Sub New(ByVal master As IGameSource,
                        ByVal servant As IGameSink)
            Contract.Requires(master IsNot Nothing)
            Contract.Requires(servant IsNot Nothing)
            Me.master = master
            Me.servant = servant
            AddHandler master.AddedGame, AddressOf c_StartedAdvertising
            AddHandler master.RemovedGame, AddressOf c_StoppedAdvertising
            AddHandler master.DisposedLink, AddressOf c_DisposedLink
        End Sub

        Protected Overrides Sub PerformDispose()
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
            Contract.Requires(member IsNot Nothing)
            Me.member = member
            AddHandler Me.member.RemovedGame, AddressOf c_StoppedAdvertising
        End Sub
        Protected Overrides Sub Finalize()
            Dispose()
        End Sub
        Private Sub c_StoppedAdvertising(ByVal sender As Links.IGameSource, ByVal game As W3GameHeader, ByVal reason As String)
            Dispose()
        End Sub

        Protected Overrides Sub PerformDispose()
            RemoveHandler Me.member.RemovedGame, AddressOf c_StoppedAdvertising
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
            Contract.Requires(master IsNot Nothing)
            Contract.Requires(servant IsNot Nothing)
            Return New DisposeLink(master, servant)
        End Function

        Private Sub New(ByVal master As INotifyingDisposable,
                        ByVal servant As INotifyingDisposable)
            Contract.Requires(master IsNot Nothing)
            Contract.Requires(servant IsNot Nothing)
            Me.master = master
            Me.servant = servant
            AddHandler master.Disposed, AddressOf c_MasterDisposed
            AddHandler servant.Disposed, AddressOf c_ServantDisposed
            If servant.IsDisposed Then
                c_ServantDisposed()
            ElseIf master.IsDisposed Then
                c_MasterDisposed()
            End If
        End Sub

        Private Sub c_MasterDisposed()
            Me.Dispose()
            servant.Dispose()
        End Sub
        Private Sub c_ServantDisposed()
            Me.Dispose()
        End Sub

        Protected Overrides Sub PerformDispose()
            RemoveHandler master.Disposed, AddressOf c_MasterDisposed
            RemoveHandler servant.Disposed, AddressOf c_ServantDisposed
        End Sub
    End Class
End Namespace
