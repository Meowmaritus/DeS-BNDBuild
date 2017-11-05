'#Const NO_ERR_HANDLING = True

Imports System.IO
Imports System.IO.Compression
Imports System.Threading

Public Class MainFrm
    Public Shared ReadOnly VALID_BND_EXTENSIONS() As String = {"BND", "MOWB", "DCX", "TPF", "BHD5", "BHD"}

    Public Const COMMENT_STR = ">"
    Public Const FRPG_ROOT = "N:\FRPG\"
    Public Const DATA_ROOT = "\DATA\"

    Public Shared CURRENT_JOB_TYPE As BNDJobType

    Public Shared bytes() As Byte
    Public Shared filename As String
    Public Shared filepath As String
    Public Shared extractPath As String

    Public Shared bigEndian As Boolean = True

    Public Shared BndThread As Thread

    Public Shared BNDJobPauseHandle As ManualResetEvent
    Public Shared BNDJobTokenSourceCancel As CancellationTokenSource
    Public Shared BNDJobTokenSourceRevertCancel As CancellationTokenSource

    Dim outputLock As New Object
    Dim workLock As New Object

    Dim ConfigWindow As Config

    Public Shared WorkBeginTrigger As New EventWaitHandle(False, EventResetMode.AutoReset)
    Public Shared WorkEndTrigger As New EventWaitHandle(False, EventResetMode.AutoReset)
    Private WorkEndTriggerThread As Thread

    Public Shared outputList As New Queue(Of String)

    Private WithEvents updateUITimer As New System.Windows.Forms.Timer()

    Dim ShiftJISEncoding As System.Text.Encoding = System.Text.Encoding.GetEncoding("shift_jis")

    Public Sub PrintLn(Optional txt As String = "")
        SyncLock outputLock
            outputList.Enqueue($"{TimeOfDay.ToLongTimeString()} - {txt}{vbCrLf}")
        End SyncLock
    End Sub

    Public Sub PrintLnErr(Optional txt As String = "")
        'TODO: MAKE ERRORS MORE OBVIOUS
        SyncLock outputLock
            outputList.Enqueue($"{TimeOfDay.ToLongTimeString()} - {txt}{vbCrLf}")
        End SyncLock
    End Sub

    Private Function GetEncoded(ByVal txt As String) As Byte()
        Return ShiftJISEncoding.GetBytes(txt)
    End Function

    Private Function EncodeFileName(ByVal filename As String, ByVal loc As UInteger) As Byte()
        'Insert string directly to main byte array
        Dim BArr = GetEncoded(filename)
        Array.Copy(BArr, 0, bytes, loc, BArr.Length)
        Return BArr
    End Function

    Private Function DecodeFileName(ByVal loc As UInteger) As String
        Dim b As New System.Collections.Generic.List(Of Byte)
        Dim cont As Boolean = True

        While cont
            If bytes(loc) > 0 Then
                b.Add(bytes(loc))
                loc += 1
            Else
                cont = False
            End If
        End While

        Return ShiftJISEncoding.GetString(b.ToArray())
    End Function

    Private Function StrFromBytes(ByVal loc As UInteger) As String
        Dim Str As String = ""
        Dim cont As Boolean = True

        While cont
            If bytes(loc) > 0 Then
                Str = Str + Convert.ToChar(bytes(loc))
                loc += 1
            Else
                cont = False
            End If
        End While

        Return Str
    End Function

    Private Function UIntFromBytes(ByVal loc As UInteger) As UInteger
        Dim tmpUint As UInteger = 0
        Dim bArr(3) As Byte

        Array.Copy(bytes, loc, bArr, 0, 4)
        If bigEndian Then
            Array.Reverse(bArr)
        End If

        tmpUint = BitConverter.ToUInt32(bArr, 0)

        Return tmpUint
    End Function

    Private Sub StrToBytes(ByVal str As String, ByVal loc As UInteger)
        'Insert string directly to main byte array
        Dim BArr() As Byte

        BArr = System.Text.Encoding.ASCII.GetBytes(str)

        Array.Copy(BArr, 0, bytes, loc, BArr.Length)
    End Sub

    Private Function StrToBytes(ByVal str As String) As Byte()
        'Return bytes of string, do not insert
        Return System.Text.Encoding.ASCII.GetBytes(str)
    End Function

    Private Sub InsBytes(ByVal bytes2() As Byte, ByVal loc As UInteger)
        Array.Copy(bytes2, 0, bytes, loc, bytes2.Length)
    End Sub

    Private Sub UIntToBytes(ByVal val As UInteger, loc As UInteger)

        Dim bArr(3) As Byte

        bArr = BitConverter.GetBytes(val)
        If bigEndian Then
            Array.Reverse(bArr)
        End If

        Array.Copy(bArr, 0, bytes, loc, 4)
    End Sub

    Private Function HashFileName(filename As String) As UInteger

        REM This code copied from https://github.com/Burton-Radons/Alexandria

        If filename Is Nothing Then
            Return 0
        End If

        Dim hash As UInteger = 0

        For Each ch As Char In filename
            hash = hash * &H25 + Asc(Char.ToLowerInvariant(ch))
        Next

        Return hash
    End Function

    Private Sub WriteBytes(ByRef fs As FileStream, ByVal byt() As Byte)
        'Write to stream at present location
        For i = 0 To byt.Length - 1
            fs.WriteByte(byt(i))
        Next
    End Sub

    Private Sub SetGUIDisabled(disabled As Boolean)

        Invoke(
            Sub()

                btnAddDirectory.Enabled = Not disabled
                btnAddFiles.Enabled = Not disabled

                btnExtract.Enabled = Not disabled
                btnRebuild.Enabled = Not disabled
                btnRestoreBackups.Enabled = Not disabled

                btnExtract.Visible = Not disabled
                btnRebuild.Visible = Not disabled
                btnRestoreBackups.Visible = Not disabled

                progressBar.Enabled = disabled
                progressBar.Visible = disabled

                UseWaitCursor = disabled

                btnSaveList.Enabled = Not disabled
                btnLoadList.Enabled = Not disabled

                ConfigWindow.propertyGridSettings.Enabled = Not disabled
                ConfigWindow.btnSaveConfig.Enabled = Not disabled
                ConfigWindow.btnReloadConfig.Enabled = Not disabled

                txtBNDfile.ReadOnly = disabled

                btnCancel.Enabled = disabled

            End Sub)
    End Sub

    Private Function GetBndFilesCleaned(inputFileList As IEnumerable(Of String)) As List(Of String)
        Dim result As New List(Of String)()

        Dim commentStartIndex As Integer = -1

        For Each line In inputFileList
            Dim actualLine As String = line
            commentStartIndex = line.IndexOf(COMMENT_STR)
            If commentStartIndex >= 0 Then
                actualLine = actualLine.Substring(0, commentStartIndex)
            End If
            actualLine = actualLine.Trim()

            If (Not String.IsNullOrWhiteSpace(actualLine)) AndAlso (Not result.Contains(actualLine)) Then

                If File.Exists(actualLine) Then
                    result.Add(actualLine)
                Else
                    If MessageBox.Show($"File '{actualLine}' does not exist and will be ignored.",
                        "Warning", MessageBoxButtons.OKCancel,
                                       MessageBoxIcon.Warning) = DialogResult.Cancel Then

                        Return Nothing

                    End If
                End If

            End If
        Next

        Return result
    End Function

    Private Sub BeginBNDJob(Job As BNDJob)

        'Trigger work begin and wait for the GUI update thread to call back before proceeding.
        WorkBeginTrigger.Set()
        WorkBeginTrigger.WaitOne()

        'Try

        Dim work As BNDJob.WorkDelegate = Nothing

        Select Case Job.Type
            Case BNDJobType.Extract : work = AddressOf EXTRACT
            Case BNDJobType.Rebuild : work = AddressOf REBUILD
            Case BNDJobType.RestoreBackups : work = AddressOf RESTORE_BACKUPS
            Case Else : Throw New Exception($"Job '{Job.Type}' not programmed in.")
        End Select

        If work IsNot Nothing Then
            Do
                If Job.CurrentFile IsNot Nothing Then
                    work(Job)
                End If
                If Not Job.Check() Then
                    WorkEndTrigger.Set()
                    WorkEndTrigger.WaitOne()
                    Return
                End If
            Loop While Job.NextInputFile()
        End If

        'Catch ex As Exception
        '    PrintLn($"An exception ocurred while extracting file(s): '{ex.Message}'")
        'Finally
        '    'Trigger work end and wait for the GUI update thread to call back before proceeding.
        '    WorkEndTrigger.Set()
        '    WorkEndTrigger.WaitOne()
        'End Try

        WorkEndTrigger.Set()
        WorkEndTrigger.WaitOne()
    End Sub

    Private Sub BNDJobThreadStart(oJob As Object)
        Dim Job As BNDJob = DirectCast(oJob, BNDJob)
        BeginBNDJob(Job)
    End Sub

    Private Function StartNewBNDJobThread(Job As BNDJob) As Thread
        Dim jobThread = New Thread(AddressOf BNDJobThreadStart) With {.IsBackground = False}
        jobThread.Start(Job)
        Return jobThread
    End Function

    Private Sub START_JOB(type As BNDJobType)
        Dim fileList = GetBndFilesCleaned(txtBNDfile.Lines)

        If fileList Is Nothing Then
            Return
        ElseIf fileList.Count = 0 Then
            MessageBox.Show("File list contains no valid BND file paths; operation cancelled.", "Notice",
                MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        progressBar.Minimum = 0
        progressBar.Value = 0
        progressBar.Maximum = fileList.Count

        SetGUIDisabled(True)
        BNDJobPauseHandle = New ManualResetEvent(True)
        BNDJobTokenSourceCancel = New CancellationTokenSource()
        BNDJobTokenSourceRevertCancel = New CancellationTokenSource()
        BndThread?.Abort()
        CURRENT_JOB_TYPE = type

        Dim nextJob = New BNDJob(BNDJobPauseHandle,
                       BNDJobTokenSourceCancel.Token,
                       BNDJobTokenSourceRevertCancel.Token,
                       type, fileList)

        AddHandler nextJob.OnFinishItem, AddressOf CurrentJob_OnFinishItem

        BndThread = StartNewBNDJobThread(nextJob)
    End Sub

    Private Sub CurrentJob_OnFinishItem()
        Invoke(
        Sub()
            progressBar.Increment(1)
        End Sub)
    End Sub

    Private Sub btnExtract_Click(sender As Object, e As EventArgs) Handles btnExtract.Click
        START_JOB(BNDJobType.Extract)
    End Sub

    Private Sub btnRebuild_Click(sender As Object, e As EventArgs) Handles btnRebuild.Click
        START_JOB(BNDJobType.Rebuild)
    End Sub

    Private Sub RESTORE_BACKUPS(Job As BNDJob)
        If Job.RestoreBackup(Job.CurrentFile.FullName) Then

            If My.Settings.EnableVerboseOutput Then
                PrintLn($"Restored backup of '{Job.CurrentFile.Name}'")
            End If

        Else
            PrintLnErr($"Could not find backup of '{Job.CurrentFile.Name}'.")
        End If
    End Sub

    Private Sub EXTRACT(Job As BNDJob)
#If Not NO_ERR_HANDLING Then
        Try
#End If
            'TODO:  Confirm endian correctness for all DeS/DaS PC/PS3 formats
            'TODO:  Bitch about the massive job that is the above

            'TODO:  Do it anyway.

            'TODO:  In the endian checks, look into why you check if it equals 0
            '       Since that can't matter, since a non-zero value will still
            '       be non-zero in either endian.

            '       Seriously, what the hell were you thinking?

            Dim frpg As Boolean = Job.Settings.UseCustomDataRootPath

            bigEndian = True
            Dim DCX As Boolean = False

            Dim currFileName As String = ""
            Dim currFilePath As String = ""

            Dim currFileInfo As FileInfo = Nothing

            Dim fileList As String = ""

            Dim BinderID As String = ""
            Dim namesEndLoc As UInteger = 0
            Dim flags As UInteger = 0
            Dim numFiles As UInteger = 0

            Dim BND As String = Job.CurrentFile.FullName

            filepath = Microsoft.VisualBasic.Left(BND, InStrRev(BND, "\"))
            filename = Microsoft.VisualBasic.Right(BND, BND.Length - filepath.Length)

            bytes = File_ReadAllBytes(filepath & filename)

            Job.CreateBackup(filepath & filename)

            'PrintLn("Beginning extraction.")

            Select Case Microsoft.VisualBasic.Left(StrFromBytes(0), 4)
                Case "BHD5"
#Region "Extract BHD5"
                    bigEndian = False
                    If Not (UIntFromBytes(&H4) And &HFF) = &HFF Then
                        bigEndian = True
                    End If

                    fileList = "BHD5,"

                    Dim currFileSize As UInteger = 0
                    Dim currFileOffset As UInteger = 0
                    Dim currFileID As UInteger = 0
                    Dim currFileNameOffset As UInteger = 0
                    Dim currFileBytes() As Byte = {}

                    Dim count As UInteger = 0

                    Dim idx As Integer
                    Dim fileidx() As String = My.Resources.fileidx.Replace(Chr(&HD), "").Split(Chr(&HA))
                    Dim hashidx(fileidx.Length - 1) As UInteger

                    For i = 0 To fileidx.Length - 1
                        hashidx(i) = HashFileName(fileidx(i))
                    Next

                    flags = UIntFromBytes(&H4)
                    numFiles = UIntFromBytes(&H10)

                    filename = Microsoft.VisualBasic.Left(filename, filename.Length - 5)

                    Job.CreateBackup(filepath & filename & ".bdt")

                    Dim BDTStream As New IO.FileStream(filepath & filename & ".bdt", IO.FileMode.Open)
                    Dim bhdOffSet As UInteger

                    BinderID = ""
                    For k = 0 To &HF
                        Dim tmpchr As Char
                        tmpchr = Chr(BDTStream.ReadByte)
                        If Not Asc(tmpchr) = 0 Then
                            BinderID = BinderID & tmpchr
                        Else
                            Exit For
                        End If
                    Next
                    fileList = fileList & BinderID & Environment.NewLine & flags & Environment.NewLine

                    For i As UInteger = 0 To numFiles - 1

                        If Not Job.Check() Then Return

                        count = UIntFromBytes(&H18 + i * &H8)
                        bhdOffSet = UIntFromBytes(&H1C + i * 8)

                        For j = 0 To count - 1

                            If Not Job.Check() Then Return

                            currFileSize = UIntFromBytes(bhdOffSet + &H4)

                            If bigEndian Then
                                currFileOffset = UIntFromBytes(bhdOffSet + &HC)
                            Else
                                currFileOffset = UIntFromBytes(bhdOffSet + &H8)
                            End If

                            ReDim currFileBytes(currFileSize - 1)

                            BDTStream.Position = currFileOffset

                            For k = 0 To currFileSize - 1
                                currFileBytes(k) = BDTStream.ReadByte
                            Next

                            currFileName = ""

                            If hashidx.Contains(UIntFromBytes(bhdOffSet)) Then
                                idx = Array.IndexOf(hashidx, UIntFromBytes(bhdOffSet))

                                currFileName = fileidx(idx)
                                currFileName = currFileName.Replace("/", "\")
                                fileList += i & "," & currFileName & Environment.NewLine

                                currFileName = filepath & filename & ".bhd5" & ".extract" & currFileName
                                currFilePath = Microsoft.VisualBasic.Left(currFileName, InStrRev(currFileName, "\"))
                            Else
                                idx = -1
                                currFileName = "NOMATCH-" & Hex(UIntFromBytes(bhdOffSet))
                                fileList += i & "," & currFileName & Environment.NewLine

                                currFileName = filepath & filename & ".bhd5" & ".extract\" & currFileName
                                currFilePath = Microsoft.VisualBasic.Left(currFileName, InStrRev(currFileName, "\"))
                            End If

                            If (Not System.IO.Directory.Exists(currFilePath)) Then
                                System.IO.Directory.CreateDirectory(currFilePath)
                            End If

                            File.WriteAllBytes(currFileName, currFileBytes)

                            If My.Settings.EnableVerboseOutput Then
                                PrintLn($"Extracted '{currFileName}'.")
                            End If

                            bhdOffSet += &H10
                        Next

                    Next
                    filename = filename & ".bhd5"
                    BDTStream.Close()
                    BDTStream.Dispose()
#End Region
                Case "BHF3"
#Region "Extract BHF3"
                    fileList = "BHF3,"

                    REM this assumes we'll always have between 1 and 16777215 files
                    bigEndian = False
                    If UIntFromBytes(&H10) >= &H1000000 Then
                        bigEndian = True
                    Else
                        bigEndian = False
                    End If

                    Dim currFileSize As UInteger = 0
                    Dim currFileOffset As UInteger = 0
                    Dim currFileID As UInteger = 0
                    Dim currFileNameOffset As UInteger = 0
                    Dim currFileBytes() As Byte = {}

                    Dim count As UInteger = 0

                    flags = UIntFromBytes(&HC)
                    numFiles = UIntFromBytes(&H10)

                    filename = Microsoft.VisualBasic.Left(filename, filename.Length - 3)

                    Job.CreateBackup(filepath & filename & "bdt")

                    Dim BDTStream As New IO.FileStream(filepath & filename & "bdt", IO.FileMode.Open)
                    Dim bhdOffSet As UInteger = &H20

                    BinderID = StrFromBytes(&H4)
                    fileList = fileList & BinderID & Environment.NewLine & flags & Environment.NewLine

                    For i As UInteger = 0 To numFiles - 1

                        If Not Job.Check() Then Return

                        currFileSize = UIntFromBytes(bhdOffSet + &H4)
                        currFileOffset = UIntFromBytes(bhdOffSet + &H8)
                        currFileID = UIntFromBytes(bhdOffSet + &HC)

                        ReDim currFileBytes(currFileSize - 1)

                        BDTStream.Position = currFileOffset

                        For k = 0 To currFileSize - 1
                            currFileBytes(k) = BDTStream.ReadByte
                        Next

                        currFileName = DecodeFileName(UIntFromBytes(bhdOffSet + &H10))
                        fileList += currFileID & "," & currFileName & Environment.NewLine

                        currFileName = filepath & filename & "bhd" & ".extract\" & currFileName
                        currFilePath = Microsoft.VisualBasic.Left(currFileName, InStrRev(currFileName, "\"))

                        If (Not System.IO.Directory.Exists(currFilePath)) Then
                            System.IO.Directory.CreateDirectory(currFilePath)
                        End If

                        File.WriteAllBytes(currFileName, currFileBytes)

                        bhdOffSet += &H18
                    Next
                    filename = filename & "bhd"
                    BDTStream.Close()
                    BDTStream.Dispose()
#End Region
                Case "BND3"
#Region "Extract BND3"
                    'TODO:  DeS, c0300.anibnd, no files found?
                    Dim currFileSize As UInteger = 0
                    Dim currFileOffset As UInteger = 0
                    Dim currFileID As UInteger = 0
                    Dim currFileNameOffset As UInteger = 0
                    Dim currFileBytes() As Byte = {}

                    BinderID = Microsoft.VisualBasic.Left(StrFromBytes(&H0), 12)
                    flags = UIntFromBytes(&HC)

                    If flags = &H74000000 Or flags = &H54000000 Or flags = &H70000000 Then bigEndian = False

                    numFiles = UIntFromBytes(&H10)
                    namesEndLoc = UIntFromBytes(&H14)

                    fileList = BinderID & Environment.NewLine & flags & Environment.NewLine

                    If numFiles = 0 Then
                        PrintLnErr("Warning: No files found in archive '" & BND & "'.")
                        Exit Sub
                    End If

                    For i As UInteger = 0 To numFiles - 1
                        If Not Job.Check() Then Return

                        Select Case flags
                            Case &H70000000
                                currFileSize = UIntFromBytes(&H24 + i * &H14)
                                currFileOffset = UIntFromBytes(&H28 + i * &H14)
                                currFileID = UIntFromBytes(&H2C + i * &H14)
                                currFileNameOffset = UIntFromBytes(&H30 + i * &H14)
                                currFileName = DecodeFileName(currFileNameOffset)
                                fileList += currFileID & "," & currFileName & Environment.NewLine

                                currFileInfo = Job.GetExtractedFile(currFileName)
                                currFilePath = currFileInfo.DirectoryName & "\"
                                currFileName = currFileInfo.Name

                            Case &H74000000, &H54000000
                                currFileSize = UIntFromBytes(&H24 + i * &H18)
                                currFileOffset = UIntFromBytes(&H28 + i * &H18)
                                currFileID = UIntFromBytes(&H2C + i * &H18)
                                currFileNameOffset = UIntFromBytes(&H30 + i * &H18)
                                currFileName = DecodeFileName(currFileNameOffset)
                                fileList += currFileID & "," & currFileName & Environment.NewLine

                                currFileInfo = Job.GetExtractedFile(currFileName)
                                currFilePath = currFileInfo.DirectoryName & "\"
                                currFileName = currFileInfo.Name

                            Case &H10100
                                currFileSize = UIntFromBytes(&H24 + i * &HC)
                                currFileOffset = UIntFromBytes(&H28 + i * &HC)
                                currFileID = i
                                currFileName = i & "." & Microsoft.VisualBasic.Left(DecodeFileName(currFileOffset), 4)
                                fileList += currFileName & Environment.NewLine

                                currFileInfo = Job.GetExtractedFile(currFileName)
                                currFilePath = currFileInfo.DirectoryName & "\"
                                currFileName = currFileInfo.Name

                            Case &HE010100
                                currFileSize = UIntFromBytes(&H24 + i * &H14)
                                currFileOffset = UIntFromBytes(&H28 + i * &H14)
                                currFileID = UIntFromBytes(&H2C + i * &H14)
                                currFileNameOffset = UIntFromBytes(&H30 + i * &H14)
                                currFileName = DecodeFileName(currFileNameOffset)
                                fileList += currFileID & "," & currFileName & Environment.NewLine

                                currFileInfo = Job.GetExtractedFile(currFileName)
                                currFilePath = currFileInfo.DirectoryName & "\"
                                currFileName = currFileInfo.Name

                            Case &H2E010100
                                currFileSize = UIntFromBytes(&H24 + i * &H18)
                                currFileOffset = UIntFromBytes(&H28 + i * &H18)
                                currFileID = UIntFromBytes(&H2C + i * &H18)
                                currFileNameOffset = UIntFromBytes(&H30 + i * &H18)
                                currFileName = DecodeFileName(currFileNameOffset)
                                fileList += currFileID & "," & currFileName & Environment.NewLine

                                currFileInfo = Job.GetExtractedFile(currFileName)
                                currFilePath = currFileInfo.DirectoryName & "\"
                                currFileName = currFileInfo.Name

                        End Select

                        If (Not System.IO.Directory.Exists(currFilePath)) Then
                            System.IO.Directory.CreateDirectory(currFilePath)
                        End If

                        ReDim currFileBytes(currFileSize - 1)
                        Array.Copy(bytes, currFileOffset, currFileBytes, 0, currFileSize)
                        File.WriteAllBytes(currFilePath & currFileName, currFileBytes)
                    Next
#End Region
                Case "TPF"
#Region "Extract TPF"
                    Dim currFileSize As UInteger = 0
                    Dim currFileOffset As UInteger = 0
                    Dim currFileID As UInteger = 0
                    Dim currFileNameOffset As UInteger = 0
                    Dim currFileBytes() As Byte = {}
                    Dim currFileFlags1 As UInteger = 0
                    Dim currFileFlags2 As UInteger = 0

                    bigEndian = False
                    If UIntFromBytes(&H8) >= &H1000000 Then
                        bigEndian = True
                    Else
                        bigEndian = False
                    End If

                    flags = UIntFromBytes(&HC)

                    If flags = &H2010200 Or flags = &H2010000 Then
                        ' Demon's Souls

                        BinderID = Microsoft.VisualBasic.Left(StrFromBytes(&H0), 3)
                        numFiles = UIntFromBytes(&H8)

                        Dim isOnlyOneTexture = (numFiles = 1)

                        currFileNameOffset = UIntFromBytes(&H10)

                        fileList = BinderID & Environment.NewLine & flags & Environment.NewLine

                        For i As UInteger = 0 To numFiles - 1
                            If Not Job.Check() Then Return

                            currFileOffset = UIntFromBytes(&H10 + i * &H20)
                            currFileSize = UIntFromBytes(&H14 + i * &H20)
                            currFileFlags1 = UIntFromBytes(&H18 + i * &H20)
                            currFileFlags2 = UIntFromBytes(&H1C + i * &H20)
                            currFileNameOffset = UIntFromBytes(&H28 + i * &H20)
                            currFileName = DecodeFileName(currFileNameOffset)
                            fileList += currFileFlags1 & "," & currFileFlags2 & "," & currFileName & Environment.NewLine

                            currFileInfo = Job.GetExtractedFile(currFileName, isOnlyOneTexture)
                            currFilePath = currFileInfo.DirectoryName & "\"
                            currFileName = currFileInfo.Name

                            If (Not System.IO.Directory.Exists(currFilePath)) Then
                                System.IO.Directory.CreateDirectory(currFilePath)
                            End If

                            ReDim currFileBytes(currFileSize - 1)
                            Array.Copy(bytes, currFileOffset, currFileBytes, 0, currFileSize)
                            File.WriteAllBytes(currFilePath & currFileName, currFileBytes)
                        Next
                    ElseIf flags = &H20300 Then
                        ' Dark Souls

                        BinderID = Microsoft.VisualBasic.Left(StrFromBytes(&H0), 3)
                        numFiles = UIntFromBytes(&H8)

                        Dim isOnlyOneTexture = (numFiles = 1)

                        fileList = BinderID & Environment.NewLine & flags & Environment.NewLine

                        For i As UInteger = 0 To numFiles - 1
                            If Not Job.Check() Then Return

                            currFileOffset = UIntFromBytes(&H10 + i * &H14)
                            currFileSize = UIntFromBytes(&H14 + i * &H14)
                            currFileFlags1 = UIntFromBytes(&H18 + i * &H14)
                            currFileNameOffset = UIntFromBytes(&H1C + i * &H14)
                            currFileFlags2 = UIntFromBytes(&H20 + i * &H14)

                            currFileName = DecodeFileName(currFileNameOffset) & ".dds"
                            fileList += currFileFlags1 & "," & currFileFlags2 & "," & currFileName & Environment.NewLine

                            currFileInfo = Job.GetExtractedFile(currFileName, isOnlyOneTexture)
                            currFilePath = currFileInfo.DirectoryName & "\"
                            currFileName = currFileInfo.Name

                            If (Not System.IO.Directory.Exists(currFilePath)) Then
                                System.IO.Directory.CreateDirectory(currFilePath)
                            End If

                            ReDim currFileBytes(currFileSize - 1)
                            Array.Copy(bytes, currFileOffset, currFileBytes, 0, currFileSize)
                            File.WriteAllBytes(currFilePath & currFileName, currFileBytes)
                        Next
                    Else
                        Throw New Exception("Unknown TPF Format")
                    End If
#End Region
                Case "DCX"

                    Select Case StrFromBytes(&H28)
                        Case "EDGE"
#Region "Extract DCX - EDGE"
                            DCX = True
                            Dim newbytes(&H10000) As Byte
                            Dim decbytes(&H10000) As Byte
                            Dim bytes2(UIntFromBytes(&H1C) - 1) As Byte

                            Dim startOffset As UInteger = UIntFromBytes(&H14) + &H20
                            Dim numChunks As UInteger = UIntFromBytes(&H68)
                            Dim DecSize As UInteger

                            fileList = DecodeFileName(&H28) & Environment.NewLine & Microsoft.VisualBasic.Left(filename, filename.Length - &H4) & Environment.NewLine

                            For i = 0 To numChunks - 1
                                If Not Job.Check() Then Return
                                If i = numChunks - 1 Then
                                    DecSize = bytes2.Length - DecSize * i
                                Else
                                    DecSize = &H10000
                                End If

                                Array.Copy(bytes, startOffset + UIntFromBytes(&H74 + i * &H10), newbytes, 0, UIntFromBytes(&H78 + i * &H10))
                                decbytes = Decompress(newbytes)
                                Array.Copy(decbytes, 0, bytes2, &H10000 * i, DecSize)
                            Next

                            'TODO:FRPG

                            currFileName = filepath & Microsoft.VisualBasic.Left(filename, filename.Length - &H4)
                            currFilePath = Microsoft.VisualBasic.Left(currFileName, InStrRev(currFileName, "\"))

                            If (Not System.IO.Directory.Exists(currFilePath)) Then
                                System.IO.Directory.CreateDirectory(currFilePath)
                            End If

                            File.WriteAllBytes(currFileName, bytes2)
#End Region
                        Case "DFLT"
#Region "Extract DCX - DFLT"
                            DCX = True
                            Dim startOffset As UInteger = UIntFromBytes(&H14) + &H22

                            Dim newbytes(UIntFromBytes(&H20) - 1) As Byte
                            Dim decbytes(UIntFromBytes(&H1C)) As Byte

                            fileList = DecodeFileName(&H28) & Environment.NewLine & Microsoft.VisualBasic.Left(filename, filename.Length - &H4) & Environment.NewLine

                            Array.Copy(bytes, startOffset, newbytes, 0, newbytes.Length - 2)

                            decbytes = Decompress(newbytes)

                            'TODO:FRPG
                            'TODO:Chain extract DCX -> BND -> actual files in one operation

                            currFileName = filepath & Microsoft.VisualBasic.Left(filename, filename.Length - &H4)
                            currFilePath = Microsoft.VisualBasic.Left(currFileName, InStrRev(currFileName, "\"))

                            If (Not System.IO.Directory.Exists(currFilePath)) Then
                                System.IO.Directory.CreateDirectory(currFilePath)
                            End If

                            File.WriteAllBytes(currFileName, decbytes)
#End Region
                    End Select

            End Select

            File.WriteAllText(Job.GetBNDTableFile().FullName, fileList)

            If My.Settings.EnableVerboseOutput Then
                PrintLn($"{filename} extracted.")
            End If

#If Not NO_ERR_HANDLING Then
        Catch ex As Exception
            PrintLnErr($"{vbCrLf}Encountered an error while extracting '{Job.CurrentFile.Name}':{vbCrLf}{vbTab}{ex.Message}{vbCrLf}")
        End Try
#End If

        'txtInfo.Text += TimeOfDay & " - " & filename & " extracted." & Environment.NewLine
    End Sub

    Private Sub REBUILD(Job As BNDJob)
        'TODO:  Confirm endian before each rebuild.

        'TODO:  List of non-DCXs that don't rebuild byte-perfect
        '   DeS, facegen.tpf
        '   DeS, i7006.tpf
        '   DeS, m07_9990.tpf
        '   DaS, m10_9999.tpf

#If Not NO_ERR_HANDLING Then
        Try
#End If

            bigEndian = True

            Dim frpg = Job.Settings.UseCustomDataRootPath

            Dim DCX As Boolean = False

            Dim currFileSize As UInteger = 0
            Dim currFileOffset As UInteger = 0
            Dim currFileNameOffset As UInteger = 0
            Dim currFileName As String = ""
            Dim currFilePath As String = ""
            Dim currFileInfo As FileInfo = Nothing
            Dim currFileBytes() As Byte = {}
            Dim currFileID As UInteger = 0
            Dim currFileListEntry As String = ""
            Dim namesEndLoc As UInteger = 0
            Dim fileList As String() = {""}
            Dim BinderID As String = ""
            Dim flags As UInteger = 0
            Dim numFiles As UInteger = 0
            Dim tmpbytes() As Byte

            Dim padding As UInteger = 0

            Dim BND As String = Job.CurrentFile.FullName

            filepath = Microsoft.VisualBasic.Left(BND, InStrRev(BND, "\"))
            filename = Microsoft.VisualBasic.Right(BND, BND.Length - filepath.Length)

            DCX = (Microsoft.VisualBasic.Right(filename, 4).ToLower = ".dcx")

            fileList = File_ReadAllLines(Job.GetBNDTableFile().FullName).Where(
                Function(line)
                    Return (Not String.IsNullOrWhiteSpace(line.Trim()))
                End Function).ToArray()

            Select Case Microsoft.VisualBasic.Left(fileList(0), 4)
                Case "BHD5"
#Region "Rebuild BHD5"
                    BinderID = fileList(0).Split(",")(1)
                    flags = fileList(1)
                    numFiles = fileList.Length - 2
                    If flags = 0 Then
                        bigEndian = True
                    Else
                        bigEndian = False
                    End If

                    Dim BDTFilename As String
                    BDTFilename = Microsoft.VisualBasic.Left(BND, InStrRev(BND, ".")) & "bdt"

                    File.Delete(BDTFilename)

                    Dim BDTStream As New IO.FileStream(BDTFilename, IO.FileMode.CreateNew)

                    BDTStream.Position = 0
                    WriteBytes(BDTStream, StrToBytes(BinderID))
                    BDTStream.Position = &H10

                    ReDim bytes(&H17)

                    Dim bins(fileList.Length - 2) As UInteger
                    Dim currBin As UInteger = 0
                    Dim totBin As UInteger = 0

                    Dim bdtoffset As UInteger = &H10

                    For i = 0 To fileList.Length - 3
                        If Not Job.Check() Then Return
                        currBin = currFileListEntry.Split(",")(0)
                        bins(currBin) += 1
                    Next

                    totBin = Val(fileList(numFiles + 1).Split(",")(0)) + 1

                    StrToBytes("BHD5", 0)
                    UIntToBytes(flags, &H4)
                    UIntToBytes(1, &H8)
                    'total file size, &HC
                    UIntToBytes(totBin, &H10)
                    UIntToBytes(&H18, &H14)

                    ReDim Preserve bytes(&H17 + totBin * &H8)
                    Dim idxOffset As UInteger
                    idxOffset = &H18 + totBin * &H8

                    For i As UInteger = 0 To totBin - 1
                        If Not Job.Check() Then Return
                        UIntToBytes(bins(i), &H18 + i * &H8)
                        UIntToBytes(idxOffset, &H1C + i * &H8)
                        idxOffset += bins(i) * &H10
                    Next

                    ReDim Preserve bytes(bytes.Length + numFiles * &H10 - 1)
                    idxOffset = &H18 + totBin * &H8

                    For i = 0 To numFiles - 1
                        If Not Job.Check() Then Return
                        currFileName = currFileListEntry.Split(",")(1)
                        If currFileName(0) = "\" Then
                            UIntToBytes(HashFileName(currFileName.Replace("\", "/")), idxOffset + i * &H10)
                        Else
                            UIntToBytes(Convert.ToUInt32(currFileName.Split("-")(1), 16), idxOffset + i * &H10)
                            currFileName = "\" & currFileName
                        End If

                        Dim fStream As New IO.FileStream(filepath & filename & ".extract" & currFileName, IO.FileMode.Open)

                        UIntToBytes(fStream.Length, idxOffset + &H4 + i * &H10)
                        If bigEndian Then
                            UIntToBytes(bdtoffset, idxOffset + &HC + i * &H10)
                        Else
                            UIntToBytes(bdtoffset, idxOffset + &H8 + i * &H10)
                        End If

                        For j = 0 To fStream.Length - 1
                            BDTStream.WriteByte(fStream.ReadByte)
                        Next

                        fStream.CopyTo(BDTStream)

                        bdtoffset = BDTStream.Position
                        If bdtoffset Mod &H10 > 0 Then
                            padding = &H10 - (bdtoffset Mod &H10)
                        Else
                            padding = 0
                        End If
                        bdtoffset += padding

                        BDTStream.Position = bdtoffset

                        fStream.Close()
                        fStream.Dispose()
                    Next

                    UIntToBytes(bytes.Length, &HC)

                    BDTStream.Close()
                    BDTStream.Dispose()
                    If My.Settings.EnableVerboseOutput Then
                        PrintLn($"{BDTFilename} rebuilt.")
                    End If
#End Region
                Case "BHF3"
#Region "Rebuild BHF3"
                    BinderID = fileList(0).Split(",")(1)
                    flags = fileList(1)
                    numFiles = fileList.Length - 2

                    Dim currNameOffset As UInteger = 0

                    Dim BDTFilename As String
                    BDTFilename = Microsoft.VisualBasic.Left(BND, BND.Length - 3) & "bdt"

                    File.Delete(BDTFilename)

                    Dim BDTStream As New IO.FileStream(BDTFilename, IO.FileMode.CreateNew)

                    BDTStream.Position = 0
                    WriteBytes(BDTStream, StrToBytes("BDF3" & BinderID))
                    BDTStream.Position = &H10

                    ReDim bytes(&H1F)

                    Dim bdtoffset As UInteger = &H10

                    StrToBytes("BHF3" & BinderID, 0)

                    UIntToBytes(flags, &HC)
                    UIntToBytes(numFiles, &H10)

                    ReDim Preserve bytes(&H1F + numFiles * &H18)
                    Dim idxOffset As UInteger
                    idxOffset = &H20

                    For i = 0 To numFiles - 1
                        currFileID = currFileListEntry.Split(",")(0)
                        currFileName = currFileListEntry.Split(",")(1)
                        currNameOffset = bytes.Length

                        Dim fStream As New IO.FileStream(filepath & filename & ".extract\" & currFileName, IO.FileMode.Open)

                        UIntToBytes(&H2000000, idxOffset + i * &H18)
                        UIntToBytes(fStream.Length, idxOffset + &H4 + i * &H18)
                        UIntToBytes(bdtoffset, idxOffset + &H8 + i * &H18)
                        UIntToBytes(currFileID, idxOffset + &HC + i * &H18)
                        UIntToBytes(currNameOffset, idxOffset + &H10 + i * &H18)
                        UIntToBytes(fStream.Length, idxOffset + &H14 + i * &H18)

                        ReDim Preserve bytes(bytes.Length + currFileName.Length)

                        EncodeFileName(currFileName, currNameOffset)

                        For j = 0 To fStream.Length - 1
                            BDTStream.WriteByte(fStream.ReadByte)
                        Next

                        bdtoffset = BDTStream.Position
                        If bdtoffset Mod &H10 > 0 Then
                            padding = &H10 - (bdtoffset Mod &H10)
                        Else
                            padding = 0
                        End If
                        bdtoffset += padding

                        BDTStream.Position = bdtoffset

                        fStream.Close()
                        fStream.Dispose()
                    Next

                    BDTStream.Close()
                    BDTStream.Dispose()

                    If My.Settings.EnableVerboseOutput Then
                        PrintLn($"{BDTFilename} rebuilt.")
                    End If

#End Region
                Case "BND3"
#Region "Rebuild BND3"
                    ReDim bytes(&H1F)
                    StrToBytes(fileList(0), 0)

                    flags = fileList(1)
                    numFiles = fileList.Length - 2

                    For i = 2 To fileList.Length - 1
                        'TODO: REDUCE FILE NAME ENCODING REDUNDANCY
                        namesEndLoc += GetEncoded(fileList(i)).Length - InStr(fileList(i), ",") + 1
                    Next

                    Select Case flags
                        Case &H70000000
                            currFileNameOffset = &H20 + &H14 * numFiles
                            namesEndLoc += currFileNameOffset
                        Case &H74000000, &H54000000
                            currFileNameOffset = &H20 + &H18 * numFiles
                            namesEndLoc += currFileNameOffset
                        Case &H10100
                            namesEndLoc = &H30 + &HC * numFiles
                        Case &HE010100
                            currFileNameOffset = &H20 + &H14 * numFiles
                            namesEndLoc += currFileNameOffset
                        Case &H2E010100
                            currFileNameOffset = &H20 + &H18 * numFiles
                            namesEndLoc += currFileNameOffset
                    End Select

                    UIntToBytes(flags, &HC)
                    If flags = &H74000000 Or flags = &H54000000 Or flags = &H70000000 Then bigEndian = False

                    UIntToBytes(numFiles, &H10)
                    UIntToBytes(namesEndLoc, &H14)

                    If namesEndLoc Mod &H10 > 0 Then
                        padding = &H10 - (namesEndLoc Mod &H10)
                    Else
                        padding = 0
                    End If

                    ReDim Preserve bytes(namesEndLoc + padding - 1)

                    currFileOffset = namesEndLoc + padding

                    For i As UInteger = 0 To numFiles - 1

                        currFileListEntry = fileList(i + 2)

                        Select Case flags
                            Case &H70000000

                                currFileName = currFileListEntry.Substring(currFileListEntry.IndexOf(",") + 1)

                                currFileInfo = Job.GetExtractedFile(currFileName)
                                currFilePath = currFileInfo.DirectoryName & "\"
                                currFileName = currFileInfo.Name

                                tmpbytes = File_ReadAllBytes(currFilePath & currFileName)
                                currFileID = Microsoft.VisualBasic.Left(currFileListEntry, InStr(currFileListEntry, ",") - 1)

                                UIntToBytes(&H40, &H20 + i * &H14)
                                UIntToBytes(tmpbytes.Length, &H24 + i * &H14)
                                UIntToBytes(currFileOffset, &H28 + i * &H14)
                                UIntToBytes(currFileID, &H2C + i * &H14)
                                UIntToBytes(currFileNameOffset, &H30 + i * &H14)

                                If tmpbytes.Length Mod &H10 > 0 Then
                                    padding = &H10 - (tmpbytes.Length Mod &H10)
                                Else
                                    padding = 0
                                End If
                                If i = numFiles - 1 Then padding = 0
                                ReDim Preserve bytes(bytes.Length + tmpbytes.Length + padding - 1)

                                InsBytes(tmpbytes, currFileOffset)

                                currFileOffset += tmpbytes.Length
                                If currFileOffset Mod &H10 > 0 Then
                                    padding = &H10 - (currFileOffset Mod &H10)
                                Else
                                    padding = 0
                                End If
                                currFileOffset += padding

                                currFileNameOffset += EncodeFileName(Microsoft.VisualBasic.Right(currFileListEntry, currFileListEntry.Length - (InStr(currFileListEntry, ","))), currFileNameOffset).Length + 1

                            Case &H74000000, &H54000000
                                currFileName = currFileListEntry.Substring(currFileListEntry.IndexOf(",") + 1)

                                currFileInfo = Job.GetExtractedFile(currFileName)
                                currFilePath = currFileInfo.DirectoryName & "\"
                                currFileName = currFileInfo.Name

                                tmpbytes = File_ReadAllBytes(currFilePath & currFileName)
                                currFileID = Microsoft.VisualBasic.Left(currFileListEntry, InStr(currFileListEntry, ",") - 1)

                                UIntToBytes(&H40, &H20 + i * &H18)
                                UIntToBytes(tmpbytes.Length, &H24 + i * &H18)
                                UIntToBytes(currFileOffset, &H28 + i * &H18)
                                UIntToBytes(currFileID, &H2C + i * &H18)
                                UIntToBytes(currFileNameOffset, &H30 + i * &H18)
                                UIntToBytes(tmpbytes.Length, &H34 + i * &H18)

                                If tmpbytes.Length Mod &H10 > 0 Then
                                    padding = &H10 - (tmpbytes.Length Mod &H10)
                                Else
                                    padding = 0
                                End If
                                If i = numFiles - 1 Then padding = 0
                                ReDim Preserve bytes(bytes.Length + tmpbytes.Length + padding - 1)

                                InsBytes(tmpbytes, currFileOffset)

                                currFileOffset += tmpbytes.Length
                                If currFileOffset Mod &H10 > 0 Then
                                    padding = &H10 - (currFileOffset Mod &H10)
                                Else
                                    padding = 0
                                End If
                                currFileOffset += padding

                                currFileNameOffset += EncodeFileName(Microsoft.VisualBasic.Right(currFileListEntry, currFileListEntry.Length - (InStr(currFileListEntry, ","))), currFileNameOffset).Length + 1
                            Case &H10100
                                currFileName = currFileListEntry

                                'More nameless entries...

                                currFileInfo = Job.GetExtractedFile(currFileName)
                                currFilePath = currFileInfo.DirectoryName & "\"
                                currFileName = currFileInfo.Name

                                tmpbytes = File_ReadAllBytes(currFilePath & currFileName)
                                currFileSize = tmpbytes.Length

                                If currFileSize Mod &H10 > 0 And i < numFiles - 1 Then
                                    padding = &H10 - (currFileSize Mod &H10)
                                Else
                                    padding = 0
                                End If

                                UIntToBytes(&H2000000, &H20 + i * &HC)
                                UIntToBytes(currFileSize, &H24 + i * &HC)
                                UIntToBytes(currFileOffset, &H28 + i * &HC)

                                ReDim Preserve bytes(bytes.Length + tmpbytes.Length + padding - 1)

                                InsBytes(tmpbytes, currFileOffset)

                                currFileOffset += tmpbytes.Length + padding

                            Case &HE010100

                                currFileName = currFileListEntry.Substring(currFileListEntry.IndexOf(",") + 1)

                                currFileInfo = Job.GetExtractedFile(currFileName)
                                currFilePath = currFileInfo.DirectoryName & "\"
                                currFileName = currFileInfo.Name

                                tmpbytes = File_ReadAllBytes(currFileName)
                                currFileID = Microsoft.VisualBasic.Left(currFileListEntry, InStr(currFileListEntry, ",") - 1)

                                UIntToBytes(&H2000000, &H20 + i * &H14)
                                UIntToBytes(tmpbytes.Length, &H24 + i * &H14)
                                UIntToBytes(currFileOffset, &H28 + i * &H14)
                                UIntToBytes(currFileID, &H2C + i * &H14)
                                UIntToBytes(currFileNameOffset, &H30 + i * &H14)

                                If tmpbytes.Length Mod &H10 > 0 Then
                                    padding = &H10 - (tmpbytes.Length Mod &H10)
                                Else
                                    padding = 0
                                End If
                                If i = numFiles - 1 Then padding = 0
                                ReDim Preserve bytes(bytes.Length + tmpbytes.Length + padding - 1)

                                InsBytes(tmpbytes, currFileOffset)

                                currFileOffset += tmpbytes.Length
                                If currFileOffset Mod &H10 > 0 Then
                                    padding = &H10 - (currFileOffset Mod &H10)
                                Else
                                    padding = 0
                                End If
                                currFileOffset += padding

                                currFileNameOffset += EncodeFileName(Microsoft.VisualBasic.Right(currFileListEntry, currFileListEntry.Length - (InStr(currFileListEntry, ","))), currFileNameOffset).Length + 1
                            Case &H2E010100

                                If frpg Then
                                    currFileName = currFileListEntry.Split(",")(1)
                                    If currFileName.ToUpper.StartsWith("N:\") Then
                                        currFileName = "C:\" & currFileName.Substring("N:\".Length)
                                    End If
                                Else
                                    currFileName = filepath & filename & ".extract\" & Microsoft.VisualBasic.Right(currFileListEntry, currFileListEntry.Length - (InStr(currFileListEntry, ",") + 3))
                                End If

                                tmpbytes = File_ReadAllBytes(currFileName)

                                currFileID = Microsoft.VisualBasic.Left(currFileListEntry, InStr(currFileListEntry, ",") - 1)

                                UIntToBytes(&H2000000, &H20 + i * &H18)
                                UIntToBytes(tmpbytes.Length, &H24 + i * &H18)
                                UIntToBytes(currFileOffset, &H28 + i * &H18)
                                UIntToBytes(currFileID, &H2C + i * &H18)
                                UIntToBytes(currFileNameOffset, &H30 + i * &H18)
                                UIntToBytes(tmpbytes.Length, &H34 + i * &H18)

                                If tmpbytes.Length Mod &H10 > 0 Then
                                    padding = &H10 - (tmpbytes.Length Mod &H10)
                                Else
                                    padding = 0
                                End If
                                If i = numFiles - 1 Then padding = 0
                                ReDim Preserve bytes(bytes.Length + tmpbytes.Length + padding - 1)

                                InsBytes(tmpbytes, currFileOffset)

                                currFileOffset += tmpbytes.Length
                                If currFileOffset Mod &H10 > 0 Then
                                    padding = &H10 - (currFileOffset Mod &H10)
                                Else
                                    padding = 0
                                End If
                                currFileOffset += padding

                                currFileNameOffset += EncodeFileName(Microsoft.VisualBasic.Right(currFileListEntry, currFileListEntry.Length - (InStr(currFileListEntry, ","))), currFileNameOffset).Length + 1
                        End Select
                    Next
#End Region
                Case "TPF"
#Region "Rebuild TPF"
                    'TODO:  Handle m10_9999 (PC) format
                    Dim currFileFlags1
                    Dim currFileFlags2
                    Dim totalFileSize = 0
                    ReDim bytes(&HF)
                    StrToBytes(fileList(0), 0)

                    flags = fileList(1)

                    Dim isOnlyOneTexture = fileList.Length <= 3

                    If flags = &H2010200 Or flags = &H201000 Then
                        ' Demon's Souls
                        'TODO:  Differentiate flag format differences

                        bigEndian = True

                        numFiles = fileList.Length - 2

                        namesEndLoc = &H10 + numFiles * &H20

                        For i = 2 To fileList.Length - 1
                            'Gonna borrow 'currFileListEntry' for a bit here to make this more readable lol
                            currFileListEntry = fileList(i).Substring(currFileListEntry.LastIndexOf(",") + 1)
                            'TODO: REDUCE FILE NAME ENCODING REDUNDANCY
                            namesEndLoc += GetEncoded(currFileListEntry).Length + 1
                        Next

                        UIntToBytes(numFiles, &H8)
                        UIntToBytes(flags, &HC)

                        If namesEndLoc Mod &H10 > 0 Then
                            padding = &H10 - (namesEndLoc Mod &H10)
                        Else
                            padding = 0
                        End If

                        ReDim Preserve bytes(namesEndLoc + padding - 1)
                        currFileOffset = namesEndLoc + padding

                        UIntToBytes(currFileOffset, &H10)

                        currFileNameOffset = &H10 + &H20 * numFiles

                        For i = 0 To numFiles - 1

                            currFileListEntry = fileList(i + 2)

                            currFileName = currFileListEntry.Substring(currFileListEntry.LastIndexOf(",") + 1)

                            currFileInfo = Job.GetExtractedFile(currFileName, isOnlyOneTexture)
                            currFilePath = currFileInfo.DirectoryName & "\"
                            currFileName = currFileInfo.Name

                            tmpbytes = File_ReadAllBytes(currFileName)

                            currFileSize = tmpbytes.Length
                            If currFileSize Mod &H10 > 0 Then
                                padding = &H10 - (currFileSize Mod &H10)
                            Else
                                padding = 0
                            End If

                            currFileFlags1 = Microsoft.VisualBasic.Left(currFileListEntry, InStr(currFileListEntry, ",") - 1)
                            currFileFlags2 = Microsoft.VisualBasic.Right(Microsoft.VisualBasic.Left(currFileListEntry, InStrRev(currFileListEntry, ",") - 1), Microsoft.VisualBasic.Left(currFileListEntry, InStrRev(currFileListEntry, ",") - 1).Length - InStr(Microsoft.VisualBasic.Left(currFileListEntry, InStrRev(currFileListEntry, ",") - 1), ","))

                            UIntToBytes(currFileOffset, &H10 + i * &H20)
                            UIntToBytes(currFileSize, &H14 + i * &H20)
                            UIntToBytes(currFileFlags1, &H18 + i * &H20)
                            UIntToBytes(currFileFlags2, &H1C + i * &H20)
                            UIntToBytes(currFileNameOffset, &H28 + i * &H20)

                            ReDim Preserve bytes(bytes.Length + currFileSize + padding - 1)

                            InsBytes(tmpbytes, currFileOffset)

                            currFileOffset += currFileSize + padding
                            totalFileSize += currFileSize

                            currFileNameOffset += EncodeFileName(Microsoft.VisualBasic.Right(currFileListEntry, currFileListEntry.Length - (InStrRev(currFileListEntry, ","))), currFileNameOffset).Length + 1
                        Next

                        UIntToBytes(totalFileSize, &H4)

                    ElseIf flags = &H20300 Then
                        ' Dark Souls
                        'TODO:  Fix this endian check in particular.

                        bigEndian = False

                        numFiles = fileList.Length - 2

                        namesEndLoc = &H10 + numFiles * &H14

                        For i = 2 To fileList.Length - 1
                            currFileName = fileList(i)
                            currFileName = currFileName.Substring(currFileName.LastIndexOf(","))
                            currFileName = currFileName.Substring(0, currFileName.Length - ".dds".Length)
                            'TODO: REDUCE FILE NAME ENCODING REDUNDANCY
                            namesEndLoc += GetEncoded(currFileName).Length + 1
                        Next

                        UIntToBytes(numFiles, &H8)
                        UIntToBytes(flags, &HC)

                        If namesEndLoc Mod &H10 > 0 Then
                            padding = &H10 - (namesEndLoc Mod &H10)
                        Else
                            padding = 0
                        End If

                        ReDim Preserve bytes(namesEndLoc + padding - 1)
                        currFileOffset = namesEndLoc + padding

                        currFileNameOffset = &H10 + &H14 * numFiles

                        For i = 0 To numFiles - 1

                            currFileListEntry = fileList(i + 2)

                            currFileName = currFileListEntry.Substring(currFileListEntry.LastIndexOf(",") + 1)

                            currFileInfo = Job.GetExtractedFile(currFileName, isOnlyOneTexture)
                            currFilePath = currFileInfo.DirectoryName & "\"
                            currFileName = currFileInfo.Name

                            tmpbytes = File_ReadAllBytes(currFilePath & currFileName)

                            currFileSize = tmpbytes.Length
                            If currFileSize Mod &H10 > 0 Then
                                padding = &H10 - (currFileSize Mod &H10)
                            Else
                                padding = 0
                            End If

                            Dim words() As String = currFileListEntry.Split(",")
                            currFileFlags1 = words(0)
                            currFileFlags2 = words(1)

                            UIntToBytes(currFileOffset, &H10 + i * &H14)
                            UIntToBytes(currFileSize, &H14 + i * &H14)
                            UIntToBytes(currFileFlags1, &H18 + i * &H14)
                            UIntToBytes(currFileNameOffset, &H1C + i * &H14)
                            UIntToBytes(currFileFlags2, &H20 + i * &H14)

                            ReDim Preserve bytes(bytes.Length + currFileSize + padding - 1)

                            InsBytes(tmpbytes, currFileOffset)

                            currFileOffset += currFileSize + padding
                            totalFileSize += currFileSize

                            currFileName = currFileName.Substring(0, currFileName.Length - ".dds".Length)
                            currFileNameOffset += EncodeFileName(currFileName, currFileNameOffset).Length + 1
                        Next

                        UIntToBytes(totalFileSize, &H4)
                    End If
#End Region
                Case "EDGE"
#Region "Rebuild EDGE"
                    Dim chunkBytes(&H10000) As Byte
                    Dim cmpChunkBytes() As Byte
                    Dim zipBytes() As Byte = {}

                    currFileName = filepath + fileList(1)
                    tmpbytes = File_ReadAllBytes(currFileName)

                    currFileSize = tmpbytes.Length

                    ReDim bytes(&H83)

                    Dim fileRemaining As Integer = tmpbytes.Length
                    Dim fileDone As Integer = 0
                    Dim fileToDo As Integer = 0
                    Dim chunks = 0
                    Dim lastchunk = 0

                    While fileRemaining > 0
                        chunks += 1

                        If fileRemaining > &H10000 Then
                            fileToDo = &H10000
                        Else
                            fileToDo = fileRemaining
                        End If

                        Array.Copy(tmpbytes, fileDone, chunkBytes, 0, fileToDo)
                        cmpChunkBytes = Compress(chunkBytes)

                        lastchunk = zipBytes.Length

                        If lastchunk Mod &H10 > 0 Then
                            padding = &H10 - (lastchunk Mod &H10)
                        Else
                            padding = 0
                        End If
                        lastchunk += padding

                        ReDim Preserve zipBytes(lastchunk + cmpChunkBytes.Length)
                        Array.Copy(cmpChunkBytes, 0, zipBytes, lastchunk, cmpChunkBytes.Length)

                        fileDone += fileToDo
                        fileRemaining -= fileToDo

                        ReDim Preserve bytes(bytes.Length + &H10)

                        UIntToBytes(lastchunk, &H64 + chunks * &H10)
                        UIntToBytes(cmpChunkBytes.Length, &H68 + chunks * &H10)
                        UIntToBytes(&H1, &H6C + chunks * &H10)

                    End While
                    ReDim Preserve bytes(bytes.Length + zipBytes.Length)

                    StrToBytes("DCX", &H0)
                    UIntToBytes(&H10000, &H4)
                    UIntToBytes(&H18, &H8)
                    UIntToBytes(&H24, &HC)
                    UIntToBytes(&H24, &H10)
                    UIntToBytes(&H50 + chunks * &H10, &H14)
                    StrToBytes("DCS", &H18)
                    UIntToBytes(currFileSize, &H1C)
                    UIntToBytes(bytes.Length - (&H70 + chunks * &H10), &H20)
                    StrToBytes("DCP", &H24)
                    StrToBytes("EDGE", &H28)
                    UIntToBytes(&H20, &H2C)
                    UIntToBytes(&H9000000, &H30)
                    UIntToBytes(&H10000, &H34)

                    UIntToBytes(&H100100, &H40)
                    StrToBytes("DCA", &H44)
                    UIntToBytes(chunks * &H10 + &H2C, &H48)
                    StrToBytes("EgdT", &H4C)
                    UIntToBytes(&H10100, &H50)
                    UIntToBytes(&H24, &H54)
                    UIntToBytes(&H10, &H58)
                    UIntToBytes(&H10000, &H5C)
                    UIntToBytes(tmpbytes.Length Mod &H10000, &H60)
                    UIntToBytes(&H24 + chunks * &H10, &H64)
                    UIntToBytes(chunks, &H68)
                    UIntToBytes(&H100000, &H6C)

                    Array.Copy(zipBytes, 0, bytes, &H70 + chunks * &H10, zipBytes.Length)
#End Region
                Case "DFLT"
#Region "Rebuild DFLT"
                    Dim cmpBytes() As Byte
                    Dim zipBytes() As Byte = {}

                    currFileName = filepath + fileList(1)
                    tmpbytes = File_ReadAllBytes(currFileName)

                    currFileSize = tmpbytes.Length

                    ReDim bytes(&H4E)

                    cmpBytes = Compress(tmpbytes)

                    ReDim Preserve bytes(bytes.Length + cmpBytes.Length)

                    StrToBytes("DCX", &H0)
                    UIntToBytes(&H10000, &H4)
                    UIntToBytes(&H18, &H8)
                    UIntToBytes(&H24, &HC)
                    UIntToBytes(&H24, &H10)
                    UIntToBytes(&H2C, &H14)
                    StrToBytes("DCS", &H18)
                    UIntToBytes(currFileSize, &H1C)
                    UIntToBytes(cmpBytes.Length + 4, &H20)
                    StrToBytes("DCP", &H24)
                    StrToBytes("DFLT", &H28)
                    UIntToBytes(&H20, &H2C)
                    UIntToBytes(&H9000000, &H30)

                    UIntToBytes(&H10100, &H40)
                    StrToBytes("DCA", &H44)
                    UIntToBytes(&H8, &H48)
                    UIntToBytes(&H78DA0000, &H4C)

                    Array.Copy(cmpBytes, 0, bytes, &H4E, cmpBytes.Length)
#End Region
            End Select
            File.WriteAllBytes(filepath & filename, bytes)

            If My.Settings.EnableVerboseOutput Then
                PrintLn($"Rebuilt '{filename}'.")
            End If

#If Not NO_ERR_HANDLING Then
        Catch ex As Exception
            PrintLnErr($"{vbCrLf}Encountered an error while rebuilding '{Job.CurrentFile.Name}':{vbCrLf}{vbTab}{ex.Message}{vbCrLf}")
        End Try
#End If

    End Sub

    Public Function Decompress(ByVal cmpBytes() As Byte) As Byte()
        Dim result() As Byte
        Using sourceFile As MemoryStream = New MemoryStream(cmpBytes)
            Using destFile As MemoryStream = New MemoryStream()
                Using compStream As New DeflateStream(sourceFile, CompressionMode.Decompress)
                    Dim myByte As Integer = compStream.ReadByte()
                    While myByte <> -1
                        destFile.WriteByte(CType(myByte, Byte))
                        myByte = compStream.ReadByte()
                    End While
                End Using
                result = destFile.ToArray()
            End Using
        End Using
        Return result
    End Function

    Public Function Compress(ByVal cmpBytes() As Byte) As Byte()
        Dim result() As Byte
        Using ms As New MemoryStream()
            Using zipStream As Stream = New DeflateStream(ms, CompressionMode.Compress, True)
                zipStream.Write(cmpBytes, 0, cmpBytes.Length)
            End Using
            ms.Position = 0
            ReDim result(ms.Length - 1)
            ms.Read(result, 0, ms.Length)
        End Using
        Return result
    End Function

    Private Sub txt_Drop(sender As Object, e As System.Windows.Forms.DragEventArgs) Handles txtBNDfile.DragDrop
        Dim file() As String = e.Data.GetData(DataFormats.FileDrop)
        AddFiles(file)
    End Sub

    Private Sub txt_DragEnter(sender As Object, e As System.Windows.Forms.DragEventArgs) Handles txtBNDfile.DragEnter
        e.Effect = DragDropEffects.Copy
    End Sub

    Public Shared Function IsConfigPathValid(path As String) As Boolean
        Return (Not String.IsNullOrWhiteSpace(path.Trim())) And (Directory.Exists(path) OrElse File.Exists(path))
    End Function

    Private Sub Des_BNDBuild_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        updateUITimer.Interval = 200
        updateUITimer.Start()

        WorkEndTriggerThread = New Thread(AddressOf WorkDoneTriggerWaitLoop) With {.IsBackground = True}
        WorkEndTriggerThread.Start()

        ConfigWindow = New Config()

        If Not IsConfigPathValid(My.Settings.DarkSoulsDataPath) Then
            Dim choice = MessageBox.Show("You must specify your Dark Souls DATA directory (where DARKSOULS.exe is) in order to continue. " &
                            "This is necessary for the relative folder structure calculations to work properly. " &
                            "Click OK to open a folder select dialog or Cancel to quit.",
                            "Specify Dark Souls Game Folder", MessageBoxButtons.OKCancel)

            If choice = DialogResult.OK Then

                Dim openDlg As New FolderSelect.FolderSelectDialog() With {.Title = "Select your directory"}

                openDlg.InitialDirectory = My.Settings.FileBrowserStartingPath

                If openDlg.ShowDialog() Then
                    My.Settings.DarkSoulsDataPath = openDlg.FileName
                    My.Settings.RemoteBNDBackupPath = openDlg.FileName.Trim("\") & "\DesBNDBuild-BNDBackups"
                    My.Settings.RemoteBNDTablePath = openDlg.FileName.Trim("\") & "\DesBNDBuild-BNDInfoTables"
                    My.Settings.UseRemoteBNDBackupPath = True
                    My.Settings.UseRemoteBNDTablePath = True
                    My.Settings.Save()

                    Dim choice2 = MessageBox.Show("It is recommended that you select a unified data directory to keep extracted files " &
                            "in order to greatly reduce the amount of subfolders you need to click through." & vbCrLf & vbCrLf &
                            "Would you like to specify such a directory and enable extracting/rebuilding to/from that directory?" & vbCrLf & vbCrLf &
                            "Note: It is recommended that you put this directory in a completely separate location from your " &
                            "Dark Souls DATA directory, to avoid confusion.",
                            "Specify Custom Data Directory?", MessageBoxButtons.YesNo, MessageBoxIcon.Question)

                    If choice2 = DialogResult.Yes Then

                        openDlg = New FolderSelect.FolderSelectDialog() With {.Title = "Select your custom data directory"}

                        Dim reselectDirectory = False

                        Do

                            If openDlg.ShowDialog() Then
                                If openDlg.FileName = My.Settings.DarkSoulsDataPath Then
                                    reselectDirectory = Not MessageBox.Show("Are you REALLY sure you want to have \DATA\data\ and \DATA\source\ be where your " &
                                                    "extracted files are? It could get confusing...",
                                                    "Really...?", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                                Else
                                    reselectDirectory = False
                                End If
                            Else
                                My.Settings.FileBrowserStartingPath = My.Settings.DarkSoulsDataPath
                                Return
                            End If

                        Loop While reselectDirectory

                        My.Settings.CustomDataRootPath = openDlg.FileName
                        My.Settings.UseCustomDataRootPath = True
                        My.Settings.FileBrowserStartingPath = My.Settings.DarkSoulsDataPath
                    End If

                Else
                    End
                End If
            Else
                End
            End If
        End If

    End Sub

    Private Sub ConfigWindow_FormClosing(sender As Object, e As FormClosingEventArgs)
        btnOpenConfigWindow.Enabled = True
        RemoveHandler ConfigWindow.FormClosing, AddressOf ConfigWindow_FormClosing
    End Sub

    Private Sub WorkDoneTriggerWaitLoop()
        While True
            'Wait for something to trigger work start:
            WorkBeginTrigger.WaitOne()

            'Do work start shit:
            SetGUIDisabled(True)
            Dim timeStart = Now

            'Trigger the callback to whatever started the work:
            WorkBeginTrigger.Set()
            'Wait for it to trigger the work end:
            WorkEndTrigger.WaitOne()

            'Do work end shit:
            SetGUIDisabled(False)

            'Trigger the callback to whatever ended the work.
            WorkEndTrigger.Set()

            Dim elapsedTime = Now.Subtract(timeStart)

            PrintLn($" >> Finished in {elapsedTime}. <<")

            CURRENT_JOB_TYPE = BNDJobType.None

            'Rinse and repeat
        End While

    End Sub

    Private Sub updateUI() Handles updateUITimer.Tick

        SyncLock outputLock
            While outputList.Count > 0
                txtInfo.AppendText(outputList.Dequeue())
                txtInfo.ScrollToCaret() 'How DARE you forget to include this, Wulf?!
            End While
        End SyncLock

        'Commented out for now because user can toggle verbosity as well as manually clear the output box:

        'If txtInfo.Lines.Count > 10000 Then
        '    txtInfo.Lines = txtInfo.Lines.Skip(9000).ToArray
        '    txtInfo.ScrollToCaret()
        'End If
    End Sub

    Private Function FilterFileList(files As IEnumerable(Of String), NewFilesOnly As Boolean) As String

        files = GetBndFilesCleaned(files)

        If files Is Nothing Then
            Return Nothing
        End If

        Dim upper = ""

        Dim curFilesCaseless As IEnumerable(Of String) = Nothing

        If NewFilesOnly Then
            curFilesCaseless = txtBNDfile.Lines.
                Select(Function(x) x.Trim().ToUpper()).
                Where(Function(y) Not String.IsNullOrWhiteSpace(y))
        Else
            curFilesCaseless = New List(Of String)()
        End If

        Return String.Join(vbLf, files.Where(
            Function(f)
                upper = f.ToUpper()
                Return VALID_BND_EXTENSIONS.Any(Function(x) upper.EndsWith(x.ToUpper())) AndAlso
                        Not curFilesCaseless.Contains(upper.Trim())
                'If newFilesOnly is false then curFilesCaseless will be empty, 
                'thus allowing all files to be added
            End Function))

    End Function

    Private Sub AddFiles(files As IEnumerable(Of String))

        Dim filesToAdd = FilterFileList(files, NewFilesOnly:=True)

        If filesToAdd Is Nothing Then
            MessageBox.Show("No valid files; nothing added to file list.", "Notice",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim oldSelLen = txtBNDfile.SelectionLength
        Dim oldSelStart = txtBNDfile.SelectionStart

        Dim curFilesCaseless As IEnumerable(Of String) = Nothing

        If txtBNDfile.TextLength > 0 Then
            txtBNDfile.SelectionLength = 0
            txtBNDfile.SelectionStart = txtBNDfile.TextLength - 1

            Dim lastLineText = txtBNDfile.Lines(txtBNDfile.Lines.Length - 1)

            If Not String.IsNullOrEmpty(lastLineText.Trim()) Then
                'If last line actually has non-space stuff, append a new line to the end.
                txtBNDfile.AppendText(vbCrLf)
            ElseIf String.IsNullOrWhiteSpace(lastLineText) Then
                'If last line is just blank space, delete it all.
                txtBNDfile.SelectionStart = txtBNDfile.GetFirstCharIndexOfCurrentLine()
                txtBNDfile.SelectedText = ""
            End If
        End If


        txtBNDfile.AppendText(filesToAdd)

        txtBNDfile.SelectionStart = oldSelStart
        txtBNDfile.SelectionLength = oldSelLen

    End Sub

    Private Sub btnAddFiles_Click(sender As Object, e As EventArgs) Handles btnAddFiles.Click
        Dim openDlg As New OpenFileDialog() With {
            .CheckFileExists = True,
            .CheckPathExists = True,
            .DefaultExt = "*BND",
            .Filter = "DeS/DaS DCX/BND File|*BND;*MOWB;*DCX;*TPF;*BHD5;*BHD",
            .Multiselect = True,
            .ShowReadOnly = False,
            .SupportMultiDottedExtensions = True,
            .Title = "Open your BND/DCX file(s)."
        }

        openDlg.InitialDirectory = My.Settings.FileBrowserStartingPath

        If openDlg.ShowDialog() = Windows.Forms.DialogResult.OK Then
            AddFiles(openDlg.FileNames)
        End If
    End Sub

    Private Sub btnAddDirectory_Click(sender As Object, e As EventArgs) Handles btnAddDirectory.Click
        Dim openDlg As New FolderSelect.FolderSelectDialog() With {.Title = "Select your directory"}

        openDlg.InitialDirectory = My.Settings.FileBrowserStartingPath

        If openDlg.ShowDialog() Then
            If Directory.GetDirectories(openDlg.FileName).Length > 0 Then
                Dim dgres = MessageBox.Show("Subdirectories were found inside of the selected directory." & vbCrLf &
                                            "Would you like to recursively iterate through ALL subdirectories " & vbCrLf &
                                            "inside of the selected directory?",
                                            "Recursive Scan?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question)

                If dgres = DialogResult.Yes Then
                    AddFiles(Directory.GetFiles(openDlg.FileName, "*.*", SearchOption.AllDirectories))
                ElseIf dgres = DialogResult.No Then
                    AddFiles(Directory.GetFiles(openDlg.FileName, "*.*", SearchOption.TopDirectoryOnly))
                End If
            Else
                AddFiles(Directory.GetFiles(openDlg.FileName, "*.*", SearchOption.TopDirectoryOnly))
            End If
        End If
    End Sub

    Private Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        DO_CANCEL()
    End Sub

    Private Function DO_CANCEL() As Boolean
        Dim result As Boolean = False
        'Pause extraction
        BNDJobPauseHandle.Reset()

        Dim choice = MessageBox.Show("Would you like to cancel the current operation?", "Cancel?", MessageBoxButtons.YesNo, MessageBoxIcon.Question)

        If choice = DialogResult.Yes Then
            BNDJobTokenSourceCancel.Cancel()
            result = True
        End If

        BNDJobPauseHandle.Set()
        Return result
    End Function

    Private Sub btnOpenConfigWindow_Click(sender As Object, e As EventArgs) Handles btnOpenConfigWindow.Click
        btnOpenConfigWindow.Enabled = False
        ConfigWindow = New Config()
        AddHandler ConfigWindow.FormClosing, AddressOf ConfigWindow_FormClosing
        ConfigWindow.Show()

        If If(BndThread?.IsAlive, False) Then
            ConfigWindow.propertyGridSettings.Enabled = False
            ConfigWindow.btnSaveConfig.Enabled = False
            ConfigWindow.btnReloadConfig.Enabled = False
        End If

    End Sub

    Private Sub btnClearOutput_Click(sender As Object, e As EventArgs) Handles btnClearOutput.Click
        txtInfo.ResetText()
    End Sub

    Private Sub btnSaveList_Click(sender As Object, e As EventArgs) Handles btnSaveList.Click
        Dim curFileList = FilterFileList(txtBNDfile.Lines, NewFilesOnly:=False)

        If curFileList Is Nothing Then
            Return
        ElseIf String.IsNullOrWhiteSpace(curFileList.Trim()) Then
            MessageBox.Show("File list contains no valid BND file paths; operation cancelled.", "Notice",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim openDlg As New SaveFileDialog() With {
            .CheckFileExists = False,
            .CheckPathExists = False,
            .DefaultExt = "wulfbndl",
            .Filter = "Wulf BND File List (*.wulfbndl)|*.wulfbndl|All files (*.*)|*.*",
            .SupportMultiDottedExtensions = True,
            .Title = "Save file list..."
        }

        openDlg.InitialDirectory = My.Settings.FileBrowserStartingPath

        If openDlg.ShowDialog() = Windows.Forms.DialogResult.OK Then
            Dim dir = New FileInfo(openDlg.FileName).DirectoryName

            If Not Directory.Exists(dir) Then
                Directory.CreateDirectory(dir)
            End If

            File.WriteAllText(openDlg.FileName, txtBNDfile.Text)
        End If
    End Sub

    Private Sub btnLoadList_Click(sender As Object, e As EventArgs) Handles btnLoadList.Click
        Dim openDlg As New OpenFileDialog() With {
            .CheckFileExists = True,
            .CheckPathExists = True,
            .DefaultExt = "wulfbndl",
            .Filter = "Wulf BND File List (*.wulfbndl)|*.wulfbndl|All files (*.*)|*.*",
            .Multiselect = True,
            .ShowReadOnly = False,
            .SupportMultiDottedExtensions = True,
            .Title = "Select one or more BND file lists to add..."
        }

        openDlg.InitialDirectory = My.Settings.FileBrowserStartingPath

        If openDlg.ShowDialog() = Windows.Forms.DialogResult.OK Then
            AddFiles(File.ReadLines(openDlg.FileName))
        End If
    End Sub

    Private Function File_ReadAllLines(filePath As String) As String()
        If Not File.Exists(filePath) Then
            Throw New Exception($"File '{filePath}' does not exist.")
            Return New String() {}
        End If

        Return File.ReadAllLines(filePath)
    End Function

    Private Function File_ReadAllBytes(filePath As String) As Byte()
        If Not File.Exists(filePath) Then
            Throw New Exception($"File '{filePath}' does not exist.")
            Return New Byte() {}
        End If

        Return File.ReadAllBytes(filePath)
    End Function

    Private Sub btnRestoreBackups_Click(sender As Object, e As EventArgs) Handles btnRestoreBackups.Click
        START_JOB(BNDJobType.RestoreBackups)
    End Sub

    Private Sub MainFrm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If CURRENT_JOB_TYPE <> BNDJobType.None Then
            e.Cancel = Not DO_CANCEL()
        End If
    End Sub
End Class