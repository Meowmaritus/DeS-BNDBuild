Imports System.IO
Imports System.Threading

Public Structure BNDJob
    Public Event OnFinishItem()

    Public Delegate Sub WorkDelegate(Job As BNDJob)

    Public Shared PauseCallback As New EventWaitHandle(False, EventResetMode.AutoReset)

    Private pauseHandle As ManualResetEvent
    Private stopToken As CancellationToken
    Private stopRevertToken As CancellationToken

    Public ReadOnly Type As BNDJobType
    Private ReadOnly FileList As List(Of FileInfo)
    Public ReadOnly Settings As SettingsSnapshot

    Private extractedFiles As List(Of FileInfo)
    Private processedInputFiles As List(Of FileInfo)
    Public ReadOnly Property CurrentFile As FileInfo
    Private currentFileIndex As Integer

    Public ReadOnly Game As GameDef

    Public Sub New(ByRef pauseHandle As ManualResetEvent,
                   ByRef stopToken As CancellationToken,
                   ByRef stopRevertToken As CancellationToken,
                   Type As BNDJobType, files As IEnumerable(Of String),
                   Game As GameDef)

        Me.pauseHandle = pauseHandle
        Me.stopToken = stopToken
        Me.stopRevertToken = stopRevertToken

        Me.Game = Game

        Me.Type = Type
        FileList = files.Select(Function(f) New FileInfo(f)).ToList()
        Settings = SettingsSnapshot.TakeCurrent()
        extractedFiles = New List(Of FileInfo)()
        processedInputFiles = New List(Of FileInfo)()

        currentFileIndex = -1
        NextInputFile()
    End Sub

    Public Function NextInputFile() As Boolean
        If CurrentFile IsNot Nothing Then
            processedInputFiles.Add(CurrentFile)
        End If

        currentFileIndex += 1

        RaiseEvent OnFinishItem()

        If currentFileIndex < FileList.Count Then
            _CurrentFile = FileList(currentFileIndex)
            Return True
        Else
            Return False
        End If
    End Function

    Public Sub ReportFileExtract(name As String)
        extractedFiles.Add(New FileInfo(name))
    End Sub

    Public Function Check() As Boolean

        PauseCallback.Set()
        pauseHandle.WaitOne()

        If stopRevertToken.IsCancellationRequested Then
            Select Case Type
                Case BNDJobType.Extract
                    For Each f In extractedFiles
                        File.Delete(f.FullName)
                    Next
                Case BNDJobType.Rebuild
                    For Each f In processedInputFiles
                        File.Copy(f.FullName & ".bak", f.FullName, True)
                    Next
            End Select

            Return False
        ElseIf stopToken.IsCancellationRequested Then
            Return False
        Else
            Return True
        End If
    End Function

    Public Function GetExtension(path As String, Optional includeDot As Boolean = True) As String
        Return path.Substring(path.LastIndexOf("."c) + If(includeDot, 0, 1))
    End Function

    Public Function GetPathRelativeToRootPath(path As String, rootPath As String)
        If path.StartsWith(rootPath) Then
            Return path.Substring(rootPath.Length)
        Else
            Return Nothing
        End If
    End Function

    Public Function GetDataRootPathOfFile(path As String) As String
        Try
            Return path.Substring(0, path.LastIndexOf(Game.DataRoot) + Game.DataRoot.Length)
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    Public Function GetPathRelativeToDATA(path As String) As String
        Dim root = GetDataRootPathOfFile(path)
        If root Is Nothing Then
            Return Nothing
        Else
            Return GetPathRelativeToRootPath(path, root)
        End If
    End Function

    Public Function GetPathRelativeToVanillaFRPG(path As String) As String
        Return GetPathRelativeToRootPath(path, Game.SourceRoot)
    End Function

    Public Function GetPathRelativeToCustomFRPG(path As String) As String
        Return GetPathRelativeToRootPath(path, $"{Settings.CustomDataRootPath.Trim("\")}\{Game.DisplayName}\")
    End Function

    Public Function GetFilePathWithoutExtension(path As String) As String
        Return path.Substring(0, path.LastIndexOf("."))
    End Function

    Public Function GetExtractedFile(internalFileName As String, Optional isOnlyFile As Boolean = False) As FileInfo
        Return New FileInfo(GetExtractedBndFileName(internalFileName, isOnlyFile))
    End Function

    Public Function GetExtractedBndFileName(internalFileName As String, Optional isOnlyFile As Boolean = False) As String

        Dim frpgPath = GetPathRelativeToVanillaFRPG(internalFileName)
        If Settings.UseCustomDataRootPath Then

            If frpgPath IsNot Nothing Then
                Return $"{Settings.CustomDataRootPath.Trim("\")}\{Game.DisplayName}\{frpgPath.Trim("\")}"
            Else
                Dim dataPath = GetPathRelativeToDATA(GetFilePathWithoutExtension(CurrentFile.FullName))

                If dataPath IsNot Nothing Then
                    dataPath = $"{Settings.CustomDataRootPath.Trim("\")}\{Game.DisplayName}\data\{Game.DvdRoot}\{dataPath.Trim("\")}"

                    If isOnlyFile AndAlso Path.GetFileNameWithoutExtension(CurrentFile.FullName) =
                            Path.GetFileNameWithoutExtension(dataPath & $"\{internalFileName.Trim("\")}") Then

                        dataPath &= GetExtension(internalFileName, includeDot:=True)
                    Else
                        dataPath &= $"\{internalFileName.Trim("\")}"
                    End If
                Else
                    dataPath = $"{Settings.CustomDataRootPath.Trim("\")}\{Game.DisplayName}\Unknown\{CurrentFile.Name.Trim("\")}\{If(frpgPath, internalFileName).Trim("\")}"
                End If

                Return dataPath
            End If

        Else
            Return $"{CurrentFile.FullName.Trim("\")}-Extracted\{If(frpgPath, internalFileName).Trim("\")}"
        End If

    End Function

    Private Function dir(file As FileInfo) As FileInfo
        If Not Directory.Exists(file.DirectoryName) Then
            Directory.CreateDirectory(file.DirectoryName)
        End If
        Return file
    End Function

    Public Function dir(file As String) As String
        Return dir(New FileInfo(file)).FullName
    End Function

    Public Function GetRemoteFilePath(filePath As String, sourceRootPath As String, remoteRootPath As String) As FileInfo
        Dim relDataPath = GetPathRelativeToRootPath(filePath, sourceRootPath)
        If relDataPath IsNot Nothing Then
            Return dir(New FileInfo($"{remoteRootPath.Trim("\")}\{relDataPath.Trim("\")}"))
        Else
            Return dir(New FileInfo($"{remoteRootPath.Trim("\")}\Unknown\{New FileInfo(filePath).Name.Trim("\")}"))
        End If
    End Function

    Public Function GetBNDTableFile() As FileInfo
        Dim root = GetDataRootPathOfFile(CurrentFile.FullName)
        If root Is Nothing Then
            Return dir(New FileInfo(CurrentFile.FullName & ".txt"))
        Else
            Return dir(GetRemoteFilePath(CurrentFile.FullName & ".txt", root, $"{root.Trim("\")}\BNDBuild-BNDTables\"))
        End If
    End Function

    Public Function GetBackupFile() As FileInfo
        Dim f As FileInfo = Nothing
        Dim root = GetDataRootPathOfFile(CurrentFile.FullName)
        If root Is Nothing Then
            Return dir(New FileInfo(CurrentFile.FullName & ".bak"))
        Else
            Return dir(GetRemoteFilePath(CurrentFile.FullName & ".bak", root, $"{root.Trim("\")}\BNDBuild-BNDBackups\"))
        End If
    End Function

    Public Sub CreateBackup(file As String)
        Dim f As FileInfo = GetBackupFile()

        If Not IO.File.Exists(f.FullName) Then
            IO.File.Copy(file, f.FullName)
        End If

    End Sub

    Public Function RestoreBackup(file As String) As Boolean
        Dim f As FileInfo = GetBackupFile()

        If IO.File.Exists(f.FullName) Then
            IO.File.Copy(f.FullName, file, True)
            Return True
        Else
            Return False
        End If

    End Function

End Structure
