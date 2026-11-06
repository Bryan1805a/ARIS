using Aris.Application.CivilStatuses.Dtos;
using Aris.UI.Localization;

namespace Aris.UI.CivilStatuses;

public sealed class CancelCivilStatusForm : Form {
    private readonly TextBox _reason = new();
    private readonly Label _error = new();

    public string Reason => _reason.Text.Trim();

    public CancelCivilStatusForm(CivilStatusRecordDto record) {
        Text = LanguageManager.T("ARIS - Cancel civil status");
        ClientSize = new Size(480, 280);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        var title = new Label {
            Text = LanguageManager.T("Cancel registration"),
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = UiTheme.Accent,
            Location = new Point(25, 18),
            AutoSize = true
        };

        var subject = new Label {
            Text = $"{record.CitizenFullName} - {record.RecordType}",
            Location = new Point(25, 58),
            AutoSize = true,
            Font = UiTheme.SemiboldFont
        };

        var lblReason = new Label {
            Text = LanguageManager.T("Reason (required)") + ":",
            Location = new Point(25, 92),
            AutoSize = true
        };

        _reason.Location = new Point(25, 116);
        _reason.Size = new Size(430, 80);
        _reason.Multiline = true;
        _reason.MaxLength = 300;

        _error.Location = new Point(25, 202);
        _error.Size = new Size(430, 24);
        _error.ForeColor = Color.Firebrick;
        _error.Visible = false;

        var save = new Button {
            Text = LanguageManager.T("Cancel registration"),
            Location = new Point(270, 236),
            Size = new Size(160, 34),
            BackColor = Color.IndianRed,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };

        save.Click += (_, _) => {
            if (string.IsNullOrWhiteSpace(Reason)) {
                _error.Text = LanguageManager.T("A cancellation reason is required.");
                _error.Visible = true;
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        };

        var close = new Button {
            Text = LanguageManager.T("Close"),
            DialogResult = DialogResult.Cancel,
            Location = new Point(370, 236),
            Size = new Size(85, 34)
        };

        Controls.AddRange(new Control[]
        {
            title, subject, lblReason, _reason, _error, save, close
        });

        UiTheme.ApplyScreen(this);
    }
}