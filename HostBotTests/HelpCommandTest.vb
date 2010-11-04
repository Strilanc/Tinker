Imports Strilbrary.Threading
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Tinker.Commands

<TestClass()>
Public Class HelpCommandTest
    Private Class TestCommand
        Inherits Command(Of Object)
        Public Sub New()
            MyBase.New("Test", "arg -option", "A test command.", "root:1", "x=test")
        End Sub
        Protected Overrides Function PerformInvoke(ByVal target As Object, ByVal user As Tinker.BotUser, ByVal argument As String) As System.Threading.Tasks.Task(Of String)
            Return "".AsTask
        End Function
    End Class
    Private Shared ReadOnly TestCommandFactory As Func(Of ICommand(Of Object)) = Function() New TestCommand()
    Private Shared Function BlockingInvoke(ByVal command As ICommand(Of Object), ByVal arg As String) As Task(Of String)
        Return BlockOnTaskValue(command.Invoke(New Object, Nothing, arg))
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
        Assert.IsTrue(BlockingInvoke(help, "").Status = TaskStatus.RanToCompletion)
        Assert.IsTrue(BlockingInvoke(help, "*").Status = TaskStatus.RanToCompletion)
        Assert.IsTrue(BlockingInvoke(help, "+").Status = TaskStatus.RanToCompletion)
        Assert.IsTrue(BlockingInvoke(help, "?").Status = TaskStatus.RanToCompletion)
        Assert.IsTrue(BlockingInvoke(help, "!").Status = TaskStatus.Faulted)
        help.AddCommand(TestCommandFactory())
        Assert.IsTrue(BlockingInvoke(help, "").Status = TaskStatus.RanToCompletion)
        Assert.IsTrue(BlockingInvoke(help, "*").Status = TaskStatus.RanToCompletion)
        Assert.IsTrue(BlockingInvoke(help, "+").Status = TaskStatus.RanToCompletion)
        Assert.IsTrue(BlockingInvoke(help, "?").Status = TaskStatus.RanToCompletion)
        Assert.IsTrue(BlockingInvoke(help, "!").Status = TaskStatus.Faulted)
    End Sub

    <TestMethod()>
    Public Sub CommandExistsTest()
        Dim help = New HelpCommand(Of Object)
        Dim testCommand = TestCommandFactory()
        Assert.IsTrue(BlockingInvoke(help, "Test").Status = TaskStatus.Faulted)
        Assert.IsTrue(BlockingInvoke(help, "test x").Status = TaskStatus.Faulted)
        help.AddCommand(testCommand)
        Assert.IsTrue(BlockingInvoke(help, "Test").Status = TaskStatus.RanToCompletion)
        Assert.IsTrue(BlockingInvoke(help, "test").Status = TaskStatus.RanToCompletion)
        Assert.IsTrue(BlockingInvoke(help, "test x").Status = TaskStatus.RanToCompletion)
        Assert.IsTrue(BlockingInvoke(help, "test y").Status = TaskStatus.Faulted)
        help.RemoveCommand(testCommand)
        Assert.IsTrue(BlockingInvoke(help, "test").Status = TaskStatus.Faulted)
        Assert.IsTrue(BlockingInvoke(help, "test x").Status = TaskStatus.Faulted)
    End Sub

    <TestMethod()>
    Public Sub CommandContentTest()
        Dim help = New HelpCommand(Of Object)
        Dim testCommand = TestCommandFactory()
        help.AddCommand(testCommand)
        Assert.IsTrue(BlockingInvoke(help, "Test x").Result = "test")
        Dim result = BlockingInvoke(help, "Test").Result
        Assert.IsTrue(result.Contains(testCommand.Description))
        Assert.IsTrue(result.Contains(testCommand.Format))
        Assert.IsTrue(result.Contains(testCommand.Permissions))
        Assert.IsTrue(result.Contains(testCommand.Name))
    End Sub
End Class
