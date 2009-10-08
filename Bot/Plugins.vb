Imports System.Reflection

Namespace Plugins
    <Serializable()>
    Public NotInheritable Class PluginException
        Inherits Exception
        Public Sub New(ByVal message As String, Optional ByVal innerException As Exception = Nothing)
            MyBase.New(message, innerException)
        End Sub
    End Class

    Public NotInheritable Class PluginProfile
        Public name As String
        Public location As String
        Public argument As String
        Private Const format_version As UInteger = 0
        Public Sub New(ByVal name As String, ByVal location As String, ByVal argument As String)
            Me.name = name
            Me.location = location
            Me.argument = argument
        End Sub
        Public Sub New(ByVal reader As IO.BinaryReader)
            Dim ver = reader.ReadUInt32()
            If ver > format_version Then Throw New IO.IOException("Saved PlayerRecord has an unrecognized format version.")
            name = reader.ReadString()
            location = reader.ReadString()
            argument = reader.ReadString()
        End Sub
        Public Function Save(ByVal writer As IO.BinaryWriter) As Boolean
            writer.Write(format_version)
            writer.Write(name)
            writer.Write(location)
            writer.Write(argument)
        End Function
    End Class

    Friend Class PluginManager
        Public ReadOnly bot As MainBot
        Public ReadOnly loadedPlugins As New List(Of Socket.Plug)
        Private ReadOnly sockets As New Dictionary(Of String, Socket)
        Public Event UnloadedPlugin(ByVal name As String, ByVal plugin As IPlugin, ByVal reason As String)

        Public Sub New(ByVal bot As MainBot)
            Me.bot = bot
        End Sub

        Friend Class Socket
            Public ReadOnly path As String
            Public ReadOnly asm As Assembly
            Public ReadOnly manager As PluginManager
            Public ReadOnly classType As Type
            Public ReadOnly name As String

            Public Sub New(ByVal name As String, ByVal manager As PluginManager, ByVal path As String)
                Me.name = name
                Me.path = path
                Me.manager = manager
                Try
                    asm = Assembly.LoadFrom(path)
                    classType = asm.GetType("HostBotPlugin")
                Catch e As Exception
                    Throw New PluginException("Error loading plugin assembly from '{1}'".frmt(path), e)
                End Try
            End Sub

            Public Function LoadPlugin() As plug
                Return New Plug(Me)
            End Function
            Private Sub UnloadPlugin(ByVal plug As Plug, ByVal reason As String)
                manager.UnloadPlugin(plug, reason)
            End Sub

            Friend Class Plug
                Implements IPlugout
                Implements IDisposable

                Public ReadOnly socket As Socket
                Public ReadOnly plugin As IPlugin
                Public Sub New(ByVal socket As Socket)
                    Me.socket = socket
                    Try
                        plugin = CType(Activator.CreateInstance(socket.classType), IPlugin)
                        plugin.Init(Me)
                    Catch e As Exception
                        Throw New PluginException("Error creating instance of plugin class: " + e.ToString, e)
                    End Try
                End Sub

                Public ReadOnly Property Bot() As MainBot Implements IPlugout.Bot
                    Get
                        Return socket.manager.bot
                    End Get
                End Property
                Public Sub DisposePlugin(ByVal reason As String) Implements IPlugout.DisposePlugin
                    socket.UnloadPlugin(Me, reason)
                End Sub

#Region "IDisposable"
                Private disposedValue As Boolean = False
                Protected Overridable Sub Dispose(ByVal disposing As Boolean)
                    If Not Me.disposedValue Then
                        Me.disposedValue = True
                        If disposing Then
                            plugin.Dispose()
                        End If
                    End If
                End Sub
                ' This code added by Visual Basic to correctly implement the disposable pattern.
                Public Sub Dispose() Implements IDisposable.Dispose
                    ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
                    Dispose(True)
                    GC.SuppressFinalize(Me)
                End Sub
#End Region
            End Class
        End Class

        Public Function LoadPlugin(ByVal name As String, ByVal path As String) As IPlugin
            If Not IO.File.Exists(path) Then
                path = Application.StartupPath + IO.Path.DirectorySeparatorChar + path
                If Not IO.File.Exists(path) Then
                    Throw New IO.IOException("No plugin exists at the specified path.")
                End If
            End If
            path = path.ToUpperInvariant
            If Not sockets.ContainsKey(path) Then
                sockets(path) = New Socket(name, Me, path)
            End If
            Dim plug = sockets(path).LoadPlugin()
            loadedPlugins.Add(plug)
            Return plug.plugin
        End Function
        Private Sub UnloadPlugin(ByVal plug As Socket.Plug, ByVal reason As String)
            If Not loadedPlugins.Contains(plug) Then Throw New InvalidOperationException("No such plugin loaded.")
            plug.Dispose()
            RaiseEvent UnloadedPlugin(plug.socket.name, plug.plugin, reason)
        End Sub
        Public Sub UnloadPlugin(ByVal name As String, ByVal reason As String)
            Dim plug = (From x In loadedPlugins
                        Where x.socket.name.ToUpperInvariant = name.ToUpperInvariant).FirstOrDefault
            If plug Is Nothing Then Throw New InvalidOperationException("No such plugin loaded.")
            UnloadPlugin(plug, reason)
        End Sub

        Public Sub finish()
            For Each pi In loadedPlugins
                pi.Dispose()
            Next pi
            loadedPlugins.Clear()
            sockets.Clear()
        End Sub
    End Class

    Public Interface IPlugout
        ReadOnly Property Bot() As MainBot
        Sub DisposePlugin(ByVal reason As String)
    End Interface

    Public Interface IPlugin
        Inherits IDisposable
        Sub Init(ByVal plugout As IPlugout)
        ReadOnly Property Description() As String
    End Interface
End Namespace
