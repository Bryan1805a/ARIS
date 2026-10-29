using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Aris.UI;

public partial class ResetPasswordForm : Form
{
    public string NewPassword => txtNewPassword.Text;
    public ResetPasswordForm()
    {
        InitializeComponent();
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        this.DialogResult = DialogResult.Cancel;
        this.Close();
    }

    private void btnSave_Click(object sender, EventArgs e)
    {
        string newPassword = txtNewPassword.Text;
        string confirmPassword = txtConfirmPassword.Text;

        // Check empty fields
        if (string.IsNullOrWhiteSpace(newPassword) ||
            string.IsNullOrWhiteSpace(confirmPassword))
        {
            MessageBox.Show("Please enter both password fields.");
            return;
        }

        // Check password length
        if (newPassword.Length < 8)
        {
            MessageBox.Show("Password must contain at least 8 characters.");
            return;
        }

        // Check password confirmation
        if (newPassword != confirmPassword)
        {
            MessageBox.Show("Passwords do not match.");
            return;
        }

        // Validation successful
        this.DialogResult = DialogResult.OK;
        this.Close();
    }
}
