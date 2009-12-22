Namespace Pickling
    <Serializable()>
    Public NotInheritable Class PicklingException
        Inherits Exception
        Public Sub New(Optional ByVal message As String = Nothing,
                       Optional ByVal innerException As Exception = Nothing)
            MyBase.New(message, innerException)
        End Sub
    End Class

    '''<summary>A base implementation of an IPackJar(Of T).</summary>
    Public MustInherit Class BasePackJar(Of T)
        Implements IPackJar(Of T)
        Private ReadOnly _name As String
        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_name IsNot Nothing)
        End Sub

        Protected Sub New(ByVal name As InvariantString)
            Me._name = name
        End Sub

        Public ReadOnly Property Name As InvariantString Implements IJarInfo.Name
            Get
                Return _name
            End Get
        End Property

        Public MustOverride Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue) Implements IPackJar(Of T).Pack
    End Class

    '''<summary>A base implementation of an IParseJar(Of T).</summary>
    Public MustInherit Class BaseParseJar(Of T)
        Implements IParseJar(Of T)
        Private ReadOnly _name As InvariantString

        Protected Sub New(ByVal name As InvariantString)
            Me._name = name
        End Sub

        Public ReadOnly Property Name As InvariantString Implements IJarInfo.Name
            Get
                Return _name
            End Get
        End Property

        Public MustOverride Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T) Implements IParseJar(Of T).Parse
    End Class

    '''<summary>A base implementation of an IJar(Of T).</summary>
    Public MustInherit Class BaseJar(Of T)
        Implements IJar(Of T)
        Private ReadOnly _name As InvariantString

        Protected Sub New(ByVal name As InvariantString)
            Me._name = name
        End Sub

        Public ReadOnly Property Name As InvariantString Implements IJarInfo.Name
            Get
                Return _name
            End Get
        End Property

        Public MustOverride Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue) Implements IPackJar(Of T).Pack
        Public MustOverride Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T) Implements IParseJar(Of T).Parse
    End Class

    '''<summary>A base implementation of an IPickle(Of T).</summary>
    <DebuggerDisplay("{ToString}")>
    Public NotInheritable Class Pickle(Of T)
        Implements IPickle(Of T)

        Private ReadOnly _data As IReadableList(Of Byte)
        Private ReadOnly _value As T
        Private ReadOnly _description As LazyValue(Of String)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_data IsNot Nothing)
            Contract.Invariant(_value IsNot Nothing)
            Contract.Invariant(_description IsNot Nothing)
        End Sub

        Public Sub New(ByVal value As T,
                       ByVal data As IReadableList(Of Byte),
                       ByVal description As LazyValue(Of String))
            Contract.Requires(value IsNot Nothing)
            Contract.Requires(data IsNot Nothing)
            Contract.Requires(description IsNot Nothing)
            Me._data = data
            Me._value = value
            Me._description = description
        End Sub

        Public Sub New(ByVal jarName As InvariantString,
                       ByVal value As T,
                       ByVal data As IReadableList(Of Byte))
            Me.new(value, data, New LazyValue(Of String)(Function() "{0}: {1}".Frmt(jarName, value)))
            Contract.Requires(value IsNot Nothing)
            Contract.Requires(data IsNot Nothing)
        End Sub
        Public Sub New(ByVal jarName As InvariantString,
                       ByVal value As T,
                       ByVal data As IReadableList(Of Byte),
                       ByVal valueDescription As Func(Of String))
            Me.new(value, data, New LazyValue(Of String)(Function() "{0}: {1}".Frmt(jarName, valueDescription())))
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
        Public ReadOnly Property Data As IReadableList(Of Byte) Implements IPickle(Of T).Data
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
End Namespace
