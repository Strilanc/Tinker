Public Module CachedValues
    Public Function WC3Path() As String
        Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
        Return My.Settings.war3path.AssumeNotNull
    End Function
    Public Function MapPath() As String
        Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
        Return My.Settings.mapPath.AssumeNotNull
    End Function

    'verification disabled due to stupid verifier
    <ContractVerification(False)>
    <Extension()> <Pure()>
    Function WC3MajorVersion(ByVal this As IExternalValues) As Byte
        Contract.Requires(this IsNot Nothing)
        Return this.WC3ExeVersion(2)
    End Function
End Module

Public Class CachedExternalValues
    Implements IExternalValues

    Private Shared _cached As Boolean = False
    Private Shared _exeVersion As IReadableList(Of Byte)
    Private Shared _exeLastModifiedTime As Date
    Private Shared _exeSize As UInt32

    Public Sub New()
        If Not _cached Then Recache()
    End Sub
    Public Shared Sub Recache()
        Contract.Ensures(_exeVersion IsNot Nothing)
        Contract.Ensures(_exeVersion.Count = 4)
        Dim path = WC3Path() + "war3.exe"
        Dim versionInfo = FileVersionInfo.GetVersionInfo(path)
        Dim fileInfo = New IO.FileInfo(path)
        Contract.Assume(versionInfo IsNot Nothing)
        _exeVersion = (From e In {versionInfo.ProductMajorPart, versionInfo.ProductMinorPart, versionInfo.ProductBuildPart, versionInfo.ProductPrivatePart}
                       Select CByte(e And &HFF)).Reverse.ToArray.AsReadableList
        Contract.Assume(_exeVersion.Count = 4)
        _exeLastModifiedTime = fileInfo.LastWriteTime
        _exeSize = CUInt(fileInfo.Length)
        _cached = True
    End Sub

    Public ReadOnly Property WC3ExeVersion As IReadableList(Of Byte) Implements IExternalValues.WC3ExeVersion
        Get
            Dim result = _exeVersion
            Contract.Assume(result IsNot Nothing)
            Contract.Assume(result.Count = 4)
            Return result
        End Get
    End Property

    Public Function GenerateRevisionCheck(ByVal folder As String, ByVal seedString As String, ByVal challengeString As String) As UInteger Implements IExternalValues.GenerateRevisionCheck
        Return Bnet.GenerateRevisionCheck(folder, seedString, challengeString)
    End Function

    Public ReadOnly Property WC3FileSize As UInteger Implements IExternalValues.WC3FileSize
        Get
            Return _exeSize
        End Get
    End Property

    Public ReadOnly Property WC3LastModifiedTime As Date Implements IExternalValues.WC3LastModifiedTime
        Get
            Return _exeLastModifiedTime
        End Get
    End Property
End Class

<ContractClass(GetType(IExternalValues.ContractClass))>
Public Interface IExternalValues
    ReadOnly Property WC3ExeVersion As IReadableList(Of Byte)
    ReadOnly Property WC3FileSize As UInt32
    ReadOnly Property WC3LastModifiedTime As Date
    Function GenerateRevisionCheck(ByVal folder As String, ByVal seedString As String, ByVal challengeString As String) As UInt32

    <ContractClassFor(GetType(IExternalValues))>
    Class ContractClass
        Implements IExternalValues

        Public Function GenerateRevisionCheck(ByVal folder As String, ByVal seedString As String, ByVal challengeString As String) As UInteger Implements IExternalValues.GenerateRevisionCheck
            Contract.Requires(folder IsNot Nothing)
            Contract.Requires(seedString IsNot Nothing)
            Contract.Requires(challengeString IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public ReadOnly Property WC3ExeVersion As IReadableList(Of Byte) Implements IExternalValues.WC3ExeVersion
            Get
                Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))() IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))().Count = 4)
                Throw New NotSupportedException
            End Get
        End Property

        Public ReadOnly Property WC3FileSize As UInteger Implements IExternalValues.WC3FileSize
            Get
                Throw New NotSupportedException
            End Get
        End Property

        Public ReadOnly Property WC3LastModifiedTime As Date Implements IExternalValues.WC3LastModifiedTime
            Get
                Throw New NotSupportedException
            End Get
        End Property
    End Class
End Interface
