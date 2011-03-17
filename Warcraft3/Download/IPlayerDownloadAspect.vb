Namespace WC3.Download
    <ContractClass(GetType(IPlayerDownloadAspect.ContractClass))>
    Public Interface IPlayerDownloadAspect
        Inherits IDisposableWithTask
        ReadOnly Property Name As InvariantString
        ReadOnly Property Id As PlayerId
        Function QueueAddPacketHandler(Of T)(packetDefinition As Protocol.Packets.Definition(Of T),
                                             handler As Func(Of T, Task)) As Task(Of IDisposable)
        Function MakePacketOtherPlayerJoined() As Protocol.Packet
        Function QueueSendPacket(packet As Protocol.Packet) As Task
        Function QueueDisconnect(expected As Boolean, reportedReason As Protocol.PlayerLeaveReason, reasonDescription As String) As Task

        <ContractClassFor(GetType(IPlayerDownloadAspect))>
        MustInherit Shadows Class ContractClass
            Implements IPlayerDownloadAspect
            Public Function MakePacketOtherPlayerJoined() As Protocol.Packet Implements IPlayerDownloadAspect.MakePacketOtherPlayerJoined
                Contract.Ensures(Contract.Result(Of Protocol.Packet)() IsNot Nothing)
                Throw New NotSupportedException
            End Function
            Public ReadOnly Property Name As InvariantString Implements IPlayerDownloadAspect.Name
                Get
                    Throw New NotSupportedException
                End Get
            End Property
            Public ReadOnly Property Id As PlayerId Implements IPlayerDownloadAspect.Id
                Get
                    Throw New NotSupportedException
                End Get
            End Property
            Public Function QueueAddPacketHandler(Of T)(packetDefinition As Protocol.Packets.Definition(Of T),
                                                        handler As Func(Of T, Task)) As Task(Of IDisposable) _
                                                        Implements IPlayerDownloadAspect.QueueAddPacketHandler
                Contract.Requires(packetDefinition IsNot Nothing)
                Contract.Requires(handler IsNot Nothing)
                Contract.Ensures(Contract.Result(Of Task(Of IDisposable))() IsNot Nothing)
                Throw New NotSupportedException
            End Function
            Public Function QueueSendPacket(packet As Protocol.Packet) As Task Implements IPlayerDownloadAspect.QueueSendPacket
                Contract.Requires(packet IsNot Nothing)
                Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
                Throw New NotSupportedException
            End Function
            Public Function QueueDisconnect(expected As Boolean, reportedReason As Protocol.PlayerLeaveReason, reasonDescription As String) As Task Implements IPlayerDownloadAspect.QueueDisconnect
                Contract.Requires(reasonDescription IsNot Nothing)
                Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
                Throw New NotSupportedException
            End Function

            Public ReadOnly Property DisposalTask As Task Implements IDisposableWithTask.DisposalTask
                Get
                    Throw New NotSupportedException
                End Get
            End Property
            Public Sub Dispose() Implements IDisposable.Dispose
                Throw New NotSupportedException
            End Sub
        End Class
    End Interface
End Namespace
