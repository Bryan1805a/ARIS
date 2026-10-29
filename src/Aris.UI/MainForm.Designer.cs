#nullable enable
namespace Aris.UI;

partial class MainForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer? components = null;

    /// <summary>
    ///  Clean up any resources being used.
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

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        pnlSidebar = new Panel();
        btnAuditLogs = new Button();
        btnTransfer = new Button();
        btnHousehold = new Button();
        btnCitizen = new Button();
        btnLogout = new Button();
        btnReports = new Button();
        btnAccounts = new Button();
        btnDashboard = new Button();
        lblLogo = new Label();
        pnlContent = new Panel();
        lblPageTitle = new Label();
        statusStrip1 = new StatusStrip();
        lblStatusUser = new ToolStripStatusLabel();
        lblStatusRole = new ToolStripStatusLabel();
        lblStatusConnection = new ToolStripStatusLabel();
        btnSystemConfiguration = new Button();
        pnlSidebar.SuspendLayout();
        pnlContent.SuspendLayout();
        statusStrip1.SuspendLayout();
        SuspendLayout();
        // 
        // pnlSidebar
        // 
        pnlSidebar.BackColor = Color.DarkSlateGray;
        pnlSidebar.Controls.Add(btnSystemConfiguration);
        pnlSidebar.Controls.Add(btnAuditLogs);
        pnlSidebar.Controls.Add(btnTransfer);
        pnlSidebar.Controls.Add(btnHousehold);
        pnlSidebar.Controls.Add(btnCitizen);
        pnlSidebar.Controls.Add(btnLogout);
        pnlSidebar.Controls.Add(btnReports);
        pnlSidebar.Controls.Add(btnAccounts);
        pnlSidebar.Controls.Add(btnDashboard);
        pnlSidebar.Controls.Add(lblLogo);
        pnlSidebar.Dock = DockStyle.Left;
        pnlSidebar.Location = new Point(0, 0);
        pnlSidebar.Name = "pnlSidebar";
        pnlSidebar.Size = new Size(220, 700);
        pnlSidebar.TabIndex = 0;
        // 
        // btnAuditLogs
        // 
        btnAuditLogs.FlatStyle = FlatStyle.Flat;
        btnAuditLogs.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        btnAuditLogs.ForeColor = Color.White;
        btnAuditLogs.Location = new Point(10, 265);
        btnAuditLogs.Name = "btnAuditLogs";
        btnAuditLogs.Size = new Size(200, 45);
        btnAuditLogs.TabIndex = 9;
        btnAuditLogs.Text = "Audit Logs";
        btnAuditLogs.TextAlign = ContentAlignment.MiddleLeft;
        btnAuditLogs.UseVisualStyleBackColor = true;
        btnAuditLogs.Click += btnAuditLogs_Click;
        // 
        // btnTransfer
        // 
        btnTransfer.FlatStyle = FlatStyle.Flat;
        btnTransfer.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        btnTransfer.ForeColor = Color.White;
        btnTransfer.Location = new Point(10, 485);
        btnTransfer.Name = "btnTransfer";
        btnTransfer.Size = new Size(200, 45);
        btnTransfer.TabIndex = 8;
        btnTransfer.Text = "Transfer Management";
        btnTransfer.TextAlign = ContentAlignment.MiddleLeft;
        btnTransfer.UseVisualStyleBackColor = true;
        btnTransfer.Click += btnTransfer_Click;
        // 
        // btnHousehold
        // 
        btnHousehold.FlatStyle = FlatStyle.Flat;
        btnHousehold.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        btnHousehold.ForeColor = Color.White;
        btnHousehold.Location = new Point(10, 430);
        btnHousehold.Name = "btnHousehold";
        btnHousehold.Size = new Size(200, 45);
        btnHousehold.TabIndex = 7;
        btnHousehold.Text = "Household Management";
        btnHousehold.TextAlign = ContentAlignment.MiddleLeft;
        btnHousehold.UseVisualStyleBackColor = true;
        btnHousehold.Click += btnHousehold_Click;
        // 
        // btnCitizen
        // 
        btnCitizen.FlatStyle = FlatStyle.Flat;
        btnCitizen.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        btnCitizen.ForeColor = Color.White;
        btnCitizen.Location = new Point(12, 375);
        btnCitizen.Name = "btnCitizen";
        btnCitizen.Size = new Size(200, 45);
        btnCitizen.TabIndex = 6;
        btnCitizen.Text = "Citizen Management";
        btnCitizen.TextAlign = ContentAlignment.MiddleLeft;
        btnCitizen.UseVisualStyleBackColor = true;
        btnCitizen.Click += btnCitizen_Click;
        // 
        // btnLogout
        // 
        btnLogout.FlatStyle = FlatStyle.Flat;
        btnLogout.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        btnLogout.ForeColor = Color.White;
        btnLogout.Location = new Point(10, 540);
        btnLogout.Name = "btnLogout";
        btnLogout.Size = new Size(200, 45);
        btnLogout.TabIndex = 4;
        btnLogout.Text = "Logout";
        btnLogout.TextAlign = ContentAlignment.MiddleLeft;
        btnLogout.UseVisualStyleBackColor = true;
        btnLogout.Click += btnLogout_Click;
        // 
        // btnReports
        // 
        btnReports.FlatStyle = FlatStyle.Flat;
        btnReports.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        btnReports.ForeColor = Color.White;
        btnReports.Location = new Point(10, 210);
        btnReports.Name = "btnReports";
        btnReports.Size = new Size(200, 45);
        btnReports.TabIndex = 3;
        btnReports.Text = "Reports";
        btnReports.TextAlign = ContentAlignment.MiddleLeft;
        btnReports.UseVisualStyleBackColor = true;
        btnReports.Click += btnReports_Click;
        // 
        // btnAccounts
        // 
        btnAccounts.FlatStyle = FlatStyle.Flat;
        btnAccounts.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        btnAccounts.ForeColor = Color.White;
        btnAccounts.Location = new Point(10, 155);
        btnAccounts.Name = "btnAccounts";
        btnAccounts.Size = new Size(200, 45);
        btnAccounts.TabIndex = 2;
        btnAccounts.Text = "Account Management";
        btnAccounts.TextAlign = ContentAlignment.MiddleLeft;
        btnAccounts.UseVisualStyleBackColor = true;
        btnAccounts.Click += btnAccounts_Click;
        // 
        // btnDashboard
        // 
        btnDashboard.FlatStyle = FlatStyle.Flat;
        btnDashboard.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        btnDashboard.ForeColor = Color.White;
        btnDashboard.Location = new Point(10, 100);
        btnDashboard.Name = "btnDashboard";
        btnDashboard.Size = new Size(200, 45);
        btnDashboard.TabIndex = 1;
        btnDashboard.Text = "Dashboard";
        btnDashboard.TextAlign = ContentAlignment.MiddleLeft;
        btnDashboard.UseVisualStyleBackColor = true;
        btnDashboard.Click += btnDashboard_Click;
        // 
        // lblLogo
        // 
        lblLogo.AutoSize = true;
        lblLogo.BackColor = Color.Transparent;
        lblLogo.Font = new Font("Segoe UI", 21.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
        lblLogo.ForeColor = Color.White;
        lblLogo.Location = new Point(65, 25);
        lblLogo.Name = "lblLogo";
        lblLogo.Size = new Size(81, 40);
        lblLogo.TabIndex = 0;
        lblLogo.Text = "ARIS";
        // 
        // pnlContent
        // 
        pnlContent.BackColor = Color.WhiteSmoke;
        pnlContent.Controls.Add(lblPageTitle);
        pnlContent.Dock = DockStyle.Fill;
        pnlContent.Location = new Point(220, 0);
        pnlContent.Name = "pnlContent";
        pnlContent.Size = new Size(804, 700);
        pnlContent.TabIndex = 1;
        // 
        // lblPageTitle
        // 
        lblPageTitle.AutoSize = true;
        lblPageTitle.BackColor = Color.Transparent;
        lblPageTitle.Font = new Font("Segoe UI", 20.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
        lblPageTitle.ForeColor = Color.DarkSlateGray;
        lblPageTitle.Location = new Point(30, 20);
        lblPageTitle.Name = "lblPageTitle";
        lblPageTitle.Size = new Size(157, 37);
        lblPageTitle.TabIndex = 0;
        lblPageTitle.Text = "Dashboard";
        // 
        // statusStrip1
        // 
        statusStrip1.AutoSize = false;
        statusStrip1.BackColor = Color.DarkSlateGray;
        statusStrip1.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
        statusStrip1.Items.AddRange(new ToolStripItem[] { lblStatusUser, lblStatusRole, lblStatusConnection });
        statusStrip1.Location = new Point(220, 664);
        statusStrip1.Name = "statusStrip1";
        statusStrip1.Size = new Size(804, 36);
        statusStrip1.SizingGrip = false;
        statusStrip1.TabIndex = 2;
        statusStrip1.Text = "statusStrip1";
        // 
        // lblStatusUser
        // 
        lblStatusUser.ForeColor = Color.White;
        lblStatusUser.Margin = new Padding(15, 3, 15, 3);
        lblStatusUser.Name = "lblStatusUser";
        lblStatusUser.Size = new Size(124, 30);
        lblStatusUser.Text = "User: Not logged in";
        lblStatusUser.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // lblStatusRole
        // 
        lblStatusRole.ForeColor = Color.White;
        lblStatusRole.Margin = new Padding(10, 3, 15, 3);
        lblStatusRole.Name = "lblStatusRole";
        lblStatusRole.Size = new Size(432, 30);
        lblStatusRole.Spring = true;
        lblStatusRole.Text = "Role: None";
        // 
        // lblStatusConnection
        // 
        lblStatusConnection.ForeColor = Color.White;
        lblStatusConnection.Margin = new Padding(10, 3, 15, 3);
        lblStatusConnection.Name = "lblStatusConnection";
        lblStatusConnection.Size = new Size(153, 30);
        lblStatusConnection.Text = "Connection: Not checked";
        lblStatusConnection.TextAlign = ContentAlignment.MiddleRight;
        // 
        // btnSystemConfiguration
        // 
        btnSystemConfiguration.FlatStyle = FlatStyle.Flat;
        btnSystemConfiguration.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        btnSystemConfiguration.ForeColor = Color.White;
        btnSystemConfiguration.Location = new Point(10, 320);
        btnSystemConfiguration.Name = "btnSystemConfiguration";
        btnSystemConfiguration.Size = new Size(200, 45);
        btnSystemConfiguration.TabIndex = 10;
        btnSystemConfiguration.Text = "System Configuration";
        btnSystemConfiguration.TextAlign = ContentAlignment.MiddleLeft;
        btnSystemConfiguration.UseVisualStyleBackColor = true;
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1024, 700);
        Controls.Add(statusStrip1);
        Controls.Add(pnlContent);
        Controls.Add(pnlSidebar);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Administrative Residence Information System (ARIS)";
        pnlSidebar.ResumeLayout(false);
        pnlSidebar.PerformLayout();
        pnlContent.ResumeLayout(false);
        pnlContent.PerformLayout();
        statusStrip1.ResumeLayout(false);
        statusStrip1.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private Panel pnlSidebar = null!;
    private Button btnDashboard = null!;
    private Label lblLogo = null!;
    private Button btnReports = null!;
    private Button btnAccounts = null!;
    private Button btnLogout = null!;
    private Panel pnlContent = null!;
    private Label lblPageTitle = null!;
    private StatusStrip statusStrip1 = null!;
    private ToolStripStatusLabel lblStatusUser = null!;
    private ToolStripStatusLabel lblStatusRole = null!;
    private ToolStripStatusLabel lblStatusConnection = null!;
    private Button btnCitizen = null!;
    private Button btnHousehold = null!;
    private Button btnTransfer = null!;
    private Button btnAuditLogs = null!;
    private Button btnSystemConfiguration = null!;
}
