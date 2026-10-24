namespace Aris.UI;

partial class ReportsControl
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
        lblReportsTitle = new Label();
        cboReportType = new ComboBox();
        btnGenerateReport = new Button();
        SuspendLayout();
        // 
        // lblReportsTitle
        // 
        lblReportsTitle.AutoSize = true;
        lblReportsTitle.Font = new Font("Segoe UI", 20.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
        lblReportsTitle.ForeColor = Color.DarkSlateGray;
        lblReportsTitle.Location = new Point(25, 25);
        lblReportsTitle.Name = "lblReportsTitle";
        lblReportsTitle.Size = new Size(118, 37);
        lblReportsTitle.TabIndex = 0;
        lblReportsTitle.Text = "Reports";
        // 
        // cboReportType
        // 
        cboReportType.DropDownStyle = ComboBoxStyle.DropDownList;
        cboReportType.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        cboReportType.FormattingEnabled = true;
        cboReportType.Items.AddRange(new object[] { "Residence Statistics", "", "Household Statistics", "", "Residence Movement Report" });
        cboReportType.Location = new Point(25, 120);
        cboReportType.Name = "cboReportType";
        cboReportType.Size = new Size(350, 28);
        cboReportType.TabIndex = 1;
        // 
        // btnGenerateReport
        // 
        btnGenerateReport.BackColor = Color.DarkSlateGray;
        btnGenerateReport.FlatStyle = FlatStyle.Flat;
        btnGenerateReport.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
        btnGenerateReport.ForeColor = Color.White;
        btnGenerateReport.Location = new Point(400, 118);
        btnGenerateReport.Name = "btnGenerateReport";
        btnGenerateReport.Size = new Size(160, 40);
        btnGenerateReport.TabIndex = 2;
        btnGenerateReport.Text = "Generate Report";
        btnGenerateReport.UseVisualStyleBackColor = false;
        // 
        // ReportsControl
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoScroll = true;
        BackColor = Color.WhiteSmoke;
        Controls.Add(btnGenerateReport);
        Controls.Add(cboReportType);
        Controls.Add(lblReportsTitle);
        Name = "ReportsControl";
        Size = new Size(800, 650);
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label lblReportsTitle;
    private ComboBox cboReportType;
    private Button btnGenerateReport;
}
