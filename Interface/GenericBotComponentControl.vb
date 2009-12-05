Public Class GenericBotComponentControl
    Private ReadOnly _component As Components.IBotComponent

    Public Sub New(ByVal component As Components.IBotComponent)
        Contract.Requires(component IsNot Nothing)
        Me.InitializeComponent()
        Me._component = component
        logControl.SetLogger(logger:=component.Logger,
                             name:="{0} {1}".Frmt(component.Type, component.Name))
    End Sub

    Private Sub OnCommand(ByVal sender As CommandControl, ByVal argument As String) Handles comWidget.IssuedCommand
        Tinker.Components.UIInvokeCommand(_component, argument)
    End Sub
End Class
