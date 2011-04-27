Namespace Pickling
    '''<summary>Holds a value, a description of the value, and a serialization of the value.</summary>
    <ContractClass(GetType(ContractClassIPickle(Of )))>
    Public Interface IPickle(Of Out T)
        ReadOnly Property Data As IRist(Of Byte)
        ReadOnly Property Jar As IJar(Of Object)
        ReadOnly Property Value As T
    End Interface

    '''<summary>Parses data and packs values into pickles.</summary>
    <ContractClass(GetType(ContractClassIJar(Of )))>
    Public Interface IJar(Of T)
        Function Pack(value As T) As IRist(Of Byte)
        Function Parse(data As IRist(Of Byte)) As ParsedValue(Of T)
        Function MakeControl() As IValueEditor(Of T)
        Function Describe(value As T) As String
        Function Parse(text As String) As T
    End Interface

    '''<summary>A named jar which parses data and packs values into pickles.</summary>
    Public Interface INamedJar(Of T)
        Inherits IJar(Of T)
        ReadOnly Property Name As InvariantString
    End Interface

    <ContractClass(GetType(ISimpleValueEditor.ContractClass))>
    Public Interface ISimpleValueEditor
        Inherits IDisposable

        ReadOnly Property Control As Control
        Property Value As Object
        Event ValueChanged(sender As ISimpleValueEditor)

        <ContractClassFor(GetType(ISimpleValueEditor))>
        MustInherit Class ContractClass
            Implements ISimpleValueEditor
            Public Event ValueChanged(sender As ISimpleValueEditor) Implements ISimpleValueEditor.ValueChanged
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
                Set(value As Object)
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
        Shadows Event ValueChanged(sender As IValueEditor(Of T))
    End Interface

    <ContractClassFor(GetType(IPickle(Of )))>
    Public MustInherit Class ContractClassIPickle(Of T)
        Implements IPickle(Of T)
        Public ReadOnly Property Value As T Implements IPickle(Of T).Value
            Get
                Contract.Ensures(Contract.Result(Of T)() IsNot Nothing)
                Throw New NotSupportedException
            End Get
        End Property
        Private ReadOnly Property Data As IRist(Of Byte) Implements IPickle(Of T).Data
            Get
                Contract.Ensures(Contract.Result(Of IRist(Of Byte))() IsNot Nothing)
                Throw New NotSupportedException
            End Get
        End Property
        Private ReadOnly Property Jar As IJar(Of Object) Implements IPickle(Of T).Jar
            Get
                Contract.Ensures(Contract.Result(Of IJar(Of Object))() IsNot Nothing)
                Throw New NotSupportedException
            End Get
        End Property
    End Class
    <ContractClassFor(GetType(IJar(Of )))>
    Public MustInherit Class ContractClassIJar(Of T)
        Implements IJar(Of T)

        <Pure()>
        Public Shadows Function Describe(value As T) As String Implements IJar(Of T).Describe
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
        Public Shadows Function Pack(value As T) As IRist(Of Byte) Implements IJar(Of T).Pack
            Contract.Requires(value IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IRist(Of Byte))() IsNot Nothing)
            Throw New NotSupportedException
        End Function
        <Pure()>
        Public Shadows Function Parse(data As IRist(Of Byte)) As ParsedValue(Of T) Implements IJar(Of T).Parse
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ParsedValue(Of T))() IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ParsedValue(Of T))().UsedDataCount <= data.Count)
            Throw New NotSupportedException
        End Function
        <Pure()>
        Public Shadows Function Parse(text As String) As T Implements IJar(Of T).Parse
            Contract.Requires(text IsNot Nothing)
            Contract.Ensures(Contract.Result(Of T)() IsNot Nothing)
            Throw New NotSupportedException
        End Function
    End Class
    <ContractClassFor(GetType(IValueEditor(Of )))>
    MustInherit Class ContractClassIValueEditor(Of T)
        Implements IValueEditor(Of T)
        Public Event SimpleValueChanged(sender As ISimpleValueEditor) Implements ISimpleValueEditor.ValueChanged
        Public Event ValueChanged(sender As IValueEditor(Of T)) Implements IValueEditor(Of T).ValueChanged
        Public Property Value As T Implements IValueEditor(Of T).Value
            Get
                Contract.Ensures(Contract.Result(Of T)() IsNot Nothing)
                Throw New NotSupportedException
            End Get
            Set(value As T)
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
            Set(value As Object)
                Throw New NotSupportedException
            End Set
        End Property
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
End Namespace
