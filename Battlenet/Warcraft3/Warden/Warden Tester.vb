Namespace Warcraft3.Warden
    '''<summary>Uses wireshark traces of warcraft 3 handling warden challenges to test WardenPacketHandler.</summary>
    Public Class WardenTester
        Private ReadOnly p_wc3 As New List(Of Byte())
        Private ReadOnly P_bnet As New List(Of Byte())
        Private ReadOnly seed As UInteger
        Private WithEvents handler As WardenPacketHandler
        Private rcv_index As Integer

        '''<summary>Loads a trace file, and initializes a handler with the correct seed.</summary>
        Public Sub New(ByVal path As String)
            Dim P(0 To 1) As ProducerConsumerStream
            P(0) = New ProducerConsumerStream()
            P(1) = New ProducerConsumerStream()
            Dim read_header = False
            Dim header(0 To 1)() As Byte
            Using sr As New IO.StreamReader(New IO.FileStream(path, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read))
                Dim b As Boolean
                Dim bb As New List(Of Byte)
                Do Until sr.EndOfStream
                    Dim line = sr.ReadLine()
                    If line Like "*{" Then
                        If bb.Count = 1 AndAlso bb(0) = 1 AndAlso b And Not read_header Then
                            read_header = True
                        Else
                            Call P(If(b, 0, 1)).Write(bb.ToArray, 0, bb.Count)
                        End If
                        bb.Clear()
                        b = line Like "*peer0*"
                    Else
                        For Each word In line.Split(" "c)
                            If word Like "0x??*" Then
                                bb.Add(CByte(dehex(word.Substring(2, 2), ByteOrder.BigEndian)))
                            End If
                        Next word
                    End If

                    For i = 0 To 1
                        If header(i) Is Nothing And P(i).Length >= 4 Then
                            ReDim header(i)(0 To 3)
                            P(i).Read(header(i), 0, 4)
                        End If
                        If header(i) IsNot Nothing Then
                            Dim plen = header(i)(2) + header(i)(3) * 256 - 4
                            If P(i).Length >= plen Then
                                Dim payload(0 To plen - 1) As Byte
                                P(i).Read(payload, 0, plen)
                                If header(i)(1) = Bnet.BnetPacketID.Warden Then
                                    If i = 0 Then
                                        p_wc3.Add(payload)
                                    Else
                                        P_bnet.Add(payload)
                                    End If
                                ElseIf header(i)(1) = Bnet.BnetPacketID.AuthenticationFinish And i = 0 Then
                                    seed = payload.SubArray(36, 4).ToUInteger(ByteOrder.LittleEndian)
                                End If
                                header(i) = Nothing
                                i -= 1
                                Continue For
                            End If
                        End If
                    Next i
                Loop
            End Using

            handler = New WardenPacketHandler(seed, New ThreadPooledCallQueue, moduleFolder:=GetTestingPath("Modules"))
        End Sub

        Private Shared Function GetTestingPath(ByVal sub_folder As String) As String
            Dim folder = GetDataFolderPath("Warden Testing")
            folder += sub_folder
            If Not IO.Directory.Exists(folder) Then IO.Directory.CreateDirectory(folder)
            folder += IO.Path.DirectorySeparatorChar
            Return folder
        End Function

        '''<summary>Runs all the tests in the default testing folder.</summary>
        Public Shared Sub run_tests()
            'Reset modules
            For Each file In IO.Directory.GetFiles(GetTestingPath("Modules"))
                IO.File.Delete(file)
            Next file
            For Each file In IO.Directory.GetFiles(GetTestingPath("Starting Modules"))
                IO.File.Copy(file, GetTestingPath("Modules") + IO.Path.GetFileName(file))
            Next file

            run_many(GetTestingPath("Traces"))
            Debug.Print("All Tests Completed.")
        End Sub

        '''<summary>Runs all the tests in a folder.</summary>
        Private Shared Sub run_many(ByVal folder As String)
            For Each file In IO.Directory.GetFiles(folder)
                Debug.Print("Running test {0}".frmt(IO.Path.GetFileName(file)))
                Call New WardenTester(file).run()
            Next file
        End Sub

        '''<summary>Gives the handler input from the trace file, causing it to produce testable output.</summary>
        Private Sub run()
            For Each payload In P_bnet
                handler.ReceiveData(payload.ToArray)
            Next payload
            Threading.Thread.Sleep(1000)
            If rcv_index < p_wc3.Count Then
                Throw New IO.IOException("DIdn't send as much data as wc3.")
            End If
        End Sub

        '''<summary>Compares handler output to the trace file output.</summary>
        Private Sub handler_send(ByVal data() As Byte) Handles handler.Send
            If rcv_index > p_wc3.Count Then
                Throw New IO.IOException("Sent data which wc3 did not send.")
            ElseIf rcv_index < p_wc3.Count Then
                If Not ArraysEqual(p_wc3(rcv_index), data) Then
                    Throw New IO.IOException("Sent different data compared to wc3.")
                End If
            End If
            rcv_index += 1
        End Sub
    End Class
End Namespace