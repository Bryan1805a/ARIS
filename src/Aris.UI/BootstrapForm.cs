using Aris.Application.Accounts;
using Aris.UI.Localization;

namespace Aris.UI;

/// <summary>
/// One-time screen shown when the database has no accounts (<c>R15</c>, <c>T6</c>).
/// </summary>
/// <remarks>
/// The form only collects input; <see cref="IBootstrapService"/> enforces "no accounts
/// yet", hashes the password, and audits the creation. Once an administrator exists this
/// screen is no longer reachable from the login form.
/// </remarks>
public sealed class BootstrapForm : Form
{
    private readonly IBootstrapService _bootstrapService;

    private readonly TextBox _username = new();
    private readonly TextBox _fullName = new();
    private readonly TextBox _password = new();
    private readonly TextBox _confirmPassword = new();
    private readonly Button _createButton = new();
    private readonly Button _cancelButton = new();

    public BootstrapForm(IBootstrapService bootstrapService)
    {
        _bootstrapService = bootstrapService;

        Text = "ARIS - Create first administrator";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(470, 315);

        BuildLayout();

        var languageButton = new Button
        {
            Text = LanguageManager.SwitchLabel,
            Location = new Point(20, 275),
            Size = new Size(100, 30),
            FlatStyle = FlatStyle.Flat
        };
        languageButton.Click += (_, _) => LanguageManager.Toggle();
        LanguageManager.LanguageChanged += () =>
            languageButton.Text = LanguageManager.SwitchLabel;
        Controls.Add(languageButton);

        LanguageManager.Apply(this);
    }

    /// <summary>The username that was created, so the login form can prefill it.</summary>
    public string? CreatedUsername { get; private set; }

    private void BuildLayout()
    {
        var title = new Label
        {
            Text = "Create the first administrator",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = Color.DarkSlateGray,
            AutoSize = true,
            Location = new Point(20, 15)
        };

        var message = new Label
        {
            Text = "No accounts exist yet. This account receives the ADMIN role and can create officers afterwards.",
            ForeColor = Color.DimGray,
            AutoSize = false,
            Size = new Size(430, 40),
            Location = new Point(20, 50)
        };

        AddRow("Username", _username, 100);
        AddRow("Full name", _fullName, 140);

        _password.UseSystemPasswordChar = true;
        AddRow("Password", _password, 180);

        _confirmPassword.UseSystemPasswordChar = true;
        AddRow("Confirm password", _confirmPassword, 220);

        var hint = new Label
        {
            Text = "Password must satisfy the PASSWORD_MIN_LENGTH system parameter (default 8).",
            ForeColor = Color.Gray,
            AutoSize = false,
            Size = new Size(430, 20),
            Location = new Point(20, 252)
        };

        _createButton.Text = "Create";
        _createButton.Location = new Point(230, 275);
        _createButton.Size = new Size(100, 30);
        _createButton.BackColor = Color.SteelBlue;
        _createButton.ForeColor = Color.White;
        _createButton.FlatStyle = FlatStyle.Flat;
        _createButton.Click += CreateButton_Click;

        _cancelButton.Text = "Cancel";
        _cancelButton.Location = new Point(340, 275);
        _cancelButton.Size = new Size(100, 30);
        _cancelButton.DialogResult = DialogResult.Cancel;

        AcceptButton = _createButton;
        CancelButton = _cancelButton;

        Controls.AddRange(new Control[]
        {
            title, message, hint, _createButton, _cancelButton
        });
    }

    private void AddRow(string labelText, TextBox textBox, int top)
    {
        var label = new Label
        {
            Text = labelText,
            AutoSize = true,
            Location = new Point(20, top + 3)
        };

        textBox.Location = new Point(140, top);
        textBox.Size = new Size(310, 23);

        Controls.Add(label);
        Controls.Add(textBox);
    }

    private async void CreateButton_Click(object? sender, EventArgs e)
    {
        var username = _username.Text.Trim();
        var fullName = _fullName.Text.Trim();
        var password = _password.Text;
        var confirmPassword = _confirmPassword.Text;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(fullName))
        {
            MessageBox.Show(
                "Username and full name are required.",
                "Missing information",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        if (password != confirmPassword)
        {
            MessageBox.Show(
                "The password and confirmation do not match.",
                "Check password",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            _confirmPassword.Focus();
            return;
        }

        _createButton.Enabled = false;
        _cancelButton.Enabled = false;

        try
        {
            var result = await _bootstrapService.CreateFirstAdminAsync(
                username, fullName, password);

            if (result.IsFailure)
            {
                MessageBox.Show(
                    string.Join(Environment.NewLine, result.Errors),
                    "Cannot create administrator",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            CreatedUsername = username;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Unexpected error while creating the administrator: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            _createButton.Enabled = true;
            _cancelButton.Enabled = true;
        }
    }
}
