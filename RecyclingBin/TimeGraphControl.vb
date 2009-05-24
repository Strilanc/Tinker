Public Class TimeGraphControl
    'Private data As RateList = Nothing
    Private innerTitle As String = "Rate Variation Graph"
    Public Property title() As String
        Get
            Return innerTitle
        End Get
        Set(ByVal value As String)
            innerTitle = value
        End Set
    End Property

            'Public Sub hook(ByVal data As RateList)
            '    Me.data = data
            'End Sub

            'Public Sub redraw()
            '    picGraph.Refresh()
            'End Sub

            'Private Sub picGraph_Paint(ByVal sender As Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles picGraph.Paint
            '    Dim g As System.Drawing.Graphics = e.Graphics()
            '    g.Clear(Color.White)
            '    If data Is Nothing Then Return

            '    g.TranslateTransform(0, picGraph.Height - 1)
            '    g.ScaleTransform(CSng(picGraph.Width / data.totalTimeSpan.Ticks), -CSng((picGraph.Height - 1) / data.numMarks))

            '    Dim dt As TimeSpan = New TimeSpan(0)
            '    Dim lastX As Single = 0, lastY As Single = 0
            '    For i As Integer = 0 To data.numMarks() - 1
            '        dt += data.getMarkTimeSpan(i)

            '        Dim curX As Single = dt.Ticks
            '        Dim curY As Single = i + 1
            '        g.DrawLine(Pens.Black, lastX, lastY, curX, lastY)
            '        g.DrawLine(Pens.Black, curX, lastY, curX, curY)
            '        lastX = curX
            '        lastY = curY
            '    Next i
            '    g.DrawLine(Pens.Black, lastX, lastY, data.totalTimeSpan.Ticks, data.numMarks)

            '    g.Save()
            '    lblTitle.Text = title + ": n=" + data.numMarks.ToString() + ", dt=" + CInt(data.totalTimeSpan.TotalMinutes).ToString() + "m"
            'End Sub
End Class
