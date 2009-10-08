Imports System.Threading
Imports HostBot.Bnet

Public Class ClientForm
    Private WithEvents bot As MainBot
    Private WithEvents client As BnetClient

    Private Shadows Sub OnLoad() Handles Me.Load
        Try
            'prep form
            Thread.CurrentThread.Name = "UI Thread"
            Me.Text = My.Application.Info.ProductName
            trayIcon.Text = My.Application.Info.ProductName

            'prep bot
            CacheIPAddresses()
            bot = New MainBot()
            For Each port In SettingsForm.ParsePortList(My.Settings.port_pool, "")
                bot.PortPool.TryAddPort(port)
            Next port
            botcMain.logBot.SetLogUnexpected(True)
            botcMain.QueueHook(bot)

            Me.Show()

            'load initial plugins
            Dim pluginNames = (From x In My.Settings.initial_plugins.Split(";"c) Where x <> "").ToList
            Dim futureLoadedPlugins = (From x In pluginNames Select bot.QueueLoadPlugin(x)).ToList
            Dim t = New ManualResetEvent(False)
            futureLoadedPlugins.Defuturized().CallWhenReady(Sub(loadException) t.Set())
            t.WaitOne()
            Dim pluginLoadOutcomes = (From x In futureLoadedPlugins
                                      Select plugin = x.TryGetValue,
                                             Exception = x.TryGetException).ToList
            For i = 0 To pluginNames.Count - 1
                Dim pluginName = pluginNames(i)
                Dim pluginOutcome = pluginLoadOutcomes(i)

                If pluginOutcome.Exception Is Nothing Then
                    bot.logger.Log("Loaded plugin '{0}'.".Frmt(pluginName), LogMessageType.Positive)
                Else
                    bot.logger.Log("Failed to load plugin '{0}': {1}".Frmt(pluginName, pluginOutcome.Exception), LogMessageType.Problem)
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
                ShowSettings()
            End If
        Catch ex As Exception
            MessageBox.Show("Error loading program: {0}.".Frmt(ex))
            Me.Close()
        End Try
    End Sub

    Private Shadows Sub OnClosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing
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

    Private Sub ShowSettings() Handles btnSettings.Click
        SettingsForm.ShowBotSettings(bot)
    End Sub

    Private Sub ShowHide() Handles mnuShowHide.Click, trayIcon.MouseDoubleClick
        mnuShowHide.Checked = Not mnuShowHide.Checked
        If mnuShowHide.Checked Then
            Me.Show()
            If Me.WindowState = FormWindowState.Minimized Then Me.WindowState = FormWindowState.Normal
        Else
            Me.Hide()
        End If
    End Sub

    Private Sub OnMenuClickClose() Handles mnuClose.Click
        Me.Close()
    End Sub
End Class
