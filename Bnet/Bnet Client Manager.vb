Imports Tinker.Components

Namespace Bnet
    Public Class ClientManager
        Inherits FutureDisposable
        Implements IBotComponent

        Private Shared ReadOnly ClientCommands As New Bnet.ClientCommands()

        Private ReadOnly _bot As Bot.MainBot
        Private ReadOnly _name As InvariantString
        Private ReadOnly _client As Bnet.Client
        Private ReadOnly _control As Control
        Private ReadOnly _hooks As New List(Of IFuture(Of IDisposable))

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_bot IsNot Nothing)
            Contract.Invariant(_client IsNot Nothing)
            Contract.Invariant(_hooks IsNot Nothing)
            Contract.Invariant(_control IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As InvariantString,
                       ByVal bot As Bot.MainBot,
                       ByVal client As Bnet.Client)
            Contract.Requires(bot IsNot Nothing)
            Contract.Requires(client IsNot Nothing)

            Me._bot = bot
            Me._name = name
            Me._client = client
            Me._control = New BnetClientControl(Me)

            Me._hooks.Add(client.QueueAddPacketHandler(
                    id:=Bnet.PacketId.ChatEvent,
                    jar:=Bnet.Packet.ServerPackets.ChatEvent,
                    handler:=Function(pickle) TaskedAction(Sub() OnReceivedChatEvent(pickle.Value))))
        End Sub

        Public ReadOnly Property Client As Bnet.Client
            Get
                Contract.Ensures(Contract.Result(Of Bnet.Client)() IsNot Nothing)
                Return _client
            End Get
        End Property
        Public ReadOnly Property Bot As Bot.MainBot
            Get
                Contract.Ensures(Contract.Result(Of Bot.MainBot)() IsNot Nothing)
                Return _bot
            End Get
        End Property
        Public ReadOnly Property Name As InvariantString Implements IBotComponent.Name
            Get
                Return _name
            End Get
        End Property
        Public ReadOnly Property Type As InvariantString Implements IBotComponent.Type
            Get
                Return "Client"
            End Get
        End Property
        Public ReadOnly Property Logger As Logger Implements IBotComponent.Logger
            Get
                Return _client.logger
            End Get
        End Property
        Public ReadOnly Property HasControl As Boolean Implements IBotComponent.HasControl
            Get
                Contract.Ensures(Contract.Result(Of Boolean)())
                Return True
            End Get
        End Property
        Public Function IsArgumentPrivate(ByVal argument As String) As Boolean Implements IBotComponent.IsArgumentPrivate
            Return ClientCommands.IsArgumentPrivate(argument)
        End Function
        Public ReadOnly Property Control As Control Implements IBotComponent.Control
            Get
                Return _control
            End Get
        End Property

        Public Function InvokeCommand(ByVal user As BotUser, ByVal argument As String) As IFuture(Of String) Implements IBotComponent.InvokeCommand
            Return ClientCommands.Invoke(Me, user, argument)
        End Function

        Private Sub OnReceivedChatEvent(ByVal vals As Dictionary(Of InvariantString, Object))
            Contract.Requires(vals IsNot Nothing)

            Dim id = CType(vals("event id"), Bnet.Packet.ChatEventId)
            Dim user = _client.Profile.users(CStr(vals("username")))
            Dim text = CStr(vals("text"))

            'Check
            If user Is Nothing Then
                Return 'user not allowed
            ElseIf id <> Bnet.Packet.ChatEventId.Talk And id <> Bnet.Packet.ChatEventId.Whisper Then
                Return 'not a message
            ElseIf text.Substring(0, My.Settings.commandPrefix.AssumeNotNull.Length) <> My.Settings.commandPrefix AndAlso text <> Tinker.Bot.MainBot.TriggerCommandText Then
                Return 'not a command
            End If

            '?Trigger command
            If text = Tinker.Bot.MainBot.TriggerCommandText Then
                _client.QueueSendWhisper(user.Name, "Command prefix: {0}".Frmt(My.Settings.commandPrefix))
                Return
            End If

            'Normal commands
            Dim commandText = text.Substring(My.Settings.commandPrefix.AssumeNotNull.Length)
            Dim commandResult = Me.InvokeCommand(user, commandText)
            commandResult.CallOnValueSuccess(
                Sub(message) _client.QueueSendWhisper(user.Name, If(message, "Command Succeeded"))
            ).Catch(
                Sub(exception) _client.QueueSendWhisper(user.Name, "Failed: {0}".Frmt(exception.Message))
            )
            Call 2.Seconds.AsyncWait().CallWhenReady(
                Sub()
                    If commandResult.State = FutureState.Unknown Then
                        _client.QueueSendWhisper(user.Name, "Command '{0}' is running... You will be informed when it finishes.".Frmt(text))
                    End If
                End Sub
            )
        End Sub

        Public Shared Function AsyncCreateFromProfile(ByVal clientName As InvariantString,
                                                      ByVal profileName As InvariantString,
                                                      ByVal bot As Bot.MainBot) As IFuture(Of ClientManager)
            Contract.Requires(bot IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of ClientManager))() IsNot Nothing)

            Dim profile = (From p In bot.Settings.GetCopyOfClientProfiles Where p.name = profileName).FirstOrDefault
            If profile Is Nothing Then Throw New ArgumentException("No profile named '{0}'".Frmt(profileName))
            Return New Bnet.ClientManager(clientName, bot, New Bnet.Client(profile)).Futurized
        End Function

        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As Strilbrary.Threading.IFuture
            For Each hook In _hooks
                Contract.Assume(hook IsNot Nothing)
                hook.CallOnValueSuccess(Sub(value) value.Dispose()).SetHandled()
            Next hook
            _client.Dispose()
            _control.Dispose()
            Return Nothing
        End Function
    End Class
End Namespace
