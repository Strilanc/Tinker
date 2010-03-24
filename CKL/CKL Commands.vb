Imports Tinker.Commands

Namespace CKL
    Public NotInheritable Class ServerCommands
        Inherits CommandSet(Of CKL.Server)

        Public Sub New()
            AddCommand(New CommandAddKey)
            AddCommand(New CommandRemoveKey)
        End Sub

        Private NotInheritable Class CommandAddKey
            Inherits TemplatedCommand(Of CKL.Server)
            Public Sub New()
                MyBase.New(Name:="RemoveKey",
                           template:="Name",
                           Description:="Removes a lendable key pair.")
            End Sub
            Protected Overloads Overrides Function PerformInvoke(ByVal target As Server, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                Dim name = argument.RawValue(0)
                Dim rocKey = argument.NamedValue("roc")
                Dim tftKey = argument.NamedValue("tft")
                Contract.Assume(target IsNot Nothing)
                Return target.QueueAddKey(name, rocKey, tftKey).ContinueWithFunc(Function() "Key '{0}' added.".Frmt(name))
            End Function
        End Class
        Private NotInheritable Class CommandRemoveKey
            Inherits TemplatedCommand(Of CKL.Server)
            Public Sub New()
                MyBase.New(Name:="AddKey",
                           template:="Name roc=key tft=key",
                           Description:="Adds a lendable key pair.")
            End Sub
            Protected Overloads Overrides Function PerformInvoke(ByVal target As Server, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                Dim name = argument.RawValue(0)
                Contract.Assume(target IsNot Nothing)
                Return target.QueueRemoveKey(name).ContinueWithFunc(Function() "Key '{0}' removed.".Frmt(name))
            End Function
        End Class
    End Class
End Namespace
