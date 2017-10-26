Public Structure SettingsSnapshot

    Public ReadOnly DarkSoulsDataPath As String

    Public ReadOnly UseCustomDataRootPath As Boolean
    Public ReadOnly CustomDataRootPath As String

    Public ReadOnly UseRemoteBNDTablePath As Boolean
    Public ReadOnly RemoteBNDTablePath As String

    Public ReadOnly UseRemoteBNDBackupPath As Boolean
    Public ReadOnly RemoteBNDBackupPath As String

    Public Sub New(ByVal DarkSoulsDataPath As String,
                   ByVal UseCustomDataRootPath As Boolean,
                   ByVal CustomDataRootPath As String,
                   ByVal UseRemoteBNDTablePath As Boolean,
                   ByVal RemoteBNDTablePath As String,
                   ByVal UseRemoteBNDBackupPath As Boolean,
                   ByVal RemoteBNDBackupPath As String)

        Me.DarkSoulsDataPath = DarkSoulsDataPath
        Me.UseCustomDataRootPath = UseCustomDataRootPath
        Me.CustomDataRootPath = CustomDataRootPath
        Me.UseRemoteBNDTablePath = UseRemoteBNDTablePath
        Me.RemoteBNDTablePath = RemoteBNDTablePath
        Me.UseRemoteBNDBackupPath = UseRemoteBNDBackupPath
        Me.RemoteBNDBackupPath = RemoteBNDBackupPath

    End Sub

    Public Shared Function TakeCurrent() As SettingsSnapshot
        Return New SettingsSnapshot(
            My.Settings.DarkSoulsDataPath,
            My.Settings.UseCustomDataRootPath,
            My.Settings.CustomDataRootPath,
            My.Settings.UseRemoteBNDTablePath,
            My.Settings.RemoteBNDTablePath,
            My.Settings.UseRemoteBNDBackupPath,
            My.Settings.RemoteBNDBackupPath)
    End Function

End Structure
