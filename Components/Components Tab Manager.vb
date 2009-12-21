Namespace Components
    ''' <summary>
    ''' Handles adding and removing tabs, containing component controls, to/from a System.Windows.Forms.TabControl.
    ''' </summary>
    Public NotInheritable Class TabManager
        Private ReadOnly _tabControl As Windows.Forms.TabControl
        Private ReadOnly _items As New Dictionary(Of Components.IBotComponent, TabPage)

        Public Sub New(ByVal tabControl As Windows.Forms.TabControl)
            Me._tabControl = tabControl
        End Sub

        Public Sub Add(ByVal component As Components.IBotComponent)
            Contract.Requires(component IsNot Nothing)
            Contract.Ensures(Me.Contains(component))
            If Not component.HasControl Then Return
            If _items.ContainsKey(component) Then Throw New InvalidOperationException("Already have a tab for the given component.")

            'create
            Dim page = New TabPage()
            _tabControl.TabPages.Add(page)
            page.Controls.Add(component.Control)
            _items.Add(component, page)

            'setup
            component.Control.Width = page.Width
            component.Control.Height = page.Height
            component.Control.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Top
            page.Text = "{0}:{1}".Frmt(component.Type, component.Name)
        End Sub

        <Pure()>
        Public Function Contains(ByVal component As Components.IBotComponent) As Boolean
            Contract.Requires(component IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Boolean)() = _items.ContainsKey(component))
            Return _items.ContainsKey(component)
        End Function

        Public Sub Remove(ByVal component As Components.IBotComponent)
            Contract.Requires(component IsNot Nothing)
            Contract.Ensures(Not Me.Contains(component))
            If Not component.HasControl Then Return
            If _items.ContainsKey(component) Then Throw New InvalidOperationException("Don't have a tab for the given component.")

            Dim tabPage = _items(component)
            tabPage.Controls.Remove(component.Control)
            _tabControl.TabPages.Remove(tabPage)
            tabPage.Dispose()
            _items.Remove(component)
        End Sub

        Public Sub Clear()
            For Each component In _items.Keys
                Contract.Assume(component IsNot Nothing)
                Remove(component)
            Next component
        End Sub
    End Class
End Namespace
