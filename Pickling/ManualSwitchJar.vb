Namespace Pickling.Jars
    Public NotInheritable Class ManualSwitchJar
        Private ReadOnly packers(0 To 255) As IPackJar(Of Object)
        Private ReadOnly parsers(0 To 255) As IParseJar(Of Object)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(packers IsNot Nothing)
            Contract.Invariant(parsers IsNot Nothing)
        End Sub

        Public Sub New()
        End Sub
        Public Sub New(ByVal parsersPackers As IDictionary(Of Byte, IJar(Of Object)))
            If parsersPackers IsNot Nothing Then
                For Each pair In parsersPackers
                    Me.packers(pair.Key) = pair.Value
                    Me.parsers(pair.Key) = pair.Value
                Next pair
            End If
        End Sub
        Public Sub New(ByVal parsers As IDictionary(Of Byte, IParseJar(Of Object)),
                       ByVal packers As IDictionary(Of Byte, IPackJar(Of Object)))
            Contract.Requires(parsers IsNot Nothing)
            Contract.Requires(packers IsNot Nothing)
            For Each pair In parsers
                Me.parsers(pair.Key) = pair.Value
            Next pair
            For Each pair In packers
                Me.packers(pair.Key) = pair.Value
            Next pair
        End Sub

        Public Overloads Function Parse(ByVal index As Byte, ByVal data As IReadableList(Of Byte)) As IPickle(Of Object)
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IPickle(Of Object))() IsNot Nothing)
            If parsers(index) Is Nothing Then Throw New PicklingException("No parser registered to {0}.".Frmt(index))
            Return parsers(index).Parse(data)
        End Function
        Public Overloads Function Pack(Of TValue)(ByVal index As Byte, ByVal value As TValue) As IPickle(Of TValue)
            Contract.Requires(value IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IPickle(Of TValue))() IsNot Nothing)
            If packers(index) Is Nothing Then Throw New PicklingException("No packer registered to {0}.".Frmt(index))
            Return packers(index).Pack(value)
        End Function

        Public Sub AddPackerParser(ByVal index As Byte, ByVal jar As IJar(Of Object))
            Contract.Requires(jar IsNot Nothing)
            If parsers(index) IsNot Nothing Then Throw New InvalidOperationException("Parser already registered to index " + index.ToString(CultureInfo.InvariantCulture))
            If packers(index) IsNot Nothing Then Throw New InvalidOperationException("Packer already registered to index " + index.ToString(CultureInfo.InvariantCulture))
            parsers(index) = jar
            packers(index) = jar
        End Sub
        Public Sub AddParser(ByVal index As Byte, ByVal parser As IParseJar(Of Object))
            Contract.Requires(parser IsNot Nothing)
            If parsers(index) IsNot Nothing Then Throw New InvalidOperationException("Parser already registered to index " + index.ToString(CultureInfo.InvariantCulture))
            parsers(index) = parser
        End Sub
        Public Sub AddPacker(ByVal index As Byte, ByVal packer As IPackJar(Of Object))
            Contract.Requires(packer IsNot Nothing)
            If packers(index) IsNot Nothing Then Throw New InvalidOperationException("Packer already registered to index " + index.ToString(CultureInfo.InvariantCulture))
            packers(index) = packer
        End Sub
    End Class
End Namespace
