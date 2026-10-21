using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Aris.UI;

public partial class AccountManagementControl : UserControl
{
    public AccountManagementControl()
    {
        InitializeComponent();
    }

    private void btnAddAccount_Click(object sender, EventArgs e)
    {
        using (AddAccountForm form = new AddAccountForm())
        {
            form.ShowDialog(this);
        }
    }

    private void btnEditAccount_Click(object sender, EventArgs e)
    {
        if (dgvAccounts.CurrentRow == null)
        {
            MessageBox.Show(
                "Please select an account to edit.",
                "No Account Selected",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
            return;
        }

        using (EditAccountForm form = new EditAccountForm())
        {
            form.ShowDialog(this);
        }
    }

    private void btnDisableAccount_Click(object sender, EventArgs e)
    {
        if (dgvAccounts.CurrentRow == null)
        {
            MessageBox.Show(
                "Please select an account to disable.",
                "No Account Selected",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
            return;
        }

        DialogResult result = MessageBox.Show(
            "Are you sure you want to disable this account?",
            "Confirm Disable",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question
        );

        if (result == DialogResult.Yes)
        {
            MessageBox.Show(
                "Database updating is not implemented yet.",
                "Information",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
    }

    private void btnSearch_Click(object sender, EventArgs e)
    {
        string keyword = txtSearch.Text.Trim();

        if (string.IsNullOrWhiteSpace(keyword))
        {
            MessageBox.Show(
                "Please enter a username to search.",
                "Search",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
            txtSearch.Focus();
            return;
        }

        MessageBox.Show(
            "Search functionality will be available after database integration.",
            "Information",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );
    }
}
