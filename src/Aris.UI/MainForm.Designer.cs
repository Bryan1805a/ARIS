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
        btnLogout = new Button();
        btnReports = new Button();
        btnAccounts = new Button();
        btnDashboard = new Button();
        lblLogo = new Label();
        pnlContent = new Panel();
        lblPageTitle = new Label();
        pnlSidebar.SuspendLayout();
        pnlContent.SuspendLayout();
        SuspendLayout();
        // 
        // pnlSidebar
        // 
        pnlSidebar.BackColor = Color.DarkSlateGray;
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
        // btnLogout
        // 
        btnLogout.FlatStyle = FlatStyle.Flat;
        btnLogout.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        btnLogout.ForeColor = Color.White;
        btnLogout.Location = new Point(10, 420);
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
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1024, 700);
        Controls.Add(pnlContent);
        Controls.Add(pnlSidebar);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Administrative Residence Information System (ARIS)";
        pnlSidebar.ResumeLayout(false);
        pnlSidebar.PerformLayout();
        pnlContent.ResumeLayout(false);
        pnlContent.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private Panel pnlSidebar;
    private Button btnDashboard;
    private Label lblLogo;
    private Button btnReports;
    private Button btnAccounts;
    private Button btnLogout;
    private Panel pnlContent;
    private Label lblPageTitle;
}
