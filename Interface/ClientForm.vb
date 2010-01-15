Imports System.Threading

<ContractVerification(False)>
Public Class ClientForm
    Private _bot As Bot.MainBot
    Private ReadOnly _exceptionForm As New ExceptionForm()

    Private Shadows Sub OnLoad() Handles Me.Load
        Contract.Assume(_bot Is Nothing)
        Try
            Thread.CurrentThread.Name = "UI Thread"
            Me.Text = Application.ProductName
            trayIcon.Text = Application.ProductName
            CacheIPAddresses()

            InitBot()
            InitMainControl()
            InitFinish()
            _exceptionForm.WindowState = FormWindowState.Minimized
            _exceptionForm.Show()
            _exceptionForm.Hide()
            Contract.Assume(_exceptionForm.IsHandleCreated)

            AddHandler _exceptionForm.ExceptionCountChanged, Sub() btnShowExceptionLog.Text = "Exception Log ({0})".Frmt(_exceptionForm.ExceptionCount)
            _bot.FutureDisposed.CallWhenReady(Sub() Me.BeginInvoke(Sub() Me.Dispose())).SetHandled()
        Catch ex As Exception
            MessageBox.Show("Error loading program: {0}.".Frmt(ex))
            Me.Close()
        End Try
    End Sub
    Private Sub InitBot()
        Contract.Requires(_bot Is Nothing)
        Contract.Ensures(_bot IsNot Nothing)

        _bot = New Bot.MainBot()
        _bot.Components.QueueAddComponent(New Bot.MainBotManager(_bot))

        'init port pool
        For Each port In SettingsForm.ParsePortList(My.Settings.port_pool, "")
            _bot.PortPool.TryAddPort(port)
        Next port

        'init settings
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
    End Sub
    Private Sub InitMainControl()
        Contract.Requires(_bot IsNot Nothing)
        Contract.Ensures(_bot Is Contract.OldValue(_bot))

        Dim componentsControl = New Tinker.Components.TabControl(_bot)
        componentsControl.Dock = DockStyle.Fill
        componentsControl.Name = "components"
        componentsControl.Height = btnSettings.Top - 3
        panelBotControl.Controls.Add(componentsControl)
        componentsControl.Focus()
    End Sub
    Private Sub InitFinish()
        Contract.Requires(_bot IsNot Nothing)

        Me.Show()

        LoadInitialPlugins()

        _bot.Logger.Log("---", LogMessageType.Typical)
        _bot.Logger.Log("Use the 'help' command for help.", LogMessageType.Typical)
        _bot.Logger.Log("---", LogMessageType.Typical)

        If My.Settings.war3path = "" Then
            My.Settings.war3path = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Warcraft III")
        End If
        If My.Settings.mapPath = "" Then
            My.Settings.mapPath = IO.Path.Combine(My.Settings.war3path, "Maps")
        End If
        If My.Settings.botstore = "" Then
            OnClickSettings()
        End If
    End Sub
    Private Sub LoadInitialPlugins()
        For Each pluginName In (From name In My.Settings.initial_plugins.Split(","c) Where name <> "").ToList
            Dim pluginName_ = pluginName
            Dim profile = (From p In _bot.Settings.PluginProfiles Where p.name = pluginName_).FirstOrDefault
            If profile Is Nothing Then
                _bot.Logger.Log("Failed to load plugin profile '{0}' because there is no profile with that name.".Frmt(pluginName), LogMessageType.Problem)
                Continue For
            End If

            Try
                Dim socket = New Plugins.Socket(profile.name, _bot, profile.location)
                Dim manager = New Plugins.PluginManager(socket)
                Dim added = _bot.Components.QueueAddComponent(manager)
                added.Catch(Sub(ex)
                                manager.Dispose()
                                _bot.Logger.Log("Failed to add plugin '{0}' to bot: {1}".Frmt(pluginName_, ex), LogMessageType.Problem)
                            End Sub)
                _bot.Logger.Log("Loaded plugin '{0}'.".Frmt(pluginName), LogMessageType.Positive)
            Catch ex As Exception
                _bot.Logger.Log("Failed to load plugin profile '{0}': {1}".Frmt(pluginName, ex.Message), LogMessageType.Problem)
                ex.RaiseAsUnexpected("Loading plugin profile '{0}'".Frmt(pluginName))
            End Try
        Next pluginName
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

    Private Sub OnClickSettings() Handles btnSettings.Click
        Dim cp As IEnumerable(Of Bot.ClientProfile) = _bot.Settings.ClientProfiles
        Dim pp As IEnumerable(Of Bot.PluginProfile) = _bot.Settings.PluginProfiles
        If Not SettingsForm.ShowWithProfiles(cp, pp, _bot.PortPool) Then Return
        _bot.Settings.UpdateProfiles(cp, pp)
        Using m = New IO.MemoryStream()
            Using w = New IO.BinaryWriter(m)
                _bot.Settings.WriteTo(w)
            End Using
            Dim data = m.ToArray
            Contract.Assume(data IsNot Nothing)
            My.Settings.botstore = data.ParseChrString(nullTerminated:=False)
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

    Private Sub btnShowExceptionLog_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnShowExceptionLog.Click
        _exceptionForm.Left = Me.Left + Me.Width \ 4
        _exceptionForm.Top = Me.Top + Me.Height \ 4
        _exceptionForm.Width = Me.Width \ 2
        _exceptionForm.Height = Me.Height \ 2
        _exceptionForm.Show()
        _exceptionForm.WindowState = FormWindowState.Normal
        _exceptionForm.Focus()
    End Sub
End Class
