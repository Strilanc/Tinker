Public Interface IFactory(Of T)
    Function make() As T
End Interface
Public Class FileStreamFactory
    Implements IFactory(Of IO.Stream)
    Implements IFactory(Of IO.FileStream)
    Private ReadOnly path As String
    Private ReadOnly mode As IO.FileMode
    Private ReadOnly access As IO.FileAccess
    Private ReadOnly share As IO.FileShare
    Public Sub New(ByVal path As String, ByVal mode As IO.FileMode, ByVal access As IO.FileAccess, ByVal share As IO.FileShare)
        Me.path = path
        Me.mode = mode
        Me.access = access
        Me.share = share
    End Sub
    Private Function makeBase() As IO.Stream Implements IFactory(Of System.IO.Stream).make
        Return make()
    End Function
    Public Function make() As IO.FileStream Implements IFactory(Of System.IO.FileStream).make
        Return New IO.FileStream(path, mode, access, share)
    End Function
End Class
