Public Interface IHookable(Of T)
    Function f_hook(ByVal child As T) As IFuture
    Function f_caption() As IFuture(Of String)
End Interface

Public Class TabControlIHookableSet(Of T, C As {Control, New, IHookable(Of T)})
    Private ReadOnly tabs As New Dictionary(Of T, TabPage)
    Private ReadOnly controls As New Dictionary(Of T, C)
    Private ReadOnly elements As New List(Of T)
    Private ReadOnly tab_collection As TabControl.TabPageCollection
    Private ReadOnly uiref As ICallQueue

    Public Sub New(ByVal tab_control As TabControl)
        Me.tab_collection = tab_control.TabPages
        Me.uiref = New InvokedCallQueue(tab_control)
    End Sub

    Public Sub add(ByVal element As T)
        If tabs.ContainsKey(element) Then Throw New InvalidOperationException("Already have a control added for element.")

        'create
        Dim page = New TabPage()
        Dim control = New C()
        tab_collection.Add(page)
        page.Controls.Add(control)

        'add
        tabs(element) = page
        controls(element) = control
        elements.Add(element)

        'setup
        control.Width = page.Width
        control.Height = page.Height
        control.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Top
        control.f_hook(element)
        page.Text = "..."
        update_text(element)
    End Sub

    Private Sub update_text(ByVal element As T)
        Dim control = controls(element)
        Dim page = tabs(element)
        Call FutureSub.Call(
            control.f_caption,
            Sub(text)
                uiref.QueueAction(
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
    Public Function update(ByVal element As T) As Outcome
        If Not tabs.ContainsKey(element) Then Return failure("Don't have a control to update for element.")
        update_text(element)
        Return success("Element updated.")
    End Function

    Public Function contains(ByVal element As T) As Boolean
        Return tabs.ContainsKey(element)
    End Function

    Public Sub replace(ByVal old_element As T, ByVal new_element As T)
        If Not tabs.ContainsKey(old_element) Then Throw New InvalidOperationException("Don't have a control to replace for element.")
        Dim control = controls(old_element)
        control.f_hook(new_element)
    End Sub

    Public Sub remove(ByVal element As T)
        If Not tabs.ContainsKey(element) Then Throw New InvalidOperationException("Don't have a control to remove for element.")

        'retrieve
        Dim page = tabs(element)
        Dim control = controls(element)

        'cleanup
        control.f_hook(Nothing)
        page.Controls.Remove(control)
        tab_collection.Remove(page)
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

    Public Sub clear()
        For Each element In elements.ToList
            remove(element)
        Next element
    End Sub
End Class

Public Interface IBotWidget
    Event add_state_string(ByVal state As String, ByVal insert_at_top As Boolean)
    Event remove_state_string(ByVal state As String)
    Event clear_state_strings()

    Function name() As String
    Function type_name() As String
    Function logger() As Logger

    Sub [stop]()
    Sub hooked()
    Sub command(ByVal text As String)
End Interface
