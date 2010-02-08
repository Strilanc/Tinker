Namespace WC3.Replay
    Public Class ReplayWriter
        Inherits FutureDisposable

        Private _stream As IO.Stream
        Private _writer As IO.BinaryWriter

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_stream IsNot Nothing)
            Contract.Invariant(_writer IsNot Nothing)
        End Sub

        Public Sub New(ByVal stream As IO.Stream)
            Contract.Requires(stream IsNot Nothing)
            Me._stream = stream
            Me._writer = New IO.BinaryWriter(stream)
            Throw New NotImplementedException
        End Sub

        Private Sub WriteHeader(ByVal compressedSize As UInt32,
                                ByVal decompressedSize As UInt32,
                                ByVal gameVersion As UInt32,
                                ByVal lengthInMs As UInt32,
                                ByVal blockCount As UInt32)
            _stream.Position = 0
            _writer.WriteNullTerminatedString(Prots.HeaderMagicValue)
            _writer.Write(Prots.HeaderSize)
            _writer.Write(compressedSize)
            _writer.Write(Prots.HeaderVersion)
            _writer.Write(decompressedSize)
            _writer.Write(blockCount)
            _writer.Write("PX3W".ToAscBytes)
            _writer.Write(gameVersion)
            _writer.Write(lengthInMs)
            Contract.Assume(_stream.Position = Prots.HeaderSize - 4)
            _stream.Position = 0
            _writer.Write(New IO.BinaryReader(_stream).ReadBytes(CInt(Prots.HeaderSize) - 4).CRC32)
        End Sub

        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As IFuture
            If finalizing Then Return Nothing
            'write header
            _stream.Dispose()
            Return Nothing
        End Function
    End Class
End Namespace
