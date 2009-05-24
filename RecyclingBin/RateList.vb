'''' <summary>
'''' Records the times when things happen
'''' </summary>
'Public Class RateList
'    Private ReadOnly marks As New List(Of Triplet(Of Date, String, TimeSpan))
'    Private startTime As Date = DateTime.Now()
'    Private lastMark As Date = startTime
'    Private paused As Boolean = False
'    Private dt_total As New TimeSpan(0)

'    Public Sub New()
'    End Sub
'    Public Sub New(ByVal s As IO.FileStream)
'        Dim r As New IO.BinaryReader(s)

'        startTime = New Date(r.ReadInt64())
'        lastMark = New Date(r.ReadInt64())
'        paused = r.ReadBoolean()
'        dt_total = New TimeSpan(r.ReadInt64())

'        Dim n As Integer = r.ReadInt32()
'        For i As Integer = 0 To n - 1
'            Dim d = New Date(r.ReadInt64())
'            Dim z = r.ReadString()
'            Dim t = New TimeSpan(r.ReadInt64())
'            marks.Add(New Triplet(Of Date, String, TimeSpan)(d, z, t))
'        Next i
'    End Sub
'    Public Sub writeTo(ByVal s As IO.Stream)
'        Dim w As New IO.BinaryWriter(s)

'        w.Write(startTime.Ticks)
'        w.Write(lastMark.Ticks)
'        w.Write(paused)
'        w.Write(dt_total.Ticks)

'        w.Write(marks.Count)
'        For i As Integer = 0 To marks.Count - 1
'            w.Write(marks(i).v1.Ticks)
'            w.Write(marks(i).v2)
'            w.Write(marks(i).v3.Ticks)
'        Next i
'    End Sub

'    Public Sub mark(ByVal tag As String)
'        mark(DateTime.Now, tag)
'    End Sub
'    Public Sub mark(ByVal d As Date, ByVal tag As String)
'        If paused Then lastMark = d
'        dt_total += d - lastMark
'        marks.Add(New Triplet(Of Date, String, TimeSpan)(d, tag, d - lastMark))
'        lastMark = d
'    End Sub
'    Public Sub pause()
'        If paused Then Return
'        paused = True
'    End Sub
'    Public Sub unpause()
'        If Not paused Then Return
'        lastMark = DateTime.Now()
'        paused = False
'    End Sub

'    Public Function getAverageTimeSpan() As TimeSpan
'        If marks.Count() <= 0 Then Return New TimeSpan(0)
'        Return New TimeSpan(CLng(totalTimeSpan.Ticks / marks.Count))
'    End Function
'    Public Function getRatePerHour() As Double
'        Return getRatePerMinute() / 60
'    End Function
'    Public Function getRatePerMinute() As Double
'        Return getRatePerSecond() / 60
'    End Function
'    Public Function getRatePerSecond() As Double
'        Return getRatePerMillisecond() / 1000
'    End Function
'    Public Function getRatePerMillisecond() As Double
'        If totalTimeSpan.TotalMilliseconds <= 0 Then Return 0
'        Return marks.Count / totalTimeSpan.TotalMilliseconds
'    End Function
'    Public Function getMarkDate(ByVal index As Integer) As Date
'        Return marks(index).v1
'    End Function
'    Public Function getMarkTag(ByVal index As Integer) As String
'        Return marks(index).v2
'    End Function
'    Public Function getMarkTimeSpan(ByVal index As Integer) As TimeSpan
'        Return marks(index).v3
'    End Function
'    Public Function numMarks() As Integer
'        Return marks.Count()
'    End Function
'    Public Function totalTimeSpan(Optional ByVal toLastMarkInsteadOfNow As Boolean = True) As TimeSpan
'        Dim dt As TimeSpan = dt_total
'        If Not paused Then dt += DateTime.Now - lastMark
'        Return dt
'    End Function
'    Public Function startDate() As Date
'        Return startTime
'    End Function
'End Class
