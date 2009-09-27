Public Interface IHookable(Of T)
    Function QueueHook(ByVal child As T) As IFuture
    Function QueueGetCaption() As IFuture(Of String)
    Function QueueDispose() As IFuture
End Interface

Public Class TabControlIHookableSet(Of TElement, TControl As {Control, New, IHookable(Of TElement)})
    Private ReadOnly tabs As New Dictionary(Of TElement, TabPage)
    Private ReadOnly controls As New Dictionary(Of TElement, TControl)
    Private ReadOnly elements As New List(Of TElement)
    Private ReadOnly tabCollection As TabControl.TabPageCollection
    Private ReadOnly ref As ICallQueue

    Public Sub New(ByVal tabControl As TabControl)
        Me.tabCollection = tabControl.TabPages
        Me.ref = New InvokedCallQueue(tabControl)
    End Sub

    Public Sub Add(ByVal element As TElement)
        If tabs.ContainsKey(element) Then Throw New InvalidOperationException("Already have a control added for element.")

        'create
        Dim page = New TabPage()
        Dim control = New TControl()
        tabCollection.Add(page)
        page.Controls.Add(control)

        'add
        tabs(element) = page
        controls(element) = control
        elements.Add(element)

        'setup
        control.Width = page.Width
        control.Height = page.Height
        control.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Top
        control.QueueHook(element)
        page.Text = "..."
        UpdateText(element)
    End Sub

    Private Sub UpdateText(ByVal element As TElement)
        Dim control = controls(element)
        Dim page = tabs(element)
        control.QueueGetCaption.CallOnValueSuccess(
            Sub(text)
                ref.QueueAction(
                    Sub()
                        Try
                            page.Text = text
                        Catch e As Exception
                        End Try
                    End Sub
                )
            End Sub
        )
    End Sub
    Public Sub Update(ByVal element As TElement)
        If Not tabs.ContainsKey(element) Then Throw New InvalidOperationException("Don't have a control to update for element.")
        UpdateText(element)
    End Sub

    Public Function Contains(ByVal element As TElement) As Boolean
        Return tabs.ContainsKey(element)
    End Function

    Public Sub Replace(ByVal oldElement As TElement, ByVal newElement As TElement)
        If Not tabs.ContainsKey(oldElement) Then Throw New InvalidOperationException("Don't have a control to replace for element.")
        Dim control = controls(oldElement)
        control.QueueHook(newElement)
    End Sub

    Public Sub Remove(ByVal element As TElement)
        If Not tabs.ContainsKey(element) Then Throw New InvalidOperationException("Don't have a control to remove for element.")

        'retrieve
        Dim page = tabs(element)
        Dim control = controls(element)

        'cleanup
        control.QueueHook(Nothing)
        page.Controls.Remove(control)
        tabCollection.Remove(page)
        Try
            control.QueueDispose()
            page.Dispose()
        Catch e As InvalidOperationException
        End Try

        'remove
        tabs.Remove(element)
        controls.Remove(element)
        elements.Remove(element)
    End Sub

    Public Sub Clear()
        For Each element In elements.ToList
            Remove(element)
        Next element
    End Sub
End Class

Public Interface IBotWidget
    Event AddStateString(ByVal state As String, ByVal shouldInsertAtTop As Boolean)
    Event RemoveStateString(ByVal state As String)
    Event ClearStateStrings()

    ReadOnly Property Name() As String
    ReadOnly Property TypeName() As String
    ReadOnly Property Logger() As Logger

    Sub [Stop]()
    Sub Hooked()
    Sub ProcessCommand(ByVal text As String)
End Interface
