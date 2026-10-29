using Aris.Application.Addresses;
using Aris.Application.Common;
using Aris.Application.Households;
using Aris.Application.Transfers;
using Aris.Application.Transfers.Dtos;
using Aris.UI.Transfers;

namespace Aris.UI;

/// <summary>
/// Maker-checker relocation inbox: create requests, inspect them and decide pending ones
/// (<c>UC-06</c>).
/// </summary>
public partial class TransferControl : UserControl
{
    private readonly ITransferService _transferService;
    private readonly IHouseholdService _householdService;
    private readonly IAddressService _addressService;
    private readonly IUserSession _session;

    private readonly ComboBox _status = new();
    private readonly DataGridView _grid = new();
    private readonly Button _refreshButton = new();
    private readonly Button _createButton = new();
    private readonly Button _openButton = new();

    private bool _loading;

    public TransferControl(
        ITransferService transferService,
        IHouseholdService householdService,
        IAddressService addressService,
        IUserSession session)
    {
        _transferService = transferService;
        _householdService = householdService;
        _addressService = addressService;
        _session = session;

        InitializeComponent();
        lblMessage.Visible = false;
        BuildLayout();

        Load += async (_, _) => await LoadRequestsAsync();
    }

    private void BuildLayout()
    {
        _status.Location = new Point(25, 80);
        _status.Size = new Size(130, 27);
        _status.DropDownStyle = ComboBoxStyle.DropDownList;
        _status.Items.AddRange(new object[]
        {
            "All", "PENDING", "APPROVED", "REJECTED", "CANCELLED"
        });
        _status.SelectedIndex = 0;

        _refreshButton.Text = "Refresh";
        _refreshButton.Location = new Point(170, 78);
        _refreshButton.Size = new Size(90, 30);

        _createButton.Text = "New request";
        _createButton.Location = new Point(610, 78);
        _createButton.Size = new Size(160, 30);
        _createButton.BackColor = Color.DarkSlateGray;
        _createButton.ForeColor = Color.White;
        _createButton.FlatStyle = FlatStyle.Flat;

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

        _openButton.Text = "Open request";
        _openButton.Location = new Point(25, 465);
        _openButton.Size = new Size(140, 32);
        _openButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _openButton.BackColor = Color.SteelBlue;
        _openButton.ForeColor = Color.White;
        _openButton.FlatStyle = FlatStyle.Flat;

        Controls.AddRange(new Control[]
        {
            _status, _refreshButton, _createButton, _grid, _openButton
        });

        _refreshButton.Click += async (_, _) => await LoadRequestsAsync();
        _createButton.Click += CreateButton_Click;
        _openButton.Click += OpenButton_Click;
        _grid.CellDoubleClick += GridCellDoubleClick;
        _status.SelectedIndexChanged += async (_, _) => await LoadRequestsAsync();
    }

    private async Task LoadRequestsAsync()
    {
        if (_loading || IsDisposed)
        {
            return;
        }

        _loading = true;
        SetBusy(true);

        try
        {
            var selectedStatus = _status.SelectedItem?.ToString();

            var result = await _transferService.GetRequestsAsync(new TransferSearchCriteria
            {
                Status = selectedStatus == "All" ? null : selectedStatus
            });

            if (result.IsFailure)
            {
                MessageBox.Show(
                    string.Join(Environment.NewLine, result.Errors),
                    "Cannot load transfer requests",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            _grid.DataSource = result.Value?.ToList() ?? new List<TransferRequestSummaryDto>();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Unexpected error while loading transfer requests: {ex.Message}",
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
        _refreshButton.Enabled = !busy;
        _createButton.Enabled = !busy;
        _openButton.Enabled = !busy;
        _status.Enabled = !busy;
    }

    private TransferRequestSummaryDto? GetSelectedRequest()
        => _grid.CurrentRow?.DataBoundItem as TransferRequestSummaryDto;

    private async void CreateButton_Click(object? sender, EventArgs e)
    {
        using var form = new TransferRequestForm(
            _transferService, _householdService, _addressService, _session);

        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await LoadRequestsAsync();
        }
    }

    private void GridCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        => OpenButton_Click(sender, e);

    private async void OpenButton_Click(object? sender, EventArgs e)
    {
        var selected = GetSelectedRequest();
        if (selected is null)
        {
            MessageBox.Show(
                "Select a request first.",
                "No selection",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        using var form = new TransferDecisionForm(
            _transferService, _session, selected.RequestId);

        form.ShowDialog(this);
        await LoadRequestsAsync();
    }
}
