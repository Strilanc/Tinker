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

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(name IsNot Nothing)
            Contract.Invariant(location IsNot Nothing)
            Contract.Invariant(argument IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As String, ByVal location As String, ByVal argument As String)
            Contract.Requires(name IsNot Nothing)
            Contract.Requires(location IsNot Nothing)
            Contract.Requires(argument IsNot Nothing)
            Me.name = name
            Me.location = location
            Me.argument = argument
        End Sub
        Public Sub New(ByVal reader As IO.BinaryReader)
            Contract.Requires(reader IsNot Nothing)
            Dim ver = reader.ReadUInt32()
            If ver > format_version Then Throw New IO.IOException("Saved PlayerRecord has an unrecognized format version.")
            name = reader.ReadString()
            location = reader.ReadString()
            argument = reader.ReadString()
        End Sub
        Public Sub Save(ByVal writer As IO.BinaryWriter)
            Contract.Requires(writer IsNot Nothing)
            writer.Write(format_version)
            writer.Write(name)
            writer.Write(location)
            writer.Write(argument)
        End Sub
    End Class

    Friend Class PluginManager
        Public ReadOnly _bot As MainBot
        Public ReadOnly _loadedPlugins As New List(Of Socket.Plug)
        Private ReadOnly sockets As New Dictionary(Of String, Socket)
        Public Event UnloadedPlugin(ByVal name As String, ByVal plugin As IPlugin, ByVal reason As String)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_loadedPlugins IsNot Nothing)
            Contract.Invariant(_bot IsNot Nothing)
            Contract.Invariant(sockets IsNot Nothing)
        End Sub
        Public ReadOnly Property Bot As MainBot
            Get
                Contract.Ensures(Contract.Result(Of MainBot)() IsNot Nothing)
                Return _bot
            End Get
        End Property
        Public ReadOnly Property LoadedPlugins As List(Of Socket.Plug)
            Get
                Contract.Ensures(Contract.Result(Of List(Of Socket.Plug))() IsNot Nothing)
                Return _loadedPlugins
            End Get
        End Property

        Public Sub New(ByVal bot As MainBot)
            Contract.Requires(bot IsNot Nothing)
            Me._bot = bot
        End Sub

        Friend Class Socket
            Public ReadOnly path As String
            Public ReadOnly asm As Assembly
            Public ReadOnly manager As PluginManager
            Public ReadOnly classType As Type
            Public ReadOnly name As String

            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(path IsNot Nothing)
                Contract.Invariant(asm IsNot Nothing)
                Contract.Invariant(manager IsNot Nothing)
                Contract.Invariant(classType IsNot Nothing)
                Contract.Invariant(name IsNot Nothing)
            End Sub

            Public Sub New(ByVal name As String, ByVal manager As PluginManager, ByVal path As String)
                Contract.Requires(name IsNot Nothing)
                Contract.Requires(manager IsNot Nothing)
                Contract.Requires(path IsNot Nothing)
                Me.name = name
                Me.path = path
                Me.manager = manager
                Try
                    asm = Assembly.LoadFrom(path)
                    Contract.Assume(asm IsNot Nothing)
                    classType = asm.GetType("HostBotPlugin")
                    Contract.Assume(classType IsNot Nothing)
                Catch e As Exception
                    Throw New PluginException("Error loading plugin assembly from '{1}'".Frmt(path), e)
                End Try
            End Sub

            Public Function LoadPlugin() As Plug
                Contract.Ensures(Contract.Result(Of Plug)() IsNot Nothing)
                Return New Plug(Me)
            End Function
            Private Sub UnloadPlugin(ByVal plug As Plug, ByVal reason As String)
                Contract.Requires(plug IsNot Nothing)
                Contract.Requires(reason IsNot Nothing)
                manager.UnloadPlugin(plug, reason)
            End Sub

            Friend Class Plug
                Implements IPlugout
                Implements IDisposable

                Private ReadOnly _socket As Socket
                Private ReadOnly _plugin As IPlugin

                <ContractInvariantMethod()> Private Sub ObjectInvariant()
                    Contract.Invariant(_socket IsNot Nothing)
                    Contract.Invariant(_plugin IsNot Nothing)
                End Sub
                Public ReadOnly Property Plugin As IPlugin
                    Get
                        Contract.Ensures(Contract.Result(Of IPlugin)() IsNot Nothing)
                        Return _plugin
                    End Get
                End Property
                Public ReadOnly Property Socket As Socket
                    Get
                        Contract.Ensures(Contract.Result(Of Socket)() IsNot Nothing)
                        Return _socket
                    End Get
                End Property

                Public Sub New(ByVal socket As Socket)
                    Contract.Requires(socket IsNot Nothing)
                    Me._socket = socket
                    Try
                        _plugin = CType(Activator.CreateInstance(socket.classType), IPlugin)
                        Contract.Assume(_plugin IsNot Nothing)
                        _plugin.Init(Me)
                    Catch e As Exception
                        Throw New PluginException("Error creating instance of plugin class: " + e.ToString, e)
                    End Try
                End Sub

                Public ReadOnly Property Bot() As MainBot Implements IPlugout.Bot
                    Get
                        Contract.Ensures(Contract.Result(Of MainBot)() IsNot Nothing)
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
                            _plugin.Dispose()
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
            Contract.Requires(name IsNot Nothing)
            Contract.Requires(path IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IPlugin)() IsNot Nothing)
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
            Return plug.Plugin
        End Function
        Private Sub UnloadPlugin(ByVal plug As Socket.Plug, ByVal reason As String)
            Contract.Requires(plug IsNot Nothing)
            Contract.Requires(reason IsNot Nothing)
            If Not loadedPlugins.Contains(plug) Then Throw New InvalidOperationException("No such plugin loaded.")
            plug.Dispose()
            RaiseEvent UnloadedPlugin(plug.socket.name, plug.Plugin, reason)
        End Sub
        Public Sub UnloadPlugin(ByVal name As String, ByVal reason As String)
            Contract.Requires(name IsNot Nothing)
            Contract.Requires(reason IsNot Nothing)
            Dim plug = (From x In loadedPlugins
                        Where x.socket.name.ToUpperInvariant = name.ToUpperInvariant).FirstOrDefault
            If plug Is Nothing Then Throw New InvalidOperationException("No such plugin loaded.")
            UnloadPlugin(plug, reason)
        End Sub

        Public Sub finish()
            For Each pi In loadedPlugins
                Contract.Assume(pi IsNot Nothing)
                pi.Dispose()
            Next pi
            loadedPlugins.Clear()
            sockets.Clear()
        End Sub
    End Class

    <ContractClass(GetType(IPlugout.ContractClass))>
    Public Interface IPlugout
        ReadOnly Property Bot() As MainBot
        Sub DisposePlugin(ByVal reason As String)

        <ContractClassFor(GetType(IPlugout))>
        Class ContractClass
            Implements IPlugout
            Public ReadOnly Property Bot As MainBot Implements IPlugout.Bot
                Get
                    Contract.Ensures(Contract.Result(Of MainBot)() IsNot Nothing)
                    Throw New NotSupportedException
                End Get
            End Property
            Public Sub DisposePlugin(ByVal reason As String) Implements IPlugout.DisposePlugin
                Contract.Requires(reason IsNot Nothing)
            End Sub
        End Class
    End Interface

    <ContractClass(GetType(IPlugin.ContractClass))>
    Public Interface IPlugin
        Inherits IDisposable
        Sub Init(ByVal plugout As IPlugout)
        ReadOnly Property Description() As String

        <ContractClassFor(GetType(IPlugin))>
        Class ContractClass
            Implements IPlugin
            Public ReadOnly Property Description As String Implements IPlugin.Description
                Get
                    Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                    Throw New NotSupportedException
                End Get
            End Property
            Public Sub Init(ByVal plugout As IPlugout) Implements IPlugin.Init
                Contract.Requires(plugout IsNot Nothing)
            End Sub
            Public Sub Dispose() Implements IDisposable.Dispose
            End Sub
        End Class
    End Interface
End Namespace
