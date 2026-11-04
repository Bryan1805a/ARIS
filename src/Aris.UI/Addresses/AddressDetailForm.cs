using Aris.Application.Addresses;
using Aris.Application.Addresses.Dtos;
using Aris.Application.Common;
using Aris.UI.Localization;

namespace Aris.UI.Addresses;

/// <summary>
/// Address profile with lineage, current households and current residents
/// (<c>ADR-02</c>, <c>ADR-03</c>).
/// </summary>
public sealed class AddressDetailForm : Form {
    private readonly IAddressService _addressService;
    private readonly IUserSession _session;
    private readonly int _addressId;

    private readonly Label _info = new();
    private readonly DataGridView _householdsGrid = new();
    private readonly DataGridView _residentsGrid = new();
    private readonly Button _retireButton = new();
    private readonly Button _refreshButton = new();
    private readonly Button _closeButton = new();

    private AddressDetailDto? _detail;

    public AddressDetailForm(
        IAddressService addressService,
        IUserSession session,
        int addressId) {
        _addressService = addressService;
        _session = session;
        _addressId = addressId;

        Text = $"{LanguageManager.T("ARIS - Address")} #{addressId}";
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(920, 620);

        BuildLayout();
        Aris.UI.UiTheme.ApplyScreen(this);
        Load += async (_, _) => await LoadDetailAsync();
    }

    private void BuildLayout() {
        var title = new Label {
            Text = $"Address #{_addressId}",
            Font = new Font("Segoe UI", 15F, FontStyle.Bold),
            ForeColor = Color.DarkSlateGray,
            AutoSize = true,
            Location = new Point(20, 15)
        };

        _info.AutoSize = false;
        _info.Size = new Size(870, 90);
        _info.Location = new Point(20, 50);
        _info.ForeColor = Color.DimGray;

        var householdsLabel = new Label {
            Text = "Current households",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(20, 150)
        };

        ConfigureGrid(_householdsGrid, new Point(20, 173), new Size(870, 150));

        var residentsLabel = new Label {
            Text = "Current residents",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(20, 338)
        };

        ConfigureGrid(_residentsGrid, new Point(20, 361), new Size(870, 160));

        _retireButton.Text = "Retire address";
        _retireButton.Location = new Point(20, 550);
        _retireButton.Size = new Size(130, 34);
        _retireButton.BackColor = Color.SteelBlue;
        _retireButton.ForeColor = Color.White;
        _retireButton.FlatStyle = FlatStyle.Flat;
        _retireButton.Click += RetireButton_Click;

        _refreshButton.Text = "Refresh";
        _refreshButton.Location = new Point(160, 550);
        _refreshButton.Size = new Size(100, 34);
        _refreshButton.Click += async (_, _) => await LoadDetailAsync();

        _closeButton.Text = "Close";
        _closeButton.Location = new Point(790, 550);
        _closeButton.Size = new Size(100, 34);
        _closeButton.DialogResult = DialogResult.Cancel;

        Controls.AddRange(new Control[]
        {
            title, _info, householdsLabel, _householdsGrid,
            residentsLabel, _residentsGrid,
            _retireButton, _refreshButton, _closeButton
        });
    }

    private static void ConfigureGrid(DataGridView grid, Point location, Size size) {
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
            var result = await _addressService.GetByIdAsync(_addressId);

            if (result.IsFailure || result.Value is null) {
                MessageBox.Show(
                    string.Join(Environment.NewLine,
                        result.Errors.DefaultIfEmpty("Address not found.")),
                    "Cannot load address",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                Close();
                return;
            }

            _detail = result.Value;

            var lines = new List<string>
            {
                $"{LanguageManager.T("Status:")} {_detail.Status}    " +
                $"{LanguageManager.T("Address code")}: {_detail.AddressCode}",
                $"{LanguageManager.T("Address:")} {_detail.AddressText}",
                $"{LanguageManager.T("Valid from")} {_detail.ValidFrom:yyyy-MM-dd}" +
                (_detail.ValidTo is { } validTo
                    ? $"    {LanguageManager.T("Valid to")} {validTo:yyyy-MM-dd}"
                    : "")
            };

            if (_detail.ReplacedByAddressId is int replacedById) {
                lines.Add(
                    $"{LanguageManager.T("Successor address")}: " +
                    $"#{replacedById} {_detail.ReplacedByAddressText}");
            }

            if (_detail.CanonicalAddressId is int canonicalId
                && canonicalId != _detail.AddressId) {
                lines.Add(
                    $"{LanguageManager.T("Canonical address")}: " +
                    $"#{canonicalId} {_detail.CanonicalAddressText} " +
                    $"({_detail.CanonicalStatus})");
            }

            _info.Text = string.Join(Environment.NewLine, lines);

            _householdsGrid.DataSource = _detail.CurrentHouseholds.ToList();
            _residentsGrid.DataSource = _detail.CurrentResidents.ToList();

            _retireButton.Enabled = _detail.Status == "ACTIVE";
        } catch (Exception ex) {
            MessageBox.Show(
                $"Unexpected error while loading the address: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private async void RetireButton_Click(object? sender, EventArgs e) {
        if (_detail is null || _detail.Status != "ACTIVE") {
            return;
        }

        using var form = new RetireAddressForm(
            _addressService, _session, _detail.AddressId, _detail.AddressText);

        if (form.ShowDialog(this) == DialogResult.OK) {
            await LoadDetailAsync();
        }
    }
}