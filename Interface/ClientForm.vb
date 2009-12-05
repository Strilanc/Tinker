Imports System.Threading

Public Class ClientForm
    Private _bot As MainBot
    Private ReadOnly _clientProfiles As New Dictionary(Of InvariantString, ClientProfile)
    Private ReadOnly _pluginProfiles As New Dictionary(Of InvariantString, Plugins.PluginProfile)
    Private ReadOnly _loadedPlugins As New Plugins.PluginSet

    Private Shadows Sub OnLoad() Handles Me.Load
        Try
            'prep form
            Thread.CurrentThread.Name = "UI Thread"
            Me.Text = Application.ProductName
            trayIcon.Text = Application.ProductName

            'Read profile data
            Dim serializedData = My.Settings.botstore
            If serializedData IsNot Nothing AndAlso serializedData <> "" Then
                Try
                    Using r = New IO.BinaryReader(New IO.MemoryStream(serializedData.ToAscBytes))
                        LoadProfiles(r)
                    End Using
                Catch e As Exception
                    _pluginProfiles.Clear()
                    _clientProfiles.Clear()
                    Dim p = New ClientProfile("Default")
                    _clientProfiles.Add(p.name, p)
                    e.RaiseAsUnexpected("Error loading profiles.")
                End Try
            Else
                Dim p = New ClientProfile("Default")
                p.users.AddUser(New BotUser(BotUserSet.NewUserKey, "games:1"))
                _clientProfiles.Add(p.name, p)
            End If

            'prep bot
            CacheIPAddresses()
            Dim portPool = New PortPool
            For Each port In SettingsForm.ParsePortList(My.Settings.port_pool, "")
                portPool.TryAddPort(port)
            Next port

            'Load bot
            _bot = New MainBot(portPool)
            Dim botManager = New Components.MainBotManager(_bot)
            Dim componentsControl = New Tinker.ComponentsControl(_bot)
            _bot.QueueAddComponent(botManager)
            componentsControl.Anchor = System.Windows.Forms.AnchorStyles.Top Or
                                       System.Windows.Forms.AnchorStyles.Bottom Or
                                       System.Windows.Forms.AnchorStyles.Left Or
                                       System.Windows.Forms.AnchorStyles.Right
            componentsControl.Location = New System.Drawing.Point(0, 0)
            componentsControl.Name = "components"
            componentsControl.Size = New System.Drawing.Size(1005, 400)
            componentsControl.Focus()
            Me.Controls.Add(componentsControl)
            _bot.QueueUpdateProfiles(_clientProfiles.Values.ToList, _pluginProfiles.Values.ToList)

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
            If _pluginProfiles.ContainsKey(Name) Then
                Dim profile = _pluginProfiles(Name)
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
        Dim cp As IEnumerable(Of ClientProfile) = _clientProfiles.Values
        Dim pp As IEnumerable(Of Plugins.PluginProfile) = _pluginProfiles.Values
        If Not SettingsForm.ShowWithProfiles(cp, pp, _bot.PortPool) Then Return
        _clientProfiles.Clear()
        _pluginProfiles.Clear()
        For Each p In cp
            _clientProfiles(p.name) = p
        Next p
        For Each p In pp
            _pluginProfiles(p.name) = p
        Next p
        Using m = New IO.MemoryStream()
            Using w = New IO.BinaryWriter(m)
                SaveProfiles(w)
            End Using
            My.Settings.botstore = m.ToArray().ParseChrString(nullTerminated:=False)
        End Using
        _bot.QueueUpdateProfiles(_clientProfiles.Values.ToList, _pluginProfiles.Values.ToList)
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

#Region "Profile Serialization"
    Private Const FormatMagic As UInteger = 7352
    Private Const FormatVersion As UInteger = 0
    Private Const FormatMinReadCompatibleVersion As UInteger = 0
    Private Const FormatMinWriteCompatibleVersion As UInteger = 0
    Private Sub SaveProfiles(ByVal writer As IO.BinaryWriter)
        'Header
        writer.Write(FormatMagic)
        writer.Write(FormatVersion)
        writer.Write(FormatMinWriteCompatibleVersion)

        'Data
        writer.Write(CUInt(_clientProfiles.Count))
        For Each profile In _clientProfiles.Values
            profile.Save(writer)
        Next profile
        writer.Write(CUInt(_pluginProfiles.Count))
        For Each profile In _pluginProfiles.Values
            profile.Save(writer)
        Next profile
    End Sub
    Private Sub LoadProfiles(ByVal reader As IO.BinaryReader)
        'Header
        Dim writerMagic = reader.ReadUInt32()
        Dim writerFormatVersion = reader.ReadUInt32()
        Dim writerMinFormatVersion = reader.ReadUInt32()
        If writerMagic <> FormatMagic Then
            Throw New IO.InvalidDataException("Corrupted profile data.")
        ElseIf writerFormatVersion < FormatMinReadCompatibleVersion Then
            Throw New IO.InvalidDataException("Profile data is saved in an earlier non-forwards-compatible version.")
        ElseIf writerMinFormatVersion > FormatVersion Then
            Throw New IO.InvalidDataException("Profile data is saved in a later non-backwards-compatible format.")
        End If

        'Data
        _clientProfiles.Clear()
        For repeat = 1UI To reader.ReadUInt32()
            Dim p = New ClientProfile(reader)
            _clientProfiles.Add(p.name, p)
        Next repeat
        _pluginProfiles.Clear()
        For repeat = 1UI To reader.ReadUInt32()
            Dim p = New Plugins.PluginProfile(reader)
            _pluginProfiles.Add(p.name, p)
        Next repeat
    End Sub
#End Region
End Class
