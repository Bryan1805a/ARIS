using Aris.Application.Citizens;
using Aris.Application.Citizens.Dtos;
using Aris.Application.Common;
using Aris.UI.Citizens;

namespace Aris.UI;

/// <summary>
/// Citizen directory: search, register, view/edit, and deactivate
/// (<c>CIT-01</c>-<c>CIT-04</c>, <c>SRCH-01</c>).
/// </summary>
public partial class CitizenControl : UserControl
{
    private readonly ICitizenService _citizenService;
    private readonly IUserSession _session;

    private readonly TextBox _search = new();
    private readonly ComboBox _status = new();
    private readonly DataGridView _grid = new();
    private readonly Button _searchButton = new();
    private readonly Button _createButton = new();
    private readonly Button _viewButton = new();
    private readonly Button _deactivateButton = new();
    private readonly Button _refreshButton = new();

    private bool _loading;

    public CitizenControl(ICitizenService citizenService, IUserSession session)
    {
        _citizenService = citizenService;
        _session = session;

        InitializeComponent();
        lblMessage.Visible = false;
        BuildLayout();

        Load += async (_, _) => await LoadCitizensAsync();
    }

    private void BuildLayout()
    {
        _search.Location = new Point(25, 80);
        _search.Size = new Size(250, 27);
        _search.PlaceholderText = "National ID or full name";

        _status.Location = new Point(290, 80);
        _status.Size = new Size(145, 27);
        _status.DropDownStyle = ComboBoxStyle.DropDownList;
        _status.Items.AddRange(new object[] { "All", "UNASSIGNED", "ACTIVE", "INACTIVE" });
        _status.SelectedIndex = 0;

        _searchButton.Text = "Search";
        _searchButton.Location = new Point(445, 78);
        _searchButton.Size = new Size(90, 30);

        _createButton.Text = "Register citizen";
        _createButton.Location = new Point(545, 78);
        _createButton.Size = new Size(125, 30);
        _createButton.BackColor = Color.DarkSlateGray;
        _createButton.ForeColor = Color.White;
        _createButton.FlatStyle = FlatStyle.Flat;

        _refreshButton.Text = "Refresh";
        _refreshButton.Location = new Point(680, 78);
        _refreshButton.Size = new Size(90, 30);

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

        _viewButton.Text = "View / Edit";
        _viewButton.Location = new Point(25, 465);
        _viewButton.Size = new Size(120, 32);
        _viewButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

        _deactivateButton.Text = "Deactivate";
        _deactivateButton.Location = new Point(155, 465);
        _deactivateButton.Size = new Size(120, 32);
        _deactivateButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _deactivateButton.BackColor = Color.Firebrick;
        _deactivateButton.ForeColor = Color.White;
        _deactivateButton.FlatStyle = FlatStyle.Flat;

        Controls.AddRange(new Control[]
        {
            _search, _status, _searchButton, _createButton, _refreshButton,
            _grid, _viewButton, _deactivateButton
        });

        _searchButton.Click += async (_, _) => await LoadCitizensAsync();
        _refreshButton.Click += async (_, _) => await LoadCitizensAsync();
        _createButton.Click += CreateButton_Click;
        _viewButton.Click += ViewButton_Click;
        _deactivateButton.Click += DeactivateButton_Click;
        _grid.CellDoubleClick += GridCellDoubleClick;
        _status.SelectedIndexChanged += async (_, _) => await LoadCitizensAsync();

        _search.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await LoadCitizensAsync();
            }
        };
    }

    private async Task LoadCitizensAsync()
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
            var looksLikeNationalId = text.Length > 0 && text.All(char.IsDigit);
            var selectedStatus = _status.SelectedItem?.ToString();

            var criteria = new CitizenSearchCriteria
            {
                NationalId = looksLikeNationalId ? text : null,
                FullName = !looksLikeNationalId && text.Length > 0 ? text : null,
                Status = selectedStatus == "All" ? null : selectedStatus
            };

            var result = await _citizenService.SearchAsync(criteria);

            if (result.IsFailure)
            {
                MessageBox.Show(
                    string.Join(Environment.NewLine, result.Errors),
                    "Cannot search citizens",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            _grid.DataSource = result.Value?.ToList() ?? new List<CitizenDto>();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Unexpected error while loading citizens: {ex.Message}",
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
        _createButton.Enabled = !busy;
        _refreshButton.Enabled = !busy;
        _viewButton.Enabled = !busy;
        _deactivateButton.Enabled = !busy;
        _status.Enabled = !busy;
    }

    private CitizenDto? GetSelectedCitizen()
        => _grid.CurrentRow?.DataBoundItem as CitizenDto;

    private async void CreateButton_Click(object? sender, EventArgs e)
    {
        using var form = new CitizenEditForm(_citizenService, _session);

        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await LoadCitizensAsync();
        }
    }

    private void GridCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        => ViewButton_Click(sender, e);

    private async void ViewButton_Click(object? sender, EventArgs e)
    {
        var selected = GetSelectedCitizen();
        if (selected is null)
        {
            MessageBox.Show(
                "Select a citizen first.",
                "No selection",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        using var form = new CitizenDetailForm(
            _citizenService, _session, selected.CitizenId);

        form.ShowDialog(this);
        await LoadCitizensAsync();
    }

    private async void DeactivateButton_Click(object? sender, EventArgs e)
    {
        var selected = GetSelectedCitizen();
        if (selected is null)
        {
            MessageBox.Show(
                "Select a citizen first.",
                "No selection",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        if (_session.UserId is not int userId)
        {
            MessageBox.Show(
                "Your session has expired. Sign in again.",
                "Session expired",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var detailResult = await _citizenService.GetByIdAsync(selected.CitizenId);
        var detail = detailResult.Value;

        if (detail is null)
        {
            MessageBox.Show(
                string.Join(Environment.NewLine, detailResult.Errors.DefaultIfEmpty("Citizen not found.")),
                "Cannot deactivate",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var otherMembers = detail.HouseholdMembers
            .Where(m => m.CitizenId != detail.CitizenId)
            .ToList();

        // A head with remaining members needs a successor; the detail form offers the picker.
        if (detail.HouseholdRole == "HEAD" && otherMembers.Count > 0)
        {
            MessageBox.Show(
                "This citizen is the household head. Open the record and choose a successor head.",
                "Successor required",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            using var form = new CitizenDetailForm(
                _citizenService, _session, detail.CitizenId);
            form.ShowDialog(this);
            await LoadCitizensAsync();
            return;
        }

        var consequence = detail.HouseholdRole == "HEAD"
            ? " This is the last member, so the household and its address will be inactivated."
            : string.Empty;

        var confirmation = MessageBox.Show(
            $"Deactivate {detail.FullName} ({detail.NationalId})?{consequence}",
            "Deactivate citizen",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirmation != DialogResult.Yes)
        {
            return;
        }

        var result = await _citizenService.DeactivateCitizenAsync(
            detail.CitizenId, userId);

        if (result.IsFailure)
        {
            MessageBox.Show(
                string.Join(Environment.NewLine, result.Errors),
                "Cannot deactivate citizen",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        MessageBox.Show(
            "The citizen was deactivated.",
            "Success",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);

        await LoadCitizensAsync();
    }
}
