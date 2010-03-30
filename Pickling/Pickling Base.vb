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
        ReadOnly Property Value As Object
        ReadOnly Property Jar As ISimpleJar

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
            Public ReadOnly Property Jar As ISimpleJar Implements ISimplePickle.Jar
                Get
                    Contract.Ensures(Contract.Result(Of ISimpleJar)() IsNot Nothing)
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

        Private ReadOnly _jar As ISimpleJar
        Private ReadOnly _data As IReadableList(Of Byte)
        Private ReadOnly _value As T

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_jar IsNot Nothing)
            Contract.Invariant(_data IsNot Nothing)
            Contract.Invariant(_value IsNot Nothing)
        End Sub

        Public Sub New(ByVal jar As ISimpleJar,
                       ByVal value As T,
                       ByVal data As IReadableList(Of Byte))
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(value IsNot Nothing)
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Me.Data Is data)
            Me._jar = jar
            Me._data = data
            Me._value = value
        End Sub

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
        Public ReadOnly Property Jar As ISimpleJar Implements ISimplePickle.Jar
            Get
                Contract.Ensures(Contract.Result(Of ISimpleJar)() IsNot Nothing)
                Return _jar
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return Me.Description()
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
    <ContractClass(GetType(ISimpleJar.ContractClass))>
    Public Interface ISimpleJar
        Inherits ISimpleParseJar
        Function Pack(ByVal value As Object) As IEnumerable(Of Byte)
        Function MakeControl() As ISimpleValueEditor
        Function Describe(ByVal value As Object) As String

        <ContractClassFor(GetType(ISimpleJar))>
        MustInherit Shadows Class ContractClass
            Implements ISimpleJar
            <Pure()>
            Public Function Describe(ByVal value As Object) As String Implements ISimpleJar.Describe
                Contract.Requires(value IsNot Nothing)
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Throw New NotSupportedException
            End Function
            <Pure()>
            Public Function MakeControl() As ISimpleValueEditor Implements ISimpleJar.MakeControl
                Contract.Ensures(Contract.Result(Of ISimpleValueEditor)() IsNot Nothing)
                Throw New NotSupportedException
            End Function
            Public Function Parse(ByVal data As IReadableList(Of Byte)) As ISimplePickle Implements ISimpleParseJar.Parse
                Throw New NotSupportedException
            End Function
            <Pure()>
            Public Function Pack(ByVal value As Object) As IEnumerable(Of Byte) Implements ISimpleJar.Pack
                Contract.Requires(value IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IEnumerable(Of Byte))() IsNot Nothing)
                Throw New NotSupportedException
            End Function
        End Class
    End Interface
    '''<summary>Parses data and packs values into pickles.</summary>
    <ContractClass(GetType(ContractClassIJar(Of )))>
    Public Interface IJar(Of T)
        Inherits ISimpleJar
        Inherits IParseJar(Of T)
        Shadows Function Pack(ByVal value As T) As IEnumerable(Of Byte)
        Shadows Function MakeControl() As IValueEditor(Of T)
        Shadows Function Describe(ByVal value As T) As String
    End Interface
    <ContractClassFor(GetType(IJar(Of )))>
    Public MustInherit Class ContractClassIJar(Of T)
        Inherits ISimpleJar.ContractClass
        Implements IJar(Of T)
        <Pure()>
        Public Shadows Function Describe(ByVal value As T) As String Implements IJar(Of T).Describe
            Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
            Throw New NotSupportedException
        End Function
        <Pure()>
        Public Shadows Function MakeControl() As IValueEditor(Of T) Implements IJar(Of T).MakeControl
            Contract.Ensures(Contract.Result(Of IValueEditor(Of T))() IsNot Nothing)
            Throw New NotSupportedException
        End Function
        <Pure()>
        Public Shadows Function Pack(ByVal value As T) As IEnumerable(Of Byte) Implements IJar(Of T).Pack
            Contract.Requires(value IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IEnumerable(Of Byte))() IsNot Nothing)
            Throw New NotSupportedException
        End Function
        Public Shadows Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T) Implements IParseJar(Of T).Parse
            Throw New NotSupportedException
        End Function
    End Class

    '''<summary>Parses data and packs values into pickles.</summary>
    Public MustInherit Class BaseJar(Of T)
        Implements IJar(Of T)

        Public MustOverride Function Pack(ByVal value As T) As IEnumerable(Of Byte) Implements IJar(Of T).Pack
        Public MustOverride Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T) Implements IParseJar(Of T).Parse
        Public MustOverride Function MakeControl() As IValueEditor(Of T) Implements IJar(Of T).MakeControl
        Public Overridable Function Describe(ByVal value As T) As String Implements IJar(Of T).Describe
            Return value.ToString()
        End Function

        Private Function SimpleMakeControl() As ISimpleValueEditor Implements ISimpleJar.MakeControl
            Return MakeControl()
        End Function
        Private Function SimplePack(ByVal value As Object) As IEnumerable(Of Byte) Implements ISimpleJar.Pack
            Return Pack(DirectCast(value, T))
        End Function
        Private Function SimpleParse(ByVal data As IReadableList(Of Byte)) As ISimplePickle Implements ISimpleParseJar.Parse
            Return Parse(data)
        End Function
        Public Function SimpleDescribe(ByVal value As Object) As String Implements ISimpleJar.Describe
            Return Describe(DirectCast(value, T))
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
        Inherits IJar(Of T)
    End Interface

    <ContractClass(GetType(ISimpleValueEditor.ContractClass))>
    Public Interface ISimpleValueEditor
        ReadOnly Property Control As Control
        Property Value As Object
        Event ValueChanged(ByVal sender As ISimpleValueEditor)

        <ContractClassFor(GetType(ISimpleValueEditor))>
        MustInherit Class ContractClass
            Implements ISimpleValueEditor
            Public Event ValueChanged(ByVal sender As ISimpleValueEditor) Implements ISimpleValueEditor.ValueChanged
            Public ReadOnly Property Control As System.Windows.Forms.Control Implements ISimpleValueEditor.Control
                Get
                    Contract.Ensures(Contract.Result(Of Control)() IsNot Nothing)
                    Throw New NotSupportedException
                End Get
            End Property
            Public Property Value As Object Implements ISimpleValueEditor.Value
                Get
                    Contract.Ensures(Contract.Result(Of Object)() IsNot Nothing)
                    Throw New NotSupportedException
                End Get
                Set(ByVal value As Object)
                    Contract.Requires(value IsNot Nothing)
                    Throw New NotSupportedException
                End Set
            End Property
        End Class
    End Interface
    <ContractClass(GetType(ContractClassIValueEditor(Of )))>
    Public Interface IValueEditor(Of T)
        Inherits ISimpleValueEditor
        Shadows Property Value As T
        Shadows Event ValueChanged(ByVal sender As IValueEditor(Of T))
    End Interface
    <ContractClassFor(GetType(IValueEditor(Of )))>
    MustInherit Class ContractClassIValueEditor(Of T)
        Inherits ISimpleValueEditor.ContractClass
        Implements IValueEditor(Of T)
        Public Shadows Event ValueChanged(ByVal sender As IValueEditor(Of T)) Implements IValueEditor(Of T).ValueChanged
        Public Shadows Property Value As T Implements IValueEditor(Of T).Value
            Get
                Contract.Ensures(Contract.Result(Of T)() IsNot Nothing)
                Throw New NotSupportedException
            End Get
            Set(ByVal value As T)
                Contract.Requires(value IsNot Nothing)
                Throw New NotSupportedException
            End Set
        End Property
    End Class

    Public Class DelegatedValueEditor(Of T)
        Implements IValueEditor(Of T)

        Private ReadOnly _getter As Func(Of T)
        Private ReadOnly _setter As Action(Of T)
        Private ReadOnly _control As Control

        Private _blockEvents As Boolean

        Public Event ValueChanged(ByVal sender As IValueEditor(Of T)) Implements IValueEditor(Of T).ValueChanged
        Private Event ValueChangedSimple(ByVal sender As ISimpleValueEditor) Implements ISimpleValueEditor.ValueChanged

        Public Sub New(ByVal control As Control,
                       ByVal getter As Func(Of T),
                       ByVal setter As Action(Of T),
                       ByVal eventAdder As Action(Of Action))
            Me._control = control
            Me._getter = getter
            Me._setter = setter
            Call eventAdder(AddressOf RaiseValueChanged)
        End Sub
        Private Sub RaiseValueChanged()
            If _blockEvents Then Return
            RaiseEvent ValueChanged(Me)
            RaiseEvent ValueChangedSimple(Me)
        End Sub

        Public ReadOnly Property Control As System.Windows.Forms.Control Implements ISimpleValueEditor.Control
            Get
                Return _control
            End Get
        End Property

        Public Property Value As T Implements IValueEditor(Of T).Value
            Get
                Return _getter()
            End Get
            Set(ByVal value As T)
                Try
                    _blockEvents = True
                    _setter(value)
                Finally
                    _blockEvents = False
                End Try
                RaiseValueChanged()
            End Set
        End Property
        Private Property ValueSimple As Object Implements ISimpleValueEditor.Value
            Get
                Return Me.Value
            End Get
            Set(ByVal value As Object)
                Me.Value = DirectCast(value, T)
            End Set
        End Property
    End Class
End Namespace
