'Imports HostBot.Functional
'Imports HostBot.Functional.Futures
'Imports HostBot.Functional.Futures.BaseImplementations
'Imports HostBot.Functional.Currying

'Public Class BotStats
'    Private WithEvents client As BnetClient = Nothing
'    Public ReadOnly playersLeft As New RateList()
'    Public ReadOnly playersJoined As New RateList()

'    Public Event updated()
'    Private ReadOnly ref As New ThreadedCallQueue(Me.gettype.name + " ref")

'    Private Class DelayedPromise
'        Inherits PromiscuousFuture(Of Boolean)
'        Public ReadOnly target As String
'        Public ReadOnly expire As Date
'        Public Sub New(ByVal target As String, ByVal expire As Date)
'            Me.target = target
'            Me.expire = expire
'        End Sub
'    End Class

'    Public Sub updateClient(ByVal c As BnetClient)
'        client = c
'    End Sub

'    Public Sub pause()
'        playersJoined.pause()
'        playersLeft.pause()
'    End Sub
'    Public Sub unpause()
'        playersJoined.unpause()
'        playersLeft.unpause()
'    End Sub

'    Public Sub recordPlayerJoined(ByVal p As W3Player)
'        playersJoined.mark(p.PERM_name + ":" + p.PERM_game.PERM_name)
'        RaiseEvent updated()
'    End Sub
'    Public Sub recordPlayerLeft(ByVal p As W3Player)
'        playersLeft.mark(p.PERM_name + ":" + p.PERM_game.PERM_name)
'        RaiseEvent updated()
'    End Sub

'    Public Function numJoined() As Integer
'        Return playersJoined.numMarks
'    End Function
'    Public Function numLeft() As Integer
'        Return playersLeft.numMarks
'    End Function

'    Public Function churnRate() As Double
'        If numJoined() = 0 Then Return 0
'        Return numLeft() / numJoined()
'    End Function

'    Public Function uiToString() As String
'        Dim s As String = "Statistics:"
'        s += Environment.NewLine + "Num Joined: " + numJoined.ToString()
'        s += Environment.NewLine + "Num Left: " + numLeft.ToString()
'        s += Environment.NewLine
'        s += Environment.NewLine + "Churn Rate: " + (churnRate() * 100).ToString() + "%"
'        s += Environment.NewLine + "Join Rate: " + CInt(playersJoined.getRatePerMinute()).ToString() + " players per minute"
'        s += Environment.NewLine + "Leave Rate: " + CInt(playersLeft.getRatePerMinute()).ToString() + " players per minute"
'        Return s
'    End Function
'End Class
