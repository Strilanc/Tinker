Namespace Bot
    Public Class Settings
        Private _clientProfiles As IRist(Of ClientProfile) = MakeRist(Of ClientProfile)()
        Private _pluginProfiles As IRist(Of PluginProfile) = MakeRist(Of PluginProfile)()
        Private ReadOnly lock As New Object()

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_clientProfiles IsNot Nothing)
            Contract.Invariant(_pluginProfiles IsNot Nothing)
        End Sub

        Public Sub UpdateProfiles(clientProfiles As IEnumerable(Of Bot.ClientProfile),
                                  pluginProfiles As IEnumerable(Of Bot.PluginProfile))
            Contract.Requires(clientProfiles IsNot Nothing)
            Contract.Requires(pluginProfiles IsNot Nothing)
            SyncLock lock
                Me._clientProfiles = clientProfiles.ToRist
                Me._pluginProfiles = pluginProfiles.ToRist
            End SyncLock
        End Sub
        <Pure()>
        Public ReadOnly Property ClientProfiles() As IRist(Of Bot.ClientProfile)
            Get
                Contract.Ensures(Contract.Result(Of IRist(Of Bot.ClientProfile))() IsNot Nothing)
                Return _clientProfiles
            End Get
        End Property
        <Pure()>
        Public ReadOnly Property PluginProfiles() As IRist(Of Bot.PluginProfile)
            Get
                Contract.Ensures(Contract.Result(Of IRist(Of Bot.PluginProfile))() IsNot Nothing)
                Return _pluginProfiles
            End Get
        End Property

#Region "Serialization"
        Private Const FormatMagic As UInteger = 7352
        Private Const FormatVersion As UInteger = 0
        Private Const FormatMinReadCompatibleVersion As UInteger = 0
        Private Const FormatMinWriteCompatibleVersion As UInteger = 0
        Public Sub WriteTo(writer As IO.BinaryWriter)
            Contract.Requires(writer IsNot Nothing)

            SyncLock lock
                'Header
                writer.Write(FormatMagic)
                writer.Write(FormatVersion)
                writer.Write(FormatMinWriteCompatibleVersion)

                'Data
                writer.Write(CUInt(_clientProfiles.Count))
                For Each profile In _clientProfiles
                    Contract.Assume(profile IsNot Nothing)
                    profile.Save(writer)
                Next profile
                writer.Write(CUInt(_pluginProfiles.Count))
                For Each profile In _pluginProfiles
                    Contract.Assume(profile IsNot Nothing)
                    profile.Save(writer)
                Next profile
            End SyncLock
        End Sub
        Public Sub ReadFrom(reader As IO.BinaryReader)
            Contract.Requires(reader IsNot Nothing)

            SyncLock lock
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
                Dim clientProfileCount = reader.ReadUInt32()
                Dim clientProfiles = (From repeat In clientProfileCount.Range
                                      Select New Bot.ClientProfile(reader)
                                      ).ToList
                Dim pluginProfileCount = reader.ReadUInt32()
                Dim pluginProfiles = (From repeat In pluginProfileCount.Range
                                      Select New Bot.PluginProfile(reader)
                                      ).ToRist
                UpdateProfiles(clientProfiles, pluginProfiles)
            End SyncLock
        End Sub
#End Region
    End Class
End Namespace
