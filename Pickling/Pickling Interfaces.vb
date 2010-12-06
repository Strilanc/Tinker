Namespace Pickling
    '''<summary>Holds an object, a description of the object, and a serialization of the object.</summary>
    <ContractClass(GetType(ISimplePickle.ContractClass))>
    Public Interface ISimplePickle
        ReadOnly Property Data As IRist(Of Byte)
        ReadOnly Property Value As Object
        ReadOnly Property Jar As ISimpleJar

        <ContractClassFor(GetType(ISimplePickle))>
        MustInherit Class ContractClass
            Implements ISimplePickle
            Public ReadOnly Property Value As Object Implements ISimplePickle.Value
                Get
                    Contract.Ensures(Contract.Result(Of Object)() IsNot Nothing)
                    Throw New NotSupportedException
                End Get
            End Property
            Public ReadOnly Property Data As IRist(Of Byte) Implements ISimplePickle.Data
                Get
                    Contract.Ensures(Contract.Result(Of IRist(Of Byte))() IsNot Nothing)
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

    '''<summary>Parses data into simple pickles and packs objects into pickles.</summary>
    <ContractClass(GetType(ISimpleJar.ContractClass))>
    Public Interface ISimpleJar
        Function Parse(ByVal data As IRist(Of Byte)) As ParsedValue(Of Object)
        Function Pack(ByVal value As Object) As IEnumerable(Of Byte)
        Function MakeControl() As ISimpleValueEditor
        Function Describe(ByVal value As Object) As String
        Function Parse(ByVal text As String) As Object

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
            Public Function Parse(ByVal data As IRist(Of Byte)) As ParsedValue(Of Object) Implements ISimpleJar.Parse
                Contract.Requires(data IsNot Nothing)
                Contract.Ensures(Contract.Result(Of ParsedValue(Of Object))() IsNot Nothing)
                Contract.Ensures(Contract.Result(Of ParsedValue(Of Object))().UsedDataCount <= data.Count)
                Throw New NotSupportedException()
            End Function
            <Pure()>
            Public Function Pack(ByVal value As Object) As IEnumerable(Of Byte) Implements ISimpleJar.Pack
                Contract.Requires(value IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IEnumerable(Of Byte))() IsNot Nothing)
                Throw New NotSupportedException
            End Function
            <Pure()>
            Public Function Parse(ByVal text As String) As Object Implements ISimpleJar.Parse
                Contract.Requires(text IsNot Nothing)
                Contract.Ensures(Contract.Result(Of Object)() IsNot Nothing)
                Throw New NotSupportedException
            End Function
        End Class
    End Interface

    '''<summary>Parses data and packs values into pickles.</summary>
    <ContractClass(GetType(ContractClassIJar(Of )))>
    Public Interface IJar(Of T)
        Inherits ISimpleJar
        Shadows Function Pack(ByVal value As T) As IEnumerable(Of Byte)
        Shadows Function Parse(ByVal data As IRist(Of Byte)) As ParsedValue(Of T)
        Shadows Function MakeControl() As IValueEditor(Of T)
        Shadows Function Describe(ByVal value As T) As String
        Shadows Function Parse(ByVal text As String) As T
    End Interface

    '''<summary>A named jar which parses data into simple pickles and packs objects into pickles.</summary>
    Public Interface ISimpleNamedJar
        Inherits ISimpleJar
        ReadOnly Property Name As InvariantString
    End Interface

    '''<summary>A named jar which parses data and packs values into pickles.</summary>
    Public Interface INamedJar(Of T)
        Inherits ISimpleNamedJar
        Inherits IJar(Of T)
    End Interface

    <ContractClass(GetType(ISimpleValueEditor.ContractClass))>
    Public Interface ISimpleValueEditor
        Inherits IDisposable

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
            Public Sub Dispose() Implements IDisposable.Dispose
            End Sub
        End Class
    End Interface

    <ContractClass(GetType(ContractClassIValueEditor(Of )))>
    Public Interface IValueEditor(Of T)
        Inherits ISimpleValueEditor
        Shadows Property Value As T
        Shadows Event ValueChanged(ByVal sender As IValueEditor(Of T))
    End Interface

    <ContractClassFor(GetType(IPickle(Of )))>
    Public MustInherit Class ContractClassIPickle(Of T)
        Implements IPickle(Of T)
        Public Shadows ReadOnly Property Value As T Implements IPickle(Of T).Value
            Get
                Contract.Ensures(Contract.Result(Of T)() IsNot Nothing)
                Throw New NotSupportedException
            End Get
        End Property
        Private ReadOnly Property SimpleValue As Object Implements ISimplePickle.Value
            Get
                Throw New NotSupportedException
            End Get
        End Property
        Private ReadOnly Property SimpleData As IRist(Of Byte) Implements ISimplePickle.Data
            Get
                Throw New NotSupportedException
            End Get
        End Property
        Private ReadOnly Property SimpleJar As ISimpleJar Implements ISimplePickle.Jar
            Get
                Throw New NotSupportedException
            End Get
        End Property
    End Class
    <ContractClassFor(GetType(IJar(Of )))>
    Public MustInherit Class ContractClassIJar(Of T)
        Implements IJar(Of T)

        <Pure()>
        Public Shadows Function Describe(ByVal value As T) As String Implements IJar(Of T).Describe
            Contract.Requires(value IsNot Nothing)
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
        <Pure()>
        Public Shadows Function Parse(ByVal data As IRist(Of Byte)) As ParsedValue(Of T) Implements IJar(Of T).Parse
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ParsedValue(Of T))() IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ParsedValue(Of T))().UsedDataCount <= data.Count)
            Throw New NotSupportedException
        End Function
        <Pure()>
        Public Shadows Function Parse(ByVal text As String) As T Implements IJar(Of T).Parse
            Contract.Requires(text IsNot Nothing)
            Contract.Ensures(Contract.Result(Of T)() IsNot Nothing)
            Throw New NotSupportedException
        End Function
        Private Function SimpleDescribe(ByVal value As Object) As String Implements ISimpleJar.Describe
            Throw New NotSupportedException
        End Function
        Private Function SimpleMakeControl() As ISimpleValueEditor Implements ISimpleJar.MakeControl
            Throw New NotSupportedException
        End Function
        Private Function SimpleParse(ByVal data As IRist(Of Byte)) As ParsedValue(Of Object) Implements ISimpleJar.Parse
            Throw New NotSupportedException()
        End Function
        Private Function SimplePack(ByVal value As Object) As IEnumerable(Of Byte) Implements ISimpleJar.Pack
            Throw New NotSupportedException
        End Function
        Private Function SimpleParse(ByVal text As String) As Object Implements ISimpleJar.Parse
            Throw New NotSupportedException
        End Function
    End Class
    <ContractClassFor(GetType(IValueEditor(Of )))>
    MustInherit Class ContractClassIValueEditor(Of T)
        Implements IValueEditor(Of T)
        Public Event SimpleValueChanged(ByVal sender As ISimpleValueEditor) Implements ISimpleValueEditor.ValueChanged
        Public Event ValueChanged(ByVal sender As IValueEditor(Of T)) Implements IValueEditor(Of T).ValueChanged
        Public Property Value As T Implements IValueEditor(Of T).Value
            Get
                Contract.Ensures(Contract.Result(Of T)() IsNot Nothing)
                Throw New NotSupportedException
            End Get
            Set(ByVal value As T)
                Contract.Requires(value IsNot Nothing)
                Throw New NotSupportedException
            End Set
        End Property
        Public ReadOnly Property Control As System.Windows.Forms.Control Implements ISimpleValueEditor.Control
            Get
                Throw New NotSupportedException
            End Get
        End Property
        Public Property SimpleValue As Object Implements ISimpleValueEditor.Value
            Get
                Throw New NotSupportedException
            End Get
            Set(ByVal value As Object)
                Throw New NotSupportedException
            End Set
        End Property
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
End Namespace
