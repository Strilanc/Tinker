Imports HostBot.Bnet.BnetPacket

Namespace CKL
    Public Enum CklPacketId As Byte
        [error] = 0
        keys = 1
    End Enum

    Public Class CklKey
        Private Shared ReadOnly cdKeyJar As New CDKeyJar("cdkey data")
        Public ReadOnly name As String
        Public ReadOnly rocKey As String
        Public ReadOnly tftKey As String
        Public Sub New(ByVal name As String,
                       ByVal rocKey As String,
                       ByVal tftKey As String)
            Me.name = name
            Me.rocKey = rocKey
            Me.tftKey = tftKey
        End Sub
        Public Function Pack(ByVal clientToken As ViewableList(Of Byte),
                             ByVal serverToken As ViewableList(Of Byte)) As Byte()
            Return Concat(From key In {rocKey, tftKey}
                          Select cdKeyJar.pack(cdKeyJar.packCDKey(key, clientToken, serverToken)).Data.ToArray)
        End Function
    End Class
    Public Class CklEncodedKey
        Public ReadOnly rocKey As Dictionary(Of String, Object)
        Public ReadOnly tftKey As Dictionary(Of String, Object)
        Public Sub New(ByVal rocKey As Dictionary(Of String, Object),
                       ByVal tftKey As Dictionary(Of String, Object))
            Me.rocKey = rocKey
            Me.tftKey = tftKey
        End Sub
    End Class

    Public NotInheritable Class BotCKLServer
        Inherits CKL.CklServer
        Implements IBotWidget

        Public Const WIDGET_TYPE_NAME As String = "W3KeyServer"

        Private Shared ReadOnly commander As New Commands.Specializations.CKLCommands()

        Private Event AddStateString(ByVal state As String, ByVal insert_at_top As Boolean) Implements IBotWidget.AddStateString
        Private Event ClearStateStrings() Implements IBotWidget.ClearStateStrings
        Private Event RemoveStateString(ByVal state As String) Implements IBotWidget.RemoveStateString

        Public Sub New(ByVal name As String,
                       ByVal listen_port As PortPool.PortHandle)
            MyBase.New(name, listen_port)
            AddHandler KeyAdded, AddressOf c_KeyAdded
            AddHandler KeyRemoved, AddressOf c_KeyRemoved
        End Sub
        Public Sub New(ByVal name As String,
                       ByVal listen_port As UShort)
            MyBase.New(name, listen_port)
            AddHandler KeyAdded, AddressOf c_KeyAdded
            AddHandler KeyRemoved, AddressOf c_KeyRemoved
        End Sub

        Private Sub c_KeyAdded(ByVal sender As CklServer, ByVal key As CklKey)
            RaiseEvent AddStateString(key.name, False)
        End Sub
        Private Sub c_KeyRemoved(ByVal sender As CklServer, ByVal key As CklKey)
            RaiseEvent RemoveStateString(key.name)
        End Sub

        Private ReadOnly Property _Logger() As Logger Implements IBotWidget.Logger
            Get
                Return logger
            End Get
        End Property
        Private ReadOnly Property _Name() As String Implements IBotWidget.Name
            Get
                Return name
            End Get
        End Property
        Private ReadOnly Property _TypeName() As String Implements IBotWidget.TypeName
            Get
                Return WIDGET_TYPE_NAME
            End Get
        End Property
        Private Sub command(ByVal text As String) Implements IBotWidget.ProcessCommand
            commander.ProcessLocalText(Me, text, logger)
        End Sub
        Private Sub hooked() Implements IBotWidget.Hooked
            RaiseEvent ClearStateStrings()
            RaiseEvent AddStateString(GetReadableIpFromBytes(GetCachedIpAddressBytes(external:=True)), False)
            For Each port In accepter.EnumPorts
                RaiseEvent AddStateString("port " + port.ToString(), False)
            Next port
            RaiseEvent AddStateString("----------", False)
            ref.QueueFunc(Function() keys.ToList()).CallOnValueSuccess(
                Sub(keys)
                    For Each key In keys
                        RaiseEvent AddStateString(key.name, False)
                    Next key
                End Sub
            )
            logger.log("Started.", LogMessageType.Negative)
        End Sub
        Public Overrides Sub [stop]() Implements IBotWidget.[Stop]
            MyBase.[stop]()
            RaiseEvent ClearStateStrings()
            logger.log("Stopped.", LogMessageType.Negative)
        End Sub
    End Class
End Namespace
