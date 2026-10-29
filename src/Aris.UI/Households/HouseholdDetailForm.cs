using Aris.Application.Citizens;
using Aris.Application.Common;
using Aris.Application.Households;
using Aris.Application.Households.Dtos;

namespace Aris.UI.Households;

/// <summary>Household profile with member tenure and address history (<c>HH-06</c>).</summary>
public sealed class HouseholdDetailForm : Form
{
    private readonly IHouseholdService _householdService;
    private readonly ICitizenService _citizenService;
    private readonly IUserSession _session;
    private readonly int _householdId;

    private readonly Label _info = new();
    private readonly DataGridView _membersGrid = new();
    private readonly DataGridView _addressGrid = new();
    private readonly Button _addButton = new();
    private readonly Button _changeHeadButton = new();
    private readonly Button _refreshButton = new();
    private readonly Button _closeButton = new();

    private HouseholdDetailDto? _detail;

    public HouseholdDetailForm(
        IHouseholdService householdService,
        ICitizenService citizenService,
        IUserSession session,
        int householdId)
    {
        _householdService = householdService;
        _citizenService = citizenService;
        _session = session;
        _householdId = householdId;

        Text = $"ARIS - Household #{householdId}";
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(920, 610);

        BuildLayout();
        Load += async (_, _) => await LoadDetailAsync();
    }

    private void BuildLayout()
    {
        var title = new Label
        {
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

        var membersLabel = new Label
        {
            Text = "Membership history",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(20, 122)
        };

        ConfigureGrid(_membersGrid, new Point(20, 145), new Size(870, 200));

        var addressLabel = new Label
        {
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

        _refreshButton.Text = "Refresh";
        _refreshButton.Location = new Point(280, 540);
        _refreshButton.Size = new Size(100, 34);
        _refreshButton.Click += async (_, _) => await LoadDetailAsync();

        _closeButton.Text = "Close";
        _closeButton.Location = new Point(790, 540);
        _closeButton.Size = new Size(100, 34);
        _closeButton.DialogResult = DialogResult.Cancel;

        Controls.AddRange(new Control[]
        {
            title, _info, membersLabel, _membersGrid, addressLabel, _addressGrid,
            _addButton, _changeHeadButton, _refreshButton, _closeButton
        });
    }

    private static void ConfigureGrid(DataGridView grid, Point location, Size size)
    {
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

    private async Task LoadDetailAsync()
    {
        try
        {
            var result = await _householdService.GetByIdAsync(_householdId);

            if (result.IsFailure || result.Value is null)
            {
                MessageBox.Show(
                    string.Join(Environment.NewLine, result.Errors.DefaultIfEmpty("Household not found.")),
                    "Cannot load household",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                Close();
                return;
            }

            _detail = result.Value;

            _info.Text =
                $"Status: {_detail.Status}    Created: {_detail.CreatedAt:yyyy-MM-dd HH:mm}" +
                (_detail.InactivatedAt is { } inactive ? $"    Inactivated: {inactive:yyyy-MM-dd HH:mm}" : "") +
                Environment.NewLine +
                $"Head: {_detail.HeadFullName ?? "(none)"}    Address: {_detail.CurrentAddressText ?? "(none)"}";

            _membersGrid.DataSource = _detail.Members.ToList();
            _addressGrid.DataSource = _detail.AddressHistory.ToList();

            var isActive = _detail.Status == "ACTIVE";
            var hasOtherMember = _detail.Members.Any(m => m.Status == "ACTIVE" && m.Role == "MEMBER");

            _addButton.Enabled = isActive;
            _changeHeadButton.Enabled = isActive && hasOtherMember;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Unexpected error while loading the household: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private async void AddButton_Click(object? sender, EventArgs e)
    {
        using var form = new AddMemberForm(
            _householdService, _citizenService, _session, _householdId);

        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await LoadDetailAsync();
        }
    }

    private async void ChangeHeadButton_Click(object? sender, EventArgs e)
    {
        if (_detail is null)
        {
            return;
        }

        var candidates = _detail.Members
            .Where(m => m.Status == "ACTIVE" && m.Role == "MEMBER")
            .ToList();

        if (candidates.Count == 0)
        {
            MessageBox.Show(
                "There is no active MEMBER to promote.",
                "No candidate",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        using var form = new ChangeHeadForm(
            _householdService, _session, _householdId, candidates);

        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await LoadDetailAsync();
        }
    }
}
