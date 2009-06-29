Public Interface IHookable(Of T)
    Function f_Hook(ByVal child As T) As IFuture
    Function f_caption() As IFuture(Of String)
End Interface

Public Class TabControlIHookableSet(Of T, C As {Control, New, IHookable(Of T)})
    Private ReadOnly tabs As New Dictionary(Of T, TabPage)
    Private ReadOnly controls As New Dictionary(Of T, C)
    Private ReadOnly elements As New List(Of T)
    Private ReadOnly tabCollection As TabControl.TabPageCollection
    Private ReadOnly ref As ICallQueue

    Public Sub New(ByVal tabControl As TabControl)
        Me.tabCollection = tabControl.TabPages
        Me.ref = New InvokedCallQueue(tabControl)
    End Sub

    Public Sub Add(ByVal element As T)
        If tabs.ContainsKey(element) Then Throw New InvalidOperationException("Already have a control added for element.")

        'create
        Dim page = New TabPage()
        Dim control = New C()
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
        control.f_Hook(element)
        page.Text = "..."
        UpdateText(element)
    End Sub

    Private Sub UpdateText(ByVal element As T)
        Dim control = controls(element)
        Dim page = tabs(element)
        control.f_caption.CallWhenValueReady(
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
    Public Function Update(ByVal element As T) As Outcome
        If Not tabs.ContainsKey(element) Then Return failure("Don't have a control to update for element.")
        UpdateText(element)
        Return success("Element updated.")
    End Function

    Public Function Contains(ByVal element As T) As Boolean
        Return tabs.ContainsKey(element)
    End Function

    Public Sub Replace(ByVal old_element As T, ByVal new_element As T)
        If Not tabs.ContainsKey(old_element) Then Throw New InvalidOperationException("Don't have a control to replace for element.")
        Dim control = controls(old_element)
        control.f_Hook(new_element)
    End Sub

    Public Sub Remove(ByVal element As T)
        If Not tabs.ContainsKey(element) Then Throw New InvalidOperationException("Don't have a control to remove for element.")

        'retrieve
        Dim page = tabs(element)
        Dim control = controls(element)

        'cleanup
        control.f_Hook(Nothing)
        page.Controls.Remove(control)
        tabCollection.Remove(page)
        Try
            control.Dispose()
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
    Event AddStateString(ByVal state As String, ByVal insert_at_top As Boolean)
    Event RemoveStateString(ByVal state As String)
    Event ClearStateStrings()

    ReadOnly Property Name() As String
    ReadOnly Property TypeName() As String
    ReadOnly Property Logger() As Logger

    Sub [Stop]()
    Sub Hooked()
    Sub ProcessCommand(ByVal text As String)
End Interface
