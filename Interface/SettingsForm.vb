Imports System.IO.Path

Public Class SettingsForm
    Private ReadOnly _clientProfiles As List(Of ClientProfile)
    Private ReadOnly _pluginProfiles As List(Of Plugins.PluginProfile)
    Private ReadOnly _portPool As PortPool
    Private wantSave As Boolean

    Public Shared Function ShowWithProfiles(ByRef clientProfiles As IEnumerable(Of ClientProfile),
                                            ByRef pluginProfiles As IEnumerable(Of Plugins.PluginProfile),
                                            ByVal portPool As PortPool) As Boolean
        Contract.Requires(clientProfiles IsNot Nothing)
        Contract.Requires(pluginProfiles IsNot Nothing)
        Contract.Requires(portPool IsNot Nothing)
        Using f = New SettingsForm(clientProfiles, pluginProfiles, portPool)
            f.ShowDialog()
            If f.wantSave Then
                clientProfiles = f._clientProfiles.ToList
                pluginProfiles = f._pluginProfiles.ToList
            End If
            Return f.wantSave
        End Using
    End Function

    Public Sub New(ByVal clientProfiles As IEnumerable(Of ClientProfile),
                   ByVal pluginProfiles As IEnumerable(Of Plugins.PluginProfile),
                   ByVal portPool As PortPool)
        Contract.Requires(clientProfiles IsNot Nothing)
        Contract.Requires(pluginProfiles IsNot Nothing)
        Contract.Requires(portPool IsNot Nothing)
        InitializeComponent()
        Me._clientProfiles = clientProfiles.ToList
        Me._pluginProfiles = pluginProfiles.ToList
        Me._portPool = portPool

        For Each profile In _clientProfiles
            AddTabForProfile(profile)
        Next profile
        For Each profile In _pluginProfiles
            gridPlugins.Rows.Add(profile.name, profile.location, profile.argument)
        Next profile

        txtPortPool.Text = My.Settings.port_pool
        txtProgramPath.Text = My.Settings.war3path
        txtCdKeyOwner.Text = My.Settings.cdKeyOwner
        txtMapPath.Text = My.Settings.mapPath
        txtExeVersion.Text = My.Settings.exeVersion
        txtExeInformation.Text = My.Settings.exeInformation
        txtCommandPrefix.Text = My.Settings.commandPrefix
        numLagLimit.Value = My.Settings.game_lag_limit
        numTickPeriod.Value = CDec(My.Settings.game_tick_period).Between(numTickPeriod.Minimum, numTickPeriod.Maximum)
        txtInGameName.Text = My.Settings.ingame_name
        txtInitialPlugins.Text = My.Settings.initial_plugins
        txtBnlsServer.Text = My.Settings.bnls
        txtGreeting.Text = My.Settings.DefaultGameGreet
    End Sub

    Public Shared Function ParsePortList(ByVal text As String, ByRef refText As String) As IEnumerable(Of UShort)
        Dim ports As New List(Of UShort)
        Dim out_words As New List(Of String)
        For Each word In text.Replace(" "c, "").Split(","c)
            If word.Contains("-"c) Then
                Dim port1 As UShort, port2 As UShort
                Dim ranges = word.Split("-"c)
                If ranges.Length = 2 AndAlso UShort.TryParse(ranges(0), port1) AndAlso UShort.TryParse(ranges(1), port2) AndAlso port2 >= port1 Then
                    For port = port1 To port2
                        ports.Add(port)
                    Next port
                    out_words.Add(word)
                End If
            Else
                Dim port As UShort
                If UShort.TryParse(word, port) Then
                    ports.Add(port)
                    out_words.Add(word)
                End If
            End If
        Next word

        refText = String.Join(",", out_words.ToArray())
        Return ports
    End Function
    Private Function GetProfileWithName(ByVal name As InvariantString) As ClientProfile
        For Each profile In _clientProfiles
            If profile.name = name Then Return profile
        Next profile
        Return Nothing
    End Function
    Private Sub btnCreateNewProfile_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCreateNewProfile.Click
        Dim name = txtNewProfileName.Text
        name = name.Trim()
        If name = "" Then
            MessageBox.Show(text:="""{0}"" is not a valid profile name.".Frmt(name),
                            caption:="Notice",
                            buttons:=MessageBoxButtons.OK,
                            icon:=MessageBoxIcon.Exclamation)
            Return
        ElseIf GetProfileWithName(name) IsNot Nothing Then
            MessageBox.Show("The profile name ""{0}"" is already used.".Frmt(name), "Notice", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            Return
        ElseIf MessageBox.Show("Create profile '{0}'?".Frmt(name), "Confirm", MessageBoxButtons.YesNo) = Windows.Forms.DialogResult.No Then
            Return
        End If

        Dim newProfile = New ClientProfile(name)
        _clientProfiles.Add(newProfile)
        AddTabForProfile(newProfile)
    End Sub
    Private Sub AddTabForProfile(ByVal profile As ClientProfile)
        Dim tab = New TabPage("Profile:" + profile.name)
        Dim cntrl = New ProfileSettingsControl()
        cntrl.LoadFromProfile(profile)
        tab.Controls.Add(cntrl)
        cntrl.Dock = DockStyle.Fill
        With tabsSettings.TabPages
            .Add(tab)
            Dim t = .Item(.Count - 1)
            .Item(.Count - 1) = .Item(.Count - 2)
            .Item(.Count - 2) = t
        End With
        AddHandler cntrl.Delete, AddressOf remove_profile_tab
    End Sub
    Private Sub remove_profile_tab(ByVal sender As ProfileSettingsControl)
        If sender.lastLoadedProfile.name = "Default" Then
            MessageBox.Show("You can't delete the default profile.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            Return
        ElseIf MessageBox.Show("Delete profile '{0}'?".Frmt(sender.lastLoadedProfile.name), "Confirm", MessageBoxButtons.YesNo) = Windows.Forms.DialogResult.No Then
            Return
        End If

        For Each tab As TabPage In tabsSettings.TabPages
            If tab.Controls.Contains(sender) Then
                _clientProfiles.Remove(sender.lastLoadedProfile)
                tabsSettings.TabPages.Remove(tab)
                RemoveHandler sender.Delete, AddressOf remove_profile_tab
                Return
            End If
        Next tab
    End Sub

    Private Sub btnSave_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSave.Click
        _pluginProfiles.Clear()
        For Each row As DataGridViewRow In gridPlugins.Rows
            If row.Cells(0).Value Is Nothing Then Continue For
            Dim name = If(row.Cells(0).Value, "").ToString
            Dim path = If(row.Cells(1).Value, "").ToString
            Dim arg = If(row.Cells(2).Value, "").ToString
            If name.Trim = "" Then Continue For
            If path.Trim = "" Then Continue For
            _pluginProfiles.Add(New Plugins.PluginProfile(name, path, arg))
        Next row

        For Each tab As TabPage In tabsSettings.TabPages
            If tab Is tabGlobalSettings Then Continue For
            If tab Is tabNewProfile Then Continue For
            If tab Is tabPlugins Then Continue For
            For Each c As ProfileSettingsControl In tab.Controls
                c.SaveToProfile(c.lastLoadedProfile)
            Next c
        Next tab

        'Sync desired port pool with bot port pool
        Dim portPoolText = txtPortPool.Text
        Dim ports = ParsePortList(portPoolText, portPoolText)
        For Each port In Me._portPool.EnumPorts
            If Not ports.Contains(port) Then
                Me._portPool.TryRemovePort(port)
            End If
        Next port
        For Each port In ports
            Me._portPool.TryAddPort(port)
        Next port

        My.Settings.war3path = txtProgramPath.Text
        My.Settings.cdKeyOwner = txtCdKeyOwner.Text
        My.Settings.mapPath = txtMapPath.Text
        My.Settings.exeVersion = txtExeVersion.Text
        My.Settings.exeInformation = txtExeInformation.Text
        My.Settings.commandPrefix = txtCommandPrefix.Text
        My.Settings.game_lag_limit = CUShort(numLagLimit.Value)
        My.Settings.game_tick_period = CUShort(numTickPeriod.Value)
        My.Settings.ingame_name = txtInGameName.Text
        My.Settings.initial_plugins = txtInitialPlugins.Text
        My.Settings.port_pool = portPoolText
        My.Settings.bnls = txtBnlsServer.Text
        My.Settings.DefaultGameGreet = txtGreeting.Text

        My.Settings.Save()
        Me.wantSave = True

        Dispose()
    End Sub

    Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click
        Dispose()
    End Sub

    Private Sub btnUserHelp_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnUserHelp.Click
        MessageBox.Show(("Name: \n" _
                        + "\tThe user's name.\n" _
                    + "Permissions: \n" _
                        + "\tThe user's permissions. Permissions are 'word:number' values which grant the user capabilities.\n" _
                        + "\tPermissions typically range from 0 to 5.\n" _
                        + "\tExample: games:2,users:3\n" _
                    + "\n" _
                    + "Special User Names:\n" _
                        + "\t'*unknown' - Unknown User [users not in access list; no *unknown means ignore unknown users]\n" _
                        + "\t'*new' - New User [default values for new users]\n" _
                    + "Common Permissions [low values give fewer capabilities]:\n" _
                        + "\t'root' - Control important bot functions like connect/disconnect\n" _
                        + "\t'users' - Add/remove users from bot\n" _
                        + "\t'games' - Host, create and control games\n" _
                    + "").Replace("\n", Environment.NewLine).Replace("\t", vbTab))
    End Sub

    Private Sub btnPluginsHelp_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnPluginsHelp.Click
        MessageBox.Show(("Plugins placed in the 'available plugins' list can be loaded with the 'LoadPlugin' command, or loaded at startup.\n" _
                    + "Use the import button to browse for a dll file and copy it to the plugins folder.\n\n" _
                    + "Name: \n" _
                        + "\tThe name of the plugin. Used to referense the plugin in commands such as loadplugin.\n" _
                    + "Location: \n" _
                        + "\tThe location of the plugin's DLL.\n" _
                    + "").Replace("\n", Environment.NewLine).Replace("\t", vbTab))
    End Sub

    Private Sub btnImportPlugin_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnImportPlugin.Click
        With OpenFileDialog
            If Not IO.Directory.Exists(Application.StartupPath + IO.Path.DirectorySeparatorChar + "Plugins") Then
                IO.Directory.CreateDirectory(Application.StartupPath + IO.Path.DirectorySeparatorChar + "Plugins")
            End If

            .InitialDirectory = My.Settings.last_plugin_dir
            .Filter = "DLL Files (*.dll)|*.dll"
            Dim result = .ShowDialog()
            If result = Windows.Forms.DialogResult.OK Then
                Try
                    Dim path = .FileName
                    My.Settings.last_plugin_dir = IO.Path.GetDirectoryName(path)
                    Dim pluginName As InvariantString = IO.Path.GetFileNameWithoutExtension(path)
                    If pluginName.EndsWith("plugin") Then
                        pluginName = pluginName.Substring(0, pluginName.Length - "plugin".Length)
                    End If
                    Dim rel_path = "Plugins{0}{1}".Frmt(IO.Path.DirectorySeparatorChar, IO.Path.GetFileName(path))
                    Dim newPath = Application.StartupPath + IO.Path.DirectorySeparatorChar + rel_path
                    If newPath = path Then
                        gridPlugins.Rows.Add(pluginName, rel_path, "")
                    ElseIf IO.File.Exists(newPath) Then
                        If MessageBox.Show("There is already a plugin with that filename in the plugins folder. Do you want to replace it?", "Replace", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = Windows.Forms.DialogResult.No Then
                            Return
                        End If
                        IO.File.Delete(newPath)
                        IO.File.Copy(path, newPath)
                    Else
                        IO.File.Copy(path, newPath)
                        gridPlugins.Rows.Add(pluginName, rel_path, "")
                    End If
                Catch ex As Exception
                    ex.RaiseAsUnexpected("Importing plugin from settings form.")
                    MessageBox.Show("Error importing plugin: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            End If
        End With
    End Sub
End Class