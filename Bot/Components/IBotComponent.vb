Namespace Components
    <ContractClass(GetType(IBotComponent.ContractClass))>
    Public Interface IBotComponent
        Inherits IFutureDisposable
        ReadOnly Property Name As InvariantString
        ReadOnly Property Type As InvariantString
        ReadOnly Property Logger As Logger
        ReadOnly Property HasControl As Boolean
        ReadOnly Property Control() As Control
        Function IsArgumentPrivate(ByVal argument As String) As Boolean
        Function InvokeCommand(ByVal user As BotUser, ByVal argument As String) As IFuture(Of String)

        <ContractClassFor(GetType(IBotComponent))>
        NotInheritable Shadows Class ContractClass
            Implements IBotComponent

            Public ReadOnly Property Logger As Logger Implements IBotComponent.Logger
                Get
                    Contract.Ensures(Contract.Result(Of Logger)() IsNot Nothing)
                    Throw New NotSupportedException
                End Get
            End Property

            Public ReadOnly Property HasControl As Boolean Implements IBotComponent.HasControl
                Get
                    Throw New NotSupportedException
                End Get
            End Property
            Public ReadOnly Property Control() As Control Implements IBotComponent.Control
                Get
                    Contract.Requires(Me.HasControl)
                    Contract.Ensures(Contract.Result(Of Control)() IsNot Nothing)
                    Throw New NotSupportedException
                End Get
            End Property

            Public Function InvokeCommand(ByVal user As BotUser, ByVal argument As String) As IFuture(Of String) Implements IBotComponent.InvokeCommand
                Contract.Requires(argument IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IFuture(Of String))() IsNot Nothing)
                Throw New NotSupportedException
            End Function

            Public Function IsArgumentPrivate(ByVal argument As String) As Boolean Implements IBotComponent.IsArgumentPrivate
                Contract.Requires(argument IsNot Nothing)
                Throw New NotSupportedException
            End Function

            Public ReadOnly Property Name As InvariantString Implements IBotComponent.Name
                Get
                    Throw New NotSupportedException
                End Get
            End Property
            Public ReadOnly Property Type As InvariantString Implements IBotComponent.Type
                Get
                    Throw New NotSupportedException
                End Get
            End Property
            Public Sub Dispose() Implements IDisposable.Dispose
            End Sub
            Public ReadOnly Property FutureDisposed As Strilbrary.Threading.IFuture Implements Strilbrary.Threading.IFutureDisposable.FutureDisposed
                Get
                    Throw New NotSupportedException
                End Get
            End Property
        End Class
    End Interface

    Public Module IBotComponentExtensions
        <Extension()>
        Public Sub UIInvokeCommand(ByVal component As IBotComponent, ByVal argument As String)
            Contract.Requires(component IsNot Nothing)
            Contract.Requires(argument IsNot Nothing)
            Try
                Dim i = argument.IndexOf(" "c)
                If i = -1 Then i = argument.Length
                Dim subcommand As InvariantString = argument.Substring(0, i)

                Dim argDesc = If(component.IsArgumentPrivate(subcommand), "{0} [arguments hidden]".Frmt(subcommand), argument)
                component.Logger.Log("Command: {0}".Frmt(argDesc), LogMessageType.Typical)

                component.Logger.FutureLog(
                    placeholder:="[running command {0}...]".Frmt(argDesc),
                    message:=component.InvokeCommand(Nothing, argument).EvalWhenValueReady(
                        Function(message, commandException)
                            If commandException IsNot Nothing Then
                                Return "Failed: {0}".Frmt(commandException.Message)
                            ElseIf message Is Nothing OrElse message = "" Then
                                Return "Command '{0}' succeeded.".Frmt(argDesc)
                            Else
                                Return message
                            End If
                        End Function))
            Catch e As Exception
                e.RaiseAsUnexpected("UIInvokeCommand for {0}:{1}".Frmt(component.Type, component.Name))
            End Try
        End Sub
    End Module
End Namespace
