Imports System.IO

Public Class Config
    Private Sub Config_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        propertyGridSettings.BrowsableAttributes =
            New ComponentModel.AttributeCollection(New Configuration.UserScopedSettingAttribute)

        propertyGridSettings.SelectedObject = My.Settings

        AddHandler My.Settings.PropertyChanged, AddressOf SettingChanged
        AddHandler My.Settings.SettingsSaving, Sub(_sender, _e) btnSaveConfig.Enabled = False

        btnSaveConfig.Enabled = False
    End Sub

    Private Sub SettingChanged(sender As Object, e As System.ComponentModel.PropertyChangedEventArgs)
        If e.PropertyName = NameOf(My.Settings.DarkSoulsDataPath) Then
            Dim newDataPath = My.Settings.DarkSoulsDataPath

            If MainFrm.IsConfigPathValid(newDataPath) Then
                If Not MainFrm.IsConfigPathValid(My.Settings.FileBrowserStartingPath) Then
                    My.Settings.FileBrowserStartingPath = newDataPath
                End If

                If Not MainFrm.IsConfigPathValid(My.Settings.RemoteBNDBackupPath) Then
                    My.Settings.RemoteBNDBackupPath = newDataPath.TrimEnd(New Char() {"\"c, "/"c}) & "_BACKUP\" 'DATA_BACKUP
                End If

                If Not MainFrm.IsConfigPathValid(My.Settings.RemoteBNDTablePath) Then
                    My.Settings.RemoteBNDTablePath = newDataPath.TrimEnd(New Char() {"\"c, "/"c}) & "_TABLE\" 'DATA_TABLE
                End If
            End If

            propertyGridSettings.Refresh()
        End If

        btnSaveConfig.Enabled = True
    End Sub

    Private Sub ReloadConfig()
        My.Settings.Reload()

        propertyGridSettings.Refresh()
    End Sub

    Private Sub SaveConfig()
        My.Settings.Save()
    End Sub

    Private Sub btnReloadConfig_Click(sender As Object, e As EventArgs) Handles btnReloadConfig.Click
        My.Settings.Reload()
    End Sub

    Private Sub btnSaveConfig_Click(sender As Object, e As EventArgs) Handles btnSaveConfig.Click
        My.Settings.Save()
    End Sub
End Class