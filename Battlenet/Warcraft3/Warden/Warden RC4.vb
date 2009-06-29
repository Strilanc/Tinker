Namespace Warcraft3.Warden
    Public Class RC4Converter
        Implements IConverter(Of Byte)
        Private ReadOnly xorSequenceGenerator As Func(Of IEnumerator(Of Byte))
        Public Sub New(ByVal seed() As Byte)
            Me.xorSequenceGenerator = Function() XorSequence(seed)
        End Sub
        Public Sub New(ByVal state As Warden_Module_Lib.RC4State)
            Me.xorSequenceGenerator = Function() XorSequence(state)
        End Sub

        Public Shared Function XorSequence(ByVal state As Warden_Module_Lib.RC4State) As IEnumerator(Of Byte)
            Dim key = (From i In Enumerable.Range(0, 256) Select CType(state.state(CByte(i)), ModByte)).ToArray
            Return XorSequence(key, state.k1, state.k2)
        End Function
        Public Shared Function XorSequence(ByVal seed() As Byte) As IEnumerator(Of Byte)
            Dim ints = Enumerable.Range(0, 256)
            Contract.Assume(ints IsNot Nothing)
            Dim bytes = From i In ints Select CType(i, ModByte)
            Contract.Assume(bytes IsNot Nothing)
            Dim key = (bytes).ToArray
            Dim p = 0
            For i = 0 To Byte.MaxValue
                p = (p + CInt(key(i)) + seed(i Mod seed.Length)) And &HFF
                Swap(key(i), key(p))
            Next i
            Return XorSequence(key, CType(0, ModByte), CType(0, ModByte))
        End Function
        Private Shared Function XorSequence(ByVal key() As ModByte, ByVal k1 As ModByte, ByVal k2 As ModByte) As IEnumerator(Of Byte)
            Return New Enumerator(Of Byte)(
                Function(controller)
                    k1 += CType(1, ModByte)
                    k2 += key(k1)
                    Swap(key(k1), key(k2))
                    Return key(key(k1) + key(k2))
                End Function)
        End Function

        Public Function Convert(ByVal sequence As IEnumerator(Of Byte)) As IEnumerator(Of Byte) Implements IConverter(Of Byte).Convert
            Dim xs = xorSequenceGenerator()
            Return New Enumerator(Of Byte)(
                Function(controller)
                    If Not sequence.MoveNext Then  Return controller.Break()
                    Return sequence.Current Xor xs.MoveNextAndReturn()
                End Function)
        End Function
    End Class
End Namespace
