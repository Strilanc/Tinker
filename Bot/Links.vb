
Namespace Links
    Public Interface IGameSource
        Event AddedGame(ByVal sender As IGameSource, ByVal game As WC3.LocalGameDescription, ByVal server As WC3.GameServer)
        Event RemovedGame(ByVal sender As IGameSource, ByVal game As WC3.LocalGameDescription, ByVal reason As String)
        Event DisposedLink(ByVal sender As IGameSource, ByVal partner As IGameSink)
    End Interface
    Public Interface IGameSink
        Sub AddGame(ByVal game As WC3.LocalGameDescription, ByVal server As WC3.GameServer)
        Sub RemoveGame(ByVal game As WC3.LocalGameDescription, ByVal reason As String)
        Sub SetAdvertisingOptions(ByVal isPrivate As Boolean)
    End Interface
    Public Interface IGameSourceSink
        Inherits IGameSource
        Inherits IGameSink
    End Interface

    Public NotInheritable Class AdvertisingLink
        Inherits FutureDisposable
        Private ReadOnly master As IGameSource
        Private ReadOnly servant As IGameSink

        Public Shared Function CreateMultiWayLink(ByVal members As IEnumerable(Of IGameSourceSink)) As IFutureDisposable
            Contract.Requires(members IsNot Nothing)
            Return DisposeLink.CreateMultiWayLink(From m1 In members, m2 In members
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

        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As ifuture
            If Not finalizing Then
                RemoveHandler master.AddedGame, AddressOf c_StartedAdvertising
                RemoveHandler master.RemovedGame, AddressOf c_StoppedAdvertising
                RemoveHandler master.DisposedLink, AddressOf c_DisposedLink
            End If
            Return Nothing
        End Function

        Private Sub c_DisposedLink(ByVal sender As IGameSource,
                                   ByVal partner As IGameSink)
            If partner Is servant Then
                Dispose()
            End If
        End Sub
        Private Sub c_StartedAdvertising(ByVal sender As IGameSource,
                                         ByVal game As WC3.LocalGameDescription,
                                         ByVal server As WC3.GameServer)
            servant.AddGame(game, server)
        End Sub
        Private Sub c_StoppedAdvertising(ByVal sender As IGameSource,
                                         ByVal game As WC3.LocalGameDescription,
                                         ByVal reason As String)
            servant.RemoveGame(game, reason)
        End Sub
    End Class

    Public NotInheritable Class AdvertisingDisposeNotifier
        Inherits FutureDisposable
        Private ReadOnly member As IGameSourceSink

        Public Sub New(ByVal member As IGameSourceSink)
            'contract bug wrt interface event implementation requires this:
            'Contract.Requires(member IsNot Nothing)
            Contract.Assume(member IsNot Nothing)
            Me.member = member
            AddHandler Me.member.RemovedGame, AddressOf CatchStoppedAdvertising
        End Sub
        Private Sub CatchStoppedAdvertising(ByVal sender As Links.IGameSource, ByVal game As WC3.GameDescription, ByVal reason As String)
            Dispose()
        End Sub

        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As ifuture
            If finalizing Then Return Nothing
            RemoveHandler Me.member.RemovedGame, AddressOf CatchStoppedAdvertising
            Return Nothing
        End Function
    End Class

    Public NotInheritable Class DisposeLink
        Inherits FutureDisposable

        Private Sub New()
        End Sub

        Public Shared Function CreateMultiWayLink(ByVal members As IEnumerable(Of IFutureDisposable)) As IFutureDisposable
            Contract.Requires(members IsNot Nothing)
            Dim result = New FutureDisposable
            For Each member In members
                Contract.Assume(member IsNot Nothing)
                DisposeLink.CreateOneWayLink(member, result)
                DisposeLink.CreateOneWayLink(result, member)
            Next member
            Return result
        End Function

        Public Shared Function CreateOneWayLink(ByVal master As IFutureDisposable,
                                                ByVal servant As IFutureDisposable) As IFutureDisposable
            Contract.Requires(master IsNot Nothing)
            Contract.Requires(servant IsNot Nothing)
            Dim result = New FutureDisposable
            master.FutureDisposed.CallOnSuccess(AddressOf servant.Dispose)
            servant.FutureDisposed.CallOnSuccess(AddressOf result.Dispose)
            Return result
        End Function
    End Class
End Namespace
