Namespace Warcraft3
    Public Enum HostTestResult As Integer
        Fail = -1
        Test = 0
        Pass = 1
    End Enum
    Public Class W3PlayerPingRecord
        Public ReadOnly salt As UInteger
        Public ReadOnly time As ModInt32
        Public Sub New(ByVal salt As UInteger, ByVal time As ModInt32)
            Me.salt = salt
            Me.time = time
        End Sub
    End Class
    Public Enum W3PlayerLeaveType As Byte
        Disconnect = 1
        Lose = 7
        MeleeLose = 8
        Win = 9
        Draw = 10
        Observer = 11
        Lobby = 13
    End Enum
End Namespace
