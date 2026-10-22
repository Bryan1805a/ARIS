using Aris.Application.Accounts;
using Aris.Application.Accounts;

namespace Aris.UI;

public partial class MainForm : Form
{
    private readonly IAccountService _accountService;

    public MainForm(IAccountService accountService)
    {
        InitializeComponent();
        _accountService = accountService;
    }

    private void btnDashboard_Click(object sender, EventArgs e)
    {
        pnlContent.Controls.Clear();

        Label lblDashboard = new Label();
        lblDashboard.Text = "Dashboard";
        lblDashboard.Font = new Font("Segoe UI", 20, FontStyle.Bold);
        lblDashboard.ForeColor = Color.DarkSlateGray;
        lblDashboard.AutoSize = true;
        lblDashboard.Location = new Point(25, 25);

        pnlContent.Controls.Add(lblDashboard);
    }

    private void btnAccounts_Click(object sender, EventArgs e)
    {
        pnlContent.Controls.Clear();

        AccountManagementControl accountControl = new AccountManagementControl(_accountService);
        accountControl.Dock = DockStyle.Fill; //cho man hinh tu gian theo kich thuoc

        pnlContent.Controls.Add(accountControl);
    }

    private void btnReports_Click(object sender, EventArgs e)
    {
        pnlContent.Controls.Clear();

        Label lblReports = new Label();
        lblReports.Text = "Reports";
        lblReports.Font = new Font("Segoe UI", 20, FontStyle.Bold);
        lblReports.ForeColor = Color.DarkSlateGray;
        lblReports.AutoSize = true;
        lblReports.Location = new Point(25, 25);

        pnlContent.Controls.Add(lblReports);
    }

    private void btnLogout_Click(object sender, EventArgs e)
    {
        DialogResult result = MessageBox.Show(
            "Are you sure you want to log out?",
            "Confirm Logout",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question
        );

        if (result == DialogResult.Yes)
        {
            this.Close();
        }
    }
}
