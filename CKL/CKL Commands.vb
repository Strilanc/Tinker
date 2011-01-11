Imports Tinker.Commands

Namespace CKL.ServerCommands
    Public NotInheritable Class CommandAddKey
        Inherits TemplatedCommand(Of CKL.Server)
        Public Sub New()
            MyBase.New(Name:="RemoveKey",
                       template:="Name",
                       Description:="Removes a lendable key pair.")
        End Sub
        <SuppressMessage("Microsoft.Contracts", "Ensures-40-81")>
        Protected Overloads Overrides Async Function PerformInvoke(ByVal target As Server, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
            Dim name = argument.RawValue(0)
            Dim rocKey = argument.NamedValue("roc")
            Dim tftKey = argument.NamedValue("tft")
            Contract.Assume(target IsNot Nothing)
            Await target.QueueAddKey(name, rocKey, tftKey)
            Return "Key '{0}' added.".Frmt(name)
        End Function
    End Class
    Public NotInheritable Class CommandRemoveKey
        Inherits TemplatedCommand(Of CKL.Server)
        Public Sub New()
            MyBase.New(Name:="AddKey",
                       template:="Name roc=key tft=key",
                       Description:="Adds a lendable key pair.")
        End Sub
        <SuppressMessage("Microsoft.Contracts", "Ensures-40-81")>
        Protected Overloads Overrides Async Function PerformInvoke(ByVal target As Server, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
            Dim name = argument.RawValue(0)
            Contract.Assume(target IsNot Nothing)
            Await target.QueueRemoveKey(name)
            Return "Key '{0}' removed.".Frmt(name)
        End Function
    End Class
End Namespace
