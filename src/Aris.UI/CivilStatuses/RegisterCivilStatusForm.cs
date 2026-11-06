using Aris.Application.Addresses;
using Aris.Application.Addresses.Dtos;
using Aris.Application.Citizens;
using Aris.Application.Citizens.Dtos;
using Aris.Application.CivilStatuses;
using Aris.Application.CivilStatuses.Dtos;
using Aris.Application.Common;
using Aris.UI.Localization;

namespace Aris.UI.CivilStatuses;

public sealed class RegisterCivilStatusForm : Form {
    private readonly ICivilStatusService _service;
    private readonly IAddressService _addressService;
    private readonly ICitizenService _citizenService;
    private readonly IUserSession _session;

    private readonly ComboBox _cboType = new();
    private readonly TextBox _txtCitizenId = new();
    private readonly Button _btnLookupCitizen = new();
    private readonly Label _lblCitizenName = new();

    private readonly Panel _spousePanel = new();
    private readonly TextBox _txtSpouseId = new();
    private readonly Button _btnLookupSpouse = new();
    private readonly Label _lblSpouseName = new();

    private readonly ComboBox _cboAddress = new();
    private readonly DateTimePicker _dtpEvent = new();
    private readonly DateTimePicker _dtpRegistration = new();
    private readonly TextBox _txtDocumentNumber = new();
    private readonly TextBox _txtNotes = new();
    private readonly Label _lblDeathWarning = new();
    private readonly Label _lblError = new();
    private readonly Button _btnSave = new();
    private readonly Button _btnCancel = new();

    private readonly List<AddressDto> _addresses = new();
    private int _citizenId;
    private int _spouseCitizenId;

    private sealed record RecordTypeOption(string Code, string Label) {
        public override string ToString() => Label;
    }

    public RegisterCivilStatusForm(
        ICivilStatusService service,
        IAddressService addressService,
        ICitizenService citizenService,
        IUserSession session) {
        _service = service;
        _addressService = addressService;
        _citizenService = citizenService;
        _session = session;

        BuildForm();
        UiTheme.ApplyScreen(this);
        Load += async (_, _) => await LoadAddressesAsync();
    }

    private void BuildForm() {
        Text = LanguageManager.T("ARIS - Register civil status");
        ClientSize = new Size(620, 510);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        var title = new Label {
            Text = LanguageManager.T("Register civil status"),
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = UiTheme.Accent,
            Location = new Point(25, 18),
            AutoSize = true
        };

        var lblType = new Label {
            Text = LanguageManager.T("Record type") + ":",
            Location = new Point(25, 62),
            AutoSize = true,
            Font = UiTheme.SemiboldFont
        };

        _cboType.Location = new Point(170, 59);
        _cboType.Size = new Size(200, 27);
        _cboType.DropDownStyle = ComboBoxStyle.DropDownList;
        _cboType.Items.Add(new RecordTypeOption("BIRTH", LanguageManager.T("Birth")));
        _cboType.Items.Add(new RecordTypeOption("DEATH", LanguageManager.T("Death")));
        _cboType.Items.Add(new RecordTypeOption("MARRIAGE", LanguageManager.T("Marriage")));
        _cboType.SelectedIndex = 0;

        var lblCitizen = new Label {
            Text = LanguageManager.T("Citizen (National ID)") + ":",
            Location = new Point(25, 102),
            AutoSize = true,
            Font = UiTheme.SemiboldFont
        };

        _txtCitizenId.Location = new Point(25, 125);
        _txtCitizenId.Size = new Size(220, 27);
        _txtCitizenId.MaxLength = 12;
        _txtCitizenId.TextChanged += (_, _) => {
            _citizenId = 0;
            _lblCitizenName.Text = string.Empty;
        };

        _btnLookupCitizen.Text = LanguageManager.T("Look up");
        _btnLookupCitizen.Location = new Point(255, 124);
        _btnLookupCitizen.Size = new Size(90, 29);
        _btnLookupCitizen.Click += async (_, _) => await LookupCitizenAsync(false);

        _lblCitizenName.Location = new Point(25, 158);
        _lblCitizenName.Size = new Size(560, 24);
        _lblCitizenName.BorderStyle = BorderStyle.FixedSingle;
        _lblCitizenName.TextAlign = ContentAlignment.MiddleLeft;

        _spousePanel.Location = new Point(25, 190);
        _spousePanel.Size = new Size(560, 72);
        _spousePanel.BackColor = Color.Transparent;

        var lblSpouse = new Label {
            Text = LanguageManager.T("Spouse (National ID)") + ":",
            Location = new Point(0, 2),
            AutoSize = true,
            Font = UiTheme.SemiboldFont
        };

        _txtSpouseId.Location = new Point(0, 25);
        _txtSpouseId.Size = new Size(220, 27);
        _txtSpouseId.MaxLength = 12;
        _txtSpouseId.TextChanged += (_, _) => {
            _spouseCitizenId = 0;
            _lblSpouseName.Text = string.Empty;
        };

        _btnLookupSpouse.Text = LanguageManager.T("Look up");
        _btnLookupSpouse.Location = new Point(230, 24);
        _btnLookupSpouse.Size = new Size(90, 29);
        _btnLookupSpouse.Click += async (_, _) => await LookupCitizenAsync(true);

        _lblSpouseName.Location = new Point(330, 25);
        _lblSpouseName.Size = new Size(230, 27);
        _lblSpouseName.BorderStyle = BorderStyle.FixedSingle;
        _lblSpouseName.TextAlign = ContentAlignment.MiddleLeft;

        _spousePanel.Controls.AddRange(new Control[]
        {
            lblSpouse, _txtSpouseId, _btnLookupSpouse, _lblSpouseName
        });

        var lblAddress = new Label {
            Text = LanguageManager.T("Address") + ":",
            Location = new Point(25, 270),
            AutoSize = true,
            Font = UiTheme.SemiboldFont
        };

        _cboAddress.Location = new Point(170, 267);
        _cboAddress.Size = new Size(415, 27);
        _cboAddress.DropDownStyle = ComboBoxStyle.DropDownList;

        var lblEvent = new Label {
            Text = LanguageManager.T("Event date") + ":",
            Location = new Point(25, 310),
            AutoSize = true,
            Font = UiTheme.SemiboldFont
        };

        _dtpEvent.Location = new Point(170, 307);
        _dtpEvent.Size = new Size(180, 27);
        _dtpEvent.Format = DateTimePickerFormat.Short;
        _dtpEvent.Value = DateTime.Today;

        var lblRegistration = new Label {
            Text = LanguageManager.T("Registration date") + ":",
            Location = new Point(370, 310),
            AutoSize = true,
            Font = UiTheme.SemiboldFont
        };

        _dtpRegistration.Location = new Point(500, 307);
        _dtpRegistration.Size = new Size(85, 27);
        _dtpRegistration.Format = DateTimePickerFormat.Short;
        _dtpRegistration.Value = DateTime.Today;

        var lblDocument = new Label {
            Text = LanguageManager.T("Document number") + ":",
            Location = new Point(25, 350),
            AutoSize = true,
            Font = UiTheme.SemiboldFont
        };

        _txtDocumentNumber.Location = new Point(170, 347);
        _txtDocumentNumber.Size = new Size(200, 27);
        _txtDocumentNumber.MaxLength = 30;

        var lblNotes = new Label {
            Text = LanguageManager.T("Notes") + ":",
            Location = new Point(25, 390),
            AutoSize = true,
            Font = UiTheme.SemiboldFont
        };

        _txtNotes.Location = new Point(170, 387);
        _txtNotes.Size = new Size(415, 27);
        _txtNotes.MaxLength = 500;

        _lblDeathWarning.Location = new Point(25, 425);
        _lblDeathWarning.Size = new Size(560, 36);
        _lblDeathWarning.ForeColor = Color.DarkOrange;
        _lblDeathWarning.Text = LanguageManager.T(
            "Death registration records the event only. Use Citizen Management to deactivate the citizen and close the residence.");
        _lblDeathWarning.Visible = false;

        _lblError.Location = new Point(25, 425);
        _lblError.Size = new Size(560, 36);
        _lblError.ForeColor = Color.Firebrick;
        _lblError.Visible = false;

        _btnSave.Text = LanguageManager.T("Save");
        _btnSave.Location = new Point(350, 465);
        _btnSave.Size = new Size(115, 34);
        _btnSave.BackColor = UiTheme.Accent;
        _btnSave.ForeColor = Color.White;
        _btnSave.FlatStyle = FlatStyle.Flat;
        _btnSave.Click += async (_, _) => await SaveAsync();

        _btnCancel.Text = LanguageManager.T("Cancel");
        _btnCancel.Location = new Point(475, 465);
        _btnCancel.Size = new Size(110, 34);
        _btnCancel.DialogResult = DialogResult.Cancel;

        _cboType.SelectedIndexChanged += (_, _) => TypeChanged();
        TypeChanged();

        Controls.AddRange(new Control[]
        {
            title,
            lblType, _cboType,
            lblCitizen, _txtCitizenId, _btnLookupCitizen, _lblCitizenName,
            _spousePanel,
            lblAddress, _cboAddress,
            lblEvent, _dtpEvent,
            lblRegistration, _dtpRegistration,
            lblDocument, _txtDocumentNumber,
            lblNotes, _txtNotes,
            _lblDeathWarning, _lblError,
            _btnSave, _btnCancel
        });
    }

    private void TypeChanged() {
        var code = SelectedCode;
        _spousePanel.Visible = code == "MARRIAGE";
        _lblDeathWarning.Visible = code == "DEATH";
        _lblError.Visible = false;
    }

    private string SelectedCode
        => (_cboType.SelectedItem as RecordTypeOption)?.Code ?? "BIRTH";

    private async Task LoadAddressesAsync() {
        var result = await _addressService.GetActiveAsync();
        if (result.IsFailure || result.Value is null) {
            return;
        }

        _addresses.Clear();
        _addresses.AddRange(result.Value);
        _cboAddress.Items.Clear();

        foreach (var address in _addresses) {
            _cboAddress.Items.Add($"[{address.AddressCode}] {address.AddressText}");
        }

        if (_cboAddress.Items.Count > 0) {
            _cboAddress.SelectedIndex = 0;
        }
    }

    private async Task LookupCitizenAsync(bool spouse) {
        var box = spouse ? _txtSpouseId : _txtCitizenId;
        var label = spouse ? _lblSpouseName : _lblCitizenName;
        var nationalId = box.Text.Trim();

        if (nationalId.Length != 12 || !nationalId.All(char.IsDigit)) {
            ShowError(LanguageManager.T("National ID must be exactly 12 digits."));
            label.Text = string.Empty;
            if (spouse) {
                _spouseCitizenId = 0;
            } else {
                _citizenId = 0;
            }
            return;
        }

        var result = await _citizenService.SearchAsync(
            new CitizenSearchCriteria { NationalId = nationalId });

        var citizen = result.Value?.FirstOrDefault(c => c.NationalId == nationalId);
        if (citizen is null) {
            ShowError(LanguageManager.T("Citizen not found."));
            label.Text = string.Empty;
            if (spouse) {
                _spouseCitizenId = 0;
            } else {
                _citizenId = 0;
            }
            return;
        }

        HideError();
        label.Text = $"{citizen.FullName} ({citizen.Status})";

        if (spouse) {
            _spouseCitizenId = citizen.CitizenId;
        } else {
            _citizenId = citizen.CitizenId;
        }
    }

    private async Task SaveAsync() {
        HideError();

        if (_citizenId == 0) {
            ShowError(LanguageManager.T("Look up the citizen first."));
            return;
        }

        var isMarriage = SelectedCode == "MARRIAGE";

        if (isMarriage && _spouseCitizenId == 0) {
            ShowError(LanguageManager.T("Look up the spouse first."));
            return;
        }

        if (_cboAddress.SelectedIndex < 0 ||
            _cboAddress.SelectedIndex >= _addresses.Count) {
            ShowError(LanguageManager.T("Select an address."));
            return;
        }

        var dto = new RecordCivilStatusDto(
            SelectedCode,
            _citizenId,
            isMarriage ? _spouseCitizenId : null,
            _addresses[_cboAddress.SelectedIndex].AddressId,
            DateOnly.FromDateTime(_dtpEvent.Value),
            DateOnly.FromDateTime(_dtpRegistration.Value),
            string.IsNullOrWhiteSpace(_txtDocumentNumber.Text)
                ? null
                : _txtDocumentNumber.Text.Trim(),
            string.IsNullOrWhiteSpace(_txtNotes.Text)
                ? null
                : _txtNotes.Text.Trim());

        _btnSave.Enabled = false;

        try {
            var result = await _service.RegisterAsync(
                dto,
                _session.UserId ?? 1);

            if (result.IsFailure) {
                ShowError(string.Join(Environment.NewLine, result.Errors));
                return;
            }

            MessageBox.Show(
                LanguageManager.T("Civil status registered."),
                LanguageManager.T("Success"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        } catch (Exception ex) {
            ShowError(ex.Message);
        } finally {
            _btnSave.Enabled = true;
        }
    }

    private void ShowError(string message) {
        _lblError.Text = message;
        _lblError.Visible = true;
    }

    private void HideError() {
        _lblError.Text = string.Empty;
        _lblError.Visible = false;
    }
}