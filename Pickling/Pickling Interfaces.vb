Namespace Pickling
    '''<summary>Stores the serialization of a value, as well as a description of the value.</summary>
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

    '''<summary>Stores the serialization of a value, as well as a description of the value and the value itself.</summary>
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

    ''' <summary>Provides access to information common to all pickling jar types.</summary>
    Public Interface IJarInfo
        ReadOnly Property Name As InvariantString
    End Interface

    '''<summary>Packs values to create pickles.</summary>
    <ContractClass(GetType(ContractClassIPackJar(Of )))>
    Public Interface IPackJar(Of In T)
        Inherits ISimplePackJar
        Shadows Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue)
    End Interface
    <ContractClassFor(GetType(IPackJar(Of )))>
    Public NotInheritable Class ContractClassIPackJar(Of T)
        Inherits ISimplePackJar.ContractClass
        Implements IPackJar(Of T)
        Public Shadows Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue) Implements IPackJar(Of T).Pack
            Contract.Requires(value IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IPickle(Of TValue))() IsNot Nothing)
            Throw New NotSupportedException()
        End Function
    End Class

    <ContractClass(GetType(ISimpleParseJar.ContractClass))>
    Public Interface ISimpleParseJar
        Function Parse(ByVal data As IReadableList(Of Byte)) As ISimplePickle

        <ContractClassFor(GetType(ISimpleParseJar))>
        Class ContractClass
            Implements ISimpleParseJar
            Public Function Parse(ByVal data As IReadableList(Of Byte)) As ISimplePickle Implements ISimpleParseJar.Parse
                Contract.Requires(data IsNot Nothing)
                Contract.Ensures(Contract.Result(Of ISimplePickle)() IsNot Nothing)
                'Contract.Ensures(Contract.Result(Of IPickle(Of T))().Data.Count <= data.Count) 'disabled because of stupid verifier
                Throw New NotSupportedException()
            End Function
        End Class
    End Interface
    <ContractClass(GetType(ISimplePackJar.ContractClass))>
    Public Interface ISimplePackJar
        Function Pack(Of T)(ByVal value As T) As IPickle(Of T)

        <ContractClassFor(GetType(ISimplePackJar))>
        Class ContractClass
            Implements ISimplePackJar
            Public Function Pack(Of T)(ByVal value As T) As IPickle(Of T) Implements ISimplePackJar.Pack
                Contract.Requires(value IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IPickle(Of T))() IsNot Nothing)
                Throw New NotSupportedException()
            End Function
        End Class
    End Interface

    '''<summary>Parses data to create pickles.</summary>
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

    Public Interface ISimpleJar
        Inherits ISimpleParseJar
        Inherits ISimplePackJar
    End Interface
    Public Interface IJar(Of T)
        Inherits ISimpleJar
        Inherits IPackJar(Of T)
        Inherits IParseJar(Of T)
    End Interface

    Public Interface ISimpleNamedJar
        Inherits ISimpleJar
        Inherits IJarInfo
    End Interface

    Public Interface INamedPackJar(Of In T)
        Inherits IJarInfo
        Inherits IPackJar(Of T)
    End Interface
    Public Interface INamedParseJar(Of Out T)
        Inherits IJarInfo
        Inherits IParseJar(Of T)
    End Interface
    '''<summary>Packs values or parses data to create pickles.</summary>
    Public Interface INamedJar(Of T)
        Inherits ISimpleNamedJar
        Inherits INamedParseJar(Of T)
        Inherits INamedPackJar(Of T)
        Inherits IJar(Of T)
    End Interface
End Namespace
