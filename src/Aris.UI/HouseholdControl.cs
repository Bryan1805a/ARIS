using Aris.Application.Addresses;
using Aris.Application.Citizens;
using Aris.Application.Common;
using Aris.Application.Households;
using Aris.Application.Households.Dtos;
using Aris.UI.Households;

namespace Aris.UI;

/// <summary>
/// Household directory: search, register, and open the household profile
/// (<c>HH-01</c>, <c>HH-06</c>, <c>SRCH-02</c>).
/// </summary>
public partial class HouseholdControl : UserControl
{
    private readonly IHouseholdService _householdService;
    private readonly ICitizenService _citizenService;
    private readonly IAddressService _addressService;
    private readonly IUserSession _session;

    private readonly TextBox _search = new();
    private readonly ComboBox _status = new();
    private readonly DataGridView _grid = new();
    private readonly Button _searchButton = new();
    private readonly Button _registerButton = new();
    private readonly Button _viewButton = new();
    private readonly Button _refreshButton = new();

    private bool _loading;

    public HouseholdControl(
        IHouseholdService householdService,
        ICitizenService citizenService,
        IAddressService addressService,
        IUserSession session)
    {
        _householdService = householdService;
        _citizenService = citizenService;
        _addressService = addressService;
        _session = session;

        InitializeComponent();
        lblMessage.Visible = false;
        BuildLayout();

        Load += async (_, _) => await LoadHouseholdsAsync();
    }

    private void BuildLayout()
    {
        _search.Location = new Point(25, 80);
        _search.Size = new Size(300, 27);
        _search.PlaceholderText = "Household #, head name, National ID or address";

        _status.Location = new Point(340, 80);
        _status.Size = new Size(120, 27);
        _status.DropDownStyle = ComboBoxStyle.DropDownList;
        _status.Items.AddRange(new object[] { "All", "ACTIVE", "INACTIVE" });
        _status.SelectedIndex = 0;

        _searchButton.Text = "Search";
        _searchButton.Location = new Point(470, 78);
        _searchButton.Size = new Size(85, 30);

        _registerButton.Text = "Register household";
        _registerButton.Location = new Point(565, 78);
        _registerButton.Size = new Size(145, 30);
        _registerButton.BackColor = Color.DarkSlateGray;
        _registerButton.ForeColor = Color.White;
        _registerButton.FlatStyle = FlatStyle.Flat;

        _refreshButton.Text = "Refresh";
        _refreshButton.Location = new Point(720, 78);
        _refreshButton.Size = new Size(80, 30);

        _grid.Location = new Point(25, 125);
        _grid.Size = new Size(745, 330);
        _grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.ReadOnly = true;
        _grid.MultiSelect = false;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.BackgroundColor = Color.White;

        _viewButton.Text = "View household";
        _viewButton.Location = new Point(25, 465);
        _viewButton.Size = new Size(140, 32);
        _viewButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _viewButton.BackColor = Color.SteelBlue;
        _viewButton.ForeColor = Color.White;
        _viewButton.FlatStyle = FlatStyle.Flat;

        Controls.AddRange(new Control[]
        {
            _search, _status, _searchButton, _registerButton, _refreshButton,
            _grid, _viewButton
        });

        _searchButton.Click += async (_, _) => await LoadHouseholdsAsync();
        _refreshButton.Click += async (_, _) => await LoadHouseholdsAsync();
        _registerButton.Click += RegisterButton_Click;
        _viewButton.Click += ViewButton_Click;
        _grid.CellDoubleClick += GridCellDoubleClick;
        _status.SelectedIndexChanged += async (_, _) => await LoadHouseholdsAsync();

        _search.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await LoadHouseholdsAsync();
            }
        };
    }

    private async Task LoadHouseholdsAsync()
    {
        if (_loading || IsDisposed)
        {
            return;
        }

        _loading = true;
        SetBusy(true);

        try
        {
            var text = _search.Text.Trim();
            var selectedStatus = _status.SelectedItem?.ToString();

            var criteria = new HouseholdSearchCriteria
            {
                HouseholdId = int.TryParse(text, out var id) ? id : null,
                SearchText = int.TryParse(text, out _) || text.Length == 0 ? null : text,
                Status = selectedStatus == "All" ? null : selectedStatus
            };

            var result = await _householdService.SearchAsync(criteria);

            if (result.IsFailure)
            {
                MessageBox.Show(
                    string.Join(Environment.NewLine, result.Errors),
                    "Cannot search households",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            _grid.DataSource = result.Value?.ToList() ?? new List<HouseholdDto>();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Unexpected error while loading households: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            _loading = false;
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        _searchButton.Enabled = !busy;
        _registerButton.Enabled = !busy;
        _refreshButton.Enabled = !busy;
        _viewButton.Enabled = !busy;
        _status.Enabled = !busy;
    }

    private HouseholdDto? GetSelectedHousehold()
        => _grid.CurrentRow?.DataBoundItem as HouseholdDto;

    private async void RegisterButton_Click(object? sender, EventArgs e)
    {
        using var form = new RegisterHouseholdForm(
            _householdService, _citizenService, _addressService, _session);

        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await LoadHouseholdsAsync();
        }
    }

    private void GridCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        => ViewButton_Click(sender, e);

    private async void ViewButton_Click(object? sender, EventArgs e)
    {
        var selected = GetSelectedHousehold();
        if (selected is null)
        {
            MessageBox.Show(
                "Select a household first.",
                "No selection",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        using var form = new HouseholdDetailForm(
            _householdService, _citizenService, _session, selected.HouseholdId);

        form.ShowDialog(this);
        await LoadHouseholdsAsync();
    }
}
