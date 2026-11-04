using Aris.Application.Addresses;
using Aris.Application.Addresses.Dtos;
using Aris.Application.Common;
using Aris.Application.Households;
using Aris.Application.Households.Dtos;
using Aris.Application.Rectifications;
using Aris.Application.Rectifications.Dtos;
using Aris.UI.Localization;

namespace Aris.UI.Rectifications;

/// <summary>
/// Voids one tenure row and records a linked replacement (<c>HIST-01</c>).
/// </summary>
/// <remarks>
/// One dialog serves memberships, household addresses and residences. For an
/// <c>ACTIVE</c> source only the start date is editable; the other fields are disabled
/// because current-state changes belong to transfer, change-head, or deactivation.
/// </remarks>
public sealed class RectifyTenureForm : Form {
    private readonly IRectificationService _rectifications;
    private readonly IAddressService _addresses;
    private readonly IHouseholdService _households;
    private readonly IUserSession _session;
    private readonly RectifyTenureRequest _request;
    private readonly bool _isActive;

    private readonly Label _sourceLabel = new();
    private readonly Label _warning = new();
    private readonly Label _addressLabel = new();
    private readonly Label _householdLabel = new();
    private readonly Label _roleLabel = new();
    private readonly Label _startDateLabel = new();
    private readonly Label _endDateLabel = new();
    private readonly Label _reasonLabel = new();
    private readonly ComboBox _address = new();
    private readonly ComboBox _household = new();
    private readonly ComboBox _role = new();
    private readonly DateTimePicker _startDate = new();
    private readonly DateTimePicker _endDate = new();
    private readonly TextBox _reason = new();
    private readonly Button _save = new();
    private readonly Button _cancel = new();

    public RectifyTenureForm(
        IRectificationService rectifications,
        IAddressService addresses,
        IHouseholdService households,
        IUserSession session,
        RectifyTenureRequest request) {
        _rectifications = rectifications;
        _addresses = addresses;
        _households = households;
        _session = session;
        _request = request;
        _isActive = request.Status == "ACTIVE";

        Text = request.Kind switch {
            RectifyTenureKind.Membership => LanguageManager.T("ARIS - Rectify membership"),
            RectifyTenureKind.HouseholdAddress => LanguageManager.T("ARIS - Rectify household address"),
            RectifyTenureKind.Residence => LanguageManager.T("ARIS - Rectify residence"),
            _ => LanguageManager.T("Correct history")
        };

        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(620, 500);
        AutoScaleMode = AutoScaleMode.Font;

        BuildLayout();
        Aris.UI.UiTheme.ApplyScreen(this);
        Load += async (_, _) => await LoadChoicesAsync();
    }

    private void BuildLayout() {
        var title = new Label {
            Text = Text,
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = Color.DarkSlateGray,
            AutoSize = true,
            Location = new Point(20, 15)
        };

        _sourceLabel.AutoSize = false;
        _sourceLabel.Size = new Size(580, 44);
        _sourceLabel.Location = new Point(20, 52);
        _sourceLabel.ForeColor = Color.DimGray;
        _sourceLabel.Text = BuildSourceText();

        _warning.AutoSize = false;
        _warning.Size = new Size(580, 44);
        _warning.Location = new Point(20, 96);
        _warning.ForeColor = Color.DarkOrange;
        _warning.Text = LanguageManager.T(
            "Active records can only have their start date corrected. " +
            "Current-state changes go through transfer, change head, or deactivation.");
        _warning.Visible = _isActive;

        ConfigureLabel(_addressLabel, "Address", 150);
        ConfigureCombo(_address, 147);
        ConfigureLabel(_householdLabel, "Household", 150);
        ConfigureCombo(_household, 147);
        ConfigureLabel(_roleLabel, "Role", 185);
        ConfigureCombo(_role, 182);

        // Only the pickers that belong to this kind are visible.
        var isAddressKind = _request.Kind != RectifyTenureKind.Membership;
        _addressLabel.Visible = isAddressKind;
        _address.Visible = isAddressKind;
        _householdLabel.Visible = !isAddressKind;
        _household.Visible = !isAddressKind;
        _roleLabel.Visible = !isAddressKind;
        _role.Visible = !isAddressKind;

        ConfigureLabel(_startDateLabel, "Start date", 220);
        _startDate.Location = new Point(170, 217);
        _startDate.Size = new Size(430, 27);
        _startDate.Format = DateTimePickerFormat.Custom;
        _startDate.CustomFormat = "yyyy-MM-dd";
        _startDate.Value = _request.StartDate.ToDateTime(TimeOnly.MinValue);

        ConfigureLabel(_endDateLabel, "End date", 255);
        _endDate.Location = new Point(170, 252);
        _endDate.Size = new Size(430, 27);
        _endDate.Format = DateTimePickerFormat.Custom;
        _endDate.CustomFormat = "yyyy-MM-dd";
        _endDate.Value = (_request.EndDate ?? _request.StartDate).ToDateTime(TimeOnly.MinValue);
        _endDate.Enabled = !_isActive;
        _endDateLabel.Enabled = !_isActive;

        ConfigureLabel(_reasonLabel, "Reason (required)", 290);
        _reason.Location = new Point(20, 315);
        _reason.Size = new Size(580, 85);
        _reason.Multiline = true;
        _reason.ScrollBars = ScrollBars.Vertical;
        _reason.MaxLength = 500;

        _address.DropDownStyle = ComboBoxStyle.DropDownList;
        _household.DropDownStyle = ComboBoxStyle.DropDownList;
        _role.DropDownStyle = ComboBoxStyle.DropDownList;
        _role.Items.AddRange(new object[] { "HEAD", "MEMBER" });
        _role.SelectedItem = _request.Role ?? "MEMBER";

        // An ACTIVE row keeps its identity fields: only the start date may change.
        if (_isActive) {
            _address.Enabled = false;
            _household.Enabled = false;
            _role.Enabled = false;
        }

        _save.Text = LanguageManager.T("Save correction");
        _save.Location = new Point(350, 430);
        _save.Size = new Size(140, 34);
        _save.BackColor = Color.SteelBlue;
        _save.ForeColor = Color.White;
        _save.FlatStyle = FlatStyle.Flat;
        _save.Click += SaveButton_Click;

        _cancel.Text = LanguageManager.T("Cancel");
        _cancel.Location = new Point(500, 430);
        _cancel.Size = new Size(100, 34);
        _cancel.DialogResult = DialogResult.Cancel;

        AcceptButton = _save;
        CancelButton = _cancel;

        Controls.AddRange(new Control[] {
            title, _sourceLabel, _warning,
            _addressLabel, _address, _householdLabel, _household, _roleLabel, _role,
            _startDateLabel, _startDate, _endDateLabel, _endDate,
            _reasonLabel, _reason, _save, _cancel
        });
    }

    private static void ConfigureLabel(Label label, string text, int y) {
        label.Text = LanguageManager.T(text);
        label.AutoSize = true;
        label.Location = new Point(20, y);
    }

    private static void ConfigureCombo(ComboBox combo, int y) {
        combo.Location = new Point(170, y);
        combo.Size = new Size(430, 27);
    }

    private string BuildSourceText() {
        var kind = _request.Kind switch {
            RectifyTenureKind.Membership => LanguageManager.T("Membership"),
            RectifyTenureKind.HouseholdAddress => LanguageManager.T("Household address"),
            RectifyTenureKind.Residence => LanguageManager.T("Residence"),
            _ => LanguageManager.T("Record")
        };

        var owner = string.IsNullOrWhiteSpace(_request.OwnerLabel)
            ? string.Empty
            : $" - {_request.OwnerLabel}";

        var end = _request.EndDate?.ToString("yyyy-MM-dd") ?? LanguageManager.T("present");

        return $"{LanguageManager.T("Source row:")} {kind} #{_request.SourceId}{owner}" +
            Environment.NewLine +
            $"{_request.Status}  {_request.StartDate:yyyy-MM-dd} -> {end}";
    }

    private async Task LoadChoicesAsync() {
        try {
            if (_request.Kind == RectifyTenureKind.Membership) {
                await LoadHouseholdsAsync();
            } else {
                await LoadAddressesAsync();
            }
        } catch (Exception ex) {
            _save.Enabled = false;
            MessageBox.Show(
                $"Unexpected error while loading the pickers: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private async Task LoadHouseholdsAsync() {
        var result = await _households.SearchAsync(new HouseholdSearchCriteria());
        if (result.IsFailure) {
            ShowLoadError(result.Errors);
            return;
        }

        var choices = result.Value?
            .Select(h => new Choice(h.HouseholdId, $"{h.DisplayName} ({h.Status})"))
            .ToList() ?? new List<Choice>();

        if (_request.HouseholdId is int householdId && choices.All(c => c.Id != householdId)) {
            var detail = await _households.GetByIdAsync(householdId);
            if (detail.Value is { } d) {
                choices.Insert(0, new Choice(
                    d.HouseholdId,
                    $"#{d.HouseholdId} - {d.HeadFullName ?? LanguageManager.T("(none)")} ({d.Status})"));
            }
        }

        _household.DisplayMember = nameof(Choice.Label);
        _household.ValueMember = nameof(Choice.Id);
        _household.DataSource = choices;

        if (_request.HouseholdId is int currentId) {
            _household.SelectedValue = currentId;
        }

        if (_household.SelectedIndex < 0 && choices.Count > 0) {
            _household.SelectedIndex = 0;
        }
    }

    private async Task LoadAddressesAsync() {
        var result = await _addresses.SearchAsync(new AddressSearchCriteria());
        if (result.IsFailure) {
            ShowLoadError(result.Errors);
            return;
        }

        var choices = result.Value?
            .Select(a => new Choice(a.AddressId, $"{a.AddressText} ({a.Status})"))
            .ToList() ?? new List<Choice>();

        if (_request.AddressId is int addressId && choices.All(c => c.Id != addressId)) {
            var detail = await _addresses.GetByIdAsync(addressId);
            if (detail.Value is { } d) {
                choices.Insert(0, new Choice(d.AddressId, $"{d.AddressText} ({d.Status})"));
            }
        }

        _address.DisplayMember = nameof(Choice.Label);
        _address.ValueMember = nameof(Choice.Id);
        _address.DataSource = choices;

        if (_request.AddressId is int currentId) {
            _address.SelectedValue = currentId;
        }

        if (_address.SelectedIndex < 0 && choices.Count > 0) {
            _address.SelectedIndex = 0;
        }
    }

    private void ShowLoadError(IReadOnlyList<string> errors) {
        _save.Enabled = false;
        MessageBox.Show(
            string.Join(Environment.NewLine, errors),
            LanguageManager.T("Cannot rectify history"),
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }

    private async void SaveButton_Click(object? sender, EventArgs e) {
        if (_session.UserId is not int userId) {
            MessageBox.Show(
                "Your session has expired. Sign in again.",
                "Session expired",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(_reason.Text)) {
            MessageBox.Show(
                LanguageManager.T("Explain why the row is wrong."),
                LanguageManager.T("Cannot rectify history"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            _reason.Focus();
            return;
        }

        _save.Enabled = false;

        try {
            var result = _request.Kind switch {
                RectifyTenureKind.Membership => await RectifyMembershipAsync(userId),
                RectifyTenureKind.HouseholdAddress => await RectifyHouseholdAddressAsync(userId),
                RectifyTenureKind.Residence => await RectifyResidenceAsync(userId),
                _ => Result.Failure("Unsupported tenure kind.")
            };

            if (result.IsFailure) {
                MessageBox.Show(
                    string.Join(Environment.NewLine, result.Errors),
                    LanguageManager.T("Cannot rectify history"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show(
                LanguageManager.T(
                    "The row was corrected. The old row is now VOID and the replacement is linked to it."),
                "Success",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        } catch (Exception ex) {
            MessageBox.Show(
                $"Unexpected error while correcting the row: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        } finally {
            _save.Enabled = true;
        }
    }

    private async Task<Result> RectifyMembershipAsync(int userId) {
        if (_household.SelectedValue is not int householdId) {
            return Result.Failure(LanguageManager.T("Select a household."));
        }

        var role = _role.SelectedItem?.ToString() ?? string.Empty;
        if (role is not ("HEAD" or "MEMBER")) {
            return Result.Failure("Select a role.");
        }

        return await _rectifications.RectifyMembershipAsync(new RectifyMembershipDto {
            MembershipId = _request.SourceId,
            HouseholdId = householdId,
            Role = role,
            StartDate = DateOnly.FromDateTime(_startDate.Value),
            EndDate = _isActive ? null : DateOnly.FromDateTime(_endDate.Value),
            Reason = _reason.Text.Trim()
        }, userId);
    }

    private async Task<Result> RectifyHouseholdAddressAsync(int userId) {
        if (_address.SelectedValue is not int addressId) {
            return Result.Failure(LanguageManager.T("Select an address."));
        }

        return await _rectifications.RectifyHouseholdAddressAsync(new RectifyHouseholdAddressDto {
            HouseholdAddressId = _request.SourceId,
            AddressId = addressId,
            StartDate = DateOnly.FromDateTime(_startDate.Value),
            EndDate = _isActive ? null : DateOnly.FromDateTime(_endDate.Value),
            Reason = _reason.Text.Trim()
        }, userId);
    }

    private async Task<Result> RectifyResidenceAsync(int userId) {
        if (_address.SelectedValue is not int addressId) {
            return Result.Failure(LanguageManager.T("Select an address."));
        }

        return await _rectifications.RectifyResidenceAsync(new RectifyResidenceDto {
            ResidenceId = _request.SourceId,
            AddressId = addressId,
            StartDate = DateOnly.FromDateTime(_startDate.Value),
            EndDate = _isActive ? null : DateOnly.FromDateTime(_endDate.Value),
            Reason = _reason.Text.Trim()
        }, userId);
    }

    /// <summary>Compact row used by the address and household pickers.</summary>
    private sealed record Choice(int Id, string Label);
}