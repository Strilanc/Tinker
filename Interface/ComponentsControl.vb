''' <summary>
''' A control which shows an asynchronously updated set of bot component controls within tabs.
''' </summary>
Public Class ComponentsControl
    Private ReadOnly _botComponentTabs As ComponentTabSet
    Private ReadOnly _hooks As New List(Of IFuture(Of IDisposable))
    Private ReadOnly inQueue As New StartableCallQueue(New InvokedCallQueue(Me))

    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(_botComponentTabs IsNot Nothing)
        Contract.Invariant(_hooks IsNot Nothing)
        Contract.Invariant(inQueue IsNot Nothing)
    End Sub

    Public Sub New(ByVal bot As MainBot)
        Contract.Assert(bot IsNot Nothing)
        InitializeComponent()

        _botComponentTabs = New ComponentTabSet(tabsBot)
        _hooks.Add(bot.QueueCreateComponentsAsyncView(
                            adder:=Sub(sender, component) inQueue.QueueAction(Sub() OnBotAddedComponent(component)),
                            remover:=Sub(sender, component) inQueue.QueueAction(Sub() OnBotRemovedComponent(component))))
    End Sub

    Private Shadows Sub OnParentChanged() Handles Me.ParentChanged
        If Me.Parent IsNot Nothing Then inQueue.Start()
    End Sub

    Private Sub OnBotAddedComponent(ByVal component As Components.IBotComponent)
        If IsDisposed Then Return
        _botComponentTabs.Add(component)
    End Sub
    Private Sub OnBotRemovedComponent(ByVal component As Components.IBotComponent)
        If IsDisposed Then Return
        _botComponentTabs.Remove(component)
    End Sub

    Private Shadows Sub OnDisposed() Handles Me.Disposed
        For Each hook In _hooks
            hook.CallOnValueSuccess(Sub(value) value.Dispose()).SetHandled()
        Next hook
    End Sub
End Class
