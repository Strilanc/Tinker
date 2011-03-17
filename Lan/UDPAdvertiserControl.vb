Namespace Lan
    'Verification disabled because of many warnings in generated code
    <ContractVerification(False)>
    Public Class UDPAdvertiserControl
        Private ReadOnly inQueue As CallQueue = MakeControlCallQueue(Me)
        Private ReadOnly _component As Lan.UDPAdvertiserComponent
        Private ReadOnly _udpAdvertiser As Lan.UDPAdvertiser
        Private ReadOnly _hooks As New List(Of Task(Of IDisposable))
        Private ReadOnly _syncedGames As New List(Of Lan.UDPAdvertiser.LanGame)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(inQueue IsNot Nothing)
            Contract.Invariant(_component IsNot Nothing)
            Contract.Invariant(_udpAdvertiser IsNot Nothing)
            Contract.Invariant(_hooks IsNot Nothing)
            Contract.Invariant(_syncedGames IsNot Nothing)
        End Sub

        Public Sub New(component As Lan.UDPAdvertiserComponent)
            Contract.Assert(component IsNot Nothing)
            InitializeComponent()

            Me._component = component
            Me._udpAdvertiser = component.Advertiser
            logClient.SetLogger(Me._udpAdvertiser.Logger, "Lan")

            _hooks.Add(Me._udpAdvertiser.ObserveGames(
                                    adder:=Sub(game) inQueue.QueueAction(Sub() OnAddedGame(game)),
                                    remover:=Sub(game) inQueue.QueueAction(Sub() OnRemovedGame(game))))
        End Sub

        Public Function QueueDispose() As Task
            Return inQueue.QueueAction(Sub() Me.Dispose())
        End Function
        Private Sub BnetClientControl_Disposed(sender As Object, e As System.EventArgs) Handles Me.Disposed
            _hooks.DisposeAllAsync()
        End Sub

        Private Sub OnAddedGame(game As Lan.UDPAdvertiser.LanGame)
            Me._syncedGames.Add(game)
            RefreshGamesLists()
        End Sub
        Private Sub OnRemovedGame(game As Lan.UDPAdvertiser.LanGame)
            Me._syncedGames.Remove(game)
            RefreshGamesLists()
        End Sub
        Private Sub RefreshGamesLists()
            lstState.Items.Clear()
            For Each game In Me._syncedGames
                lstState.Items.Add(game.GameDescription.Name)
                lstState.Items.Add(game.GameDescription.GameStats.AdvertisedPath)
                lstState.Items.Add("Game id: {0}".Frmt(game.GameDescription.GameId))
                lstState.Items.Add("----------")
            Next game
        End Sub

        Private Sub OnIssuedCommand(sender As CommandControl, argument As String) Handles comLanAdvertiser.IssuedCommand
            Contract.Requires(argument IsNot Nothing)
            Tinker.Components.UIInvokeCommand(_component, argument)
        End Sub
    End Class
End Namespace
