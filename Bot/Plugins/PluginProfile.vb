Imports System.Reflection

Namespace Plugins
    Public NotInheritable Class PluginProfile
        Public name As InvariantString
        Public location As InvariantString
        Public argument As String
        Private Const format_version As UInteger = 0

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(argument IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As InvariantString, ByVal location As InvariantString, ByVal argument As String)
            Contract.Requires(argument IsNot Nothing)
            Me.name = name
            Me.location = location
            Me.argument = argument
        End Sub
        Public Sub New(ByVal reader As IO.BinaryReader)
            Contract.Requires(reader IsNot Nothing)
            Dim ver = reader.ReadUInt32()
            If ver > format_version Then Throw New IO.InvalidDataException("Saved PlayerRecord has an unrecognized format version.")
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
End Namespace
