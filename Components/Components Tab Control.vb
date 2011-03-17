Namespace Components

    ''' <summary>
    ''' A control which shows a set of components as controls within tabs.
    ''' </summary>
    <ContractVerification(False)>
    Public Class TabControl 'Verification disabled because of many warnings in generated code
        Private ReadOnly _botComponentTabs As Components.TabManager
        Private ReadOnly _hooks As New List(Of Task(Of IDisposable))
        Private ReadOnly inQueue As CallQueue = MakeControlCallQueue(Me)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_botComponentTabs IsNot Nothing)
            Contract.Invariant(_hooks IsNot Nothing)
            Contract.Invariant(inQueue IsNot Nothing)
        End Sub

        Public Sub New(bot As Bot.MainBot)
            Contract.Assert(bot IsNot Nothing)
            InitializeComponent()

            _botComponentTabs = New Components.TabManager(tabsComponents)
            _hooks.Add(bot.Components.ObserveComponents(
                                adder:=Sub(component) inQueue.QueueAction(Sub() OnBotAddedComponent(component)),
                                remover:=Sub(component) inQueue.QueueAction(Sub() OnBotRemovedComponent(component))))
        End Sub

        Private Sub OnBotAddedComponent(component As Components.IBotComponent)
            Contract.Requires(component IsNot Nothing)
            If IsDisposed Then Return
            _botComponentTabs.Add(component)
        End Sub
        Private Sub OnBotRemovedComponent(component As Components.IBotComponent)
            Contract.Requires(component IsNot Nothing)
            If IsDisposed Then Return
            _botComponentTabs.Remove(component)
        End Sub

        Private Shadows Sub OnDisposed() Handles Me.Disposed
            _hooks.DisposeAllAsync()
        End Sub
    End Class
End Namespace
