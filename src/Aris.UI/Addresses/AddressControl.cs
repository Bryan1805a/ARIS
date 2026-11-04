using Aris.Application.Addresses;
using Aris.Application.Addresses.Dtos;
using Aris.Application.Common;
using Aris.UI;

namespace Aris.UI.Addresses;

/// <summary>
/// Address directory: search, register, view, and retire
/// (<c>ADR-02</c>, <c>ADR-03</c>, <c>SRCH-03</c>).
/// </summary>
public sealed class AddressControl : UserControl {
    private readonly IAddressService _addressService;
    private readonly IUserSession _session;

    private readonly Label _title = new();
    private readonly TextBox _search = new();
    private readonly ComboBox _status = new();
    private readonly Button _searchButton = new();
    private readonly Button _registerButton = new();
    private readonly Button _viewButton = new();
    private readonly Button _retireButton = new();
    private readonly Button _refreshButton = new();
    private readonly DataGridView _grid = new();

    private bool _loading;

    public AddressControl(IAddressService addressService, IUserSession session) {
        _addressService = addressService;
        _session = session;

        BuildLayout();
        UiTheme.ApplyScreen(this);
        Load += async (_, _) => await LoadAddressesAsync();
    }

    private void BuildLayout() {
        _title.Text = "Address Management";
        _title.Font = new Font("Segoe UI", 20.25F, FontStyle.Bold);
        _title.ForeColor = Color.DarkSlateGray;
        _title.AutoSize = true;
        _title.Location = new Point(25, 25);

        _search.Location = new Point(25, 80);
        _search.Size = new Size(300, 27);
        _search.PlaceholderText = "Search by code, street, ward, district or province";

        _status.Location = new Point(340, 80);
        _status.Size = new Size(120, 27);
        _status.DropDownStyle = ComboBoxStyle.DropDownList;
        _status.Items.AddRange(new object[] { "All", "ACTIVE", "INACTIVE" });
        _status.SelectedIndex = 0;

        _searchButton.Text = "Search";
        _searchButton.Location = new Point(470, 78);
        _searchButton.Size = new Size(85, 30);

        _registerButton.Text = "Register address";
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
        _grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom
            | AnchorStyles.Left | AnchorStyles.Right;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.ReadOnly = true;
        _grid.MultiSelect = false;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.BackgroundColor = Color.White;

        _viewButton.Text = "View address";
        _viewButton.Location = new Point(25, 465);
        _viewButton.Size = new Size(125, 32);
        _viewButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _viewButton.BackColor = Color.SteelBlue;
        _viewButton.ForeColor = Color.White;
        _viewButton.FlatStyle = FlatStyle.Flat;

        _retireButton.Text = "Retire address";
        _retireButton.Location = new Point(160, 465);
        _retireButton.Size = new Size(130, 32);
        _retireButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

        Controls.AddRange(new Control[]
        {
            _title, _search, _status, _searchButton, _registerButton,
            _refreshButton, _grid, _viewButton, _retireButton
        });

        _searchButton.Click += async (_, _) => await LoadAddressesAsync();
        _refreshButton.Click += async (_, _) => await LoadAddressesAsync();
        _registerButton.Click += RegisterButton_Click;
        _viewButton.Click += ViewButton_Click;
        _retireButton.Click += RetireButton_Click;
        _grid.CellDoubleClick += GridCellDoubleClick;
        _status.SelectedIndexChanged += async (_, _) => await LoadAddressesAsync();

        _search.KeyDown += async (_, e) => {
            if (e.KeyCode == Keys.Enter) {
                e.SuppressKeyPress = true;
                await LoadAddressesAsync();
            }
        };
    }

    private async Task LoadAddressesAsync() {
        if (_loading || IsDisposed) {
            return;
        }

        _loading = true;
        SetBusy(true);

        try {
            var text = _search.Text.Trim();
            var selectedStatus = _status.SelectedItem?.ToString();

            var criteria = new AddressSearchCriteria {
                SearchText = text.Length == 0 ? null : text,
                Status = selectedStatus == "All" ? null : selectedStatus
            };

            var result = await _addressService.SearchAsync(criteria);

            if (result.IsFailure) {
                MessageBox.Show(
                    string.Join(Environment.NewLine, result.Errors),
                    "Cannot search addresses",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            _grid.DataSource = result.Value?.ToList() ?? new List<AddressDto>();
        } catch (Exception ex) {
            MessageBox.Show(
                $"Unexpected error while loading addresses: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        } finally {
            _loading = false;
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy) {
        _searchButton.Enabled = !busy;
        _registerButton.Enabled = !busy;
        _refreshButton.Enabled = !busy;
        _viewButton.Enabled = !busy;
        _retireButton.Enabled = !busy;
        _status.Enabled = !busy;
    }

    private AddressDto? GetSelectedAddress()
        => _grid.CurrentRow?.DataBoundItem as AddressDto;

    private async void RegisterButton_Click(object? sender, EventArgs e) {
        using var form = new AddressEditForm(_addressService, _session);

        if (form.ShowDialog(this) == DialogResult.OK) {
            await LoadAddressesAsync();
        }
    }

    private void GridCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        => ViewButton_Click(sender, e);

    private async void ViewButton_Click(object? sender, EventArgs e) {
        var selected = GetSelectedAddress();
        if (selected is null) {
            MessageBox.Show(
                "Select an address first.",
                "No selection",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        using var form = new AddressDetailForm(
            _addressService, _session, selected.AddressId);

        form.ShowDialog(this);
        await LoadAddressesAsync();
    }

    private async void RetireButton_Click(object? sender, EventArgs e) {
        var selected = GetSelectedAddress();
        if (selected is null) {
            MessageBox.Show(
                "Select an address first.",
                "No selection",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        if (selected.Status != "ACTIVE") {
            MessageBox.Show(
                "Only an ACTIVE address can be retired.",
                "Cannot retire",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        using var form = new RetireAddressForm(
            _addressService, _session, selected.AddressId, selected.AddressText);

        if (form.ShowDialog(this) == DialogResult.OK) {
            await LoadAddressesAsync();
        }
    }
}