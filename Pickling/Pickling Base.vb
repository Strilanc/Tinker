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

    '''<summary>Holds an object, a description of the object, and a serialization of the object.</summary>
    <ContractClass(GetType(ISimplePickle.ContractClass))>
    Public Interface ISimplePickle
        ReadOnly Property Data As IReadableList(Of Byte)
        ReadOnly Property Description As Lazy(Of String)
        ReadOnly Property Value As Object

        <ContractClassFor(GetType(ISimplePickle))>
        Class ContractClass
            Implements ISimplePickle
            Public ReadOnly Property Value As Object Implements ISimplePickle.Value
                Get
                    Contract.Ensures(Contract.Result(Of Object)() IsNot Nothing)
                    Throw New NotSupportedException
                End Get
            End Property
            Public ReadOnly Property Data As IReadableList(Of Byte) Implements ISimplePickle.Data
                Get
                    Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))() IsNot Nothing)
                    Throw New NotSupportedException
                End Get
            End Property
            Public ReadOnly Property Description As Lazy(Of String) Implements ISimplePickle.Description
                Get
                    Contract.Ensures(Contract.Result(Of Lazy(Of String))() IsNot Nothing)
                    Throw New NotSupportedException
                End Get
            End Property
        End Class
    End Interface
    '''<summary>Holds a value, a description of the value, and a serialization of the value.</summary>
    <ContractClass(GetType(ContractClassIPickle(Of )))>
    Public Interface IPickle(Of Out T)
        Inherits ISimplePickle
        Shadows ReadOnly Property Value As T
    End Interface
    <ContractClassFor(GetType(IPickle(Of )))>
    Public NotInheritable Class ContractClassIPickle(Of T)
        Inherits ISimplePickle.ContractClass
        Implements IPickle(Of T)
        Public Shadows ReadOnly Property Value As T Implements IPickle(Of T).Value
            Get
                Contract.Ensures(Contract.Result(Of T)() IsNot Nothing)
                Throw New NotSupportedException
            End Get
        End Property
    End Class
    '''<summary>Holds a value, a description of the value, and a serialization of the value.</summary>
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

        Public ReadOnly Property Description As Lazy(Of String) Implements ISimplePickle.Description
            Get
                Contract.Ensures(Contract.Result(Of Lazy(Of String))() Is _description)
                Return _description
            End Get
        End Property
        Public ReadOnly Property Data As IReadableList(Of Byte) Implements ISimplePickle.Data
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
        Private ReadOnly Property SimpleValue As Object Implements ISimplePickle.Value
            Get
                Return _value
            End Get
        End Property

        Public Overrides Function ToString() As String
            Contract.Assume(Description.Value IsNot Nothing)
            Return Description.Value
        End Function
    End Class

    '''<summary>Packs objects into pickles.</summary>
    <ContractClass(GetType(ISimplePackJar.ContractClass))>
    Public Interface ISimplePackJar
        Function Pack(Of T)(ByVal value As T) As IPickle(Of T)

        <ContractClassFor(GetType(ISimplePackJar))>
        MustInherit Class ContractClass
            Implements ISimplePackJar
            Public Function Pack(Of T)(ByVal value As T) As IPickle(Of T) Implements ISimplePackJar.Pack
                Contract.Requires(value IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IPickle(Of T))() IsNot Nothing)
                Throw New NotSupportedException()
            End Function
        End Class
    End Interface
    '''<summary>Packs values into pickles.</summary>
    <ContractClass(GetType(ContractClassIPackJar(Of )))>
    Public Interface IPackJar(Of In T)
        Inherits ISimplePackJar
        Shadows Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue)
    End Interface
    <ContractClassFor(GetType(IPackJar(Of )))>
    Public MustInherit Class ContractClassIPackJar(Of T)
        Inherits ISimplePackJar.ContractClass
        Implements IPackJar(Of T)
        Public Shadows Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue) Implements IPackJar(Of T).Pack
            Contract.Requires(value IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IPickle(Of TValue))() IsNot Nothing)
            Throw New NotSupportedException()
        End Function
    End Class

    '''<summary>Parses data into simple pickles.</summary>
    <ContractClass(GetType(ISimpleParseJar.ContractClass))>
    Public Interface ISimpleParseJar
        Function Parse(ByVal data As IReadableList(Of Byte)) As ISimplePickle

        <ContractClassFor(GetType(ISimpleParseJar))>
        MustInherit Class ContractClass
            Implements ISimpleParseJar
            Public Function Parse(ByVal data As IReadableList(Of Byte)) As ISimplePickle Implements ISimpleParseJar.Parse
                Contract.Requires(data IsNot Nothing)
                Contract.Ensures(Contract.Result(Of ISimplePickle)() IsNot Nothing)
                'Contract.Ensures(Contract.Result(Of IPickle(Of T))().Data.Count <= data.Count) 'disabled because of stupid verifier
                Throw New NotSupportedException()
            End Function
        End Class
    End Interface
    '''<summary>Parses data to into pickles.</summary>
    <ContractClass(GetType(ContractClassIParseJar(Of )))>
    Public Interface IParseJar(Of Out T)
        Inherits ISimpleParseJar
        Shadows Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T)
    End Interface
    <ContractClassFor(GetType(IParseJar(Of )))>
    Public NotInheritable Class ContractClassIParseJar(Of T)
        Inherits ISimpleParseJar.ContractClass
        Implements IParseJar(Of T)
        Public Shadows Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T) Implements IParseJar(Of T).Parse
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IPickle(Of T))() IsNot Nothing)
            'Contract.Ensures(Contract.Result(Of IPickle(Of T))().Data.Count <= data.Count) 'disabled because of stupid verifier
            Throw New NotSupportedException()
        End Function
    End Class

    '''<summary>Parses data into simple pickles and packs objects into pickles.</summary>
    Public Interface ISimpleJar
        Inherits ISimpleParseJar
        Inherits ISimplePackJar
    End Interface
    '''<summary>Parses data and packs values into pickles.</summary>
    Public Interface IJar(Of T)
        Inherits ISimpleJar
        Inherits IPackJar(Of T)
        Inherits IParseJar(Of T)
    End Interface
    '''<summary>Parses data and packs values into pickles.</summary>
    Public MustInherit Class BaseJar(Of T)
        Implements IJar(Of T)

        Public MustOverride Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue) Implements IPackJar(Of T).Pack
        Public MustOverride Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T) Implements IParseJar(Of T).Parse

        <ContractVerification(False)>
        Private Function SimplePack(Of TValue)(ByVal value As TValue) As IPickle(Of TValue) Implements ISimplePackJar.Pack
            Dim pickle = Pack(value.DynamicDirectCastTo(Of T)())
            Return value.Pickled(pickle.Data, pickle.Description)
        End Function
        Private Function SimpleParse(ByVal data As IReadableList(Of Byte)) As ISimplePickle Implements ISimpleParseJar.Parse
            Return Parse(data)
        End Function
    End Class

    '''<summary>A named jar which parses data into simple pickles and packs objects into pickles.</summary>
    Public Interface ISimpleNamedJar
        Inherits ISimpleJar
        ReadOnly Property Name As InvariantString
    End Interface
    '''<summary>A named jar which parses data and packs values into pickles.</summary>
    Public Interface INamedJar(Of T)
        Inherits ISimpleNamedJar
        Inherits IParseJar(Of T)
        Inherits IPackJar(Of T)
        Inherits IJar(Of T)
    End Interface
End Namespace
