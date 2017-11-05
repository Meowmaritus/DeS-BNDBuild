<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class MainFrm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(MainFrm))
        Me.txtBNDfile = New System.Windows.Forms.RichTextBox()
        Me.lblGAFile = New System.Windows.Forms.Label()
        Me.btnRebuild = New System.Windows.Forms.Button()
        Me.btnExtract = New System.Windows.Forms.Button()
        Me.txtInfo = New System.Windows.Forms.TextBox()
        Me.lblVersion = New System.Windows.Forms.Label()
        Me.btnAddDirectory = New System.Windows.Forms.Button()
        Me.btnAddFiles = New System.Windows.Forms.Button()
        Me.btnCancel = New System.Windows.Forms.Button()
        Me.btnOpenConfigWindow = New System.Windows.Forms.Button()
        Me.btnClearOutput = New System.Windows.Forms.Button()
        Me.btnSaveList = New System.Windows.Forms.Button()
        Me.btnLoadList = New System.Windows.Forms.Button()
        Me.btnRestoreBackups = New System.Windows.Forms.Button()
        Me.progressBar = New System.Windows.Forms.ProgressBar()
        Me.SuspendLayout()
        '
        'txtBNDfile
        '
        Me.txtBNDfile.AllowDrop = True
        Me.txtBNDfile.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtBNDfile.Font = New System.Drawing.Font("Segoe UI", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtBNDfile.Location = New System.Drawing.Point(9, 25)
        Me.txtBNDfile.Name = "txtBNDfile"
        Me.txtBNDfile.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.ForcedBoth
        Me.txtBNDfile.ShowSelectionMargin = True
        Me.txtBNDfile.Size = New System.Drawing.Size(686, 215)
        Me.txtBNDfile.TabIndex = 26
        Me.txtBNDfile.Text = ""
        Me.txtBNDfile.WordWrap = False
        '
        'lblGAFile
        '
        Me.lblGAFile.AutoSize = True
        Me.lblGAFile.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblGAFile.Location = New System.Drawing.Point(9, 5)
        Me.lblGAFile.Name = "lblGAFile"
        Me.lblGAFile.Size = New System.Drawing.Size(68, 17)
        Me.lblGAFile.TabIndex = 28
        Me.lblGAFile.Text = "BND File(s):"
        Me.lblGAFile.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.lblGAFile.UseCompatibleTextRendering = True
        '
        'btnRebuild
        '
        Me.btnRebuild.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnRebuild.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.btnRebuild.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnRebuild.Location = New System.Drawing.Point(645, 246)
        Me.btnRebuild.Margin = New System.Windows.Forms.Padding(0)
        Me.btnRebuild.Name = "btnRebuild"
        Me.btnRebuild.Size = New System.Drawing.Size(50, 23)
        Me.btnRebuild.TabIndex = 30
        Me.btnRebuild.Text = "Rebuild"
        Me.btnRebuild.UseVisualStyleBackColor = True
        '
        'btnExtract
        '
        Me.btnExtract.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnExtract.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.btnExtract.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnExtract.Location = New System.Drawing.Point(595, 246)
        Me.btnExtract.Margin = New System.Windows.Forms.Padding(0)
        Me.btnExtract.Name = "btnExtract"
        Me.btnExtract.Size = New System.Drawing.Size(50, 23)
        Me.btnExtract.TabIndex = 29
        Me.btnExtract.Text = "Extract"
        Me.btnExtract.UseVisualStyleBackColor = True
        '
        'txtInfo
        '
        Me.txtInfo.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtInfo.Location = New System.Drawing.Point(9, 275)
        Me.txtInfo.Multiline = True
        Me.txtInfo.Name = "txtInfo"
        Me.txtInfo.ReadOnly = True
        Me.txtInfo.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.txtInfo.Size = New System.Drawing.Size(686, 135)
        Me.txtInfo.TabIndex = 31
        Me.txtInfo.Text = resources.GetString("txtInfo.Text")
        '
        'lblVersion
        '
        Me.lblVersion.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblVersion.Location = New System.Drawing.Point(8, 419)
        Me.lblVersion.Margin = New System.Windows.Forms.Padding(0)
        Me.lblVersion.Name = "lblVersion"
        Me.lblVersion.Size = New System.Drawing.Size(100, 13)
        Me.lblVersion.TabIndex = 42
        Me.lblVersion.Text = "2017-11-4-25"
        Me.lblVersion.TextAlign = System.Drawing.ContentAlignment.BottomLeft
        Me.lblVersion.UseCompatibleTextRendering = True
        '
        'btnAddDirectory
        '
        Me.btnAddDirectory.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.btnAddDirectory.Location = New System.Drawing.Point(8, 246)
        Me.btnAddDirectory.Name = "btnAddDirectory"
        Me.btnAddDirectory.Size = New System.Drawing.Size(119, 23)
        Me.btnAddDirectory.TabIndex = 49
        Me.btnAddDirectory.Text = "Add Directory to List..."
        Me.btnAddDirectory.UseVisualStyleBackColor = True
        '
        'btnAddFiles
        '
        Me.btnAddFiles.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.btnAddFiles.Location = New System.Drawing.Point(131, 246)
        Me.btnAddFiles.Name = "btnAddFiles"
        Me.btnAddFiles.Size = New System.Drawing.Size(131, 23)
        Me.btnAddFiles.TabIndex = 50
        Me.btnAddFiles.Text = "Add BND File(s) to List..."
        Me.btnAddFiles.UseVisualStyleBackColor = True
        '
        'btnCancel
        '
        Me.btnCancel.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnCancel.Enabled = False
        Me.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.btnCancel.Font = New System.Drawing.Font("Segoe UI", 6.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnCancel.Location = New System.Drawing.Point(448, 248)
        Me.btnCancel.Margin = New System.Windows.Forms.Padding(0)
        Me.btnCancel.Name = "btnCancel"
        Me.btnCancel.Size = New System.Drawing.Size(36, 19)
        Me.btnCancel.TabIndex = 51
        Me.btnCancel.Text = "Cancel"
        Me.btnCancel.UseVisualStyleBackColor = True
        '
        'btnOpenConfigWindow
        '
        Me.btnOpenConfigWindow.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnOpenConfigWindow.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.btnOpenConfigWindow.Location = New System.Drawing.Point(560, 413)
        Me.btnOpenConfigWindow.Margin = New System.Windows.Forms.Padding(0)
        Me.btnOpenConfigWindow.Name = "btnOpenConfigWindow"
        Me.btnOpenConfigWindow.Size = New System.Drawing.Size(135, 23)
        Me.btnOpenConfigWindow.TabIndex = 52
        Me.btnOpenConfigWindow.Text = "Open Config Window"
        Me.btnOpenConfigWindow.UseVisualStyleBackColor = True
        '
        'btnClearOutput
        '
        Me.btnClearOutput.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnClearOutput.Location = New System.Drawing.Point(482, 413)
        Me.btnClearOutput.Name = "btnClearOutput"
        Me.btnClearOutput.Size = New System.Drawing.Size(75, 23)
        Me.btnClearOutput.TabIndex = 53
        Me.btnClearOutput.Text = "Clear Output"
        Me.btnClearOutput.UseVisualStyleBackColor = True
        '
        'btnSaveList
        '
        Me.btnSaveList.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.btnSaveList.Location = New System.Drawing.Point(266, 246)
        Me.btnSaveList.Name = "btnSaveList"
        Me.btnSaveList.Size = New System.Drawing.Size(69, 23)
        Me.btnSaveList.TabIndex = 54
        Me.btnSaveList.Text = "Save List..."
        Me.btnSaveList.UseVisualStyleBackColor = True
        '
        'btnLoadList
        '
        Me.btnLoadList.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.btnLoadList.Location = New System.Drawing.Point(339, 246)
        Me.btnLoadList.Name = "btnLoadList"
        Me.btnLoadList.Size = New System.Drawing.Size(62, 23)
        Me.btnLoadList.TabIndex = 55
        Me.btnLoadList.Text = "Add List..."
        Me.btnLoadList.UseVisualStyleBackColor = True
        '
        'btnRestoreBackups
        '
        Me.btnRestoreBackups.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnRestoreBackups.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnRestoreBackups.Location = New System.Drawing.Point(486, 246)
        Me.btnRestoreBackups.Name = "btnRestoreBackups"
        Me.btnRestoreBackups.Size = New System.Drawing.Size(109, 23)
        Me.btnRestoreBackups.TabIndex = 56
        Me.btnRestoreBackups.Text = "Restore Backups"
        Me.btnRestoreBackups.UseVisualStyleBackColor = True
        '
        'progressBar
        '
        Me.progressBar.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.progressBar.Enabled = False
        Me.progressBar.Location = New System.Drawing.Point(487, 246)
        Me.progressBar.MarqueeAnimationSpeed = 16
        Me.progressBar.Name = "progressBar"
        Me.progressBar.Size = New System.Drawing.Size(207, 23)
        Me.progressBar.Step = 1
        Me.progressBar.TabIndex = 57
        Me.progressBar.Visible = False
        '
        'MainFrm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(704, 441)
        Me.Controls.Add(Me.btnRestoreBackups)
        Me.Controls.Add(Me.btnLoadList)
        Me.Controls.Add(Me.btnSaveList)
        Me.Controls.Add(Me.btnClearOutput)
        Me.Controls.Add(Me.btnOpenConfigWindow)
        Me.Controls.Add(Me.btnCancel)
        Me.Controls.Add(Me.btnAddFiles)
        Me.Controls.Add(Me.btnAddDirectory)
        Me.Controls.Add(Me.btnExtract)
        Me.Controls.Add(Me.btnRebuild)
        Me.Controls.Add(Me.lblVersion)
        Me.Controls.Add(Me.txtInfo)
        Me.Controls.Add(Me.txtBNDfile)
        Me.Controls.Add(Me.lblGAFile)
        Me.Controls.Add(Me.progressBar)
        Me.DoubleBuffered = True
        Me.MinimumSize = New System.Drawing.Size(720, 360)
        Me.Name = "MainFrm"
        Me.Text = "Wulf's BND Rebuilder 2.0"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents txtBNDfile As System.Windows.Forms.RichTextBox
    Friend WithEvents lblGAFile As System.Windows.Forms.Label
    Friend WithEvents btnRebuild As System.Windows.Forms.Button
    Friend WithEvents btnExtract As System.Windows.Forms.Button
    Friend WithEvents txtInfo As System.Windows.Forms.TextBox
    Friend WithEvents lblVersion As System.Windows.Forms.Label
    Friend WithEvents btnAddDirectory As Button
    Friend WithEvents btnAddFiles As Button
    Friend WithEvents btnCancel As Button
    Friend WithEvents btnOpenConfigWindow As Button
    Friend WithEvents btnClearOutput As Button
    Friend WithEvents btnSaveList As Button
    Friend WithEvents btnLoadList As Button
    Friend WithEvents btnRestoreBackups As Button
    Friend WithEvents progressBar As ProgressBar
End Class
