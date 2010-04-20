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
            Protected Overrides Function PerformInvoke(ByVal target As T, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                'Run download on a separate thread to avoid blocking anything
                Return ThreadedFunc(
                    Function()
                        Dim site = argument.NamedValue("site").ToInvariant
                        Dim mapId = argument.RawValue(0)
                        Select Case site
                            Case "epicwar"
                                Return DownloadEpicWar(mapId)
                            Case "mapgnome"
                                Throw New NotImplementedException("MapGnome support not implemented.")
                            Case Else
                                Throw New NotSupportedException("'{0}' is not a supported download site.".Frmt(site))
                        End Select
                    End Function
                )
            End Function
            Private Shared Function DownloadEpicWar(ByVal id As String) As String
                Dim path As String = Nothing
                Dim dlPath As String = Nothing
                Dim started = False
                Try
                    Dim http As New Net.WebClient()
                    Dim httpFile = http.DownloadString("http://epicwar.com/maps/{0}/".Frmt(id))
                    Contract.Assume(httpFile IsNot Nothing)

                    'Find download link
                    Dim i = httpFile.IndexOf("alt=""Download""", StringComparison.OrdinalIgnoreCase)
                    i = httpFile.IndexOf("a href=""", i, StringComparison.OrdinalIgnoreCase)
                    If i = -1 Then Throw New IO.InvalidDataException("Unrecognized page format from epicwar.")
                    i += "a href=""".Length
                    Dim j = httpFile.IndexOf(">", i, StringComparison.OrdinalIgnoreCase)
                    If j = -1 Then Throw New IO.InvalidDataException("Unrecognized page format from epicwar.")
                    Contract.Assume(i >= 0)
                    Contract.Assume(j >= i)
                    Contract.Assume(j < httpFile.Length)
                    Dim link = "http://epicwar.com{0}".Frmt(httpFile.Substring(i, j - i))
                    Contract.Assume(link.Length > 0)

                    'Find filename
                    i = httpFile.IndexOf("Download ", i, StringComparison.OrdinalIgnoreCase) + "Download ".Length
                    If i = -1 Then Throw New IO.InvalidDataException("Unrecognized page format from epicwar.")
                    j = httpFile.IndexOf("<", i, StringComparison.OrdinalIgnoreCase)
                    If j = -1 Then Throw New IO.InvalidDataException("Unrecognized page format from epicwar.")
                    Contract.Assume(i >= 0)
                    Contract.Assume(j >= i)
                    Contract.Assume(j < httpFile.Length)
                    Dim filename = httpFile.Substring(i, j - i)
                    path = My.Settings.mapPath.AssumeNotNull + filename
                    Contract.Assume(path.Length > 0)
                    dlPath = "{0}.dl".Frmt(path)
                    Contract.Assume(dlPath.Length > 0)

                    'Check for existing files
                    If IO.File.Exists(dlPath) Then
                        Throw New InvalidOperationException("A map with the filename '{0}' is already being downloaded.".Frmt(filename))
                    ElseIf IO.File.Exists(path) Then
                        Throw New InvalidOperationException("A map with the filename '{0}' already exists.".Frmt(filename))
                    End If

                    'Download
                    started = True
                    http.DownloadFile(link, dlPath)
                    IO.File.Move(dlPath, path)

                    'Finished
                    Return "Finished downloading map with filename '{0}'.".Frmt(filename)
                Catch e As Exception
                    If started Then
                        'cleanup
                        IO.File.Delete(dlPath)
                        IO.File.Delete(path)
                    End If
                    Throw
                End Try
            End Function
        End Class

        Public NotInheritable Class CommandFindMaps(Of T)
            Inherits Command(Of T)
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
