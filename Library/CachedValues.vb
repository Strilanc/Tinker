Public Module CachedValues
    Private CachedWc3Version As Integer()

    Public Sub CacheWc3Version()
        Contract.Ensures(CachedWc3Version IsNot Nothing)
        Contract.Ensures(CachedWc3Version.Length = 4)
        Dim r = System.Diagnostics.FileVersionInfo.GetVersionInfo(WC3Path() + "war3.exe")
        CachedWc3Version = {r.ProductMajorPart, r.ProductMinorPart, r.ProductBuildPart, r.ProductPrivatePart}
    End Sub

    Public Function GetWC3MajorVersion() As Byte
        If CachedWc3Version Is Nothing Then CacheWc3Version()
        Return CByte(CachedWc3Version(1) And &HFF)
    End Function
    Public Function GetWC3ExeVersion() As Byte()
        If CachedWc3Version Is Nothing Then CacheWc3Version()
        Return (From e In CachedWc3Version.Reverse Select CByte(e And &HFF)).ToArray
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
