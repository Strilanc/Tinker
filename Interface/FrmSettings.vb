Imports System.IO.Path

Public Class FrmSettings
    Private bot As MainBot
    Dim profiles As List(Of ClientProfile)

    Public Shared Sub ShowBotSettings(ByVal bot As MainBot)
        Using f = New FrmSettings
            f.load_from_bot(bot)
            f.ShowDialog()
        End Using
    End Sub

    Public Sub load_from_bot(ByVal bot As MainBot)
        Me.bot = bot
        Me.profiles = bot.clientProfiles.ToList
        For Each profile In profiles
            add_profile_tab(profile)
        Next profile
        For Each profile In bot.pluginProfiles
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
    End Sub

    Public Shared Function parse_port_list(ByVal text As String, ByRef out_text As String) As IEnumerable(Of UShort)
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

        out_text = String.Join(",", out_words.ToArray())
        Return ports
    End Function
    Private Function get_profile_with_name(ByVal name As String) As ClientProfile
        For Each profile In profiles
            If profile.name.ToLower = name.ToLower Then Return profile
        Next profile
        Return Nothing
    End Function
    Private Sub btnCreateNewProfile_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCreateNewProfile.Click
        Dim name = txtNewProfileName.Text
        name = name.Trim()
        If name = "" Then
            MessageBox.Show("""{0}"" is not a valid profile name.".frmt(name), "Notice", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            Return
        ElseIf get_profile_with_name(name) IsNot Nothing Then
            MessageBox.Show("The profile name ""{0}"" is already used.".frmt(name), "Notice", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            Return
        ElseIf MessageBox.Show("Create profile '{0}'?".frmt(name), "Confirm", MessageBoxButtons.YesNo) = Windows.Forms.DialogResult.No Then
            Return
        End If

        Dim new_profile = New ClientProfile(name)
        profiles.Add(new_profile)
        add_profile_tab(new_profile)
    End Sub
    Private Sub add_profile_tab(ByVal profile As ClientProfile)
        Dim tab = New TabPage("Profile:" + profile.name)
        Dim cntrl = New ProfileSettingsControl()
        cntrl.load_from_profile(profile)
        tab.Controls.Add(cntrl)
        cntrl.Dock = DockStyle.Fill
        With tabsSettings.TabPages
            .Add(tab)
            Dim t = .Item(.Count - 1)
            .Item(.Count - 1) = .Item(.Count - 2)
            .Item(.Count - 2) = t
        End With
        AddHandler cntrl.delete, AddressOf remove_profile_tab
    End Sub
    Private Sub remove_profile_tab(ByVal sender As ProfileSettingsControl)
        If sender.last_loaded_profile.name = "Default" Then
            MessageBox.Show("You can't delete the default profile.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            Return
        ElseIf MessageBox.Show("Delete profile '{0}'?".frmt(sender.last_loaded_profile.name), "Confirm", MessageBoxButtons.YesNo) = Windows.Forms.DialogResult.No Then
            Return
        End If

        For Each tab As TabPage In tabsSettings.TabPages
            If tab.Controls.Contains(sender) Then
                profiles.Remove(sender.last_loaded_profile)
                tabsSettings.TabPages.Remove(tab)
                RemoveHandler sender.delete, AddressOf remove_profile_tab
                Return
            End If
        Next tab
    End Sub

    Private Sub btnSave_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSave.Click
        bot.clientProfiles.Clear()
        For Each profile In profiles
            bot.clientProfiles.Add(profile)
        Next profile
        bot.pluginProfiles.Clear()
        For Each row As DataGridViewRow In gridPlugins.Rows
            If row.Cells(0).Value Is Nothing Then Continue For
            Dim name = If(row.Cells(0).Value, "").ToString
            Dim path = If(row.Cells(1).Value, "").ToString
            Dim arg = If(row.Cells(2).Value, "").ToString
            If name.Trim = "" Then Continue For
            If path.Trim = "" Then Continue For
            bot.pluginProfiles.Add(New Plugins.PluginProfile(name, path, arg))
        Next row

        For Each tab As TabPage In tabsSettings.TabPages
            If tab Is tabGlobalSettings Then Continue For
            If tab Is tabNewProfile Then Continue For
            If tab Is tabPlugins Then Continue For
            For Each c As ProfileSettingsControl In tab.Controls
                c.save_to_profile(c.last_loaded_profile)
            Next c
        Next tab

        'Sync desired port pool with bot port pool
        Dim port_pool_text = txtPortPool.Text
        Dim ports = parse_port_list(port_pool_text, port_pool_text)
        For Each port In bot.portPool.EnumPorts
            If Not ports.Contains(port) Then
                bot.portPool.TryRemovePort(port)
            End If
        Next port
        For Each port In ports
            bot.portPool.TryAddPort(port)
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
        My.Settings.port_pool = port_pool_text

        Using m As New IO.MemoryStream()
            Using w As New IO.BinaryWriter(m)
                bot.Save(w)
            End Using
            My.Settings.botstore = m.ToArray().ParseChrString(nullTerminated:=False)
        End Using

        My.Settings.Save()

        Dispose()
    End Sub

    Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click
        Dispose()
    End Sub

    Private Sub btnUserHelp_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnUserHelp.Click
        MessageBox.Show(("Name: \n" _
                        + "\tThe user's name.\n" _
                    + "Permissions: \n" _
                        + "\tThe user's permissions. Permissions are 'word=number' values which grant the user capabilities.\n" _
                        + "\tPermissions typically range from 0 to 5.\n" _
                        + "\tExample: games=2;users=3\n" _
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
        MessageBox.Show(("Plugins placed in the 'available plugins' list can be loaded with the 'loadplugin' command, or loaded at startup.\n" _
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
                    Dim plugin_name = IO.Path.GetFileNameWithoutExtension(path)
                    If plugin_name.Substring(plugin_name.Length - "plugin".Length).ToLower = "plugin" Then
                        plugin_name = plugin_name.Substring(0, plugin_name.Length - "plugin".Length)
                    End If
                    Dim rel_path = "Plugins{0}{1}".frmt(IO.Path.DirectorySeparatorChar, IO.Path.GetFileName(path))
                    Dim new_path = Application.StartupPath + IO.Path.DirectorySeparatorChar + rel_path
                    If new_path = path Then
                        gridPlugins.Rows.Add(plugin_name, rel_path, "")
                    ElseIf IO.File.Exists(new_path) Then
                        If MessageBox.Show("There is already a plugin with that filename in the plugins folder. Do you want to replace it?", "Replace", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = Windows.Forms.DialogResult.No Then
                            Return
                        End If
                        IO.File.Delete(new_path)
                        IO.File.Copy(path, new_path)
                    Else
                        IO.File.Copy(path, new_path)
                        gridPlugins.Rows.Add(plugin_name, rel_path, "")
                    End If
                Catch ex As Exception
                    Logging.logUnexpectedException("Importing plugin from settings form.", ex)
                    MessageBox.Show("Error importing plugin: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            End If
        End With
    End Sub
End Class