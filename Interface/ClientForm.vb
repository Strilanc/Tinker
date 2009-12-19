Imports System.Threading

Public Class ClientForm
    Private _bot As MainBot
    Private ReadOnly _loadedPlugins As New Plugins.PluginSet

    Private Shadows Sub OnLoad() Handles Me.Load
        Try
            'prep form
            Thread.CurrentThread.Name = "UI Thread"
            Me.Text = Application.ProductName
            trayIcon.Text = Application.ProductName

            'prep bot
            CacheIPAddresses()
            Dim portPool = New PortPool
            For Each port In SettingsForm.ParsePortList(My.Settings.port_pool, "")
                portPool.TryAddPort(port)
            Next port

            'Load bot
            _bot = New MainBot(portPool)
            Dim serializedData = My.Settings.botstore
            If serializedData IsNot Nothing AndAlso serializedData <> "" Then
                Try
                    Using r = New IO.BinaryReader(New IO.MemoryStream(serializedData.ToAscBytes))
                        _bot.Settings.ReadFrom(r)
                    End Using
                Catch e As Exception
                    _bot.Settings.UpdateProfiles({New Bot.ClientProfile("Default")}, {})
                    e.RaiseAsUnexpected("Error loading profiles.")
                End Try
            Else
                _bot.Settings.UpdateProfiles({New Bot.ClientProfile("Default")}, {})
            End If
            Dim botManager = New Components.MainBotManager(_bot)
            Dim componentsControl = New Tinker.ComponentsControl(_bot)
            _bot.Components.QueueAddComponent(botManager)
            componentsControl.Anchor = System.Windows.Forms.AnchorStyles.Top Or
                                       System.Windows.Forms.AnchorStyles.Bottom Or
                                       System.Windows.Forms.AnchorStyles.Left Or
                                       System.Windows.Forms.AnchorStyles.Right
            componentsControl.Location = New System.Drawing.Point(0, 0)
            componentsControl.Name = "components"
            componentsControl.Size = New System.Drawing.Size(1005, 400)
            componentsControl.Focus()
            Me.Controls.Add(componentsControl)

            'show
            Me.Show()
            LoadInitialPlugins()
            _bot.Logger.Log("---", LogMessageType.Typical)
            _bot.Logger.Log("Use the 'help' command for help.", LogMessageType.Typical)
            _bot.Logger.Log("---", LogMessageType.Typical)
            If My.Settings.war3path = "" Then
                My.Settings.war3path = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Warcraft III", "")
            End If
            If My.Settings.mapPath = "" Then
                My.Settings.mapPath = IO.Path.Combine(My.Settings.war3path, "Maps", "")
            End If
            If My.Settings.botstore = "" Then
                ShowSettings()
            End If

            _bot.FutureDisposed.CallWhenReady(Sub() Me.BeginInvoke(Sub() Me.Dispose())).SetHandled()
        Catch ex As Exception
            MessageBox.Show("Error loading program: {0}.".Frmt(ex))
            Me.Close()
        End Try
    End Sub

    Private Sub LoadInitialPlugins()
        'Start loading
        Dim pluginNames = (From name In My.Settings.initial_plugins.Split(";"c) Where name <> "").ToList
        Dim futureLoadedPlugins = New List(Of IFuture(Of Plugins.PluginSocket))()
        For Each pluginName In pluginNames
            Dim pluginName_ = pluginName
            Dim profile = (From p In _bot.Settings.GetCopyOfPluginProfiles Where p.name = pluginName_).FirstOrDefault
            If profile IsNot Nothing Then
                futureLoadedPlugins.Add(_loadedPlugins.QueueLoadPlugin(profile.name, profile.location, _bot))
            Else
                Dim f = New FutureFunction(Of Plugins.PluginSocket)
                f.SetFailed(New InvalidOperationException("No plugin profile named '{0}'.".Frmt(pluginName)))
                futureLoadedPlugins.Add(f)
            End If
        Next pluginName

        'Wait
        Dim t = New ManualResetEvent(False)
        futureLoadedPlugins.Defuturized().CallWhenReady(Sub(loadException) t.Set())
        t.WaitOne()

        'Report problems
        For i = 0 To pluginNames.Count - 1
            If futureLoadedPlugins(i).State = FutureState.Succeeded Then
                _bot.Logger.Log("Loaded plugin '{0}'.".Frmt(pluginNames(i)), LogMessageType.Positive)
            Else
                _bot.Logger.Log("Failed to load plugin '{0}': {1}".Frmt(pluginNames(i), futureLoadedPlugins(i).Exception), LogMessageType.Problem)
            End If
        Next i
    End Sub

    Private Shadows Sub OnClosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing
        If _bot Is Nothing Then Return

        If e.CloseReason = CloseReason.UserClosing Then
            If MessageBox.Show(text:="Are you sure you want to close {0}?".Frmt(Application.ProductName),
                               caption:="Confirm Close",
                               buttons:=MessageBoxButtons.YesNo,
                               icon:=MessageBoxIcon.Question,
                               defaultButton:=MessageBoxDefaultButton.Button2) = Windows.Forms.DialogResult.No Then
                e.Cancel = True
                Return
            End If
        End If

        _bot.Dispose()
        My.Settings.Save()
    End Sub

    Private Sub ShowSettings() Handles btnSettings.Click
        Dim cp As IEnumerable(Of Bot.ClientProfile) = _bot.Settings.GetCopyOfClientProfiles
        Dim pp As IEnumerable(Of Bot.PluginProfile) = _bot.Settings.GetCopyOfPluginProfiles
        If Not SettingsForm.ShowWithProfiles(cp, pp, _bot.PortPool) Then Return
        _bot.Settings.UpdateProfiles(cp, pp)
        Using m = New IO.MemoryStream()
            Using w = New IO.BinaryWriter(m)
                _bot.Settings.WriteTo(w)
            End Using
            My.Settings.botstore = m.ToArray().ParseChrString(nullTerminated:=False)
        End Using
    End Sub

    Private Sub OnMenuClickRestore() Handles mnuRestore.Click, trayIcon.MouseDoubleClick
        Me.Show()
        If Me.WindowState = FormWindowState.Minimized Then Me.WindowState = FormWindowState.Normal
        trayIcon.Visible = False
    End Sub

    Private Sub OnMenuClickClose() Handles mnuClose.Click
        Me.Close()
    End Sub

    Private Sub OnClickMinimizeToTray() Handles btnMinimizeToTray.Click
        trayIcon.Visible = True
        Me.Visible = False
    End Sub
End Class
