using Aris.Application.Common;
using Aris.Application.Developer;

namespace Aris.UI;

/// <summary>
/// Local developer tools: create an account without an authenticated admin and
/// seed the demo residence dataset. Deliberately not part of the business UI.
/// </summary>
public sealed class DeveloperToolsForm : Form {
    private readonly IDeveloperService _developerService;

    private readonly TextBox _username = new();
    private readonly TextBox _fullName = new();
    private readonly TextBox _password = new();
    private readonly ComboBox _role = new();
    private readonly Button _createAccount = new();
    private readonly Button _seedDemo = new();
    private readonly Button _close = new();
    private readonly Label _status = new();

    public DeveloperToolsForm(IDeveloperService developerService) {
        _developerService = developerService;

        Text = "ARIS - Developer mode";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(620, 600);
        AutoScaleMode = AutoScaleMode.Font;

        BuildLayout();
        UiTheme.ApplyScreen(this);
    }

    private void BuildLayout() {
        var title = new Label {
            Text = "Developer mode",
            AutoSize = true,
            Location = new Point(24, 18)
        };
        UiTheme.StyleTitle(title);

        var warning = new Label {
            Text = "Local prototype only. These actions bypass the normal administrator " +
                "authorization check and write to the current local database.",
            AutoSize = false,
            Size = new Size(560, 42),
            Location = new Point(24, 60)
        };
        UiTheme.StyleMuted(warning);

        var accountGroup = new GroupBox {
            Text = "Create an account",
            Location = new Point(24, 112),
            Size = new Size(560, 280)
        };

        AddLabel(accountGroup, "Username", 20, 35);
        ConfigureText(_username, accountGroup, 165, 32);

        AddLabel(accountGroup, "Full name", 20, 76);
        ConfigureText(_fullName, accountGroup, 165, 73);

        AddLabel(accountGroup, "Password", 20, 117);
        ConfigureText(_password, accountGroup, 165, 114);
        _password.UseSystemPasswordChar = true;

        AddLabel(accountGroup, "Role", 20, 158);
        _role.Location = new Point(165, 155);
        _role.Size = new Size(180, 27);
        _role.DropDownStyle = ComboBoxStyle.DropDownList;
        _role.Items.AddRange(new object[] { "ADMIN", "OFFICER" });
        _role.SelectedIndex = 0;
        accountGroup.Controls.Add(_role);

        var hint = new Label {
            Text = "The new account is active immediately and can sign in with this password.",
            AutoSize = false,
            Size = new Size(520, 24),
            Location = new Point(20, 244)
        };
        UiTheme.StyleMuted(hint);
        accountGroup.Controls.Add(hint);

        _createAccount.Text = "Create account";
        _createAccount.Location = new Point(340, 190);
        _createAccount.Size = new Size(195, 38);
        _createAccount.BackColor = UiTheme.Accent;
        _createAccount.Click += CreateAccount_Click;
        accountGroup.Controls.Add(_createAccount);

        var demoGroup = new GroupBox {
            Text = "Sample residence data",
            Location = new Point(24, 408),
            Size = new Size(560, 110)
        };

        var demoHint = new Label {
            Text = "Seeds the standard demo addresses, citizens and households. " +
                "It is idempotent and is audited as the most recently created active administrator.",
            AutoSize = false,
            Size = new Size(520, 38),
            Location = new Point(20, 24)
        };
        UiTheme.StyleMuted(demoHint);
        demoGroup.Controls.Add(demoHint);

        _seedDemo.Text = "Seed demo data";
        _seedDemo.Location = new Point(340, 58);
        _seedDemo.Size = new Size(195, 36);
        _seedDemo.BackColor = UiTheme.Accent;
        _seedDemo.Click += SeedDemo_Click;
        demoGroup.Controls.Add(_seedDemo);

        _status.AutoSize = false;
        _status.Size = new Size(430, 44);
        _status.Location = new Point(24, 530);
        _status.ForeColor = UiTheme.Muted;
        _status.Font = UiTheme.BaseFont;
        _status.Text = "Ready.";

        _close.Text = "Close";
        _close.Location = new Point(500, 548);
        _close.Size = new Size(84, 34);
        _close.DialogResult = DialogResult.Cancel;

        AcceptButton = _createAccount;
        CancelButton = _close;

        Controls.AddRange(new Control[] {
            title, warning, accountGroup, demoGroup, _status, _close
        });
    }

    private static void AddLabel(
        Control parent,
        string text,
        int x,
        int y) {
        var label = new Label {
            Text = text,
            AutoSize = true,
            Location = new Point(x, y + 4)
        };
        parent.Controls.Add(label);
    }

    private static void ConfigureText(
        TextBox textBox,
        Control parent,
        int x,
        int y) {
        textBox.Location = new Point(x, y);
        textBox.Size = new Size(370, 27);
        parent.Controls.Add(textBox);
    }

    private async void CreateAccount_Click(object? sender, EventArgs e) {
        SetBusy(true);
        _status.Text = "Creating account...";
        _status.ForeColor = UiTheme.Muted;

        try {
            var result = await _developerService.CreateAccountAsync(
                new CreateDeveloperAccountDto {
                    Username = _username.Text.Trim(),
                    FullName = _fullName.Text.Trim(),
                    Password = _password.Text,
                    Role = _role.SelectedItem?.ToString() ?? "OFFICER"
                });

            if (result.IsFailure) {
                ShowError(result.Errors);
                return;
            }

            var username = _username.Text.Trim();
            var role = _role.SelectedItem?.ToString() ?? "OFFICER";
            _status.ForeColor = Color.FromArgb(21, 128, 61);
            _status.Text = $"Created {role} account '{username}'. You can sign in now.";
            _password.Clear();
        } catch (Exception ex) {
            ShowError(new[] { ex.Message });
        } finally {
            SetBusy(false);
        }
    }

    private async void SeedDemo_Click(object? sender, EventArgs e) {
        SetBusy(true);
        _status.Text = "Seeding demo data...";
        _status.ForeColor = UiTheme.Muted;

        try {
            var result = await _developerService.SeedDemoDataAsync();

            if (result.IsFailure || result.Value is null) {
                ShowError(result.IsFailure
                    ? result.Errors
                    : new[] { "The demo seed returned no summary." });
                return;
            }

            var summary = result.Value;
            _status.ForeColor = Color.FromArgb(21, 128, 61);
            _status.Text =
                $"Seeded {summary.Addresses} addresses, {summary.Citizens} citizens " +
                $"and {summary.Households} households.";
        } catch (Exception ex) {
            ShowError(new[] { ex.Message });
        } finally {
            SetBusy(false);
        }
    }

    private void ShowError(IReadOnlyList<string> errors) {
        _status.ForeColor = UiTheme.Danger;
        _status.Text = string.Join(Environment.NewLine, errors);
    }

    private void SetBusy(bool busy) {
        _createAccount.Enabled = !busy;
        _seedDemo.Enabled = !busy;
        _close.Enabled = !busy;
        UseWaitCursor = busy;
    }
}
