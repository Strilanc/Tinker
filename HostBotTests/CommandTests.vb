Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Tinker.Commands
Imports Strilbrary.Threading

<TestClass()>
Public Class CommandTests
    Private Class TestTemplatedCommand
        Inherits TemplatedCommand(Of UInt32)
        Public Sub New()
            MyBase.New("TestTemplatedCommand", "arg1 arg2=value -arg3=value -arg4", "description", hasPrivateArguments:=True)
        End Sub
        Protected Overloads Overrides Function PerformInvoke(ByVal target As UInteger, ByVal user As Tinker.BotUser, ByVal argument As CommandArgument) As Task(Of String)
            Dim result = target.ToString + " " + argument.RawValue(0)
            result += " "+argument.NamedValue("arg2")
            If argument.HasOptionalNamedValue("arg3") Then result += " " + argument.OptionalNamedValue("arg3")
            If argument.HasOptionalSwitch("arg4") Then result += " " + "arg4"
            Return result.AsTask
        End Function
    End Class
    <TestMethod()>
    Public Sub TemplatedCommandTest()
        Dim c = New TestTemplatedCommand()
        Assert.IsTrue(c.Invoke(5, Nothing, "v1 arg2=v2").WaitValue() = "5 v1 v2")
        Assert.IsTrue(c.Invoke(4, Nothing, "w1 arg2=w2 -arg3=t -arg4").WaitValue() = "4 w1 w2 t arg4")
    End Sub

    Private Class TestPartialCommand
        Inherits PartialCommand(Of UInt32)
        Public Sub New()
            MyBase.New("TestPartialCommand", "head", "description")
        End Sub
        Protected Overloads Overrides Function PerformInvoke(ByVal target As UInteger, ByVal user As Tinker.BotUser, ByVal argumentHead As String, ByVal argumentRest As String) As Task(Of String)
            Return (argumentRest + target.ToString + argumentHead).AsTask
        End Function
    End Class
    <TestMethod()>
    Public Sub PartialCommandTest()
        Dim c = New TestPartialCommand()
        Assert.IsTrue(c.Invoke(5, Nothing, "head rest").WaitValue() = "rest5head")
        Assert.IsTrue(c.Invoke(4, Nothing, "head mid more tail").WaitValue() = "mid more tail4head")
    End Sub

    Private Class TestPermissionCommand
        Inherits BaseCommand(Of UInt32)
        Public Property InvokeCount As Integer
        Public Sub New()
            MyBase.New("TestPermissionCommand", "format", "description", "perm:3")
        End Sub
        Protected Overrides Function PerformInvoke(ByVal target As UInteger, ByVal user As Tinker.BotUser, ByVal argument As String) As System.Threading.Tasks.Task(Of String)
            InvokeCount += 1
            Return "allowed".AsTask
        End Function
    End Class
    <TestMethod()>
    Public Sub PermissionTest()
        Dim c = New TestPermissionCommand()
        Assert.IsTrue(c.InvokeCount = 0)
        c.Invoke(0, New Tinker.BotUser("name", "perm=3"), "").Wait()
        Assert.IsTrue(c.InvokeCount = 1)
        ExpectException(Of AggregateException)(Sub() c.Invoke(0, New Tinker.BotUser("name", "perm=2"), "").Wait())
        Assert.IsTrue(c.InvokeCount = 1)
        c.Invoke(0, New Tinker.BotUser("name", "perm=4"), "").Wait()
        Assert.IsTrue(c.InvokeCount = 2)
        c.Invoke(0, Nothing, "").Wait()
        Assert.IsTrue(c.InvokeCount = 3)
    End Sub

    <TestMethod()>
    Public Sub CommandSetTest()
        'Add
        Dim c = New CommandSet(Of UInt32)
        Dim f1 = c.IncludeCommand(New TestPartialCommand())
        Dim f2 = c.IncludeCommand(New TestPermissionCommand())
        Dim f3 = c.IncludeCommand(New TestTemplatedCommand())
        ExpectException(Of InvalidOperationException)(Sub() c.IncludeCommand(New TestTemplatedCommand()))

        'Invoke
        Assert.IsTrue(c.Invoke(3, Nothing, "TestTemplatedCommand v1 arg2=v2").WaitValue() = "3 v1 v2")
        Assert.IsTrue(c.Invoke(3, Nothing, "TestPermissionCommand").WaitValue() = "allowed")
        ExpectException(Of AggregateException)(Sub() c.Invoke(3, New Tinker.BotUser("name", "perm=2"), "TestPermissionCommand").Wait())
        Assert.IsTrue(c.Invoke(3, Nothing, "TestPartialCommand head rest").WaitValue() = "rest3head")
        ExpectException(Of AggregateException)(Sub() c.Invoke(3, Nothing, "missing waaaa").Wait())

        'Private args
        Assert.IsTrue(c.IsArgumentPrivate("TestTemplatedCommand v1 arg2=v2"))
        Assert.IsTrue(Not c.IsArgumentPrivate("TestPermissionCommand"))

        'Remove
        f1.Dispose()
        ExpectException(Of AggregateException)(Sub() c.Invoke(3, Nothing, "TestPartialCommand head rest").Wait())
    End Sub

    <TestMethod()>
    Public Sub ProjectedCommandTest()
        Dim c = New ProjectedCommand(Of String, UInt32)(New TestPartialCommand(), Function(v) UInt32.Parse(v) + 1UI)
        Assert.IsTrue(c.Invoke("5", Nothing, "head rest").WaitValue = "rest6head")
        Dim c2 = New TestPartialCommand().ProjectedFrom(Function(v As String) UInt32.Parse(v) + 1UI)
        Assert.IsTrue(c2.Invoke("5", Nothing, "head rest").WaitValue = "rest6head")
    End Sub
End Class
