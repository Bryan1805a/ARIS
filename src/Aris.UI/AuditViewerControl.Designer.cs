namespace Aris.UI;

partial class AuditViewerControl
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Component Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        dgvAuditLogs = new DataGridView();
        ((System.ComponentModel.ISupportInitialize)dgvAuditLogs).BeginInit();
        SuspendLayout();
        //
        // dgvAuditLogs
        //
        dgvAuditLogs.AllowUserToAddRows = false;
        dgvAuditLogs.AllowUserToDeleteRows = false;
        dgvAuditLogs.AllowUserToResizeRows = false;
        dgvAuditLogs.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
        dgvAuditLogs.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvAuditLogs.Dock = DockStyle.Fill;
        dgvAuditLogs.Location = new Point(0, 0);
        dgvAuditLogs.MultiSelect = false;
        dgvAuditLogs.Name = "dgvAuditLogs";
        dgvAuditLogs.ReadOnly = true;
        dgvAuditLogs.RowHeadersVisible = false;
        dgvAuditLogs.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvAuditLogs.Size = new Size(475, 310);
        dgvAuditLogs.TabIndex = 0;
        //
        // AuditViewerControl
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        Controls.Add(dgvAuditLogs);
        Name = "AuditViewerControl";
        Size = new Size(475, 310);
        ((System.ComponentModel.ISupportInitialize)dgvAuditLogs).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private DataGridView dgvAuditLogs;
}
