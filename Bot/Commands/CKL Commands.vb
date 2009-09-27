Imports HostBot.Commands
Imports HostBot.Bnet
Imports HostBot.Warcraft3
Imports HostBot.Links
Imports HostBot.CKL

Namespace Commands.Specializations
    Public Class CKLCommands
        Inherits CommandSet(Of CKLServer)

        Public Sub New()
            AddCommand(New CommandAddKey)
            AddCommand(New CommandRemoveKey)
        End Sub

        '''<summary>Starts advertising a game.</summary>
        Private Class CommandAddKey
            Inherits BaseCommand(Of CKLServer)
            Public Sub New()
                MyBase.New("AddKey",
                           3, ArgumentLimitType.Exact,
                           "[--AddKey Name RocKey TftKey] Adds a key for lending.",
                           "", "", True)
            End Sub
            Public Overrides Function Process(ByVal target As CKLServer,
                                              ByVal user As BotUser,
                                              ByVal arguments As IList(Of String)) As IFuture(Of String)
                Dim name = arguments(0)
                Dim rocKey = arguments(1)
                Dim tftKey = arguments(2)
                Return target.AddKey(name, rocKey, tftKey).EvalOnSuccess(Function() "Key '{0}' added.".Frmt(name))
            End Function
        End Class

        '''<summary>Stops advertising a game.</summary>
        Private Class CommandRemoveKey
            Inherits BaseCommand(Of CKLServer)
            Public Sub New()
                MyBase.New("RemoveKey",
                           1, ArgumentLimitType.Exact,
                           "[--RemoveKey Name] Removes a game being advertised.")
            End Sub
            Public Overrides Function Process(ByVal target As CKLServer,
                                              ByVal user As BotUser,
                                              ByVal arguments As IList(Of String)) As IFuture(Of String)
                Dim name = arguments(0)
                Return target.RemoveKey(name).EvalOnSuccess(Function() "Key '{0}' added.".Frmt(name))
            End Function
        End Class
    End Class
End Namespace
