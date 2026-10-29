using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Aris.Application.Accounts;
using Aris.Application.Authentication;

namespace Aris.UI;

public partial class AddAccountForm : Form
{
    private readonly IAccountService _accountService;

    public AddAccountForm(IAccountService accountService)
    {
        InitializeComponent();
        _accountService = accountService;
    }
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public LoginResult? CurrentUser { get; set; }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        this.Close();
    }

    private async void btnSave_Click(object sender, EventArgs e)
    {
        string username = txtUsername.Text.Trim();
        string password = txtPassword.Text;
        string fullName = txtFullName.Text.Trim();

        if (string.IsNullOrWhiteSpace(username))
        {
            MessageBox.Show("Please enter a username.");
            txtUsername.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            MessageBox.Show("Please enter a full name.");
            txtFullName.Focus();
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

        btnSave.Enabled = false;

        try
        {
            if (CurrentUser == null)
            {
                MessageBox.Show("User session not found.");
                return;
            }

            bool created = await _accountService.CreateAccountAsync(
                username,
                fullName,
                password,
                cboRole.SelectedItem!.ToString()!,
                CurrentUser.UserId
            );

            if (!created)
            {
                MessageBox.Show(
                    "Username already exists.",
                    "Duplicate Username",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            MessageBox.Show(
                "Account created successfully.",
                "Success",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        catch (Exception)
        {
            MessageBox.Show(
                "Failed to create account.",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
        finally
        {
            btnSave.Enabled = true;
        }
    }

    private void lblUsername_Click(object sender, EventArgs e)
    {

    }
}
