Imports System.Threading
Imports HostBot.Bnet

Public Class FrmClient
    Private WithEvents bot As MainBot
    Private WithEvents client As BnetClient

    Private Sub c_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Try
            'prep form
            Thread.CurrentThread.Name = "UI Thread"
            Me.Text = My.Application.Info.ProductName
            trayIcon.Text = My.Application.Info.ProductName

            'prep bot
            CacheIPAddresses()
            bot = New MainBot(New InvokedCallQueue(Me))
            For Each port In FrmSettings.parsePortList(My.Settings.port_pool, "")
                bot.portPool.TryAddPort(port)
            Next port
            botcMain.logBot.SetLogUnexpected(True)
            botcMain.QueueHook(bot)

            Me.Show()

            Try
                Dim x = New Warden_Module_Lib.ModuleHandler
                x.UnloadModule()
            Catch ex As Exception
                botcMain.logBot.LogMessage("Error loading warden module library:{0}{1}".frmt(vbNewLine, ex), Color.Red)
            End Try

            'load initial plugins
            Dim pluginNames = (From x In My.Settings.initial_plugins.Split(";"c) Where x <> "").ToList
            Dim futureLoadedPlugins = (From x In pluginNames Select bot.QueueLoadPlugin(x)).ToList
            Dim t = New ManualResetEvent(False)
            FutureCompress(futureLoadedPlugins).CallWhenReady(Sub() t.Set())
            t.WaitOne()
            Dim pluginLoadOutcomes = (From x In futureLoadedPlugins Select x.Value()).ToList
            For i = 0 To pluginNames.Count - 1
                Dim plugin = pluginNames(i)
                Dim loaded = pluginLoadOutcomes(i)

                If loaded.succeeded Then
                    bot.logger.log("Loaded plugin '" + plugin + "'.", LogMessageType.Positive)
                Else
                    bot.logger.log("Failed to load plugin '" + plugin + "': " + loaded.message, LogMessageType.Problem)
                End If
            Next i

            'show ready
            botcMain.logBot.LogMessage("---", Color.Black)
            botcMain.logBot.LogMessage("Use the 'help' command for a list of commands.", Color.DarkGreen)
            botcMain.logBot.LogMessage("---", Color.Black)

            'show settings on first run
            If My.Settings.war3path = "" Then
                My.Settings.war3path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + IO.Path.DirectorySeparatorChar + "Warcraft III" + IO.Path.DirectorySeparatorChar
            End If
            If My.Settings.mapPath = "" Then
                My.Settings.mapPath = My.Settings.war3path + "Maps" + IO.Path.DirectorySeparatorChar
            End If
            If My.Settings.botstore = "" Then
                btnSettings_Click(Nothing, Nothing)
            End If
        Catch ex As Exception
            MessageBox.Show(GenerateUnexpectedExceptionDescription("Error loading " + My.Resources.ProgramName, ex))
            Me.Close()
        End Try
    End Sub

    Private Sub c_Closing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing
        If bot Is Nothing Then Return

        If e.CloseReason = CloseReason.UserClosing Then
            If MessageBox.Show("Are you sure you want to close " + My.Resources.ProgramName + "?",
                               "Confirm Close",
                               MessageBoxButtons.YesNo,
                               MessageBoxIcon.Question,
                               MessageBoxDefaultButton.Button2) = Windows.Forms.DialogResult.No Then
                e.Cancel = True
                Return
            End If
        End If

        botcMain.QueueHook(Nothing)
        botcMain.Dispose()

        With bot
            bot = Nothing
            client = Nothing
            .QueueKill()
        End With
        My.Settings.Save()
    End Sub

    Private Sub btnSettings_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSettings.Click
        FrmSettings.ShowBotSettings(bot)
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
End Class
