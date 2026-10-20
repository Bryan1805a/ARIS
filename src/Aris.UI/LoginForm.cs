
using System;
using System.Windows.Forms;

using Aris.Application.Authentication;

namespace Aris.UI;

public partial class LoginForm : Form
{
    private readonly IAuthService _authService;

    public LoginForm(IAuthService authService)
    {
        InitializeComponent();
        _authService = authService;
    }

    private async void btnLogin_Click(object sender, EventArgs e)
    {
        string username = txtUsername.Text.Trim();
        string password = txtPassword.Text;

        if (string.IsNullOrWhiteSpace(username))
        {
            MessageBox.Show("Please enter your username.");
            txtUsername.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            MessageBox.Show("Please enter your password.");
            txtPassword.Focus();
            return;
        }

        btnLogin.Enabled = false;

        try
        {
            var result = await _authService.LoginAsync(username, password);

            if (result.IsFailure)
            {
                MessageBox.Show(
                    string.Join("\n", result.Errors),
                    "Login Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            MessageBox.Show("Login successful.");
        }
        catch (Exception)
        {
            MessageBox.Show(
                "An unexpected error occurred during login.",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            btnLogin.Enabled = true;
        }
    }
}
