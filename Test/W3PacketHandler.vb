Imports HostBot.Warcraft3

Namespace Testing
    Public NotInheritable Class ParseHandleResult
        Public ReadOnly parseResult As IFuture
        Public ReadOnly handleResult As ifuture
        Public Sub New(ByVal parseResult As IFuture, ByVal handleResult As IFuture)
            Me.parseResult = parseResult
            Me.handleResult = handleResult
        End Sub
    End Class
    Public Interface IHandler
        Function TryParseAndHandle(ByVal data As ViewableList(Of Byte)) As ParseHandleResult
    End Interface
    Public Interface IKeyedHandler(Of Out K)
        Inherits IHandler
        ReadOnly Property Key As K
    End Interface

    Public NotInheritable Class Handler(Of T)
        Implements IHandler
        Private ReadOnly parser As IParseJar(Of T)
        Private ReadOnly handler As Func(Of T, IFuture)

        Public Sub New(ByVal parser As IParseJar(Of T),
                       ByVal handler As Func(Of T, IFuture))
            Me.parser = parser
            Me.handler = handler
        End Sub

        Public Function TryParseAndHandle(ByVal data As ViewableList(Of Byte)) As ParseHandleResult Implements IHandler.TryParseAndHandle
            Dim parseResult = parser.Parse(data)
            Dim handleResult = handler(parseResult.Value)
            Return New ParseHandleResult(parseResult.Value.Futurized, handleResult)
        End Function
    End Class

    Public NotInheritable Class PacketParseJar(Of T)
        Inherits ParseJar(Of T)
        Private ReadOnly parser As Func(Of ViewableList(Of Byte), T)
        Public Sub New(ByVal name As String, ByVal parser As Func(Of ViewableList(Of Byte), T))
            MyBase.new(name)
            Me.parser = parser
        End Sub
        Public Overrides Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of T)
            Return New Pickle(Of T)(Name, parser(data), data)
        End Function
    End Class

    Public NotInheritable Class PingPacket
        Public ReadOnly seed As UInteger
        Public Sub New(ByVal seed As UInteger)
            Me.seed = seed
        End Sub
        Public Shared Function FromData(ByVal data As ViewableList(Of Byte)) As PingPacket
            If data.Length <> 4 Then Throw New InvalidOperationException()
            Return New PingPacket(data.ToUInt32)
        End Function
        Public Function ToData() As ViewableList(Of Byte)
            Return seed.Bytes.ToView
        End Function
    End Class

    'Public Class Dic
    '    Private ReadOnly jar As TupleParseJar
    '    Public Sub New(ByVal jar As TupleParseJar)
    '        Me.jar = jar
    '    End Sub
    'End Class

    'Public Module M
    '    Private ReadOnly PingPacketJar As New PacketParseJar(Of PingPacket)(W3PacketId.Ping.ToString, AddressOf PingPacket.FromData)
    '    Public Function Pico(Of T)(ByVal k As T, ByVal jar As IParseJar(Of T), ByVal handler As Action(Of T)) As Handler(Of T)
    '        Return New Handler(Of T)(jar, handler)
    '    End Function
    'End Module
End Namespace
