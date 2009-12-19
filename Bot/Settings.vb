Namespace Bot
    Public Class Settings
        Private _clientProfiles As New List(Of ClientProfile)
        Private _pluginProfiles As New List(Of PluginProfile)
        Private ReadOnly lock As New Object()

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_clientProfiles IsNot Nothing)
            Contract.Invariant(_pluginProfiles IsNot Nothing)
        End Sub

        Public Sub UpdateProfiles(ByVal clientProfiles As IEnumerable(Of Bot.ClientProfile),
                                  ByVal pluginProfiles As IEnumerable(Of Bot.PluginProfile))
            Contract.Requires(clientProfiles IsNot Nothing)
            Contract.Requires(pluginProfiles IsNot Nothing)
            SyncLock lock
                Me._clientProfiles = clientProfiles.ToList
                Me._pluginProfiles = pluginProfiles.ToList
            End SyncLock
        End Sub
        <Pure()>
        Public Function GetCopyOfClientProfiles() As IList(Of Bot.ClientProfile)
            Contract.Ensures(Contract.Result(Of IList(Of Bot.ClientProfile))() IsNot Nothing)
            SyncLock lock
                Return _clientProfiles.ToList
            End SyncLock
        End Function
        <Pure()>
        Public Function GetCopyOfPluginProfiles() As IList(Of Bot.PluginProfile)
            Contract.Ensures(Contract.Result(Of IList(Of Bot.PluginProfile))() IsNot Nothing)
            SyncLock lock
                Return _pluginProfiles.ToList
            End SyncLock
        End Function

#Region "Serialization"
        Private Const FormatMagic As UInteger = 7352
        Private Const FormatVersion As UInteger = 0
        Private Const FormatMinReadCompatibleVersion As UInteger = 0
        Private Const FormatMinWriteCompatibleVersion As UInteger = 0
        Public Sub WriteTo(ByVal writer As IO.BinaryWriter)
            Contract.Requires(writer IsNot Nothing)

            SyncLock lock
                'Header
                writer.Write(FormatMagic)
                writer.Write(FormatVersion)
                writer.Write(FormatMinWriteCompatibleVersion)

                'Data
                writer.Write(CUInt(_clientProfiles.Count))
                For Each profile In _clientProfiles
                    profile.Save(writer)
                Next profile
                writer.Write(CUInt(_pluginProfiles.Count))
                For Each profile In _pluginProfiles
                    profile.Save(writer)
                Next profile
            End SyncLock
        End Sub
        Public Sub ReadFrom(ByVal reader As IO.BinaryReader)
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
                _clientProfiles.Clear()
                For repeat = 1UI To reader.ReadUInt32()
                    Dim p = New Bot.ClientProfile(reader)
                    _clientProfiles.Add(p)
                Next repeat
                _pluginProfiles.Clear()
                For repeat = 1UI To reader.ReadUInt32()
                    Dim p = New Bot.PluginProfile(reader)
                    _pluginProfiles.Add(p)
                Next repeat
            End SyncLock
        End Sub
#End Region
    End Class
End Namespace
