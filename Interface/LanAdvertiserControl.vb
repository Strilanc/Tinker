<ContractVerification(False)>
Public Class LanAdvertiserControl
    Private ReadOnly inQueue As New StartableCallQueue(New InvokedCallQueue(Me))
    Private ReadOnly _manager As Components.LanAdvertiserManager
    Private ReadOnly _lanAdvertiser As WC3.LanAdvertiser
    Private ReadOnly _hooks As New List(Of IFuture(Of IDisposable))
    Private ReadOnly _syncedGames As New List(Of WC3.LanAdvertiser.LanGame)

    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(inQueue IsNot Nothing)
        Contract.Invariant(_manager IsNot Nothing)
        Contract.Invariant(_lanAdvertiser IsNot Nothing)
        Contract.Invariant(_hooks IsNot Nothing)
        Contract.Invariant(_syncedGames IsNot Nothing)
    End Sub

    Private Shadows Sub OnParentChanged() Handles Me.ParentChanged
        If Me.Parent IsNot Nothing Then inQueue.Start()
    End Sub

    Public Sub New(ByVal manager As Components.LanAdvertiserManager)
        Contract.Assert(manager IsNot Nothing)
        InitializeComponent()

        Me._manager = manager
        Me._lanAdvertiser = manager.LanAdvertiser
        logClient.SetLogger(Me._lanAdvertiser.Logger, "Lan")

        _hooks.Add(Me._lanAdvertiser.QueueWeaveGames(adder:=Sub(sender, game) inQueue.QueueAction(Sub() OnAddedGame(game)),
                                                     remover:=Sub(sender, game) inQueue.QueueAction(Sub() OnRemovedGame(game))))
    End Sub

    Public Function QueueDispose() As IFuture
        Return inQueue.QueueAction(Sub() Me.Dispose())
    End Function
    Private Sub BnetClientControl_Disposed(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Disposed
        For Each hook In _hooks
            Contract.Assume(hook IsNot Nothing)
            hook.CallOnValueSuccess(Sub(value) value.Dispose()).SetHandled()
        Next hook
    End Sub

    Private Sub OnAddedGame(ByVal game As WC3.LanAdvertiser.LanGame)
        Me._syncedGames.Add(game)
        RefreshGamesLists()
    End Sub
    Private Sub OnRemovedGame(ByVal game As WC3.LanAdvertiser.LanGame)
        Me._syncedGames.Remove(game)
        RefreshGamesLists()
    End Sub
    Private Sub RefreshGamesLists()
        lstState.Items.Clear()
        For Each game In Me._syncedGames
            lstState.Items.Add(game.GameDescription.Name)
            lstState.Items.Add(game.GameDescription.GameStats.relativePath)
            lstState.Items.Add("Game id: {0}".Frmt(game.GameDescription.GameId))
            lstState.Items.Add("----------")
        Next game
    End Sub

    Private Sub OnIssuedCommand(ByVal sender As CommandControl, ByVal argument As String) Handles comLanAdvertiser.IssuedCommand
        Contract.Requires(argument IsNot Nothing)
        Tinker.Components.UIInvokeCommand(_manager, argument)
    End Sub
End Class
