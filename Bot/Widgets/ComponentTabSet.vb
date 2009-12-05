Public NotInheritable Class ComponentTabSet
    Private ReadOnly parent As TabControl
    Private ReadOnly entries As New Dictionary(Of Components.IBotComponent, TabPage)

    Public Sub New(ByVal tabControl As TabControl)
        Me.parent = tabControl
    End Sub

    Public Sub Add(ByVal component As Components.IBotComponent)
        Contract.Requires(component IsNot Nothing)
        If entries.ContainsKey(component) Then Throw New InvalidOperationException("Already have a control added for element.")

        'create
        Dim page = New TabPage()
        parent.TabPages.Add(page)
        page.Controls.Add(component.Control)
        entries(component) = page

        'setup
        component.Control.Width = page.Width
        component.Control.Height = page.Height
        component.Control.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Top
        page.Text = "{0}:{1}".Frmt(component.Type, component.Name)
    End Sub

    Public Function Contains(ByVal component As Components.IBotComponent) As Boolean
        Return entries.ContainsKey(component)
    End Function

    Public Sub Remove(ByVal component As Components.IBotComponent)
        Contract.Requires(component IsNot Nothing)
        If Not entries.ContainsKey(component) Then Throw New InvalidOperationException("Don't have a control to remove for element.")

        Dim tabPage = entries(component)
        tabPage.Controls.Remove(component.Control)
        parent.TabPages.Remove(tabPage)
        tabPage.Dispose()
        entries.Remove(component)
    End Sub

    Public Sub Clear()
        For Each component In entries.Keys
            Remove(component)
        Next component
    End Sub
End Class
