Namespace Pickling
    <Serializable()>
    Public Class PicklingException
        Inherits Exception
        Public Sub New(Optional ByVal message As String = Nothing,
                       Optional ByVal innerException As Exception = Nothing)
            MyBase.New(message, innerException)
        End Sub
    End Class

    <ContractClass(GetType(ContractClassIPickle(Of )))>
    Public Interface IPickle(Of Out T)
        ReadOnly Property Data As ViewableList(Of Byte)
        ReadOnly Property Value As T
        ReadOnly Property Description As ExpensiveValue(Of String)
    End Interface

    <ContractClass(GetType(ContractClassForIJarInfo))>
    Public Interface IJarInfo
        ReadOnly Property Name As String
    End Interface
    <ContractClass(GetType(ContractClassIPackJar(Of )))>
    Public Interface IPackJar(Of In T)
        Inherits IJarInfo
        Function Pack(Of R As T)(ByVal value As R) As IPickle(Of R)
    End Interface
    <ContractClass(GetType(ContractClassIParseJar(Of )))>
    Public Interface IParseJar(Of Out T)
        Inherits IJarInfo
        Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of T)
    End Interface
    Public Interface IJar(Of T)
        Inherits IJarInfo
        Inherits IPackJar(Of T)
        Inherits IParseJar(Of T)
    End Interface

    <ContractClassFor(GetType(IPickle(Of )))>
    Public Class ContractClassIPickle(Of T)
        Implements IPickle(Of T)
        Public ReadOnly Property Data As ViewableList(Of Byte) Implements IPickle(Of T).Data
            Get
                Contract.Ensures(Contract.Result(Of ViewableList(Of Byte))() IsNot Nothing)
                Return Nothing
            End Get
        End Property
        Public ReadOnly Property Description As ExpensiveValue(Of String) Implements IPickle(Of T).Description
            Get
                Contract.Ensures(Contract.Result(Of ExpensiveValue(Of String))() IsNot Nothing)
                Return Nothing
            End Get
        End Property
        Public ReadOnly Property Value As T Implements IPickle(Of T).Value
            Get
                Return Nothing
            End Get
        End Property
    End Class

    <ContractClassFor(GetType(IJarInfo))>
    Public Class ContractClassForIJarInfo
        Implements IJarInfo
        Public ReadOnly Property Name As String Implements IJarInfo.Name
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Throw New NotSupportedException()
            End Get
        End Property
    End Class

    <ContractClassFor(GetType(IPackJar(Of )))>
    Public Class ContractClassIPackJar(Of T)
        Implements IPackJar(Of T)
        Public ReadOnly Property Name As String Implements IJarInfo.Name
            Get
                Throw New NotSupportedException()
            End Get
        End Property
        Public Function Pack(Of R As T)(ByVal value As R) As IPickle(Of R) Implements IPackJar(Of T).Pack
            Contract.Ensures(Contract.Result(Of IPickle(Of R))() IsNot Nothing)
            Throw New NotSupportedException()
        End Function
    End Class

    <ContractClassFor(GetType(IParseJar(Of )))>
    Public Class ContractClassIParseJar(Of T)
        Implements IParseJar(Of T)
        Public ReadOnly Property Name As String Implements IJarInfo.Name
            Get
                Throw New NotSupportedException()
            End Get
        End Property
        Public Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of T) Implements IParseJar(Of T).Parse
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IPickle(Of T))() IsNot Nothing)
            Throw New NotSupportedException()
        End Function
    End Class
End Namespace