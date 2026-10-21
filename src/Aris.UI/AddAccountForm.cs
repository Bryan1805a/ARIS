using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Aris.UI;

public partial class AddAccountForm : Form
{
    public AddAccountForm()
    {
        InitializeComponent();
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        this.Close();
    }

    private void btnSave_Click(object sender, EventArgs e)
    {
        string username = txtUsername.Text.Trim();
        string password = txtPassword.Text;

        if (string.IsNullOrWhiteSpace(username))
        {
            MessageBox.Show("Please enter a username.");
            txtUsername.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            MessageBox.Show("Please enter a password.");
            txtPassword.Focus();
            return;
        }

        if (cboRole.SelectedIndex == -1)
        {
            MessageBox.Show("Please select a role.");
            cboRole.Focus();
            return;
        }

        MessageBox.Show(
            "Input is valid. Database saving is not implemented yet.",
            "Validation Successful",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );
    }
}
