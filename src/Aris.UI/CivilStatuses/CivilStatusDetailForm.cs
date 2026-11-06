using Aris.Application.CivilStatuses.Dtos;
using Aris.UI.Localization;

namespace Aris.UI.CivilStatuses;

public sealed class CivilStatusDetailForm : Form {
    private int _y = 60;

    public CivilStatusDetailForm(CivilStatusRecordDto record) {
        Text = LanguageManager.T("ARIS - Civil status detail");
        ClientSize = new Size(620, 470);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        var title = new Label {
            Text = LanguageManager.T("Civil status detail"),
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = UiTheme.Accent,
            Location = new Point(25, 18),
            AutoSize = true
        };
        Controls.Add(title);

        AddRow(LanguageManager.T("Record type"), Translate(record.RecordType));
        AddRow(LanguageManager.T("Citizen"), $"{record.CitizenFullName} ({record.CitizenNationalId})");
        AddRow(LanguageManager.T("Related citizen"),
            record.RelatedFullName is null
                ? null
                : $"{record.RelatedFullName} ({record.RelatedNationalId})");
        AddRow(LanguageManager.T("Address"), record.AddressText);
        AddRow(LanguageManager.T("Event date"), record.EventDate.ToString("dd/MM/yyyy"));
        AddRow(LanguageManager.T("Registration date"), record.RegistrationDate.ToString("dd/MM/yyyy"));
        AddRow(LanguageManager.T("Document number"), record.DocumentNumber);
        AddRow(LanguageManager.T("Status"), record.Status);
        AddRow(LanguageManager.T("Notes"), record.Notes);
        AddRow(LanguageManager.T("Cancel reason"), record.CancelReason);
        AddRow(LanguageManager.T("Created At"), record.CreatedAt.ToString("dd/MM/yyyy HH:mm"));
        AddRow(LanguageManager.T("Created By"), record.CreatedByUsername);
        AddRow(LanguageManager.T("Cancelled"),
            record.CancelledAt?.ToString("dd/MM/yyyy HH:mm"));

        var close = new Button {
            Text = LanguageManager.T("Close"),
            DialogResult = DialogResult.OK,
            Location = new Point(480, 420),
            Size = new Size(110, 34)
        };

        Controls.Add(close);
        UiTheme.ApplyScreen(this);
    }

    private void AddRow(string label, string? value) {
        var labelControl = new Label {
            Text = label + ":",
            Location = new Point(25, _y),
            AutoSize = true,
            Font = UiTheme.SemiboldFont
        };

        var valueControl = new Label {
            Text = string.IsNullOrWhiteSpace(value) ? "-" : value,
            Location = new Point(190, _y),
            Size = new Size(400, 22),
            AutoEllipsis = true
        };

        Controls.Add(labelControl);
        Controls.Add(valueControl);
        _y += 26;
    }

    private static string Translate(string recordType) {
        return recordType switch {
            "BIRTH" => LanguageManager.T("Birth"),
            "DEATH" => LanguageManager.T("Death"),
            "MARRIAGE" => LanguageManager.T("Marriage"),
            _ => recordType
        };
    }
}