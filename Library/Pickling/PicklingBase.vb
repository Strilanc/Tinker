Namespace Pickling
    Public Class Pickle(Of T)
        Implements IPickle(Of T)

        Private ReadOnly _data As ViewableList(Of Byte)
        Private ReadOnly _value As T
        Private ReadOnly _description As ExpensiveValue(Of String)
        Public Sub New(ByVal value As T,
                       ByVal data As ViewableList(Of Byte),
                       ByVal description As ExpensiveValue(Of String))
            Me._data = data
            Me._value = value
            Me._description = description
        End Sub

        Public Sub New(ByVal jarName As String,
                       ByVal value As T,
                       ByVal data As ViewableList(Of Byte))
            Me.new(value, data, New ExpensiveValue(Of String)(Function() jarName + " = " + value.ToString))
        End Sub
        Public Sub New(ByVal jarName As String,
                       ByVal value As T,
                       ByVal data As ViewableList(Of Byte),
                       ByVal valueDescription As Func(Of String))
            Me.new(value, data, New ExpensiveValue(Of String)(Function() jarName + " = " + valueDescription()))
        End Sub

        Public Shared Function MakeListDescription(Of X)(ByVal pickles As IEnumerable(Of IPickle(Of X))) As String
            Return "{" + Environment.NewLine +
                        indent(String.Join(Environment.NewLine,
                               (From e In pickles Select e.Description.Value).ToArray)) +
                    Environment.NewLine + "}"
        End Function

        Public ReadOnly Property Description As ExpensiveValue(Of String) Implements IPickle(Of T).Description
            Get
                Return _description
            End Get
        End Property
        Public ReadOnly Property Data As ViewableList(Of Byte) Implements IPickle(Of T).Data
            Get
                Return _data
            End Get
        End Property
        Public ReadOnly Property Value As T Implements IPickle(Of T).Value
            Get
                Return _value
            End Get
        End Property
    End Class

    Public MustInherit Class PackJar(Of T)
        Implements IPackJar(Of T)
        Private ReadOnly _name As String
        <ContractInvariantMethod()> Protected Sub Invariant()
            Contract.Invariant(_name IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As String)
            Contract.Requires(name IsNot Nothing)
            Me._name = name
        End Sub

        Public ReadOnly Property Name As String Implements IJarInfo.Name
            Get
                Return _name
            End Get
        End Property

        Public MustOverride Function Pack(Of R As T)(ByVal value As R) As IPickle(Of R) Implements IPackJar(Of T).Pack
    End Class
    Public MustInherit Class ParseJar(Of T)
        Implements IParseJar(Of T)
        Private ReadOnly _name As String
        <ContractInvariantMethod()> Protected Sub Invariant()
            Contract.Invariant(_name IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As String)
            Contract.Requires(name IsNot Nothing)
            Me._name = name
        End Sub

        Public ReadOnly Property Name As String Implements IJarInfo.Name
            Get
                Return _name
            End Get
        End Property

        Public MustOverride Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of T) Implements IParseJar(Of T).Parse
    End Class
    Public MustInherit Class Jar(Of T)
        Implements IJar(Of T)
        Private ReadOnly _name As String
        <ContractInvariantMethod()> Protected Overridable Sub Invariant()
            Contract.Invariant(_name IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As String)
            Contract.Requires(name IsNot Nothing)
            Me._name = name
        End Sub

        Public ReadOnly Property Name As String Implements IJarInfo.Name
            Get
                Return _name
            End Get
        End Property

        Public MustOverride Function Pack(Of R As T)(ByVal value As R) As IPickle(Of R) Implements IPackJar(Of T).Pack
        Public MustOverride Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of T) Implements IParseJar(Of T).Parse
    End Class

    Public Module PicklingExtensionMethods
        <Extension()> Public Function Weaken(Of T)(ByVal jar As IJar(Of T)) As IJar(Of Object)
            Return New WeakJar(Of T)(jar)
        End Function
        <Extension()> Public Function Weaken(Of T)(ByVal jar As IPackJar(Of T)) As IPackJar(Of Object)
            Return New WeakPackJar(Of T)(jar)
        End Function
        <Extension()> Public Function Weaken(Of T)(ByVal jar As IParseJar(Of T)) As IParseJar(Of Object)
            Return New WeakParseJar(Of T)(jar)
        End Function

        Private Class WeakJar(Of T)
            Inherits Jar(Of Object)
            Private ReadOnly subjar As IJar(Of T)
            Public Sub New(ByVal jar As IJar(Of T))
                MyBase.New(jar.Name)
                Me.subjar = jar
            End Sub
            Public Overrides Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of Object)
                Dim p = subjar.Parse(data)
                Return New Pickle(Of Object)(p.Value, p.Data, p.Description)
            End Function
            Public Overrides Function Pack(Of R As Object)(ByVal value As R) As IPickle(Of R)
                Dim p = subjar.Pack(CType(CType(value, Object), T))
                Return New Pickle(Of R)(value, p.Data, p.Description)
            End Function
        End Class
        Private Class WeakPackJar(Of T)
            Inherits PackJar(Of Object)
            Private ReadOnly subjar As IPackJar(Of T)
            Public Sub New(ByVal jar As IPackJar(Of T))
                MyBase.New(jar.Name)
                Me.subjar = jar
            End Sub
            Public Overrides Function Pack(Of R As Object)(ByVal value As R) As IPickle(Of R)
                Dim p = subjar.Pack(CType(CType(value, Object), T))
                Return New Pickle(Of R)(value, p.Data, p.Description)
            End Function
        End Class
        Private Class WeakParseJar(Of T)
            Inherits ParseJar(Of Object)
            Private ReadOnly subjar As IParseJar(Of T)
            Public Sub New(ByVal jar As IParseJar(Of T))
                MyBase.New(jar.Name)
                Me.subjar = jar
            End Sub
            Public Overrides Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of Object)
                Dim p = subjar.Parse(data)
                Return New Pickle(Of Object)(p.Value, p.Data, p.Description)
            End Function
        End Class
    End Module
End Namespace