using System.Text;

using Aris.Application.Addresses;
using Aris.Application.Citizens;
using Aris.Application.Citizens.Dtos;
using Aris.Application.Common;
using Aris.Application.Households;
using Aris.Application.Rectifications;
using Aris.UI.Localization;
using Aris.UI.Rectifications;

namespace Aris.UI.Citizens;

/// <summary>
/// Citizen profile with household, canonical address and residency timeline
/// (<c>CIT-02</c>), plus the residence rectification entry point (<c>HIST-01</c>).
/// </summary>
public sealed partial class CitizenDetailForm : Form {
    private readonly ICitizenService _citizenService;
    private readonly IRectificationService _rectifications;
    private readonly IAddressService _addresses;
    private readonly IHouseholdService _households;
    private readonly IUserSession _session;
    private readonly int _citizenId;
    private readonly TextBox _summary = new();
    private readonly DataGridView _residencyGrid = new();
    private readonly Button _rectifyResidenceButton = new();

    private CitizenDetailDto? _detail;

    public CitizenDetailForm(
        ICitizenService citizenService,
        IRectificationService rectifications,
        IAddressService addresses,
        IHouseholdService households,
        IUserSession session,
        int citizenId) {
        _citizenService = citizenService;
        _rectifications = rectifications;
        _addresses = addresses;
        _households = households;
        _session = session;
        _citizenId = citizenId;

        InitializeComponent();

        Text = "ARIS - Citizen detail";
        label6.Text = "Citizen Detail";
        cmbGender.Items.Clear();
        cmbGender.Items.AddRange(new object[] { "MALE", "FEMALE", "OTHER" });

        ClientSize = new Size(1040, 630);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        label6.Left = (ClientSize.Width - label6.Width) / 2;

        _summary.Location = new Point(420, 70);
        _summary.Size = new Size(590, 190);
        _summary.Multiline = true;
        _summary.ReadOnly = true;
        _summary.ScrollBars = ScrollBars.Vertical;
        _summary.BackColor = Color.White;
        Controls.Add(_summary);

        var residencyLabel = new Label {
            Text = "Residency history",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(420, 268)
        };

        _residencyGrid.Location = new Point(420, 295);
        _residencyGrid.Size = new Size(590, 230);
        _residencyGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _residencyGrid.AllowUserToAddRows = false;
        _residencyGrid.AllowUserToDeleteRows = false;
        _residencyGrid.ReadOnly = true;
        _residencyGrid.MultiSelect = false;
        _residencyGrid.RowHeadersVisible = false;
        _residencyGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _residencyGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _residencyGrid.BackgroundColor = Color.White;

        _rectifyResidenceButton.Text = "Rectify residence";
        _rectifyResidenceButton.Location = new Point(840, 560);
        _rectifyResidenceButton.Size = new Size(170, 34);
        _rectifyResidenceButton.BackColor = Color.SteelBlue;
        _rectifyResidenceButton.ForeColor = Color.White;
        _rectifyResidenceButton.FlatStyle = FlatStyle.Flat;
        _rectifyResidenceButton.Click += RectifyResidenceButton_Click;

        // The original buttons sit where the grid now lives; move them to the bottom row.
        btnSave.Location = new Point(20, 560);
        btnSave.Size = new Size(168, 45);
        btnDeactivate.Location = new Point(200, 560);
        btnDeactivate.Size = new Size(210, 45);

        btnSave.Text = "Update information";
        btnDeactivate.Text = "Deactivate citizen";

        Controls.Add(residencyLabel);
        Controls.Add(_residencyGrid);
        Controls.Add(_rectifyResidenceButton);

        btnSave.Click += SaveButton_Click;
        btnDeactivate.Click += DeactivateButton_Click;
        Aris.UI.UiTheme.ApplyScreen(this);

        // Defer the initial service call until the form is shown so construction
        // remains synchronous and the dialog can report load failures itself.
        Load += async (_, _) => await LoadCitizenAsync();
    }

    private async Task LoadCitizenAsync() {
        var result = await _citizenService.GetByIdAsync(_citizenId);

        if (result.IsFailure || result.Value is null) {
            MessageBox.Show(
                string.Join(Environment.NewLine, result.Errors.DefaultIfEmpty("Citizen not found.")),
                "Cannot load citizen",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            Close();
            return;
        }

        // Keep a fresh detail snapshot because child dialogs can change the
        // citizen or residency data while this form is open.
        _detail = result.Value;

        txtNationalId.Text = _detail.NationalId;
        txtNationalId.ReadOnly = true;
        txtFullName.Text = _detail.FullName;
        dtpDateOfBirth.Value = _detail.DateOfBirth.ToDateTime(TimeOnly.MinValue);
        cmbGender.SelectedItem = _detail.Gender;
        txtStatus.Text = _detail.Status;
        txtStatus.ReadOnly = true;

        _summary.Text = BuildSummary(_detail);
        Text = $"ARIS - {_detail.FullName}";

        // The grid is a read-only history view; rectification is performed
        // through the dedicated workflow so validation and audit rules apply.
        _residencyGrid.DataSource = _detail.ResidencyTimeline.ToList();
        _rectifyResidenceButton.Enabled = _detail.ResidencyTimeline.Count > 0;

        if (_residencyGrid.Rows.Count > 0 && _residencyGrid.Columns.Count > 0) {
            // Give the user a predictable initial selection after rebinding.
            _residencyGrid.CurrentCell = _residencyGrid.Rows[0].Cells[0];
        }

        var isInactive = _detail.Status == "INACTIVE";
        // Inactive citizens are historical records and cannot be edited or
        // deactivated a second time.
        btnSave.Enabled = !isInactive;
        btnDeactivate.Enabled = !isInactive;
    }

    private static string BuildSummary(CitizenDetailDto detail) {
        var builder = new StringBuilder();

        // Keep the profile summary in one localized block so the main form
        // shows current household context without additional service calls.
        builder.AppendLine($"{LanguageManager.T("Created:")}    {detail.CreatedAt:yyyy-MM-dd HH:mm}");
        builder.AppendLine($"{LanguageManager.T("Updated:")}    {detail.UpdatedAt:yyyy-MM-dd HH:mm}");
        builder.AppendLine($"{LanguageManager.T("Deactivated:")} {detail.DeactivatedAt?.ToString("yyyy-MM-dd HH:mm") ?? " -"}");
        builder.AppendLine();
        builder.AppendLine($"{LanguageManager.T("Household:")}  {detail.HouseholdId?.ToString() ?? "none"}");
        builder.AppendLine($"{LanguageManager.T("Role:")}       {detail.HouseholdRole ?? " -"}");
        builder.AppendLine($"{LanguageManager.T("Address:")}    {detail.CurrentAddressText ?? "none"}");
        builder.AppendLine();

        builder.AppendLine(LanguageManager.T("Household members:"));
        if (detail.HouseholdMembers.Count == 0) {
            builder.AppendLine($"  {LanguageManager.T("(none)")}");
        } else {
            foreach (var member in detail.HouseholdMembers) {
                builder.AppendLine($"  {member.FullName} ({member.NationalId}) {member.Status}");
            }
        }

        return builder.ToString();
    }

    private async void SaveButton_Click(object? sender, EventArgs e) {
        if (_detail is null) {
            return;
        }

        // Editing is delegated to the specialized dialog; reload only after
        // that dialog reports a successful save.
        using var form = new CitizenEditForm(_citizenService, _session, _detail);

        if (form.ShowDialog(this) == DialogResult.OK) {
            await LoadCitizenAsync();
        }
    }

    private async void RectifyResidenceButton_Click(object? sender, EventArgs e) {
        if (_detail is null) {
            return;
        }

        var selected = _residencyGrid.CurrentRow?.DataBoundItem as ResidencyPeriodDto;
        if (selected is null) {
            MessageBox.Show(
                LanguageManager.T("Select a row first."),
                "No selection",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        // Convert the selected history row into the common request understood
        // by the residence, membership, and household-address workflows.
        var request = new RectifyTenureRequest {
            Kind = RectifyTenureKind.Residence,
            SourceId = selected.ResidenceId,
            Status = selected.Status,
            StartDate = selected.StartDate,
            EndDate = selected.EndDate,
            AddressId = selected.AddressId,
            OwnerLabel = $"{_detail.FullName} ({_detail.NationalId})"
        };

        using var form = new RectifyTenureForm(
            _rectifications, _addresses, _households, _session, request);

        if (form.ShowDialog(this) == DialogResult.OK) {
            await LoadCitizenAsync();
        }
    }

    private async void DeactivateButton_Click(object? sender, EventArgs e) {
        if (_detail is null || _detail.Status == "INACTIVE") {
            return;
        }

        // The service requires the authenticated operator for audit attribution.
        if (_session.UserId is not int userId) {
            MessageBox.Show(
                "Your session has expired. Sign in again.",
                "Session expired",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        int? successorId = null;
        var otherMembers = _detail.HouseholdMembers
            .Where(m => m.CitizenId != _detail.CitizenId)
            .ToList();

        if (_detail.HouseholdRole == "HEAD" && otherMembers.Count > 0) {
            // A household head must nominate a successor before deactivation
            // when another household member is available.
            using var picker = new SuccessorHeadForm(otherMembers);

            if (picker.ShowDialog(this) != DialogResult.OK) {
                return;
            }

            successorId = picker.SelectedCitizenId;
        }

        var consequence = _detail.HouseholdRole == "HEAD" && otherMembers.Count == 0
            ? " This is the last member, so the household and its address will be inactivated."
            : string.Empty;

        // Explain cascading household consequences before requesting consent.
        var confirmation = MessageBox.Show(
            $"Deactivate {_detail.FullName} ({_detail.NationalId})?{consequence}",
            "Deactivate citizen",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirmation != DialogResult.Yes) {
            return;
        }

        // Prevent duplicate clicks while the deactivation transaction is running.
        btnDeactivate.Enabled = false;

        try {
            var result = await _citizenService.DeactivateCitizenAsync(
                _detail.CitizenId, userId, successorId);

            if (result.IsFailure) {
                MessageBox.Show(
                    string.Join(Environment.NewLine, result.Errors),
                    "Cannot deactivate citizen",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // Closing with OK tells the owner form that this citizen list may
            // need to be refreshed after the successful state change.
            MessageBox.Show(
                "The citizen was deactivated.",
                "Success",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        } catch (Exception ex) {
            MessageBox.Show(
                $"Unexpected error while deactivating the citizen: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        } finally {
            btnDeactivate.Enabled = true;
        }
    }
}