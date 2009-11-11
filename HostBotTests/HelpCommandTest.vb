Imports Strilbrary
Imports Strilbrary.Streams
Imports Strilbrary.Threading
Imports Strilbrary.Enumeration
Imports Strilbrary.Numerics
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports HostBot
Imports HostBot.Commands

<TestClass()>
Public Class HelpCommandTest
    Private Shared ReadOnly TestCommandFactory As Func(Of Command(Of Object)) = Function() New DelegatedTemplatedCommand(Of Object)(
        name:="Test",
        template:="arg -option",
        description:="A test command.",
        permissions:="root=1",
        func:=Function(target, user, arg)
                  Return "".Futurized
              End Function,
        extraHelp:="x=test")
    Private Shared Function BlockingInvoke(ByVal command As Command(Of Object), ByVal arg As String) As IFuture(Of String)
        Return BlockOnFutureValue(command.Invoke(New Object, Nothing, arg))
    End Function

    <TestMethod()>
    <ExpectedException(GetType(InvalidOperationException))>
    Public Sub DuplicateTest()
        Dim help = New HelpCommand(Of Object)
        Dim testCommand = TestCommandFactory()
        help.AddCommand(testCommand)
        help.AddCommand(testCommand)
    End Sub

    <TestMethod()>
    Public Sub FixedContentTest()
        Dim help = New HelpCommand(Of Object)
        Assert.IsTrue(BlockingInvoke(help, "").State = FutureState.Succeeded)
        Assert.IsTrue(BlockingInvoke(help, "*").State = FutureState.Succeeded)
        Assert.IsTrue(BlockingInvoke(help, "+").State = FutureState.Succeeded)
        Assert.IsTrue(BlockingInvoke(help, "?").State = FutureState.Succeeded)
        Assert.IsTrue(BlockingInvoke(help, "!").State = FutureState.Failed)
    End Sub

    <TestMethod()>
    Public Sub CommandExistsTest()
        Dim help = New HelpCommand(Of Object)
        Dim testCommand = TestCommandFactory()
        Assert.IsTrue(BlockingInvoke(help, "Test").State = FutureState.Failed)
        Assert.IsTrue(BlockingInvoke(help, "test x").State = FutureState.Failed)
        help.AddCommand(testCommand)
        Assert.IsTrue(BlockingInvoke(help, "Test").State = FutureState.Succeeded)
        Assert.IsTrue(BlockingInvoke(help, "test").State = FutureState.Succeeded)
        Assert.IsTrue(BlockingInvoke(help, "test x").State = FutureState.Succeeded)
        Assert.IsTrue(BlockingInvoke(help, "test y").State = FutureState.Failed)
        help.RemoveCommand(testCommand)
        Assert.IsTrue(BlockingInvoke(help, "test").State = FutureState.Failed)
        Assert.IsTrue(BlockingInvoke(help, "test x").State = FutureState.Failed)
    End Sub

    <TestMethod()>
    Public Sub CommandContentTest()
        Dim help = New HelpCommand(Of Object)
        Dim testCommand = TestCommandFactory()
        help.AddCommand(testCommand)
        Assert.IsTrue(BlockingInvoke(help, "Test x").Value = "test")
        Dim result = BlockingInvoke(help, "Test").Value
        Assert.IsTrue(result.Contains(testCommand.Description))
        Assert.IsTrue(result.Contains(testCommand.Format))
        Assert.IsTrue(result.Contains(testCommand.Permissions))
        Assert.IsTrue(result.Contains(testCommand.Name))
    End Sub
End Class
