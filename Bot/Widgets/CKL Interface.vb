Imports HostBot.Commands
Imports HostBot.Bnet.BnetPacket

Namespace CKL
    Public Enum CKLPacketId As Byte
        [Error] = 0
        Keys = 1
    End Enum

    Public Class CKLKey
        Private Shared ReadOnly cdKeyJar As New CDKeyJar("cdkey data")
        Public ReadOnly name As String
        Public ReadOnly cdKeyROC As String
        Public ReadOnly cdKeyTFT As String
        Public Sub New(ByVal name As String,
                       ByVal cdKeyROC As String,
                       ByVal cdKeyTFT As String)
            Me.name = name
            Me.cdKeyROC = cdKeyROC
            Me.cdKeyTFT = cdKeyTFT
        End Sub
        Public Function Pack(ByVal clientToken As ViewableList(Of Byte),
                             ByVal serverToken As ViewableList(Of Byte)) As Byte()
            Return Concat(From key In {cdKeyROC, cdKeyTFT}
                          Select cdKeyJar.pack(cdKeyJar.PackCDKey(key, clientToken, serverToken)).Data.ToArray)
        End Function
    End Class
    Public Class CKLEncodedKey
        Public ReadOnly CDKeyROC As Dictionary(Of String, Object)
        Public ReadOnly CDKeyTFT As Dictionary(Of String, Object)
        Public Sub New(ByVal cdKeyROC As Dictionary(Of String, Object),
                       ByVal cdKeyTFT As Dictionary(Of String, Object))
            Me.CDKeyROC = cdKeyROC
            Me.CDKeyTFT = cdKeyTFT
        End Sub
    End Class

    Public NotInheritable Class BotCKLServer
        Inherits CKL.CKLServer
        Implements IBotWidget

        Public Const WidgetTypeName As String = "W3KeyServer"

        Private Shared ReadOnly commander As New Commands.Specializations.CKLCommands()

        Private Event AddStateString(ByVal state As String, ByVal insert_at_top As Boolean) Implements IBotWidget.AddStateString
        Private Event ClearStateStrings() Implements IBotWidget.ClearStateStrings
        Private Event RemoveStateString(ByVal state As String) Implements IBotWidget.RemoveStateString

        Public Sub New(ByVal name As String,
                       ByVal listenPort As PortPool.PortHandle)
            MyBase.New(name, listenPort)
            AddHandler KeyAdded, AddressOf c_KeyAdded
            AddHandler KeyRemoved, AddressOf c_KeyRemoved
        End Sub
        Public Sub New(ByVal name As String,
                       ByVal listenPort As UShort)
            MyBase.New(name, listenPort)
            AddHandler KeyAdded, AddressOf c_KeyAdded
            AddHandler KeyRemoved, AddressOf c_KeyRemoved
        End Sub

        Private Sub c_KeyAdded(ByVal sender As CKLServer, ByVal key As CKLKey)
            RaiseEvent AddStateString(key.name, False)
        End Sub
        Private Sub c_KeyRemoved(ByVal sender As CKLServer, ByVal key As CKLKey)
            RaiseEvent RemoveStateString(key.name)
        End Sub

        Private ReadOnly Property _Logger() As Logger Implements IBotWidget.Logger
            Get
                Return Logger
            End Get
        End Property
        Private ReadOnly Property _Name() As String Implements IBotWidget.Name
            Get
                Return name
            End Get
        End Property
        Private ReadOnly Property _TypeName() As String Implements IBotWidget.TypeName
            Get
                Return WidgetTypeName
            End Get
        End Property
        Private Sub command(ByVal text As String) Implements IBotWidget.ProcessCommand
            commander.ProcessLocalText(Me, text, Logger)
        End Sub
        Private Sub hooked() Implements IBotWidget.Hooked
            RaiseEvent ClearStateStrings()
            RaiseEvent AddStateString(GetReadableIPFromBytes(GetCachedIPAddressBytes(external:=True)), False)
            For Each port In Accepter.EnumPorts
                RaiseEvent AddStateString("port {0}".Frmt(port), False)
            Next port
            RaiseEvent AddStateString("----------", False)
            ref.QueueFunc(Function() keys.ToList()).CallOnValueSuccess(
                Sub(keys)
                    For Each key In keys
                        RaiseEvent AddStateString(key.name, False)
                    Next key
                End Sub
            )
            Logger.Log("Started.", LogMessageType.Negative)
        End Sub
        Public Overrides Sub [Stop]() Implements IBotWidget.[Stop]
            MyBase.[Stop]()
            RaiseEvent ClearStateStrings()
            Logger.Log("Stopped.", LogMessageType.Negative)
        End Sub
    End Class
End Namespace
