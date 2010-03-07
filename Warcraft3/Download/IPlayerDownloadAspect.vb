Imports Tinker.Pickling

Namespace WC3.Download
    <ContractClass(GetType(IPlayerDownloadAspect.ContractClass))>
    Public Interface IPlayerDownloadAspect
        Inherits IDisposableWithTask
        ReadOnly Property Name As InvariantString
        ReadOnly Property Id As PlayerId
        Function QueueAddPacketHandler(Of T)(ByVal packetDefinition As Protocol.Packets.Definition(Of T),
                                             ByVal handler As Func(Of IPickle(Of T), Task)) As Task(Of IDisposable)
        Function MakePacketOtherPlayerJoined() As Protocol.Packet
        Function QueueSendPacket(ByVal packet As Protocol.Packet) As Task
        Function QueueDisconnect(ByVal expected As Boolean, ByVal reportedReason As Protocol.PlayerLeaveReason, ByVal reasonDescription As String) As Task

        <ContractClassFor(GetType(IPlayerDownloadAspect))>
        NotInheritable Shadows Class ContractClass
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
            Public Function QueueAddPacketHandler(Of T)(ByVal packetDefinition As Protocol.Packets.Definition(Of T),
                                                        ByVal handler As Func(Of IPickle(Of T), Task)) As Task(Of IDisposable) _
                                                        Implements IPlayerDownloadAspect.QueueAddPacketHandler
                Contract.Requires(packetDefinition IsNot Nothing)
                Contract.Requires(handler IsNot Nothing)
                Contract.Ensures(Contract.Result(Of Task(Of IDisposable))() IsNot Nothing)
                Throw New NotSupportedException
            End Function
            Public Function QueueSendPacket(ByVal packet As Protocol.Packet) As Task Implements IPlayerDownloadAspect.QueueSendPacket
                Contract.Requires(packet IsNot Nothing)
                Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
                Throw New NotSupportedException
            End Function
            Public ReadOnly Property DisposalTask As Task Implements IDisposableWithTask.DisposalTask
                Get
                    Throw New NotSupportedException
                End Get
            End Property
            Public Sub Dispose() Implements IDisposable.Dispose
            End Sub
            Public Function QueueDisconnect(ByVal expected As Boolean, ByVal reportedReason As Protocol.PlayerLeaveReason, ByVal reasonDescription As String) As Task Implements IPlayerDownloadAspect.QueueDisconnect
                Contract.Requires(reasonDescription IsNot Nothing)
                Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
                Throw New NotSupportedException
            End Function
        End Class
    End Interface
End Namespace
