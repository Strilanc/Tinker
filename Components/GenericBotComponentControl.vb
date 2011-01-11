Namespace Components
    'Verification disabled because of many warnings in generated code
    <ContractVerification(False)>
    Public Class GenericBotComponentControl
        Private ReadOnly _component As Components.IBotComponent

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_component IsNot Nothing)
        End Sub

        Public Sub New(ByVal component As Components.IBotComponent)
            Contract.Assume(component IsNot Nothing)
            Me.InitializeComponent()
            Me._component = component
            logControl.SetLogger(logger:=component.Logger,
                                 name:="{0} {1}".Frmt(component.Type, component.Name))
        End Sub

        Private Sub OnCommand(ByVal sender As CommandControl, ByVal argument As String) Handles comWidget.IssuedCommand
            Contract.Requires(argument IsNot Nothing)
            Tinker.Components.UIInvokeCommand(_component, argument)
        End Sub
    End Class
End Namespace
