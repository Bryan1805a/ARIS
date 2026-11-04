using Aris.Application.Addresses;
using Aris.Application.Citizens;
using Aris.Application.Common;
using Aris.Application.Households;
using Aris.Application.Households.Dtos;
using Aris.Application.Rectifications;
using Aris.UI.Localization;
using Aris.UI.Rectifications;

namespace Aris.UI.Households;

/// <summary>
/// Household profile with member tenure and address history (<c>HH-06</c>) and the
/// rectification entry points (<c>HIST-01</c>).
/// </summary>
public sealed class HouseholdDetailForm : Form {
    private readonly IHouseholdService _householdService;
    private readonly ICitizenService _citizenService;
    private readonly IRectificationService _rectifications;
    private readonly IAddressService _addresses;
    private readonly IUserSession _session;
    private readonly int _householdId;

    private readonly Label _info = new();
    private readonly DataGridView _membersGrid = new();
    private readonly DataGridView _addressGrid = new();
    private readonly Button _addButton = new();
    private readonly Button _changeHeadButton = new();
    private readonly Button _rectifyMemberButton = new();
    private readonly Button _refreshButton = new();
    private readonly Button _rectifyAddressButton = new();
    private readonly Button _closeButton = new();

    private HouseholdDetailDto? _detail;

    public HouseholdDetailForm(
        IHouseholdService householdService,
        ICitizenService citizenService,
        IRectificationService rectifications,
        IAddressService addresses,
        IUserSession session,
        int householdId) {
        _householdService = householdService;
        _citizenService = citizenService;
        _rectifications = rectifications;
        _addresses = addresses;
        _session = session;
        _householdId = householdId;

        Text = $"{LanguageManager.T("ARIS - Household")} #{householdId}";
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(920, 610);

        BuildLayout();
        Aris.UI.UiTheme.ApplyScreen(this);
        // Loading is deferred until the form is displayed so the dialog opens
        // without blocking construction while the household is fetched.
        Load += async (_, _) => await LoadDetailAsync();
    }

    private void BuildLayout() {
        // The form uses fixed sections so membership and address histories
        // remain visible together while the action buttons stay at the bottom.
        var title = new Label {
            Text = $"Household #{_householdId}",
            Font = new Font("Segoe UI", 15F, FontStyle.Bold),
            ForeColor = Color.DarkSlateGray,
            AutoSize = true,
            Location = new Point(20, 15)
        };

        _info.AutoSize = false;
        _info.Size = new Size(870, 65);
        _info.Location = new Point(20, 52);
        _info.ForeColor = Color.DimGray;

        var membersLabel = new Label {
            Text = "Membership history",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(20, 122)
        };

        ConfigureGrid(_membersGrid, new Point(20, 145), new Size(870, 200));

        var addressLabel = new Label {
            Text = "Address history",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(20, 355)
        };

        ConfigureGrid(_addressGrid, new Point(20, 378), new Size(870, 130));

        _addButton.Text = "Add member";
        _addButton.Location = new Point(20, 540);
        _addButton.Size = new Size(120, 34);
        _addButton.BackColor = Color.SteelBlue;
        _addButton.ForeColor = Color.White;
        _addButton.FlatStyle = FlatStyle.Flat;
        _addButton.Click += AddButton_Click;

        _changeHeadButton.Text = "Change head";
        _changeHeadButton.Location = new Point(150, 540);
        _changeHeadButton.Size = new Size(120, 34);
        _changeHeadButton.Click += ChangeHeadButton_Click;

        _rectifyMemberButton.Text = "Rectify member";
        _rectifyMemberButton.Location = new Point(280, 540);
        _rectifyMemberButton.Size = new Size(140, 34);
        _rectifyMemberButton.Click += RectifyMemberButton_Click;

        _refreshButton.Text = "Refresh";
        _refreshButton.Location = new Point(430, 540);
        _refreshButton.Size = new Size(90, 34);
        _refreshButton.Click += async (_, _) => await LoadDetailAsync();

        _rectifyAddressButton.Text = "Rectify address";
        _rectifyAddressButton.Location = new Point(530, 540);
        _rectifyAddressButton.Size = new Size(140, 34);
        _rectifyAddressButton.Click += RectifyAddressButton_Click;

        _closeButton.Text = "Close";
        _closeButton.Location = new Point(790, 540);
        _closeButton.Size = new Size(100, 34);
        _closeButton.DialogResult = DialogResult.Cancel;

        Controls.AddRange(new Control[]
        {
            title, _info, membersLabel, _membersGrid, addressLabel, _addressGrid,
            _addButton, _changeHeadButton, _rectifyMemberButton, _refreshButton,
            _rectifyAddressButton, _closeButton
        });
    }

    private static void ConfigureGrid(DataGridView grid, Point location, Size size) {
        // Histories are read-only views; edits must go through the dedicated
        // workflows so validation, auditing, and authorization are preserved.
        grid.Location = location;
        grid.Size = size;
        grid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.ReadOnly = true;
        grid.MultiSelect = false;
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.BackgroundColor = Color.White;
    }

    private async Task LoadDetailAsync() {
        try {
            var result = await _householdService.GetByIdAsync(_householdId);

            if (result.IsFailure || result.Value is null) {
                MessageBox.Show(
                    string.Join(Environment.NewLine, result.Errors.DefaultIfEmpty("Household not found.")),
                    "Cannot load household",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                Close();
                return;
            }

            // Replace the cached snapshot on every load. Button availability and
            // selected rows must reflect changes made in child dialogs.
            _detail = result.Value;

            _info.Text =
                $"{LanguageManager.T("Status:")} {_detail.Status}    " +
                $"{LanguageManager.T("Created:")} {_detail.CreatedAt:yyyy-MM-dd HH:mm}" +
                (_detail.InactivatedAt is { } inactive
                    ? $"    {LanguageManager.T("Inactivated:")} {inactive:yyyy-MM-dd HH:mm}"
                    : "") +
                Environment.NewLine +
                $"{LanguageManager.T("Head:")} {_detail.HeadFullName ?? "(none)"}    " +
                $"{LanguageManager.T("Address:")} {_detail.CurrentAddressText ?? "(none)"}";

            _membersGrid.DataSource = _detail.Members.ToList();
            _addressGrid.DataSource = _detail.AddressHistory.ToList();

            var isActive = _detail.Status == "ACTIVE";
            // Changing the head requires another active MEMBER; a household
            // with no eligible candidate cannot start that workflow.
            var hasOtherMember = _detail.Members.Any(m => m.Status == "ACTIVE" && m.Role == "MEMBER");

            _addButton.Enabled = isActive;
            _changeHeadButton.Enabled = isActive && hasOtherMember;
            _rectifyMemberButton.Enabled = _detail.Members.Count > 0;
            _rectifyAddressButton.Enabled = _detail.AddressHistory.Count > 0;

            if (_membersGrid.Rows.Count > 0 && _membersGrid.Columns.Count > 0) {
                // Select the first row after rebinding so keyboard actions and
                // rectification have a predictable initial target.
                _membersGrid.CurrentCell = _membersGrid.Rows[0].Cells[0];
            }

            if (_addressGrid.Rows.Count > 0 && _addressGrid.Columns.Count > 0) {
                _addressGrid.CurrentCell = _addressGrid.Rows[0].Cells[0];
            }
        } catch (Exception ex) {
            // Keep the dialog open so the user can retry with Refresh after a
            // transient service or database failure.
            MessageBox.Show(
                $"Unexpected error while loading the household: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private async void AddButton_Click(object? sender, EventArgs e) {
        // The child dialog owns input validation; reload only after it commits.
        using var form = new AddMemberForm(
            _householdService, _citizenService, _session, _householdId);

        if (form.ShowDialog(this) == DialogResult.OK) {
            await LoadDetailAsync();
        }
    }

    private async void ChangeHeadButton_Click(object? sender, EventArgs e) {
        if (_detail is null) {
            return;
        }

        var candidates = _detail.Members
            .Where(m => m.Status == "ACTIVE" && m.Role == "MEMBER")
            .ToList();

        if (candidates.Count == 0) {
            MessageBox.Show(
                "There is no active MEMBER to promote.",
                "No candidate",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        // Pass only eligible members to prevent the change-head dialog from
        // offering inactive records or the current head.
        using var form = new ChangeHeadForm(
            _householdService, _session, _householdId, candidates);

        if (form.ShowDialog(this) == DialogResult.OK) {
            await LoadDetailAsync();
        }
    }

    private async void RectifyMemberButton_Click(object? sender, EventArgs e) {
        if (_detail is null) {
            return;
        }

        var selected = _membersGrid.CurrentRow?.DataBoundItem as HouseholdMemberDto;
        if (selected is null) {
            MessageBox.Show(
                LanguageManager.T("Select a row first."),
                "No selection",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        // Convert the selected grid row into the common rectification request
        // used by the shared tenure dialog.
        var request = new RectifyTenureRequest {
            Kind = RectifyTenureKind.Membership,
            SourceId = selected.MembershipId,
            Status = selected.Status,
            StartDate = selected.StartDate,
            EndDate = selected.EndDate,
            HouseholdId = _householdId,
            Role = selected.Role,
            OwnerLabel = $"{selected.FullName} ({selected.NationalId})"
        };

        using var form = new RectifyTenureForm(
            _rectifications, _addresses, _householdService, _session, request);

        if (form.ShowDialog(this) == DialogResult.OK) {
            await LoadDetailAsync();
        }
    }

    private async void RectifyAddressButton_Click(object? sender, EventArgs e) {
        if (_detail is null) {
            return;
        }

        var selected = _addressGrid.CurrentRow?.DataBoundItem as HouseholdAddressHistoryDto;
        if (selected is null) {
            MessageBox.Show(
                LanguageManager.T("Select a row first."),
                "No selection",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        // Address rectification uses the same dialog as membership rectification,
        // but identifies the selected household-address history row instead.
        var request = new RectifyTenureRequest {
            Kind = RectifyTenureKind.HouseholdAddress,
            SourceId = selected.HouseholdAddressId,
            Status = selected.Status,
            StartDate = selected.StartDate,
            EndDate = selected.EndDate,
            HouseholdId = _householdId,
            AddressId = selected.AddressId,
            OwnerLabel = selected.AddressText
        };

        using var form = new RectifyTenureForm(
            _rectifications, _addresses, _householdService, _session, request);

        if (form.ShowDialog(this) == DialogResult.OK) {
            await LoadDetailAsync();
        }
    }
}