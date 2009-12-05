Imports Tinker.Commands

Namespace CKL
    Public Enum CKLPacketId As Byte
        [Error] = 0
        Keys = 1
    End Enum

    Public NotInheritable Class CKLKey
        Private Shared ReadOnly cdKeyJar As New Bnet.Packet.CDKeyJar("cdkey data")
        Private ReadOnly _name As InvariantString
        Private ReadOnly _cdKeyROC As InvariantString
        Private ReadOnly _cdKeyTFT As InvariantString

        Public Sub New(ByVal name As InvariantString,
                       ByVal cdKeyROC As String,
                       ByVal cdKeyTFT As String)
            Contract.Requires(cdKeyROC IsNot Nothing)
            Contract.Requires(cdKeyTFT IsNot Nothing)
            Me._name = name
            Me._cdKeyROC = cdKeyROC
            Me._cdKeyTFT = cdKeyTFT
        End Sub

        Public ReadOnly Property Name As InvariantString
            Get
                Return _name
            End Get
        End Property
        Public ReadOnly Property CDKeyROC As InvariantString
            Get
                Return _cdKeyROC
            End Get
        End Property
        Public ReadOnly Property CDKeyTFT As InvariantString
            Get
                Return _cdKeyTFT
            End Get
        End Property

        Public Function Pack(ByVal clientToken As UInt32,
                             ByVal serverToken As UInt32) As ViewableList(Of Byte)
            Contract.Ensures(Contract.Result(Of ViewableList(Of Byte))() IsNot Nothing)
            Return Concat(From key In {CDKeyROC, CDKeyTFT}
                          Select vals = Bnet.Packet.CDKeyJar.PackCDKey(key, clientToken, serverToken)
                          Select cdKeyJar.Pack(vals).Data.ToArray).ToView
        End Function
    End Class
    Public NotInheritable Class CKLEncodedKey
        Public ReadOnly CDKeyROC As Dictionary(Of InvariantString, Object)
        Public ReadOnly CDKeyTFT As Dictionary(Of InvariantString, Object)
        Public Sub New(ByVal cdKeyROC As Dictionary(Of InvariantString, Object),
                       ByVal cdKeyTFT As Dictionary(Of InvariantString, Object))
            Me.CDKeyROC = cdKeyROC
            Me.CDKeyTFT = cdKeyTFT
        End Sub
    End Class
End Namespace
