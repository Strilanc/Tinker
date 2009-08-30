''Imports HostBot.Immutable
''Imports HostBot.Mpq
''Imports HostBot.Mpq.Compression
''Imports HostBot.Mpq.Common
''Imports HostBot.Pickling.Jars
''Imports System.IO.Compression
''Imports System.Net
''Imports System.Net.Sockets

'Public Module Test
'End Module
'Public Module MX
'    '    Private Sub XXX()
'    '        Dim archive = New Mpq.MpqArchive("C:\Program Files (x86)\Warcraft III\Maps\Test\Castle Fight v1.13b.mpq")
'    '        Dim file = archive.OpenFile("war3map.w3i")
'    '        Dim data(0 To CInt(file.Length) - 1) As Byte
'    '        file.Read(data, 0, data.Length)

'    '    End Sub
'    '    Private Sub compare(ByVal s1 As IO.Stream, ByVal s2 As IO.Stream)
'    '        Do
'    '            Dim b1 = s1.ReadByte, b2 = s2.ReadByte
'    '            If b1 <> b2 Then b1 = b1
'    '            If b1 = -1 Or b2 = -1 Then Exit Do
'    '        Loop
'    '    End Sub
'    Private Sub FixJ(ByVal reader As IO.StreamReader, ByVal writer As IO.StreamWriter)
'        Dim bufferedLine = ""
'        Dim wasReturning = False
'        Dim indent = 0
'        Dim nextIndent = 0
'        Dim prevIndent = 0
'        While Not reader.EndOfStream
'            Dim line = reader.ReadLine().Trim()
'            If line = "" Then Continue While

'            'Track indentation level to make output more readable
'            indent = nextIndent
'            If line Like "if*" Or line Like "function*" Or line Like "constant function*" Or line Like "globals*" Or line Like "loop*" Then
'                nextIndent = indent + 1
'            ElseIf line Like "else*" Then
'                indent -= 1
'            ElseIf line Like "end*" Then
'                indent -= 1
'                nextIndent = indent
'            End If

'            'Place DoNothing() between double return lines, fooling the return bug detector
'            Dim returning = line Like "return*"
'            If wasReturning Then
'                Dim prevIndentation = New String(" "c, prevIndent * 4)
'                writer.WriteLine(prevIndentation + bufferedLine)
'                writer.WriteLine(prevIndentation + "call DoNothing()")
'            End If

'            'Split IF statements where a block ends in a return, avoiding false-positives from the return bug detector
'            Dim indentation = New String(" "c, indent * 4)
'            If wasReturning And line Like "elseif*" Then
'                writer.WriteLine(indentation + "endif")
'                writer.WriteLine(indentation + line.Substring(4))
'            ElseIf wasReturning And line Like "else*" Then
'                writer.WriteLine(indentation + "endif")
'                writer.WriteLine(indentation + "if true then")
'            ElseIf Not returning Then
'                writer.WriteLine(indentation + line)
'            Else
'                'written by next iteration inside 'If wasReturning then' block
'                bufferedLine = line
'            End If

'            wasReturning = returning
'            prevIndent = indent
'        End While
'    End Sub
'    Public Sub FixMap(ByVal path As String)
'        Dim archive = New Mpq.MpqArchive(path)
'        Dim j = If(archive.hashTable.contains("war3map.j"), "war3map.j", "scripts\war3map.j")
'        Dim file = archive.OpenFile(j)
'        Dim reader = New IO.StreamReader(file)
'        If IO.File.Exists(path & ".j") Then IO.File.Delete(path & ".j")
'        Dim writer = New IO.StreamWriter(New IO.FileStream(path & ".j", IO.FileMode.OpenOrCreate, IO.FileAccess.Write, IO.FileShare.None))
'        FixJ(reader, writer)
'        reader.Dispose()
'        writer.Dispose()
'        file.Dispose()
'        If archive.hashTable.contains("(attributes)") Then
'            archive.WriteToFile(path & "copy.w3x", j, "replace " + path & ".j", "(attributes)", "delete")
'        Else
'            archive.WriteToFile(path & "copy.w3x", j, "replace " + path & ".j")
'        End If
'    End Sub
'End Module
''Public Module TestModule
''    'send different initialization data to each player, for multiple players in a single slot
''    '    Case "Download\multimap.w3x"
''    '        Dim address = unpackUInteger(packHexString("28 06 00 00"))
''    '        For Each receiver In players_L
''    '            If receiver.is_fake Then Continue For
''    '            Dim data = New Byte() {}
''    '            For Each player In players_L
''    '                If player.is_fake Then Continue For
''    '                Dim slot = get_player_slot_L(player)
''    '                If player Is receiver Then
''    '                    slot = slot.oppslot
''    '                End If
''    '                Dim text = "hero" + slot.index.ToString() + player.name_P
''    '                Dim pd = packeteer.packData_fireChatTrigger(slots_L(0).player, text, address)
''    '                data = concat(data, New Byte() {slots_L(0).player.index_I}, packUInteger(CUShort(pd.Length), 2), pd)
''    '            Next player
''    '            receiver.queue_tick_R(New W3PlayerTickRecord(100, Environment.TickCount))
''    '            receiver.send_packet_R(packeteer.packPacket_GAME_TICK_HOST(100, data))
''    '        Next receiver

''    Public Sub Connect(ByVal hostname As String, ByVal port As UShort)
''        '...
''        '... do whatever you're doing leading up to a connection
''        '...

''        Dim client = New TcpClient
''        Try
''            client.BeginConnect(hostname, port, AddressOf EndConnect, client)
''        Catch e As SocketException
''            'deal with failure to connect
''        End Try

''        '...
''        '... do whatever you're doing leading up to a connection
''        '...

''        FutureConnectX(hostname, port).CallWhenValueReady(
''            Sub(possibleClient)
''                If possibleClient.Exception IsNot Nothing Then
''                    'deal with failure to connect
''                    Return
''                End If
''                Dim client2 = possibleClient.Value

''                '...
''                'do whatever you wanted to do with the client once it was connected
''                '...
''            End Sub
''        )

''        '...
''    End Sub
''    Private Sub EndConnect(ByVal ar As IAsyncResult)
''        Dim client = CType(ar.AsyncState, TcpClient)
''        Try
''            client.EndConnect(ar)
''        Catch e As SocketException
''            'deal with failure to connect
''        End Try

''        '...
''        'do whatever you wanted to do with the client once it was connected
''        '...
''    End Sub

''    Public Function FutureConnectX(ByVal hostname As String,
''                                  ByVal port As UShort) As IFuture(Of PossibleException(Of TcpClient, SocketException))
''        Dim f = New Future(Of PossibleException(Of TcpClient, SocketException))
''        Try
''            Dim client = New TcpClient
''            client.BeginConnect(hostname, port, Sub(ar)
''                                                    Try
''                                                        client.EndConnect(ar)
''                                                        f.SetValue(client)
''                                                    Catch e As SocketException
''                                                        f.SetValue(e)
''                                                    End Try
''                                                End Sub, Nothing)
''        Catch e As SocketException
''            f.SetValue(e)
''        End Try
''        Return f
''    End Function
''End Module

''Public Class BankAccount
''    Private balance As UInteger
''    Private ReadOnly lock As New Object()
''    Public Function Deposit(ByVal value As UInteger) As UInteger
''        SyncLock lock
''            balance += value
''            Return balance
''        End SyncLock
''    End Function
''    Public Function Withdraw(ByVal value As UInteger) As UInteger?
''        SyncLock lock
''            If balance < value Then Return Nothing
''            balance -= value
''            Return balance
''        End SyncLock
''    End Function
''End Class

''Public Class FutureBankAccount
''    Private balance As UInteger
''    Private queue As ICallQueue = New ThreadPooledCallQueue()
''    Public Function FutureDeposit(ByVal value As UInteger) As IFuture(Of UInteger)
''        Return queue.QueueFunc(Function()
''                                   balance += value
''                                   Return balance
''                               End Function)
''    End Function
''    Public Function Withdraw(ByVal value As UInteger) As IFuture(Of UInteger?)
''        Return queue.QueueFunc(Function() As UInteger?
''                                   If balance < value Then  Return Nothing
''                                   balance -= value
''                                   Return balance
''                               End Function)
''    End Function
''End Class

''Public Module Main
''    Public Sub Main()
''        Const NumPhilosphers As Integer = 5
''        Dim chopSticks(0 To NumPhilosphers - 1) As Chopstick
''        Dim philosophers(0 To NumPhilosphers - 1) As DiningPhilosopher
''        For i = 0 To NumPhilosphers - 1
''            chopSticks(i) = New Chopstick()
''        Next i
''        Dim r = New Random()
''        For i = 0 To NumPhilosphers - 1
''            philosophers(i) = New DiningPhilosopher(r, chopSticks(i), chopSticks((i + 1) Mod NumPhilosphers))
''        Next i
''    End Sub
''End Module

''Public Class Chopstick
''    Public queue As ICallQueue = New ThreadPooledCallQueue()
''    Private inUse As Boolean

''    Public Function FuturePickUp() As IFuture(Of Boolean)
''        Return queue.QueueFunc(Function()
''                                   If inUse Then  Return False
''                                   inUse = True
''                               End Function)
''    End Function
''    Public Function FuturePutDown() As IFuture
''        Return queue.QueueAction(Sub()
''                                     inUse = False
''                                 End Sub)
''    End Function
''End Class

''Public Class DiningPhilosopher
''    Private ReadOnly leftChopStick As Chopstick
''    Private ReadOnly rightChopStick As Chopstick

''    Public Sub New(ByVal r As Random, ByVal leftChopStick As Chopstick, ByVal rightChopStick As Chopstick)
''        Me.leftChopStick = leftChopStick
''        Me.rightChopStick = rightChopStick
''        Call New Threading.Thread(Sub() TryGrabChopSticks(r)).Start()
''    End Sub

''    Private Sub TryGrabChopSticks(ByVal r As Random)
''        Dim futureGrabLeft = leftChopStick.FuturePickUp()
''        Dim futureGrabRight = rightChopStick.FuturePickUp()

''        futureGrabLeft.CallWhenValueReady(
''            Sub(grabbedLeft)
''                futureGrabRight.CallWhenValueReady(
''                    Sub(grabbedRight)
''                        If Not grabbedRight Or Not grabbedLeft Then
''                            'Not enough to eat, better try again later
''                            If grabbedLeft Then  leftChopStick.FuturePutDown()
''                            If grabbedRight Then  rightChopStick.FuturePutDown()
''                            Threading.Thread.Sleep(100 + r.Next(100))
''                        Else
''                            'Yay! Food!
''                            Threading.Thread.Sleep(500 + r.Next(500))
''                            leftChopStick.FuturePutDown()
''                            rightChopStick.FuturePutDown()

''                            'Sleep
''                            Threading.Thread.Sleep(500 + r.Next(500))
''                        End If

''                        'Hungry!
''                        Call TryGrabChopSticks(r)
''                    End Sub
''                )
''            End Sub
''        )
''    End Sub
''End Class
