using Aris.Application.Accounts;

using System;
using System.Windows.Forms;

namespace Aris.UI;

public partial class ChangePasswordForm : Form
{
    private readonly IAccountService _accountService;
    private readonly int _userId;

    public ChangePasswordForm(
        IAccountService accountService,
        int userId)
    {
        InitializeComponent();

        _accountService = accountService;
        _userId = userId;

        btnChangePassword.Click += btnChangePassword_Click;
    }

    private void ChangePasswordForm_Load(
        object sender, EventArgs e)
    {
        txtNewPassword.Focus();
    }

    private async void btnChangePassword_Click(
        object? sender, EventArgs e)
    {
        string currentPassword = txtCurrentPassword.Text;
        string newPassword = txtNewPassword.Text;
        string confirmPassword = txtConfirmPassword.Text;

        if (string.IsNullOrWhiteSpace(currentPassword) ||
         string.IsNullOrWhiteSpace(newPassword) ||
         string.IsNullOrWhiteSpace(confirmPassword))
            {
                MessageBox.Show(
                    "Please fill in all password fields.");
                return;
            }

        if (newPassword != confirmPassword)
        {
            MessageBox.Show(
                "Passwords do not match.");
            return;
        }

        btnChangePassword.Enabled = false;

        try
        {
            bool changed = await _accountService
             .ChangeOwnPasswordAsync(
                 _userId,
                 currentPassword,
                 newPassword);

            if (changed)
            {
                MessageBox.Show(
                    "Password changed successfully.");

                DialogResult = DialogResult.OK;
                Close();
            }
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(
                ex.Message,
                "Invalid Password",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        catch (Exception)
        {
            MessageBox.Show(
                "Unable to change password. Please try again.",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            btnChangePassword.Enabled = true;
        }
    }

    private void txtNewPassword_TextChanged(
        object sender, EventArgs e)
        {
        }

    private void textBox1_TextChanged(object sender, EventArgs e)
    {
    }
    private void txtConfirmPassword_TextChanged(object sender, EventArgs e)
    {

    }
}