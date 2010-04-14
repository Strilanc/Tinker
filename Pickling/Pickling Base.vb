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

    Public NotInheritable Class ParsedValue(Of T)
        Private ReadOnly _value As T
        Private ReadOnly _usedDataCount As Int32

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_value IsNot Nothing)
            Contract.Invariant(_usedDataCount >= 0)
        End Sub

        Public Sub New(ByVal value As T, ByVal usedDataCount As Int32)
            Contract.Requires(value IsNot Nothing)
            Contract.Requires(usedDataCount >= 0)
            Contract.Ensures(Me.UsedDataCount = usedDataCount)
            Me._value = value
            Me._usedDataCount = usedDataCount
        End Sub

        Public ReadOnly Property Value As T
            Get
                Contract.Ensures(Contract.Result(Of T)() IsNot Nothing)
                Return _value
            End Get
        End Property
        Public ReadOnly Property UsedDataCount As Int32
            Get
                Contract.Ensures(Contract.Result(Of Int32)() >= 0)
                Return _usedDataCount
            End Get
        End Property
    End Class

    '''<summary>Parses data and packs values into pickles.</summary>
    Public MustInherit Class BaseJar(Of T)
        Implements IJar(Of T)

        Public MustOverride Function Pack(ByVal value As T) As IEnumerable(Of Byte) Implements IJar(Of T).Pack
        Public MustOverride Function Parse(ByVal data As IReadableList(Of Byte)) As ParsedValue(Of T) Implements IJar(Of T).Parse
        Public MustOverride Function Parse(ByVal text As String) As T Implements IJar(Of T).Parse
        Public Overridable Function Describe(ByVal value As T) As String Implements IJar(Of T).Describe
            Return value.ToString()
        End Function
        Public Overridable Function MakeControl() As IValueEditor(Of T) Implements IJar(Of T).MakeControl
            Dim control = New TextBox()
            control.Text = ""
            Dim defaultValue = [Default](Of T)()
            If defaultValue IsNot Nothing Then '[T is a value type]
                control.Text = Describe(defaultValue)
            End If
            Return New DelegatedValueEditor(Of T)(
                control:=control,
                eventAdder:=Sub(action) AddHandler control.TextChanged, Sub() action(),
                getter:=Function() Parse(control.Text),
                setter:=Sub(value) control.Text = Describe(value),
                disposer:=Sub() control.Dispose())
        End Function

        Private Function SimpleMakeControl() As ISimpleValueEditor Implements ISimpleJar.MakeControl
            Return MakeControl()
        End Function
        Private Function SimplePack(ByVal value As Object) As IEnumerable(Of Byte) Implements ISimpleJar.Pack
            Return Pack(DirectCast(value, T).AssumeNotNull)
        End Function
        Private Function SimpleParse(ByVal data As IReadableList(Of Byte)) As ParsedValue(Of Object) Implements ISimpleJar.Parse
            Dim result = Parse(data)
            Return New ParsedValue(Of Object)(result.Value, result.UsedDataCount)
        End Function
        Public Function SimpleDescribe(ByVal value As Object) As String Implements ISimpleJar.Describe
            Return Describe(DirectCast(value, T).AssumeNotNull)
        End Function
        Public Function SimpleParse(ByVal text As String) As Object Implements ISimpleJar.Parse
            Return Parse(text)
        End Function
    End Class

    '''<summary>Parses data of a fixed size and packs values into pickles.</summary>
    <ContractClass(GetType(BaseFixedSizeJarContractClass(Of )))>
    Public MustInherit Class BaseFixedSizeJar(Of T)
        Inherits BaseJar(Of T)
        Protected MustOverride ReadOnly Property DataSize As UInt16
        Protected MustOverride Function FixedSizeParse(ByVal data As IReadableList(Of Byte)) As T
        <ContractVerification(False)>
        Public NotOverridable Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As ParsedValue(Of T)
            If data.Count < DataSize Then Throw New PicklingNotEnoughDataException("{0} requires {1} bytes.".Frmt(Me.GetType.Name, DataSize))
            Return FixedSizeParse(data.SubView(0, DataSize)).ParsedWithDataCount(DataSize)
        End Function
    End Class
    <ContractClassFor(GetType(BaseFixedSizeJar(Of )))>
    Public MustInherit Class BaseFixedSizeJarContractClass(Of T)
        Inherits BaseFixedSizeJar(Of T)
        Protected Overrides ReadOnly Property DataSize As UShort
            Get
                Throw New NotSupportedException
            End Get
        End Property
        Protected Overrides Function FixedSizeParse(ByVal data As IReadableList(Of Byte)) As T
            Contract.Requires(data IsNot Nothing)
            Contract.Requires(data.Count = DataSize)
            Contract.Ensures(Contract.Result(Of T)() IsNot Nothing)
            Throw New NotSupportedException
        End Function
        Public Overrides Function Pack(ByVal value As T) As System.Collections.Generic.IEnumerable(Of Byte)
            Throw New NotSupportedException
        End Function
        Public Overloads Overrides Function Parse(ByVal text As String) As T
            Throw New NotSupportedException
        End Function
    End Class

    Public NotInheritable Class DelegatedValueEditor(Of T)
        Inherits DisposableWithTask
        Implements IValueEditor(Of T)

        Private ReadOnly _getter As Func(Of T)
        Private ReadOnly _setter As Action(Of T)
        Private ReadOnly _control As Control
        Private ReadOnly _disposer As Action

        Private _blockEvents As Boolean

        Public Event ValueChanged(ByVal sender As IValueEditor(Of T)) Implements IValueEditor(Of T).ValueChanged
        Private Event ValueChangedSimple(ByVal sender As ISimpleValueEditor) Implements ISimpleValueEditor.ValueChanged

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_getter IsNot Nothing)
            Contract.Invariant(_setter IsNot Nothing)
            Contract.Invariant(_control IsNot Nothing)
            Contract.Invariant(_disposer IsNot Nothing)
        End Sub

        Public Sub New(ByVal control As Control,
                       ByVal getter As Func(Of T),
                       ByVal setter As Action(Of T),
                       ByVal eventAdder As Action(Of Action),
                       ByVal disposer As Action)
            Contract.Requires(control IsNot Nothing)
            Contract.Requires(getter IsNot Nothing)
            Contract.Requires(setter IsNot Nothing)
            Contract.Requires(eventAdder IsNot Nothing)
            Contract.Requires(disposer IsNot Nothing)
            Me._control = control
            Me._getter = getter
            Me._setter = setter
            Me._disposer = disposer
            Call eventAdder(AddressOf RaiseValueChanged)
        End Sub
        Private Sub RaiseValueChanged()
            If _blockEvents Then Return
            RaiseEvent ValueChanged(Me)
            RaiseEvent ValueChangedSimple(Me)
        End Sub

        Public ReadOnly Property Control As Control Implements ISimpleValueEditor.Control
            Get
                Return _control
            End Get
        End Property

        Public Property Value As T Implements IValueEditor(Of T).Value
            Get
                Return _getter().AssumeNotNull
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
                Me.Value = DirectCast(value, T).AssumeNotNull
            End Set
        End Property

        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As Task
            If finalizing Then Return Nothing
            Call _disposer()
            Return Nothing
        End Function
    End Class
End Namespace
