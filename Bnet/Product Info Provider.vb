<ContractClass(GetType(IProductInfoProvider.ContractClass))>
Public Interface IProductInfoProvider
    ReadOnly Property ExeVersion As IRist(Of Byte)
    ReadOnly Property FileSize As UInt32
    ReadOnly Property LastModifiedTime As Date
    Function GenerateRevisionCheck(folder As String, challengeSeed As String, challengeInstructions As String) As UInt32

    <ContractClassFor(GetType(IProductInfoProvider))>
    MustInherit Class ContractClass
        Implements IProductInfoProvider

        Public Function GenerateRevisionCheck(folder As String, challengeSeed As String, challengeInstructions As String) As UInteger Implements IProductInfoProvider.GenerateRevisionCheck
            Contract.Requires(folder IsNot Nothing)
            Contract.Requires(challengeSeed IsNot Nothing)
            Contract.Requires(challengeInstructions IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public ReadOnly Property ExeVersion As IRist(Of Byte) Implements IProductInfoProvider.ExeVersion
            Get
                Contract.Ensures(Contract.Result(Of IRist(Of Byte))() IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IRist(Of Byte))().Count = 4)
                Throw New NotSupportedException
            End Get
        End Property

        Public ReadOnly Property FileSize As UInteger Implements IProductInfoProvider.FileSize
            Get
                Throw New NotSupportedException
            End Get
        End Property

        Public ReadOnly Property LastModifiedTime As Date Implements IProductInfoProvider.LastModifiedTime
            Get
                Throw New NotSupportedException
            End Get
        End Property
    End Class
End Interface

Public Module WC3InfoProviderExtensions
    <Extension()> <Pure()>
    Public Function MajorVersion(provider As IProductInfoProvider) As Byte
        Contract.Requires(provider IsNot Nothing)
        Return provider.ExeVersion(2)
    End Function
End Module

Public Class CachedWC3InfoProvider
    Implements IProductInfoProvider

    Private Shared _cached As Boolean = False
    Private Shared _exeVersion As IRist(Of Byte)
    Private Shared _exeLastModifiedTime As Date
    Private Shared _exeSize As UInt32

    Public Sub New()
        If Not _cached Then
            Throw New InvalidStateException("WC3 info not yet cached.")
        End If
    End Sub

    Public Shared Function TryCache(programFolder As String) As Boolean
        Contract.Requires(programFolder IsNot Nothing)
        Contract.Ensures(Not Contract.Result(Of Boolean)() OrElse _exeVersion IsNot Nothing)
        Contract.Ensures(Not Contract.Result(Of Boolean)() OrElse _exeVersion.Count = 4)

        Dim path = IO.Path.Combine(programFolder, "war3.exe")
        If Not IO.File.Exists(path) Then Return False

        Dim versionInfo = FileVersionInfo.GetVersionInfo(path)
        Dim fileInfo = New IO.FileInfo(path)
        Contract.Assume(versionInfo IsNot Nothing)
        _exeVersion = MakeRist(versionInfo.ProductPrivatePart,
                               versionInfo.ProductBuildPart,
                               versionInfo.ProductMinorPart,
                               versionInfo.ProductMajorPart).Select(Function(e) CByte(e And &HFF))
        Contract.Assume(_exeVersion.Count = 4)
        _exeLastModifiedTime = fileInfo.LastWriteTime
        _exeSize = CUInt(fileInfo.Length)
        _cached = True
        Return True
    End Function

    Public ReadOnly Property ExeVersion As IRist(Of Byte) Implements IProductInfoProvider.ExeVersion
        Get
            Contract.Assume(_exeVersion.Count = 4)
            Return _exeVersion
        End Get
    End Property

    Public Function GenerateRevisionCheck(folder As String,
                                          challengeSeed As String,
                                          challengeInstructions As String) As UInt32 Implements IProductInfoProvider.GenerateRevisionCheck
        Return Bnet.GenerateRevisionCheck(folder, challengeSeed, challengeInstructions)
    End Function

    Public ReadOnly Property FileSize As UInteger Implements IProductInfoProvider.FileSize
        Get
            Return _exeSize
        End Get
    End Property

    Public ReadOnly Property LastModifiedTime As Date Implements IProductInfoProvider.LastModifiedTime
        Get
            Return _exeLastModifiedTime
        End Get
    End Property
End Class
