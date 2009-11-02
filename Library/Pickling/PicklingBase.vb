Namespace Pickling
    <DebuggerDisplay("{ToString}")>
    Public NotInheritable Class Pickle(Of T)
        Implements IPickle(Of T)

        Private ReadOnly _data As ViewableList(Of Byte)
        Private ReadOnly _value As T
        Private ReadOnly _description As LazyValue(Of String)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_data IsNot Nothing)
            Contract.Invariant(_value IsNot Nothing)
            Contract.Invariant(_description IsNot Nothing)
        End Sub

        Public Sub New(ByVal value As T,
                       ByVal data As ViewableList(Of Byte),
                       ByVal description As LazyValue(Of String))
            Contract.Requires(value IsNot Nothing)
            Contract.Requires(data IsNot Nothing)
            Contract.Requires(description IsNot Nothing)
            Me._data = data
            Me._value = value
            Me._description = description
        End Sub

        Public Sub New(ByVal jarName As String,
                       ByVal value As T,
                       ByVal data As ViewableList(Of Byte))
            Me.new(value, data, New LazyValue(Of String)(Function() "{0}: {1}".Frmt(jarName, value)))
            Contract.Requires(jarName IsNot Nothing)
            Contract.Requires(value IsNot Nothing)
            Contract.Requires(data IsNot Nothing)
        End Sub
        Public Sub New(ByVal jarName As String,
                       ByVal value As T,
                       ByVal data As ViewableList(Of Byte),
                       ByVal valueDescription As Func(Of String))
            Me.new(value, data, New LazyValue(Of String)(Function() "{0}: {1}".Frmt(jarName, valueDescription())))
            Contract.Requires(jarName IsNot Nothing)
            Contract.Requires(value IsNot Nothing)
            Contract.Requires(data IsNot Nothing)
            Contract.Requires(valueDescription IsNot Nothing)
        End Sub

        Public Shared Function MakeListDescription(ByVal pickles As IEnumerable(Of IPickle(Of T))) As String
            Contract.Requires(pickles IsNot Nothing)
            Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
            Return "{{\n{0}\n}}".Linefy.Frmt((From e In pickles
                                              Select e.Description.Value
                                              ).StringJoin(Environment.NewLine).Indent("    "))
        End Function

        Public ReadOnly Property Description As LazyValue(Of String) Implements IPickle(Of T).Description
            Get
                Return _description
            End Get
        End Property
        Public ReadOnly Property Data As ViewableList(Of Byte) Implements IPickle(Of T).Data
            Get
                Return _data
            End Get
        End Property
        Public ReadOnly Property Value As T Implements IPickle(Of T).Value
            Get
                Return _value
            End Get
        End Property

        Public Overrides Function ToString() As String
            Contract.Assume(Description.Value IsNot Nothing)
            Return Description.Value
        End Function
    End Class

    Public MustInherit Class PackJar(Of T)
        Implements IPackJar(Of T)
        Private ReadOnly _name As String
        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_name IsNot Nothing)
        End Sub

        Protected Sub New(ByVal name As String)
            Contract.Requires(name IsNot Nothing)
            Me._name = name
        End Sub

        Public ReadOnly Property Name As String Implements IJarInfo.Name
            Get
                Return _name
            End Get
        End Property

        Public MustOverride Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue) Implements IPackJar(Of T).Pack
    End Class
    Public MustInherit Class ParseJar(Of T)
        Implements IParseJar(Of T)
        Private ReadOnly _name As String
        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_name IsNot Nothing)
        End Sub

        Protected Sub New(ByVal name As String)
            Contract.Requires(name IsNot Nothing)
            Me._name = name
        End Sub

        Public ReadOnly Property Name As String Implements IJarInfo.Name
            Get
                Return _name
            End Get
        End Property

        Public MustOverride Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of T) Implements IParseJar(Of T).Parse
    End Class
    Public MustInherit Class Jar(Of T)
        Implements IJar(Of T)
        Private ReadOnly _name As String
        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_name IsNot Nothing)
        End Sub

        Protected Sub New(ByVal name As String)
            Contract.Requires(name IsNot Nothing)
            Me._name = name
        End Sub

        Public ReadOnly Property Name As String Implements IJarInfo.Name
            Get
                Return _name
            End Get
        End Property

        Public MustOverride Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue) Implements IPackJar(Of T).Pack
        Public MustOverride Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of T) Implements IParseJar(Of T).Parse
    End Class

    Public Module PicklingExtensionMethods
        <Extension()> <Pure()>
        Public Function Weaken(Of T)(ByVal jar As IJar(Of T)) As IJar(Of Object)
            Contract.Requires(jar IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IJar(Of Object))() IsNot Nothing)
            Return New WeakJar(Of T)(jar)
        End Function
        <Extension()> <Pure()>
        Public Function Weaken(Of T)(ByVal jar As IPackJar(Of T)) As IPackJar(Of Object)
            Contract.Requires(jar IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IPackJar(Of Object))() IsNot Nothing)
            Return New WeakPackJar(Of T)(jar)
        End Function
        <Extension()> <Pure()>
        Public Function Weaken(Of T)(ByVal jar As IParseJar(Of T)) As IParseJar(Of Object)
            Contract.Requires(jar IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IParseJar(Of Object))() IsNot Nothing)
            Return New WeakParseJar(Of T)(jar)
        End Function

        Private NotInheritable Class WeakJar(Of T)
            Inherits Jar(Of Object)
            Private ReadOnly subJar As IJar(Of T)

            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(subJar IsNot Nothing)
            End Sub

            Public Sub New(ByVal jar As IJar(Of T))
                MyBase.New(jar.Name)
                Contract.Requires(jar IsNot Nothing)
                Me.subJar = jar
            End Sub
            Public Overrides Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of Object)
                Dim p = subJar.Parse(data)
                Return New Pickle(Of Object)(p.Value, p.Data, p.Description)
            End Function
            Public Overrides Function Pack(Of R As Object)(ByVal value As R) As IPickle(Of R)
                Contract.Assume(value IsNot Nothing)
                Dim p = subJar.Pack(CType(CType(value, Object), T).AssumeNotNull)
                Return New Pickle(Of R)(value, p.Data, p.Description)
            End Function
        End Class
        Private NotInheritable Class WeakPackJar(Of T)
            Inherits PackJar(Of Object)
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
        Private NotInheritable Class WeakParseJar(Of T)
            Inherits ParseJar(Of Object)
            Private ReadOnly subJar As IParseJar(Of T)

            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(subJar IsNot Nothing)
            End Sub

            Public Sub New(ByVal jar As IParseJar(Of T))
                MyBase.New(jar.Name)
                Contract.Requires(jar IsNot Nothing)
                Me.subJar = jar
            End Sub
            Public Overrides Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of Object)
                Dim p = subjar.Parse(data)
                Return New Pickle(Of Object)(p.Value, p.Data, p.Description)
            End Function
        End Class
    End Module
End Namespace