Namespace Pickling
    '''<summary>Stores the serialization of a value, as well as a description of the value.</summary>
    <ContractClass(GetType(IPickle.ContractClass))>
    Public Interface IPickle
        ReadOnly Property Data As IReadableList(Of Byte)
        ReadOnly Property Description As LazyValue(Of String)

        <ContractClassFor(GetType(IPickle))>
        Class ContractClass
            Implements IPickle
            Public ReadOnly Property Data As IReadableList(Of Byte) Implements IPickle.Data
                Get
                    Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))() IsNot Nothing)
                    Throw New NotSupportedException
                End Get
            End Property
            Public ReadOnly Property Description As LazyValue(Of String) Implements IPickle.Description
                Get
                    Contract.Ensures(Contract.Result(Of LazyValue(Of String))() IsNot Nothing)
                    Throw New NotSupportedException
                End Get
            End Property
        End Class
    End Interface

    '''<summary>Stores the serialization of a value, as well as a description of the value and the value itself.</summary>
    <ContractClass(GetType(ContractClassIPickle(Of )))>
    Public Interface IPickle(Of Out T)
        Inherits IPickle
        ReadOnly Property Value As T
    End Interface
    <ContractClassFor(GetType(IPickle(Of )))>
    Public NotInheritable Class ContractClassIPickle(Of T)
        Implements IPickle(Of T)
        Public ReadOnly Property Data As IReadableList(Of Byte) Implements IPickle.Data
            Get
                Throw New NotSupportedException
            End Get
        End Property
        Public ReadOnly Property Description As LazyValue(Of String) Implements IPickle.Description
            Get
                Throw New NotSupportedException
            End Get
        End Property
        Public ReadOnly Property Value As T Implements IPickle(Of T).Value
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
        Inherits IJarInfo
        Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue)
    End Interface
    <ContractClassFor(GetType(IPackJar(Of )))>
    Public NotInheritable Class ContractClassIPackJar(Of T)
        Implements IPackJar(Of T)
        Public ReadOnly Property Name As InvariantString Implements IJarInfo.Name
            Get
                Throw New NotSupportedException()
            End Get
        End Property
        Public Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue) Implements IPackJar(Of T).Pack
            Contract.Requires(value IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IPickle(Of TValue))() IsNot Nothing)
            Throw New NotSupportedException()
        End Function
    End Class

    '''<summary>Parses data to create pickles.</summary>
    <ContractClass(GetType(ContractClassIParseJar(Of )))>
    Public Interface IParseJar(Of Out T)
        Inherits IJarInfo
        Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T)
    End Interface
    <ContractClassFor(GetType(IParseJar(Of )))>
    Public NotInheritable Class ContractClassIParseJar(Of T)
        Implements IParseJar(Of T)
        Public ReadOnly Property Name As InvariantString Implements IJarInfo.Name
            Get
                Throw New NotSupportedException()
            End Get
        End Property
        Public Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T) Implements IParseJar(Of T).Parse
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IPickle(Of T))() IsNot Nothing)
            'Contract.Ensures(Contract.Result(Of IPickle(Of T))().Data.Count <= data.Count) 'disabled because of stupid verifier
            Throw New NotSupportedException()
        End Function
    End Class

    '''<summary>Packs values or parses data to create pickles.</summary>
    Public Interface IJar(Of T)
        Inherits IJarInfo
        Inherits IPackJar(Of T)
        Inherits IParseJar(Of T)
    End Interface
End Namespace
