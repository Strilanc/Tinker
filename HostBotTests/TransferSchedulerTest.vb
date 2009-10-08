Imports Strilbrary.Threading
Imports Strilbrary.Numerics
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports HostBot

<TestClass()>
Public Class TransferSchedulerTest
    <TestMethod()>
    Public Sub AddTest_Single()
        Dim ts = New TransferScheduler(Of Integer)(typicalRate:=1, typicalSwitchTime:=1, fileSize:=1)
        Dim f = ts.AddClient(0, True)
        BlockOnFuture(f)
        Assert.IsTrue(f.State = FutureState.Succeeded)
    End Sub
    <TestMethod()>
    Public Sub AddTest_Double()
        Dim ts = New TransferScheduler(Of Integer)(typicalRate:=1, typicalSwitchTime:=1, fileSize:=1)
        ts.AddClient(0, True)
        Dim f = ts.AddClient(1, True)
        BlockOnFuture(f)
        Assert.IsTrue(f.State = FutureState.Succeeded)
    End Sub
    <TestMethod()>
    Public Sub AddTest_Collide()
        Dim ts = New TransferScheduler(Of Integer)(typicalRate:=1, typicalSwitchTime:=1, fileSize:=1)
        ts.AddClient(0, True)
        Dim f = ts.AddClient(0, True)
        BlockOnFuture(f)
        Assert.IsTrue(f.State = FutureState.Failed)
    End Sub

    <TestMethod()>
    Public Sub SetLinkTest_Hit()
        Dim ts = New TransferScheduler(Of Integer)(typicalRate:=1, typicalSwitchTime:=1, fileSize:=1)
        ts.AddClient(0, True)
        ts.AddClient(1, True)
        Dim f = ts.SetLink(0, 1, True)
        BlockOnFuture(f)
        Assert.IsTrue(f.State = FutureState.Succeeded)
    End Sub
    <TestMethod()>
    Public Sub SetLinkTest_Miss()
        Dim ts = New TransferScheduler(Of Integer)(typicalRate:=1, typicalSwitchTime:=1, fileSize:=1)
        ts.AddClient(0, True)
        ts.AddClient(1, True)
        Dim f = ts.SetLink(0, 2, True)
        BlockOnFuture(f)
        Assert.IsTrue(f.State = FutureState.Failed)
    End Sub

    <TestMethod()>
    Public Sub RemoveTest_Miss()
        Dim ts = New TransferScheduler(Of Integer)(typicalRate:=1, typicalSwitchTime:=1, fileSize:=1)
        Dim f = ts.RemoveClient(0)
        BlockOnFuture(f)
        Assert.IsTrue(f.State = FutureState.Failed)
    End Sub
    <TestMethod()>
    Public Sub RemoveTest_Hit()
        Dim ts = New TransferScheduler(Of Integer)(typicalRate:=1, typicalSwitchTime:=1, fileSize:=1)
        ts.AddClient(0, True)
        Dim f = ts.RemoveClient(0)
        BlockOnFuture(f)
        Assert.IsTrue(f.State = FutureState.Succeeded)
    End Sub
    <TestMethod()>
    Public Sub RemoveTest_DoubleHit()
        Dim ts = New TransferScheduler(Of Integer)(typicalRate:=1, typicalSwitchTime:=1, fileSize:=1)
        ts.AddClient(0, True)
        ts.RemoveClient(0)
        Dim f = ts.RemoveClient(0)
        BlockOnFuture(f)
        Assert.IsTrue(f.State = FutureState.Failed)
    End Sub

    <TestMethod()>
    Public Sub UpdateTest_Count()
        Dim count = 0
        Dim ts = New TransferScheduler(Of Integer)(typicalRate:=1, typicalSwitchTime:=1, fileSize:=1)
        AddHandler ts.Actions, Sub()
                                   System.Threading.Interlocked.Increment(count)
                               End Sub
        ts.AddClient(0, completed:=True)
        ts.AddClient(1, completed:=False)
        BlockOnFuture(ts.SetLink(0, 1, True))
        Assert.IsTrue(count = 0)
        ts.Update()
        ts.Update()
        BlockOnFuture(ts.Update())
        Assert.IsTrue(count = 1)
    End Sub
    <TestMethod()>
    Public Sub UpdateTest_Start()
        Dim flag = False
        Dim ts = New TransferScheduler(Of Integer)(typicalRate:=1, typicalSwitchTime:=1, fileSize:=1)
        ts.AddClient(0, completed:=True)
        ts.AddClient(1, completed:=False)
        ts.SetLink(0, 1, True)
        AddHandler ts.Actions, Sub(added As List(Of TransferScheduler(Of Integer).TransferEndpoints), removed As List(Of TransferScheduler(Of Integer).TransferEndpoints))
                                   flag = added.Count = 1 _
                                               AndAlso removed.Count = 0 _
                                               AndAlso added(0).source = 0 _
                                               AndAlso added(0).destination = 1
                               End Sub
        Dim f = ts.Update()
        BlockOnFuture(f)
        Assert.IsTrue(f.State = FutureState.Succeeded)
        Assert.IsTrue(flag)
    End Sub
    <TestMethod()>
    Public Sub UpdateTest_SwitchBetter1()
        Dim flag = False
        Dim ts = New TransferScheduler(Of Integer)(typicalRate:=1, typicalSwitchTime:=1, fileSize:=100000, minSwitchPeriodMilliseconds:=0)
        ts.AddClient(0, completed:=True)
        ts.AddClient(1, completed:=False, expectedRate:=1000)
        ts.SetLink(0, 1, True)
        ts.Update()
        ts.AddClient(2, completed:=True, expectedRate:=1000)
        BlockOnFuture(ts.SetLink(1, 2, True))
        AddHandler ts.Actions, Sub(added As List(Of TransferScheduler(Of Integer).TransferEndpoints), removed As List(Of TransferScheduler(Of Integer).TransferEndpoints))
                                   flag = added.Count = 0 _
                                               AndAlso removed.Count = 1 _
                                               AndAlso removed(0).source = 0 _
                                               AndAlso removed(0).destination = 1
                               End Sub
        Dim f = ts.Update()
        Dim fold = ts.GetClientState(0)
        BlockOnFuture(fold)
        Assert.IsTrue(f.State = FutureState.Succeeded)
        Assert.IsTrue(fold.State = FutureState.Succeeded)
        Assert.IsTrue(fold.Value = ClientTransferState.Ready)
        Assert.IsTrue(flag)
    End Sub
    <TestMethod()>
    Public Sub UpdateTest_SwitchBetter2()
        Dim flag = False
        Dim ts = New TransferScheduler(Of Integer)(typicalRate:=1, typicalSwitchTime:=1, fileSize:=100000, minSwitchPeriodMilliseconds:=0)
        ts.AddClient(0, completed:=True)
        ts.AddClient(1, completed:=False, expectedRate:=1000)
        ts.SetLink(0, 1, True)
        ts.Update()
        ts.AddClient(2, completed:=True, expectedRate:=1000)
        ts.SetLink(1, 2, True)
        BlockOnFuture(ts.Update)
        AddHandler ts.Actions, Sub(added As List(Of TransferScheduler(Of Integer).TransferEndpoints), removed As List(Of TransferScheduler(Of Integer).TransferEndpoints))
                                   flag = added.Count = 1 _
                                               AndAlso removed.Count = 0 _
                                               AndAlso added(0).source = 2 _
                                               AndAlso added(0).destination = 1
                               End Sub
        Dim f = ts.Update()
        Dim fold = ts.GetClientState(0)
        Dim fdown = ts.GetClientState(1)
        Dim fup = ts.GetClientState(2)
        BlockOnFuture(fup)
        Assert.IsTrue(f.State = FutureState.Succeeded)
        Assert.IsTrue(fdown.State = FutureState.Succeeded)
        Assert.IsTrue(fold.State = FutureState.Succeeded)
        Assert.IsTrue(fup.State = FutureState.Succeeded)
        Assert.IsTrue(fold.Value = ClientTransferState.Ready)
        Assert.IsTrue(fup.Value = ClientTransferState.Uploading)
        Assert.IsTrue(fdown.Value = ClientTransferState.Downloading)
        Assert.IsTrue(flag)
    End Sub
    <TestMethod()>
    Public Sub UpdateTest_Select()
        Dim flag = False
        Dim ts = New TransferScheduler(Of Integer)(typicalRate:=1, typicalSwitchTime:=1, fileSize:=100000)
        ts.AddClient(0, completed:=True)
        ts.AddClient(1, completed:=False, expectedRate:=1000)
        ts.AddClient(2, completed:=True, expectedRate:=1000)
        ts.SetLink(0, 1, True)
        ts.SetLink(1, 2, True)
        AddHandler ts.Actions, Sub(added As List(Of TransferScheduler(Of Integer).TransferEndpoints), removed As List(Of TransferScheduler(Of Integer).TransferEndpoints))
                                   flag = added.Count = 1 _
                                               AndAlso removed.Count = 0 _
                                               AndAlso added(0).source = 2 _
                                               AndAlso added(0).destination = 1
                               End Sub
        Dim f = ts.Update()
        BlockOnFuture(f)
        Assert.IsTrue(f.State = FutureState.Succeeded)
        Assert.IsTrue(flag)
    End Sub
    <TestMethod()>
    Public Sub UpdateTest_Freeze()
        Dim flag = False
        Dim ts = New TransferScheduler(Of Integer)(typicalRate:=1, typicalSwitchTime:=1, fileSize:=1, freezePeriodMilliseconds:=0)
        ts.AddClient(0, completed:=True)
        ts.AddClient(1, completed:=False)
        ts.SetLink(0, 1, True)
        BlockOnFuture(ts.Update)
        AddHandler ts.Actions, Sub(added As List(Of TransferScheduler(Of Integer).TransferEndpoints), removed As List(Of TransferScheduler(Of Integer).TransferEndpoints))
                                   flag = added.Count = 0 _
                                               AndAlso removed.Count = 1 _
                                               AndAlso removed(0).source = 0 _
                                               AndAlso removed(0).destination = 1
                               End Sub
        Dim f = ts.Update()
        BlockOnFuture(f)
        Assert.IsTrue(f.State = FutureState.Succeeded)
        Assert.IsTrue(flag)
    End Sub
    <TestMethod()>
    Public Sub UpdateTest_Stable()
        Dim flag = True
        Dim ts = New TransferScheduler(Of Integer)(typicalRate:=1, typicalSwitchTime:=1, fileSize:=1)
        ts.AddClient(0, completed:=True)
        ts.AddClient(1, completed:=False)
        ts.SetLink(0, 1, True)
        BlockOnFuture(ts.Update)
        AddHandler ts.Actions, Sub(added As List(Of TransferScheduler(Of Integer).TransferEndpoints), removed As List(Of TransferScheduler(Of Integer).TransferEndpoints))
                                   flag = False
                               End Sub
        Dim f = ts.Update()
        BlockOnFuture(f)
        Assert.IsTrue(f.State = FutureState.Succeeded)
        Assert.IsTrue(flag)
    End Sub

    <TestMethod()>
    Public Sub SetNotTransfering_Finish()
        Dim ts = New TransferScheduler(Of Integer)(typicalRate:=1, typicalSwitchTime:=1, fileSize:=1)
        ts.AddClient(0, completed:=True)
        ts.AddClient(1, completed:=False)
        ts.SetLink(0, 1, True)
        ts.Update()
        Dim f = ts.SetNotTransfering(1, completed:=True)
        Dim fs0 = ts.GetClientState(0)
        Dim fs1 = ts.GetClientState(1)
        BlockOnFuture(f)
        Assert.IsTrue(f.State = FutureState.Succeeded)
        Assert.IsTrue(fs0.State = FutureState.Succeeded)
        Assert.IsTrue(fs1.State = FutureState.Succeeded)
        Assert.IsTrue(fs0.Value = ClientTransferState.Ready)
        Assert.IsTrue(fs1.Value = ClientTransferState.Ready)
    End Sub
    <TestMethod()>
    Public Sub SetNotTransfering_Cancel()
        Dim ts = New TransferScheduler(Of Integer)(typicalRate:=1, typicalSwitchTime:=1, fileSize:=1)
        ts.AddClient(0, completed:=True)
        ts.AddClient(1, completed:=False)
        ts.SetLink(0, 1, True)
        ts.Update()
        Dim f = ts.SetNotTransfering(1, completed:=False)
        Dim fs0 = ts.GetClientState(0)
        Dim fs1 = ts.GetClientState(1)
        BlockOnFuture(f)
        Assert.IsTrue(f.State = FutureState.Succeeded)
        Assert.IsTrue(fs0.State = FutureState.Succeeded)
        Assert.IsTrue(fs1.State = FutureState.Succeeded)
        Assert.IsTrue(fs0.Value = ClientTransferState.Ready)
        Assert.IsTrue(fs1.Value = ClientTransferState.Idle)
    End Sub
    <TestMethod()>
    Public Sub SetNotTransfering_MissFinish()
        Dim ts = New TransferScheduler(Of Integer)(typicalRate:=1, typicalSwitchTime:=1, fileSize:=1)
        ts.AddClient(0, completed:=False)
        Dim f = ts.SetNotTransfering(0, completed:=True)
        Dim fs0 = ts.GetClientState(0)
        BlockOnFuture(f)
        Assert.IsTrue(f.State = FutureState.Succeeded)
        Assert.IsTrue(fs0.State = FutureState.Succeeded)
        Assert.IsTrue(fs0.Value = ClientTransferState.Ready)
    End Sub

    <TestMethod()>
    Public Sub GetClientStateTest_Miss()
        Dim ts = New TransferScheduler(Of Integer)(typicalRate:=1, typicalSwitchTime:=1, fileSize:=1)
        Dim f = ts.GetClientState(clientKey:=0)
        BlockOnFuture(f)
        Assert.IsTrue(f.State = FutureState.Failed)
    End Sub
    <TestMethod()>
    Public Sub GetClientStateTest_Idle()
        Dim ts = New TransferScheduler(Of Integer)(typicalRate:=1, typicalSwitchTime:=1, fileSize:=1)
        ts.AddClient(0, completed:=False)
        Dim f = ts.GetClientState(0)
        BlockOnFuture(f)
        Assert.IsTrue(f.State = FutureState.Succeeded)
        Assert.IsTrue(f.Value = ClientTransferState.Idle)
    End Sub
    <TestMethod()>
    Public Sub GetClientStateTest_Ready()
        Dim ts = New TransferScheduler(Of Integer)(typicalRate:=1, typicalSwitchTime:=1, fileSize:=1)
        ts.AddClient(0, completed:=True)
        Dim f = ts.GetClientState(0)
        BlockOnFuture(f)
        Assert.IsTrue(f.State = FutureState.Succeeded)
        Assert.IsTrue(f.Value = ClientTransferState.Ready)
    End Sub
    <TestMethod()>
    Public Sub GetClientStateTest_Uploading()
        Dim ts = New TransferScheduler(Of Integer)(typicalRate:=1, typicalSwitchTime:=1, fileSize:=1)
        ts.AddClient(0, completed:=True)
        ts.AddClient(1, completed:=False)
        ts.SetLink(0, 1, True)
        ts.Update()
        Dim f = ts.GetClientState(0)
        BlockOnFuture(f)
        Assert.IsTrue(f.State = FutureState.Succeeded)
        Assert.IsTrue(f.Value = ClientTransferState.Uploading)
    End Sub
    <TestMethod()>
    Public Sub GetClientStateTest_Downloading()
        Dim ts = New TransferScheduler(Of Integer)(typicalRate:=1, typicalSwitchTime:=1, fileSize:=1)
        ts.AddClient(0, completed:=False)
        ts.AddClient(1, completed:=True)
        ts.SetLink(0, 1, True)
        ts.Update()
        Dim f = ts.GetClientState(0)
        BlockOnFuture(f)
        Assert.IsTrue(f.State = FutureState.Succeeded)
        Assert.IsTrue(f.Value = ClientTransferState.Downloading)
    End Sub
    <TestMethod()>
    Public Sub GetClientStateTest_Finish()
        Dim ts = New TransferScheduler(Of Integer)(typicalRate:=1, typicalSwitchTime:=1, fileSize:=1)
        ts.AddClient(0, completed:=False)
        ts.AddClient(1, completed:=True)
        ts.SetLink(0, 1, True)
        ts.Update()
        ts.SetNotTransfering(0, completed:=True)
        Dim f = ts.GetClientState(0)
        BlockOnFuture(f)
        Assert.IsTrue(f.State = FutureState.Succeeded)
        Assert.IsTrue(f.Value = ClientTransferState.Ready)
    End Sub
    <TestMethod()>
    Public Sub GetClientStateTest_Cancel()
        Dim ts = New TransferScheduler(Of Integer)(typicalRate:=1, typicalSwitchTime:=1, fileSize:=1)
        ts.AddClient(0, completed:=False)
        ts.AddClient(1, completed:=True)
        ts.SetLink(0, 1, True)
        ts.Update()
        ts.SetNotTransfering(0, completed:=False)
        Dim f = ts.GetClientState(0)
        BlockOnFuture(f)
        Assert.IsTrue(f.State = FutureState.Succeeded)
        Assert.IsTrue(f.Value = ClientTransferState.Idle)
    End Sub
End Class
