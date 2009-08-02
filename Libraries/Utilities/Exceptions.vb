<Serializable()>
Public Class OperationFailedException
    Inherits Exception
    Public Sub New(Optional ByVal message As String = Nothing,
                   Optional ByVal inner_exception As Exception = Nothing)
        MyBase.New(message, inner_exception)
    End Sub
End Class

<Serializable()>
Public Class UnreachableException
    Inherits InvalidStateException
    Public Sub New(Optional ByVal message As String = Nothing,
                   Optional ByVal innerException As Exception = Nothing)
        MyBase.New(If(message, "Reached a state which was expected to not be reachable."), innerException)
    End Sub
End Class

<Serializable()>
Public Class InvalidStateException
    Inherits Exception
    Public Sub New(Optional ByVal message As String = Nothing,
                   Optional ByVal innerException As Exception = Nothing)
        MyBase.New(If(message, "Reached an invalid state."), innerException)
    End Sub
End Class
