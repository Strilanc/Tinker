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
                      Dim name = argument.RawValue(0)
                      Dim rocKey = argument.NamedValue("roc")
                      Dim tftKey = argument.NamedValue("tft")
                      Return target.QueueAddKey(name, rocKey, tftKey).ContinueWithFunc(Function() "Key '{0}' added.".Frmt(name))
                  End Function)

        Private Shared ReadOnly RemoveKey As New DelegatedTemplatedCommand(Of CKL.Server)(
            Name:="RemoveKey",
            template:="Name",
            Description:="Removes a lendable key pair.",
            func:=Function(target, user, argument)
                      Dim name = argument.RawValue(0)
                      Return target.QueueRemoveKey(name).ContinueWithFunc(Function() "Key '{0}' removed.".Frmt(name))
                  End Function)
    End Class
End Namespace
