Imports Tinker.CKL

Namespace Commands.Specializations
    Public NotInheritable Class CKLCommands
        Inherits CommandSet(Of CKLServer)

        Public Sub New()
            AddCommand(AddKey)
            AddCommand(RemoveKey)
        End Sub

        Private Shared ReadOnly AddKey As New DelegatedTemplatedCommand(Of CKLServer)(
            Name:="AddKey",
            template:="Name roc=key tft=key",
            Description:="Adds a lendable key pair.",
            func:=Function(target, user, argument)
                      Dim name = argument.RawValue(0).AssumeNotNull
                      Dim rocKey = argument.NamedValue("roc").AssumeNotNull
                      Dim tftKey = argument.NamedValue("tft").AssumeNotNull
                      Return target.AddKey(name, rocKey, tftKey).EvalOnSuccess(Function() "Key '{0}' added.".Frmt(name))
                  End Function)

        Private Shared ReadOnly RemoveKey As New DelegatedTemplatedCommand(Of CKLServer)(
            Name:="RemoveKey",
            template:="Name",
            Description:="Removes a lendable key pair.",
            func:=Function(target, user, argument)
                      Dim name = argument.RawValue(0).AssumeNotNull
                      Return target.RemoveKey(name).EvalOnSuccess(Function() "Key '{0}' removed.".Frmt(name))
                  End Function)
    End Class
End Namespace
