Public Structure SettingsSnapshot

    'Public ReadOnly Property DarkSoulsDataPath As String

    Public ReadOnly Property UseCustomDataRootPath As Boolean
    Public ReadOnly Property CustomDataRootPath As String

    'Public ReadOnly Property UseRemoteBNDTablePath As Boolean
    'Public ReadOnly Property RemoteBNDTablePath As String

    'Public ReadOnly Property UseRemoteBNDBackupPath As Boolean
    'Public ReadOnly Property RemoteBNDBackupPath As String

    'Public Sub New(ByVal DarkSoulsDataPath As String,
    '               ByVal UseCustomDataRootPath As Boolean,
    '               ByVal CustomDataRootPath As String,
    '               ByVal UseRemoteBNDTablePath As Boolean,
    '               ByVal RemoteBNDTablePath As String,
    '               ByVal UseRemoteBNDBackupPath As Boolean,
    '               ByVal RemoteBNDBackupPath As String)

    '    Me.DarkSoulsDataPath = DarkSoulsDataPath
    '    Me.UseCustomDataRootPath = UseCustomDataRootPath
    '    Me.CustomDataRootPath = CustomDataRootPath
    '    Me.UseRemoteBNDTablePath = UseRemoteBNDTablePath
    '    Me.RemoteBNDTablePath = RemoteBNDTablePath
    '    Me.UseRemoteBNDBackupPath = UseRemoteBNDBackupPath
    '    Me.RemoteBNDBackupPath = RemoteBNDBackupPath

    'End Sub

    Public Shared Function TakeCurrent() As SettingsSnapshot
        Return New SettingsSnapshot() With {
            ._UseCustomDataRootPath = My.Settings.UseCustomDataRootPath,
            ._CustomDataRootPath = My.Settings.CustomDataRootPath
        }
    End Function

End Structure
