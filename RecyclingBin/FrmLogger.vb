Public Class FrmLogger
    Public Sub New(ByVal name As String, _
                   ByVal logger As MultiLogger, _
                   Optional ByVal showDataEvents As LoggerControl.CallbackMode = LoggerControl.CallbackMode.Unspecified, _
                   Optional ByVal showDataParsed As LoggerControl.CallbackMode = LoggerControl.CallbackMode.Unspecified, _
                   Optional ByVal showDataRaw As LoggerControl.CallbackMode = LoggerControl.CallbackMode.Unspecified)
        MyBase.New()
        InitializeComponent()
        Text = "Logger: " + name
        logMain.setLogger(logger, name, showDataEvents, showDataParsed, showDataRaw)
    End Sub
End Class