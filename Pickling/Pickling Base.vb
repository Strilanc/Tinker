Namespace Pickling
    <Serializable()>
    Public Class PicklingException
        Inherits Exception
        Public Sub New(Optional ByVal message As String = Nothing,
                       Optional ByVal innerException As Exception = Nothing)
            MyBase.New(message, innerException)
        End Sub
    End Class

    <Serializable()>
    Public Class PicklingNotEnoughDataException
        Inherits PicklingException
        Public Sub New(Optional ByVal message As String = Nothing,
                       Optional ByVal innerException As Exception = Nothing)
            MyBase.New(If(message, "Not enough data."), innerException)
        End Sub
    End Class

    Public MustInherit Class BasePackJar(Of T)
        Implements IPackJar(Of T)
        Public MustOverride Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue) Implements IPackJar(Of T).Pack
    End Class
    Public MustInherit Class BaseJar(Of T)
        Implements IJar(Of T)

        Public MustOverride Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue) Implements IPackJar(Of T).Pack
        Public MustOverride Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T) Implements IParseJar(Of T).Parse
    End Class

    '''<summary>A base implementation of an IPickle(Of T).</summary>
    <DebuggerDisplay("{ToString}")>
    Public NotInheritable Class Pickle(Of T)
        Implements IPickle(Of T)

        Private ReadOnly _data As IReadableList(Of Byte)
        Private ReadOnly _value As T
        Private ReadOnly _description As Lazy(Of String)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_data IsNot Nothing)
            Contract.Invariant(_value IsNot Nothing)
            Contract.Invariant(_description IsNot Nothing)
        End Sub

        Public Sub New(ByVal value As T,
                       ByVal data As IReadableList(Of Byte),
                       ByVal description As Lazy(Of String))
            Contract.Requires(value IsNot Nothing)
            Contract.Requires(data IsNot Nothing)
            Contract.Requires(description IsNot Nothing)
            Contract.Ensures(Me.Description Is description)
            Contract.Ensures(Me.Data Is data)
            Me._data = data
            Me._value = value
            Me._description = description
        End Sub

        Public ReadOnly Property Description As Lazy(Of String) Implements IPickle(Of T).Description
            Get
                Contract.Ensures(Contract.Result(Of Lazy(Of String))() Is _description)
                Return _description
            End Get
        End Property
        Public ReadOnly Property Data As IReadableList(Of Byte) Implements IPickle(Of T).Data
            Get
                Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))() Is _data)
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
