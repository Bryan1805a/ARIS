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
        dgvReports = new DataGridView();
        dtpFromDate = new DateTimePicker();
        dtpToDate = new DateTimePicker();
        lblFromDate = new Label();
        lblToDate = new Label();
        btnExportCsv = new Button();
        ((System.ComponentModel.ISupportInitialize)dgvReports).BeginInit();
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
        cboReportType.Items.AddRange(new object[] { "Residence Statistics", "Household Statistics", "Residence Movement Report" });
        cboReportType.Location = new Point(25, 90);
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
        btnGenerateReport.Location = new Point(400, 88);
        btnGenerateReport.Name = "btnGenerateReport";
        btnGenerateReport.Size = new Size(160, 40);
        btnGenerateReport.TabIndex = 2;
        btnGenerateReport.Text = "Generate Report";
        btnGenerateReport.UseVisualStyleBackColor = false;
        btnGenerateReport.Click += btnGenerateReport_Click;
        // 
        // dgvReports
        // 
        dgvReports.AllowUserToAddRows = false;
        dgvReports.AllowUserToDeleteRows = false;
        dgvReports.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        dgvReports.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvReports.BackgroundColor = Color.White;
        dgvReports.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvReports.Location = new Point(25, 215);
        dgvReports.MultiSelect = false;
        dgvReports.Name = "dgvReports";
        dgvReports.ReadOnly = true;
        dgvReports.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvReports.Size = new Size(735, 370);
        dgvReports.TabIndex = 3;
        // 
        // dtpFromDate
        // 
        dtpFromDate.Format = DateTimePickerFormat.Short;
        dtpFromDate.Location = new Point(25, 165);
        dtpFromDate.Name = "dtpFromDate";
        dtpFromDate.Size = new Size(200, 23);
        dtpFromDate.TabIndex = 4;
        dtpFromDate.Visible = false;
        // 
        // dtpToDate
        // 
        dtpToDate.Format = DateTimePickerFormat.Short;
        dtpToDate.Location = new Point(300, 165);
        dtpToDate.Name = "dtpToDate";
        dtpToDate.Size = new Size(200, 23);
        dtpToDate.TabIndex = 5;
        dtpToDate.Visible = false;
        // 
        // lblFromDate
        // 
        lblFromDate.AutoSize = true;
        lblFromDate.Location = new Point(25, 140);
        lblFromDate.Name = "lblFromDate";
        lblFromDate.Size = new Size(65, 15);
        lblFromDate.TabIndex = 6;
        lblFromDate.Text = "From Date:";
        lblFromDate.Visible = false;
        // 
        // lblToDate
        // 
        lblToDate.AutoSize = true;
        lblToDate.Location = new Point(300, 140);
        lblToDate.Name = "lblToDate";
        lblToDate.Size = new Size(50, 15);
        lblToDate.TabIndex = 7;
        lblToDate.Text = "To Date:";
        lblToDate.Visible = false;
        // 
        // btnExportCsv
        // 
        btnExportCsv.BackColor = Color.DarkSlateGray;
        btnExportCsv.Enabled = false;
        btnExportCsv.FlatStyle = FlatStyle.Flat;
        btnExportCsv.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
        btnExportCsv.ForeColor = Color.White;
        btnExportCsv.Location = new Point(577, 88);
        btnExportCsv.Name = "btnExportCsv";
        btnExportCsv.Size = new Size(160, 40);
        btnExportCsv.TabIndex = 8;
        btnExportCsv.Text = "Export CSV";
        btnExportCsv.UseVisualStyleBackColor = false;
        btnExportCsv.Click += btnExportCsv_Click;
        // 
        // ReportsControl
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoScroll = true;
        BackColor = Color.WhiteSmoke;
        Controls.Add(btnExportCsv);
        Controls.Add(lblToDate);
        Controls.Add(lblFromDate);
        Controls.Add(dtpToDate);
        Controls.Add(dtpFromDate);
        Controls.Add(dgvReports);
        Controls.Add(btnGenerateReport);
        Controls.Add(cboReportType);
        Controls.Add(lblReportsTitle);
        Name = "ReportsControl";
        Size = new Size(800, 650);
        ((System.ComponentModel.ISupportInitialize)dgvReports).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label lblReportsTitle;
    private ComboBox cboReportType;
    private Button btnGenerateReport;
    private DataGridView dgvReports;
    private DateTimePicker dtpFromDate;
    private DateTimePicker dtpToDate;
    private Label lblFromDate;
    private Label lblToDate;
    private Button btnExportCsv;
}
