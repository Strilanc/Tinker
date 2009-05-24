'Namespace Settings
'    Public Class SettingException
'        Inherits Exception
'        Public Sub New(ByVal message As String, Optional ByVal innerException As Exception = Nothing)
'            MyBase.New(message, innerException)
'        End Sub
'    End Class

'    Public Interface itfSetting
'        Property value() As String
'        Function validate(ByVal potential_value As String) As Boolean
'        Function description() As String
'        Function name() As String
'        Function [default]() As String
'        Function [type]() As String
'    End Interface

'    Public MustInherit Class BaseSetting
'        Implements itfSetting
'        Protected curDesc As String
'        Protected ReadOnly name As String
'        Private curValue As String = Nothing
'        Public Sub New(ByVal name As String, ByVal description As String)
'            Me.curDesc = description
'            Me.name = name
'        End Sub

'        Public Function getName() As String Implements itfSetting.name
'            Return name
'        End Function
'        Public Function description() As String Implements itfSetting.description
'            Return curDesc
'        End Function

'        Public MustOverride Function getDefault() As String Implements itfSetting.default
'        Public MustOverride Function validate(ByVal potential_value As String) As Boolean Implements itfSetting.validate
'        Public MustOverride Function type() As String Implements itfSetting.type

'        Public Property value() As String Implements itfSetting.value
'            Get
'                Return curValue
'            End Get
'            Set(ByVal value As String)
'                If Not validate(value) Then Throw New SettingException(String.Format("Invalid value for {0}: {1}", name, value))
'                curValue = value
'            End Set
'        End Property
'    End Class

'    Public Class TextSetting
'        Inherits BaseSetting
'        Private [default] As String
'        Public Sub New(ByVal name As String, ByVal [default] As String, ByVal description As String)
'            MyBase.New(name, description)
'            Me.default = [default]
'        End Sub
'        Public Overrides Function validate(ByVal potential_value As String) As Boolean
'            Return True
'        End Function
'        Public Overrides Function getDefault() As String
'            Return Me.default
'        End Function
'        Public Overrides Function type() As String
'            Return "Text"
'        End Function
'    End Class
'    Public Class NumericSetting
'        Inherits BaseSetting
'        Private min As Integer, max As Integer
'        Private [default] As String
'        Public Sub New(ByVal name As String, ByVal [default] As Integer, ByVal description As String, Optional ByVal min As Integer = 0, Optional ByVal max As Integer = Integer.MaxValue)
'            MyBase.New(name, description)
'            Me.min = min
'            Me.max = max
'            Me.default = Me.min.ToString()
'        End Sub
'        Public Overrides Function validate(ByVal potential_value As String) As Boolean
'            Dim n As Integer
'            If Not Integer.TryParse(potential_value, n) Then Return False
'            If n < Me.min Or n > Me.max Then Return False
'            Return True
'        End Function
'        Public Overrides Function getDefault() As String
'            Return Me.default
'        End Function
'        Public Overrides Function type() As String
'            'If max <> Integer.MaxValue And min = 0 Then
'            '    Return String.Format("Number[{0}:{1}]", min.ToString, max.ToString())
'            'ElseIf max <> Integer.MaxValue Then
'            '    Return String.Format("Number[:{0}]", max.ToString())
'            'ElseIf min <> 0 Then
'            '    Return String.Format("Number[{0}:]", min.ToString())
'            'End If
'            Return "Number"
'        End Function
'    End Class
'    Public Class EnumSetting
'        Inherits BaseSetting
'        Private possible_values As New List(Of String)
'        Public Sub New(ByVal name As String, ByVal description As String, ByVal possible_values As IEnumerable(Of String))
'            MyBase.New(name, description)
'            If possible_values.checkNotNull("possible_values")
'            For Each s As String In possible_values
'                Me.possible_values.Add(s)
'            Next s
'            If Me.possible_values.Count = 0 Then Throw New ArgumentException("Must have at least one possible value", "possible_values")
'        End Sub
'        Public Overrides Function validate(ByVal potential_value As String) As Boolean
'            For Each s As String In possible_values
'                If potential_value.ToLower() = s.ToLower() Then Return True
'            Next s
'            Return False
'        End Function
'        Public Overrides Function getDefault() As String
'            Return possible_values(0)
'        End Function
'        Public Overrides Function type() As String
'            return "Enum"
'        End Function
'    End Class
'    Public Class BoolSetting
'        Inherits EnumSetting
'        Public Sub New(ByVal name As String, ByVal description As String)
'            MyBase.New(name, description, New String() {"False", "True"})
'        End Sub
'        Public Overrides Function type() As String
'            Return "Boolean"
'        End Function
'    End Class

'    Public Class Settings
'        Public settings As New List(Of itfSetting)
'    End Class
'End Namespace