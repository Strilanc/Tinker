Imports HostBot.BNET

Public Class FrmClient
    Private WithEvents bot As MainBot
    Private WithEvents client As BnetClient

    Private Sub load_form(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Try
            'prep form
            Threading.Thread.CurrentThread.Name = "UI Thread"
            Me.Text = My.Application.Info.ProductName
            trayIcon.Text = My.Application.Info.ProductName

            'prep bot
            BeginCacheExternalIP()
            bot = New MainBot()
            For Each port In FrmSettings.parse_port_list(My.Settings.port_pool, "")
                bot.port_pool.TryAddPort(port)
            Next port
            botcMain.logBot.setLogUnexpected(True)
            botcMain.f_hook(bot)

            Me.Show()

            'load initial plugins
            Dim plugin_names = (From x In My.Settings.initial_plugins.Split(";"c) Where x <> "").ToList
            Dim future_plugin_loads = (From x In plugin_names Select bot.loadPlugin_R(x)).ToList
            BlockOnFutures(future_plugin_loads)
            Dim plugin_loads = (From x In future_plugin_loads Select x.getValue()).ToList
            For i = 0 To plugin_names.Count - 1
                Dim plugin = plugin_names(i)
                Dim loaded = plugin_loads(i)

                If loaded.outcome = Outcomes.succeeded Then
                    bot.logger.log("Loaded plugin '" + plugin + "'.", LogMessageTypes.PositiveEvent)
                Else
                    bot.logger.log("Failed to load plugin '" + plugin + "': " + loaded.message, LogMessageTypes.Problem)
                End If
            Next i

            'show ready
            botcMain.logBot.logMessage("---", Color.Black)
            botcMain.logBot.logMessage("Use the 'help' command for a list of commands.", Color.DarkGreen)
            botcMain.logBot.logMessage("---", Color.Black)

            'show settings on first run
            If My.Settings.botstore = "" Then
                uiBtnSettings(Nothing, Nothing)
            End If
        Catch ex As Exception
            MessageBox.Show("Error loading " + My.Resources.ProgramName + Environment.NewLine + ex.ToString())
            Me.Close()
        End Try
    End Sub

    Private Sub uiForm_Closing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing
        If bot Is Nothing Then Return

        If e.CloseReason = CloseReason.UserClosing Then
            If MessageBox.Show("Are you sure you want to close " + My.Resources.ProgramName + "?", _
                               "Confirm Close", _
                               MessageBoxButtons.YesNo, _
                               MessageBoxIcon.Question, _
                               MessageBoxDefaultButton.Button2) = Windows.Forms.DialogResult.No Then
                e.Cancel = True
                Return
            End If
        End If

        botcMain.f_hook(Nothing)
        botcMain.Dispose()

        With bot
            bot = Nothing
            client = Nothing
            .kill_R()
        End With
        My.Settings.Save()
    End Sub

    Private Sub uiBtnSettings(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSettings.Click
        FrmSettings.showBotSettings(bot)
    End Sub

    Private Sub mnuShowHide_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles mnuShowHide.Click
        mnuShowHide.Checked = Not mnuShowHide.Checked
        If mnuShowHide.Checked Then
            Me.Show()
            If Me.WindowState = FormWindowState.Minimized Then Me.WindowState = FormWindowState.Normal
        Else
            Me.Hide()
        End If
    End Sub

    Private Sub trayIcon_MouseDoubleClick(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles trayIcon.MouseDoubleClick
        Call mnuShowHide_Click(Nothing, Nothing)
    End Sub

    Private Sub FrmClient_Resize(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Resize
        If Me.WindowState = FormWindowState.Minimized Then
            Me.Hide()
            mnuShowHide.Checked = False
        End If
    End Sub

    Private Sub mnuClose_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles mnuClose.Click
        Me.Close()
    End Sub

    Private Sub btnConnectDefault_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)

    End Sub
End Class
