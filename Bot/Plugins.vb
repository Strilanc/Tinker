Imports System.Reflection

Namespace Plugins
    <Serializable()>
    Public Class PluginException
        Inherits Exception
        Public Sub New(ByVal message As String, Optional ByVal innerException As Exception = Nothing)
            MyBase.New(message, innerException)
        End Sub
    End Class

    Public Class PluginProfile
        Public name As String
        Public location As String
        Public argument As String
        Private Const format_version As UInteger = 0
        Public Sub New(ByVal name As String, ByVal location As String, ByVal argument As String)
            Me.name = name
            Me.location = location
            Me.argument = argument
        End Sub
        Public Sub New(ByVal br As IO.BinaryReader)
            Dim ver = br.ReadUInt32()
            If ver > format_version Then Throw New IO.IOException("Saved PlayerRecord has an unrecognized format version.")
            name = br.ReadString()
            location = br.ReadString()
            argument = br.ReadString()
        End Sub
        Public Function save(ByVal bw As IO.BinaryWriter) As Boolean
            bw.Write(format_version)
            bw.Write(name)
            bw.Write(location)
            bw.Write(argument)
        End Function
    End Class

    Friend Class PluginManager
        Public ReadOnly bot As MainBot
        Public ReadOnly loaded_plugs As New List(Of Socket.Plug)
        Private ReadOnly sockets As New Dictionary(Of String, Socket)
        Public Event UnloadedPlugin(ByVal name As String, ByVal plugin As IPlugin, ByVal reason As String)

        Public Sub New(ByVal bot As MainBot)
            Me.bot = bot
        End Sub

        Friend Class Socket
            Public ReadOnly path As String
            Public ReadOnly asm As Assembly
            Public ReadOnly manager As PluginManager
            Public ReadOnly class_type As Type
            Public ReadOnly name As String

            Public Sub New(ByVal name As String, ByVal manager As PluginManager, ByVal path As String)
                Me.name = name
                Me.manager = manager
                Try
                    asm = Assembly.LoadFrom(path)
                    class_type = asm.GetType("HostBotPlugin")
                Catch e As Exception
                    Throw New PluginException("Error loading plugin assembly from '{1}'".frmt(path), e)
                End Try
            End Sub

            Public Function load_plugin() As plug
                Return New Plug(Me)
            End Function
            Private Sub unload_plugin(ByVal plug As Plug, ByVal reason As String)
                manager.unload_plugin(plug, reason)
            End Sub

            Friend Class Plug
                Implements IPlugout
                Implements IDisposable

                Public ReadOnly socket As Socket
                Public ReadOnly plugin As IPlugin
                Public Sub New(ByVal socket As Socket)
                    Me.socket = socket
                    Try
                        plugin = CType(Activator.CreateInstance(socket.class_type), IPlugin)
                        plugin.init(Me)
                    Catch e As Exception
                        Throw New PluginException("Error creating instance of plugin class: " + e.ToString, e)
                    End Try
                End Sub

                Public Function getBot() As MainBot Implements IPlugout.getBot
                    Return socket.manager.bot
                End Function
                Public Sub pull_the_plug(ByVal reason As String) Implements IPlugout.pull_the_plug
                    socket.unload_plugin(Me, reason)
                End Sub

#Region "IDisposable"
                Private disposedValue As Boolean = False
                Protected Overridable Sub Dispose(ByVal disposing As Boolean)
                    If Not Me.disposedValue Then
                        Me.disposedValue = True
                        If disposing Then
                            plugin.finish()
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

        Public Function LoadPlugin(ByVal name As String, ByVal path As String) As Outcome(Of IPlugin)
            Try
                If Not IO.File.Exists(path) Then
                    path = Application.StartupPath + IO.Path.DirectorySeparatorChar + path
                    If Not IO.File.Exists(path) Then
                        Return failure("No file exists at plugin's specified path.")
                    End If
                End If
                path = path.ToLower
                If Not sockets.ContainsKey(path) Then
                    sockets(path) = New Socket(name, Me, path)
                End If
                Dim plug = sockets(path).load_plugin()
                loaded_plugs.Add(plug)
                Return successVal(plug.plugin, "Loaded plugin succesfully. Plugin Description is: '" + plug.plugin.description + "'")
            Catch e As Exception
                Return failure("Error loading plugin: " + e.ToString)
            End Try
        End Function
        Private Function unload_plugin(ByVal plug As Socket.Plug, ByVal reason As String) As Outcome
            If Not loaded_plugs.Contains(plug) Then Return failure("That plugin is not loaded.")
            plug.Dispose()
            RaiseEvent UnloadedPlugin(plug.socket.name, plug.plugin, reason)
            Return success("Unloaded plugin.")
        End Function
        Public Function unload_plugin(ByVal name As String, ByVal reason As String) As Outcome
            Dim plug = (From x In loaded_plugs Where x.socket.name.ToLower = name.ToLower).FirstOrDefault
            If plug Is Nothing Then Return failure("No loaded plugin by that name.")
            Return unload_plugin(plug, reason)
        End Function

        Public Sub finish()
            For Each pi In loaded_plugs
                pi.Dispose()
            Next pi
            loaded_plugs.Clear()
            sockets.Clear()
        End Sub
    End Class

    Public Interface IPlugout
        Function getBot() As MainBot
        Sub pull_the_plug(ByVal reason As String)
    End Interface

    Public Interface IPlugin
        Sub init(ByVal plugout As IPlugout)
        Sub finish()
        Function description() As String
    End Interface
End Namespace
