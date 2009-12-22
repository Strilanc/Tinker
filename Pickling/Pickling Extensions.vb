Namespace Pickling
    Public Module PicklingExtensions
        '''<summary>Weakens the type of an IJar from T to Object.</summary>
        <Extension()> <Pure()>
        Public Function Weaken(Of T)(ByVal jar As IJar(Of T)) As IJar(Of Object)
            Contract.Requires(jar IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IJar(Of Object))() IsNot Nothing)
            Return New WeakJar(Of T)(jar)
        End Function

        '''<summary>Weakens the type of an IPackJar from T to Object.</summary>
        <Extension()> <Pure()>
        Public Function Weaken(Of T)(ByVal jar As IPackJar(Of T)) As IPackJar(Of Object)
            Contract.Requires(jar IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IPackJar(Of Object))() IsNot Nothing)
            Return New WeakPackJar(Of T)(jar)
        End Function

        '''<summary>Weakens the type of an IParseJar from T to Object.</summary>
        <Extension()> <Pure()>
        Public Function Weaken(Of T)(ByVal jar As IParseJar(Of T)) As IParseJar(Of Object)
            Contract.Requires(jar IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IParseJar(Of Object))() IsNot Nothing)
            Return New WeakParseJar(Of T)(jar)
        End Function

        '''<summary>Exposes an IJar of arbitrary type as an IJar(Of Object).</summary>
        Private NotInheritable Class WeakJar(Of T)
            Inherits BaseJar(Of Object)
            Private ReadOnly subJar As IJar(Of T)

            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(subJar IsNot Nothing)
            End Sub

            Public Sub New(ByVal jar As IJar(Of T))
                MyBase.New(jar.Name)
                Contract.Requires(jar IsNot Nothing)
                Me.subJar = jar
            End Sub
            Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of Object)
                Dim p = subJar.Parse(data)
                Return New Pickle(Of Object)(p.Value, p.Data, p.Description)
            End Function
            Public Overrides Function Pack(Of R As Object)(ByVal value As R) As IPickle(Of R)
                Contract.Assume(value IsNot Nothing)
                Dim p = subJar.Pack(CType(CType(value, Object), T).AssumeNotNull)
                Return New Pickle(Of R)(value, p.Data, p.Description)
            End Function
        End Class
        '''<summary>Exposes an IPackJar of arbitrary type as an IPackJar(Of Object).</summary>
        Private NotInheritable Class WeakPackJar(Of T)
            Inherits BasePackJar(Of Object)
            Private ReadOnly subJar As IPackJar(Of T)

            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(subJar IsNot Nothing)
            End Sub

            Public Sub New(ByVal jar As IPackJar(Of T))
                MyBase.New(jar.Name)
                Contract.Requires(jar IsNot Nothing)
                Me.subJar = jar
            End Sub
            Public Overrides Function Pack(Of R As Object)(ByVal value As R) As IPickle(Of R)
                Contract.Assume(value IsNot Nothing)
                Dim p = subJar.Pack(CType(CType(value, Object), T).AssumeNotNull)
                Return New Pickle(Of R)(value, p.Data, p.Description)
            End Function
        End Class
        '''<summary>Exposes an IParseJar of arbitrary type as an IParseJar(Of Object).</summary>
        Private NotInheritable Class WeakParseJar(Of T)
            Inherits BaseParseJar(Of Object)
            Private ReadOnly subJar As IParseJar(Of T)

            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(subJar IsNot Nothing)
            End Sub

            Public Sub New(ByVal jar As IParseJar(Of T))
                MyBase.New(jar.Name)
                Contract.Requires(jar IsNot Nothing)
                Me.subJar = jar
            End Sub
            Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of Object)
                Dim p = subJar.Parse(data)
                Return New Pickle(Of Object)(p.Value, p.Data, p.Description)
            End Function
        End Class
    End Module
End Namespace
