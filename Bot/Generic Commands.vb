Imports Tinker.Commands

Namespace Bot
    Public NotInheritable Class GenericCommands
        Private Sub New()
        End Sub

        Public NotInheritable Class CommandRecacheIP(Of T)
            Inherits TemplatedCommand(Of T)
            Public Sub New()
                MyBase.New(Name:="RecacheIP",
                           template:="",
                           Description:="Recaches external and internal IP addresses",
                           Permissions:="root:5")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As T, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                CacheIPAddresses()
                Return "Recaching addresses.".AsTask
            End Function
        End Class

        Public NotInheritable Class CommandDownloadMap(Of T)
            Inherits TemplatedCommand(Of T)
            Public Sub New()
                MyBase.New(Name:="DownloadMap",
                           template:="mapid site={EpicWar,MapGnome}",
                           Description:="Downloads a map directly from a supported web site.",
                           Permissions:="root:2")
            End Sub
            <ContractVerification(False)>
            Protected Overrides Async Function PerformInvoke(ByVal target As T, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                Dim site = argument.NamedValue("site").ToInvariant
                Dim mapId = argument.RawValue(0)
                Select Case site
                    Case "epicwar"
                        Return Await DownloadEpicWarAsync(mapId)
                    Case "mapgnome"
                        Throw New NotImplementedException("MapGnome support not implemented.")
                    Case Else
                        Throw New NotSupportedException("'{0}' is not a supported download site.".Frmt(site))
                End Select
            End Function

            Private Shared Function TryExtractDownloadLink(ByVal html As String) As String
                Contract.Requires(html IsNot Nothing)

                Dim posDownload = html.IndexAfter("alt=""Download""", 0, StringComparison.OrdinalIgnoreCase)
                If posDownload Is Nothing Then Return Nothing

                Contract.Assume(posDownload.Value >= 0)
                Contract.Assume(posDownload.Value < html.Length)
                Dim posLink = html.IndexAfter("a href=""", posDownload.Value, StringComparison.OrdinalIgnoreCase)
                If posLink Is Nothing Then Return Nothing

                Contract.Assume(posLink.Value >= 0)
                Contract.Assume(posLink.Value < html.Length)
                Dim posLinkEnd = html.IndexOf(">", posLink.Value, StringComparison.OrdinalIgnoreCase)
                If posLinkEnd < 0 Then Return Nothing

                Return "http://epicwar.com{0}".Frmt(html.Substring(posLink.Value, posLinkEnd - posLink.Value))
            End Function
            Private Shared Function TryExtractEpicWarFilename(ByVal html As String) As String
                Contract.Requires(html IsNot Nothing)

                Dim posName = html.IndexAfter("Download ", 0, StringComparison.OrdinalIgnoreCase)
                If posName Is Nothing Then Return Nothing

                Contract.Assume(posName.Value >= 0)
                Contract.Assume(posName.Value < html.Length)
                Dim posNameEnd = html.IndexOf("<", posName.Value, StringComparison.OrdinalIgnoreCase)
                If posNameEnd < 0 Then Return Nothing

                Return html.Substring(posName.Value, posNameEnd - posName.Value)
            End Function
            Private Shared Async Function DownloadEpicWarAsync(ByVal id As String) As Task(Of String)
                Using http As New Net.WebClient()
                    Dim html = Await http.DownloadStringTaskAsync("http://epicwar.com/maps/{0}/".Frmt(id))
                    Contract.Assume(html IsNot Nothing)

                    'Extract info from page
                    Dim downloadLink = TryExtractDownloadLink(html)
                    Dim filename = TryExtractEpicWarFilename(html)
                    If downloadLink Is Nothing OrElse filename Is Nothing Then
                        Throw New IO.InvalidDataException("Unrecognized page format from epicwar.")
                    End If

                    'Check for existing files
                    Dim destPath = My.Settings.mapPath.AssumeNotNull + filename
                    Dim downloadTempPath = "{0}.dl".Frmt(destPath)
                    Contract.Assume(destPath.Length > 0)
                    Contract.Assume(downloadTempPath.Length > 0)
                    If IO.File.Exists(downloadTempPath) Then
                        Throw New InvalidOperationException("A map with the filename '{0}' is already being downloaded.".Frmt(filename))
                    ElseIf IO.File.Exists(destPath) Then
                        Throw New InvalidOperationException("A map with the filename '{0}' already exists.".Frmt(filename))
                    End If

                    'Download
                    Try
                        Await http.DownloadFileTaskAsync(downloadLink, downloadTempPath)
                        IO.File.Move(downloadTempPath, destPath)
                    Catch ex As Exception
                        IO.File.Delete(destPath)
                        Throw
                    Finally
                        IO.File.Delete(downloadTempPath)
                    End Try

                    Return "Finished downloading map with filename '{0}'.".Frmt(filename)
                End Using
            End Function
        End Class

        Public NotInheritable Class CommandFindMaps(Of T)
            Inherits BaseCommand(Of T)
            Public Sub New()
                MyBase.New(Name:="FindMaps",
                           Description:="Returns the first five maps matching a search query. The first match is the map used by other commands given the same query (eg. host).",
                           Format:="MapQuery...",
                           Permissions:="games:1")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As T, ByVal user As BotUser, ByVal argument As String) As Task(Of String)
                Dim results = FindFilesMatching(fileQuery:="*{0}*".Frmt(argument),
                                                likeQuery:="*.[wW]3[mxMX]",
                                                directory:=My.Settings.mapPath.AssumeNotNull,
                                                maxResults:=5)
                If results.Count = 0 Then Return "No matching maps.".AsTask
                Return results.StringJoin(", ").AsTask
            End Function
        End Class
    End Class
End Namespace
