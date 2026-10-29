using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Aris.UI;

public partial class EditAccountForm : Form
{
    public EditAccountForm()
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

        if (string.IsNullOrWhiteSpace(username))
        {
            MessageBox.Show("Please enter a username.");
            txtUsername.Focus();
            return;
        }

        if (cboRole.SelectedIndex == -1)
        {
            MessageBox.Show("Please select a role.");
            cboRole.Focus();
            return;
        }

        if (cboStatus.SelectedIndex == -1)
        {
            MessageBox.Show("Please select a status.");
            cboStatus.Focus();
            return;
        }

        MessageBox.Show(
            "Input is valid. Database updating is not implemented yet.",
            "Validation Successful",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );
    }
}
