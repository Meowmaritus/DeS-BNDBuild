<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Config
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
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
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.btnReloadConfig = New System.Windows.Forms.Button()
        Me.btnSaveConfig = New System.Windows.Forms.Button()
        Me.propertyGridSettings = New System.Windows.Forms.PropertyGrid()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.SuspendLayout()
        '
        'btnReloadConfig
        '
        Me.btnReloadConfig.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnReloadConfig.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.btnReloadConfig.Location = New System.Drawing.Point(286, 2)
        Me.btnReloadConfig.Margin = New System.Windows.Forms.Padding(0)
        Me.btnReloadConfig.Name = "btnReloadConfig"
        Me.btnReloadConfig.Size = New System.Drawing.Size(48, 23)
        Me.btnReloadConfig.TabIndex = 52
        Me.btnReloadConfig.Text = "Reload"
        Me.btnReloadConfig.UseVisualStyleBackColor = True
        '
        'btnSaveConfig
        '
        Me.btnSaveConfig.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnSaveConfig.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.btnSaveConfig.Location = New System.Drawing.Point(334, 2)
        Me.btnSaveConfig.Margin = New System.Windows.Forms.Padding(0)
        Me.btnSaveConfig.Name = "btnSaveConfig"
        Me.btnSaveConfig.Size = New System.Drawing.Size(48, 23)
        Me.btnSaveConfig.TabIndex = 51
        Me.btnSaveConfig.Text = "Save"
        Me.btnSaveConfig.UseVisualStyleBackColor = True
        '
        'propertyGridSettings
        '
        Me.propertyGridSettings.Dock = System.Windows.Forms.DockStyle.Fill
        Me.propertyGridSettings.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.propertyGridSettings.LineColor = System.Drawing.SystemColors.ControlDark
        Me.propertyGridSettings.Location = New System.Drawing.Point(0, 0)
        Me.propertyGridSettings.Name = "propertyGridSettings"
        Me.propertyGridSettings.Size = New System.Drawing.Size(384, 427)
        Me.propertyGridSettings.TabIndex = 50
        '
        'Label1
        '
        Me.Label1.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(91, 7)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(193, 13)
        Me.Label1.TabIndex = 53
        Me.Label1.Text = "Note: This does not save automatically."
        '
        'Config
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(384, 427)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.btnReloadConfig)
        Me.Controls.Add(Me.btnSaveConfig)
        Me.Controls.Add(Me.propertyGridSettings)
        Me.DoubleBuffered = True
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow
        Me.Name = "Config"
        Me.Text = "Config"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents btnReloadConfig As Button
    Friend WithEvents btnSaveConfig As Button
    Friend WithEvents propertyGridSettings As PropertyGrid
    Friend WithEvents Label1 As Label
End Class
