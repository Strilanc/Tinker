Public Module CachedValues
    Private _cached As Boolean = False
    Private _exeVersion As Integer()
    Private _exeLastModifiedTime As Date
    Private _exeSize As UInt32

    Public Sub CacheExeInformation()
        Contract.Ensures(_exeVersion IsNot Nothing)
        Contract.Ensures(_exeVersion.Length = 4)
        Dim path = WC3Path() + "war3.exe"
        Dim versionInfo = FileVersionInfo.GetVersionInfo(path)
        Dim fileInfo = New IO.FileInfo(path)
        _exeVersion = {versionInfo.ProductMajorPart, versionInfo.ProductMinorPart, versionInfo.ProductBuildPart, versionInfo.ProductPrivatePart}
        _exeLastModifiedTime = fileInfo.LastWriteTime
        _exeSize = CUInt(fileInfo.Length)
        _cached = True
    End Sub

    Public Function GetWC3FileSize() As UInt32
        If Not _cached Then CacheExeInformation()
        Return _exeSize
    End Function
    Public Function GetWC3LastModifiedTime() As Date
        If Not _cached Then CacheExeInformation()
        Return _exeLastModifiedTime
    End Function
    Public Function GetWC3MajorVersion() As Byte
        If Not _cached Then CacheExeInformation()
        Return CByte(_exeVersion(1) And &HFF)
    End Function
    Public Function GetWC3ExeVersion() As IReadableList(Of Byte)
        Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))() IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))().Count = 4)
        If Not _cached Then CacheExeInformation()
        Dim result = (From e In _exeVersion.Reverse Select CByte(e And &HFF)).ToArray.AsReadableList
        Contract.Assume(result.Count = 4)
        Return result
    End Function
    Public Function WC3Path() As String
        Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
        Return My.Settings.war3path.AssumeNotNull
    End Function
    Public Function MapPath() As String
        Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
        Return My.Settings.mapPath.AssumeNotNull
    End Function
End Module
