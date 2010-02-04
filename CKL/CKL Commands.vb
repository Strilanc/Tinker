Imports Tinker.Commands

Namespace CKL
    Public NotInheritable Class ServerCommands
        Inherits CommandSet(Of CKL.Server)

        Public Sub New()
            AddCommand(AddKey)
            AddCommand(RemoveKey)
        End Sub

        Private Shared ReadOnly AddKey As New DelegatedTemplatedCommand(Of CKL.Server)(
            Name:="AddKey",
            template:="Name roc=key tft=key",
            Description:="Adds a lendable key pair.",
            func:=Function(target, user, argument)
                      Dim name = argument.RawValue(0).AssumeNotNull
                      Dim rocKey = argument.NamedValue("roc").AssumeNotNull
                      Dim tftKey = argument.NamedValue("tft").AssumeNotNull
                      Return target.QueueAddKey(name, rocKey, tftKey).EvalOnSuccess(Function() "Key '{0}' added.".Frmt(name))
                  End Function)

        Private Shared ReadOnly RemoveKey As New DelegatedTemplatedCommand(Of CKL.Server)(
            Name:="RemoveKey",
            template:="Name",
            Description:="Removes a lendable key pair.",
            func:=Function(target, user, argument)
                      Dim name = argument.RawValue(0).AssumeNotNull
                      Return target.QueueRemoveKey(name).EvalOnSuccess(Function() "Key '{0}' removed.".Frmt(name))
                  End Function)
    End Class
End Namespace
