Imports System.Net.Sockets
Imports HostBot
Imports Strilbrary.Numerics
Imports Strilbrary.Threading
Imports Strilbrary.Enumeration

Friend Class StreamTester
    Implements IDisposable
    Private ReadOnly testStream As IO.Stream
    Private ReadOnly dataReader As IO.StreamReader

    Public Sub New(ByVal testStream As IO.Stream, ByVal data As IO.StreamReader)
        Me.testStream = testStream
        Me.dataReader = data
    End Sub

    Public Function AsyncRun() As ifuture
        Dim result = New FutureAction()
        ThreadPooledAction(Sub() RunContinue(result))
        Return result
    End Function
    Private Sub RunContinue(ByVal result As FutureAction)
        Try
            If dataReader.EndOfStream Then
                result.SetSucceeded()
                Return
            End If
            Select Case dataReader.ReadLine.ToUpper
                Case "IGNORE"
                    Dim data = dataReader.ReadLine.FromHexStringToBytes
                    testStream.FutureRead(data, 0, data.Length).CallWhenValueReady(
                        Sub(value, exception)
                            If exception IsNot Nothing Then
                                result.SetFailed(exception)
                            ElseIf value < data.Length Then
                                result.SetFailed(New IO.IOException("Data ended before expected."))
                            Else
                                ThreadPooledAction(Sub() RunContinue(result))
                            End If
                        End Sub)
                Case "READ"
                    Dim expectedData = dataReader.ReadLine.FromHexStringToBytes
                    Dim data(0 To expectedData.Length - 1) As Byte
                    testStream.FutureRead(data, 0, data.Length).CallWhenValueReady(
                        Sub(value, exception)
                            If exception IsNot Nothing Then
                                result.SetFailed(exception)
                            ElseIf value < data.Length Then
                                result.SetFailed(New IO.IOException("Data ended before expected."))
                            Else
                                If expectedData.HasSameItemsAs(data) Then
                                    ThreadPooledAction(Sub() RunContinue(result))
                                Else
                                    result.SetFailed(New IO.IOException("Incorrect data."))
                                End If
                            End If
                        End Sub)
                Case "WRITE"
                    Dim data = dataReader.ReadLine.FromHexStringToBytes
                    testStream.Write(data, 0, data.Length)
                    ThreadPooledAction(Sub() RunContinue(result))
                Case "CLOSE"
                    testStream.Close()
                    ThreadPooledAction(Sub() RunContinue(result))
                Case Else
                    result.SetFailed(New IO.IOException("Unrecognized command in test data."))
            End Select
        Catch e As Exception
            result.SetFailed(e)
        End Try
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        testStream.Close()
        GC.SuppressFinalize(Me)
    End Sub
End Class