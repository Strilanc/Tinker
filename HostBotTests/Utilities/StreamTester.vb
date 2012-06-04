Imports Tinker
Imports Strilbrary.Values
Imports Strilbrary.Threading
Imports Strilbrary.Collections
Imports System.Threading.Tasks
Imports System.Threading

Friend Class StreamTester
    Implements IDisposable
    Private ReadOnly testStream As IO.Stream
    Private ReadOnly dataReader As IO.StreamReader

    Public Sub New(testStream As IO.Stream, data As IO.StreamReader)
        Me.testStream = testStream
        Me.dataReader = data
    End Sub

    Public Function AsyncRun() As Task
        Dim result = New TaskCompletionSource(Of Boolean)()
        ThreadPool.QueueUserWorkItem(Sub() RunContinue(result))
        Return result.Task
    End Function
    Private Sub RunContinue(result As TaskCompletionSource(Of Boolean))
        Try
            If dataReader.EndOfStream Then
                result.SetResult(True)
                Return
            End If
            Select Case dataReader.ReadLine.ToUpper
                Case "IGNORE"
                    Dim data = dataReader.ReadLine.FromHexStringToBytes
                    testStream.ReadAsync(data, 0, data.Length).ContinueWith(
                        Sub(task)
                            If task.Status = TaskStatus.Faulted Then
                                result.SetException(task.Exception.InnerExceptions)
                            ElseIf task.Result < data.Length Then
                                result.SetException(New IO.InvalidDataException("Data ended before expected."))
                            Else
                                ThreadPool.QueueUserWorkItem(Sub() RunContinue(result))
                            End If
                        End Sub)
                Case "READ"
                    Dim expectedData = dataReader.ReadLine.FromHexStringToBytes
                    Dim data(0 To expectedData.Length - 1) As Byte
                    testStream.ReadAsync(data, 0, data.Length).ContinueWith(
                        Sub(task)
                            If task.Status = TaskStatus.Faulted Then
                                result.SetException(task.Exception.InnerExceptions)
                            ElseIf task.Result < data.Length Then
                                result.SetException(New IO.InvalidDataException("Data ended before expected."))
                            Else
                                If expectedData.SequenceEqual(data) Then
                                    ThreadPool.QueueUserWorkItem(Sub() RunContinue(result))
                                Else
                                    result.SetException(New IO.IOException("Incorrect data."))
                                End If
                            End If
                        End Sub)
                Case "WRITE"
                    Dim data = dataReader.ReadLine.FromHexStringToBytes
                    testStream.Write(data, 0, data.Length)
                    ThreadPool.QueueUserWorkItem(Sub() RunContinue(result))
                Case "CLOSE"
                    testStream.Close()
                    ThreadPool.QueueUserWorkItem(Sub() RunContinue(result))
                Case Else
                    result.SetException(New IO.InvalidDataException("Unrecognized command in test data."))
            End Select
        Catch e As Exception
            result.SetException(e)
        End Try
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        testStream.Close()
        GC.SuppressFinalize(Me)
    End Sub
End Class