Imports Strilbrary.Values
Imports Strilbrary.Collections
Imports Strilbrary.Time
Imports Strilbrary.Threading
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports Tinker
Imports Tinker.Components
Imports TinkerTests.PicklingTest
Imports System.Threading.Tasks

<TestClass()>
Public Class ComponentTest
    Private Class BaseTestComponent
        Inherits DisposableWithTask
        Implements IBotComponent
        Private ReadOnly _logger As New Logger
        Private ReadOnly _name As InvariantString
        Private ReadOnly _type As InvariantString
        Public Sub New(ByVal name As InvariantString, ByVal type As InvariantString)
            Me._name = name
            Me._type = type
        End Sub
        Public ReadOnly Property Control As Windows.Forms.Control Implements IBotComponent.Control
            Get
                Throw New InvalidOperationException
            End Get
        End Property
        Public ReadOnly Property HasControl As Boolean Implements Tinker.Components.IBotComponent.HasControl
            Get
                Return False
            End Get
        End Property

        Public Function InvokeCommand(ByVal user As BotUser, ByVal argument As String) As Task(Of String) Implements IBotComponent.InvokeCommand
            Return "".AsTask
        End Function

        Public Function IsArgumentPrivate(ByVal argument As String) As Boolean Implements IBotComponent.IsArgumentPrivate
            Return False
        End Function

        Public ReadOnly Property Logger As Tinker.Logger Implements IBotComponent.Logger
            Get
                Return _logger
            End Get
        End Property

        Public ReadOnly Property Name As InvariantString Implements IBotComponent.Name
            Get
                Return _name
            End Get
        End Property
        Public ReadOnly Property Type As InvariantString Implements IBotComponent.Type
            Get
                Return _type
            End Get
        End Property
        Public Function IncludeCommand(ByVal command As Commands.ICommand(Of IBotComponent)) As Task(Of IDisposable) Implements IBotComponent.IncludeCommand
            Throw New NotSupportedException
        End Function
    End Class
    Private Class TestComponent1
        Inherits BaseTestComponent
        Public Sub New(ByVal name As InvariantString)
            MyBase.New(name, "t2")
        End Sub
    End Class
    Private Class TestComponent2
        Inherits BaseTestComponent
        Public Sub New(ByVal name As InvariantString)
            MyBase.New(name, "t3")
        End Sub
    End Class

    <TestMethod()>
    Public Sub ComponentSetTest_AddComponent()
        Dim t1 = New TestComponent1("t1")
        Dim t2 = New TestComponent2("t2")
        Dim c = New ComponentSet()
        Assert.IsTrue(WaitValue(c.QueueGetAllComponents()).SequenceEqual({}))
        WaitUntilTaskSucceeds(c.QueueAddComponent(t1))
        Assert.IsTrue(WaitValue(c.QueueGetAllComponents()).SequenceEqual({t1}))
        WaitUntilTaskFails(c.QueueAddComponent(t1))
        WaitUntilTaskFails(c.QueueAddComponent(New TestComponent1("t1")))
        Assert.IsTrue(WaitValue(c.QueueGetAllComponents()).SequenceEqual({t1}))
        WaitUntilTaskSucceeds(c.QueueAddComponent(t2))
        Assert.IsTrue(WaitValue(c.QueueGetAllComponents()).SequenceEqual({t1, t2}))
    End Sub
    <TestMethod()>
    Public Sub ComponentSetTest_RemoveComponentByDispose()
        Dim t1 = New TestComponent1("t1")
        Dim t2 = New TestComponent2("t2")
        Dim c = New ComponentSet()
        c.QueueAddComponent(t1)
        WaitUntilTaskSucceeds(c.QueueAddComponent(t2))
        Assert.IsTrue(WaitValue(c.QueueGetAllComponents()).SequenceEqual({t1, t2}))
        t1.Dispose()
        'may take a moment to propagate
        Dim flag = False
        For i = 1 To 100
            If WaitValue(c.QueueGetAllComponents()).SequenceEqual({t2}) Then
                flag = True
                Exit For
            End If
            Threading.Thread.Sleep(1)
        Next i
        Assert.IsTrue(flag)
    End Sub
    <TestMethod()>
    Public Sub ComponentSetTest_Dispose()
        Dim t1 = New TestComponent1("t1")
        Dim t2 = New TestComponent2("t2")
        Dim c = New ComponentSet()
        c.QueueAddComponent(t1)
        c.QueueAddComponent(t2)
        c.Dispose()
        WaitUntilTaskSucceeds(c.DisposalTask)
        WaitUntilTaskSucceeds(t1.DisposalTask)
        WaitUntilTaskSucceeds(t2.DisposalTask)
    End Sub
    <TestMethod()>
    Public Sub ComponentSetTest_AsyncView()
        Dim t1 = New TestComponent1("t1")
        Dim t2 = New TestComponent2("t2")
        Dim addedItems = New List(Of IBotComponent)
        Dim removedItems = New List(Of IBotComponent)
        Dim addedItemsLock = New Threading.AutoResetEvent(initialState:=False)
        Dim removedItemsLock = New Threading.AutoResetEvent(initialState:=False)
        Dim c = New ComponentSet()
        c.QueueAddComponent(t1)
        c.QueueObserveComponents(Sub(sender, item)
                                     addedItems.Add(item)
                                     If addedItems.Count = 2 Then addedItemsLock.Set()
                                 End Sub,
                               Sub(sender, item)
                                   removedItems.Add(item)
                                   If removedItems.Count = 1 Then removedItemsLock.Set()
                               End Sub)
        c.QueueAddComponent(t2)
        t1.Dispose()

        Assert.IsTrue(addedItemsLock.WaitOne(millisecondsTimeout:=10000))
        Assert.IsTrue(removedItemsLock.WaitOne(millisecondsTimeout:=10000))
        Assert.IsTrue(addedItems.SequenceEqual({t1, t2}))
        Assert.IsTrue(removedItems.SequenceEqual({t1}))
    End Sub
    <TestMethod()>
    Public Sub ComponentSetTest_AsyncViewT()
        Dim t1 = New TestComponent1("t1")
        Dim t2 = New TestComponent1("t2")
        Dim t3 = New TestComponent2("t3") '[should be ignored due to type]
        Dim addedItems = New List(Of TestComponent1)
        Dim removedItems = New List(Of TestComponent1)
        Dim addedItemsLock = New Threading.AutoResetEvent(initialState:=False)
        Dim removedItemsLock = New Threading.AutoResetEvent(initialState:=False)
        Dim c = New ComponentSet()
        c.QueueAddComponent(t2)
        c.QueueObserveComponentsOfType(Of TestComponent1)(Sub(sender, item)
                                                              addedItems.Add(item)
                                                              If addedItems.Count = 2 Then addedItemsLock.Set()
                                                          End Sub,
                                                  Sub(sender, item)
                                                      removedItems.Add(item)
                                                      If removedItems.Count = 1 Then removedItemsLock.Set()
                                                  End Sub)
        c.QueueAddComponent(t1)
        c.QueueAddComponent(t3)
        t2.Dispose()
        t3.Dispose()
        Assert.IsTrue(addedItemsLock.WaitOne(millisecondsTimeout:=10000))
        Assert.IsTrue(removedItemsLock.WaitOne(millisecondsTimeout:=10000))
        Assert.IsTrue(addedItems.SequenceEqual({t2, t1}))
        Assert.IsTrue(removedItems.SequenceEqual({t2}))
    End Sub
    <TestMethod()>
    Public Sub ComponentSetTest_GetAllComponentsT()
        Dim t1 = New TestComponent1("t1")
        Dim t2 = New TestComponent1("t2")
        Dim t3 = New TestComponent2("t3")
        Dim c = New ComponentSet()
        c.QueueAddComponent(t1)
        c.QueueAddComponent(t2)
        c.QueueAddComponent(t3)
        Assert.IsTrue(WaitValue(c.QueueGetAllComponents(Of TestComponent1)).SequenceEqual({t1, t2}))
        Assert.IsTrue(WaitValue(c.QueueGetAllComponents(Of TestComponent2)).SequenceEqual({t3}))
    End Sub
    <TestMethod()>
    Public Sub ComponentSetTest_FindComponent()
        Dim t1 = New TestComponent1("t1")
        Dim t2 = New TestComponent1("t2")
        Dim t3 = New TestComponent2("t1")
        Dim c = New ComponentSet()
        c.QueueAddComponent(t1)
        c.QueueAddComponent(t2)
        c.QueueAddComponent(t3)
        Assert.IsTrue(WaitValue(c.QueueFindComponent("t2", "t1")) Is t1)
        Assert.IsTrue(WaitValue(c.QueueFindComponent("t2", "t2")) Is t2)
        Assert.IsTrue(WaitValue(c.QueueFindComponent("t3", "t1")) Is t3)
        WaitUntilTaskFails(c.QueueFindComponent("t4", "t2"))
    End Sub
    <TestMethod()>
    Public Sub ComponentSetTest_FindComponentT()
        Dim t1 = New TestComponent1("t1")
        Dim t2 = New TestComponent1("t2")
        Dim t3 = New TestComponent2("t1")
        Dim c = New ComponentSet()
        c.QueueAddComponent(t1)
        c.QueueAddComponent(t2)
        c.QueueAddComponent(t3)
        Assert.IsTrue(WaitValue(c.QueueFindComponent(Of TestComponent1)("t1")) Is t1)
        Assert.IsTrue(WaitValue(c.QueueFindComponent(Of TestComponent1)("t2")) Is t2)
        Assert.IsTrue(WaitValue(c.QueueFindComponent(Of TestComponent2)("t1")) Is t3)
        WaitUntilTaskFails(c.QueueFindComponent(Of TestComponent2)("t2"))
    End Sub
    <TestMethod()>
    Public Sub ComponentSetTest_QueueGetOrConstructTest()
        Dim c = New ComponentSet()
        Dim t1 = New TestComponent1("a")
        Dim t2 = New TestComponent2("b")
        Dim flag1 = False
        Dim flag2 = True
        Dim flag3 = False
        Assert.IsTrue(t1 Is WaitValue(c.QueueFindOrElseConstructComponent(Of TestComponent1)(
            Function()
                flag1 = True
                Return t1
            End Function)))
        Assert.IsTrue(t1 Is WaitValue(c.QueueFindOrElseConstructComponent(Of TestComponent1)(
            Function()
                flag2 = False
                Return t1
            End Function)))
        Assert.IsTrue(t2 Is WaitValue(c.QueueFindOrElseConstructComponent(Of TestComponent2)(
            Function()
                flag3 = True
                Return t2
            End Function)))
        Assert.IsTrue(flag1)
        Assert.IsTrue(flag2)
        Assert.IsTrue(flag3)
    End Sub
End Class
