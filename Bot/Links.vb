Imports HostBot.Warcraft3
Imports HostBot.Bnet

Namespace Links
    Public Interface IGameSource
        Event AddedGame(ByVal sender As IGameSource, ByVal game As W3GameHeader, ByVal server As W3Server)
        Event RemovedGame(ByVal sender As IGameSource, ByVal game As W3GameHeader, ByVal reason As String)
        Event DisposedLink(ByVal sender As IGameSource, ByVal partner As IGameSink)
    End Interface
    Public Interface IGameSink
        Sub AddGame(ByVal game As W3GameHeader, ByVal server As W3Server)
        Sub RemoveGame(ByVal game As W3GameHeader, ByVal reason As String)
        Sub SetAdvertisingOptions(ByVal [private] As Boolean)
    End Interface
    Public Interface IGameSourceSink
        Inherits IGameSource
        Inherits IGameSink
    End Interface

    Public Class AdvertisingLink
        Inherits FutureDisposable
        Private ReadOnly master As IGameSource
        Private ReadOnly servant As IGameSink

        Public Shared Function CreateMultiWayLink(ByVal members As IEnumerable(Of IGameSourceSink)) As IFutureDisposable
            Contract.Requires(members IsNot Nothing)
            Dim members_ = members
            Return DisposeLink.CreateMultiWayLink(From m1 In members_, m2 In members_
                                                  Where m1 IsNot m2
                                                  Select AdvertisingLink.CreateOneWayLink(m1, m2))
        End Function
        Public Shared Function CreateOneWayLink(ByVal master As IGameSource,
                                                ByVal servant As IGameSink) As IFutureDisposable
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

        Protected Overrides Sub PerformDispose(ByVal finalizing As Boolean)
            If Not finalizing Then
                RemoveHandler master.AddedGame, AddressOf c_StartedAdvertising
                RemoveHandler master.RemovedGame, AddressOf c_StoppedAdvertising
                RemoveHandler master.DisposedLink, AddressOf c_DisposedLink
            End If
        End Sub

        Private Sub c_DisposedLink(ByVal sender As IGameSource,
                                   ByVal partner As IGameSink)
            If partner Is servant Then
                Dispose()
            End If
        End Sub
        Private Sub c_StartedAdvertising(ByVal sender As IGameSource,
                                         ByVal game As W3GameHeader,
                                         ByVal server As W3Server)
            servant.AddGame(game, server)
        End Sub
        Private Sub c_StoppedAdvertising(ByVal sender As IGameSource,
                                         ByVal game As W3GameHeader,
                                         ByVal reason As String)
            servant.RemoveGame(game, reason)
        End Sub
    End Class

    Public Class AdvertisingDisposeNotifier
        Inherits FutureDisposable
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

        Protected Overrides Sub PerformDispose(ByVal finalizing As Boolean)
            If Not finalizing Then
                RemoveHandler Me.member.RemovedGame, AddressOf CatchStoppedAdvertising
            End If
        End Sub
    End Class

    Public Class DisposeLink
        Inherits FutureDisposable
        Private ReadOnly master As IFutureDisposable
        Private ReadOnly servant As IFutureDisposable

        Public Shared Function CreateMultiWayLink(ByVal members As IEnumerable(Of IFutureDisposable)) As IFutureDisposable
            Contract.Requires(members IsNot Nothing)
            Dim center As New FutureDisposable
            For Each member In members
                Contract.Assume(member IsNot Nothing)
                DisposeLink.CreateOneWayLink(member, center)
                DisposeLink.CreateOneWayLink(center, member)
            Next member
            Return center
        End Function
        Public Shared Function CreateOneWayLink(ByVal master As IFutureDisposable,
                                                ByVal servant As IFutureDisposable) As IFutureDisposable
            'contract bug wrt interface event implementation requires this:
            'Contract.Requires(master IsNot Nothing)
            'Contract.Requires(servant IsNot Nothing)
            Contract.Assume(master IsNot Nothing)
            Contract.Assume(servant IsNot Nothing)
            Return New DisposeLink(master, servant)
        End Function

        Private Sub New(ByVal master As IFutureDisposable,
                        ByVal servant As IFutureDisposable)
            'contract bug wrt interface event implementation requires this:
            'Contract.Requires(master IsNot Nothing)
            'Contract.Requires(servant IsNot Nothing)
            Contract.Assume(master IsNot Nothing)
            Contract.Assume(servant IsNot Nothing)
            Me.master = master
            Me.servant = servant
            master.FutureDisposed.CallOnSuccess(AddressOf servant.Dispose)
            servant.FutureDisposed.CallOnSuccess(AddressOf Me.Dispose)
        End Sub
    End Class
End Namespace
