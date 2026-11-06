using Aris.Application.Addresses;
using Aris.Application.Citizens;
using Aris.Application.CivilStatuses;
using Aris.Application.CivilStatuses.Dtos;
using Aris.Application.Common;
using Aris.UI.Localization;

namespace Aris.UI.CivilStatuses;

public sealed class CivilStatusControl : UserControl {
    private readonly ICivilStatusService _service;
    private readonly IAddressService _addressService;
    private readonly ICitizenService _citizenService;
    private readonly IUserSession _session;

    private readonly Label _title = new();
    private readonly TextBox _search = new();
    private readonly ComboBox _recordType = new();
    private readonly ComboBox _status = new();
    private readonly Button _searchButton = new();
    private readonly Button _registerButton = new();
    private readonly Button _detailButton = new();
    private readonly Button _cancelButton = new();
    private readonly Button _refreshButton = new();
    private readonly DataGridView _grid = new();

    private readonly Panel _top = new();
    private readonly Panel _bottom = new();
    private readonly Panel _center = new();

    private bool _loading;

    public CivilStatusControl(
        ICivilStatusService service,
        IAddressService addressService,
        ICitizenService citizenService,
        IUserSession session) {
        _service = service;
        _addressService = addressService;
        _citizenService = citizenService;
        _session = session;

        BuildLayout();
        UiTheme.ApplyScreen(this);
        Load += async (_, _) => await LoadDataAsync();
    }

    private void BuildLayout() {
        Size = new Size(1000, 600);
        MinimumSize = new Size(820, 500);
        AutoScroll = true;

        _top.Dock = DockStyle.Top;
        _top.Height = 118;
        _top.BackColor = Color.Transparent;

        _title.Text = LanguageManager.T("Civil Status Management");
        _title.Font = new Font("Segoe UI", 20.25F, FontStyle.Bold);
        _title.ForeColor = UiTheme.Accent;
        _title.AutoSize = true;
        _title.Location = new Point(25, 18);

        _search.Location = new Point(25, 74);
        _search.Size = new Size(250, 28);
        _search.PlaceholderText = LanguageManager.T("National ID or full name");

        _recordType.Location = new Point(285, 74);
        _recordType.Size = new Size(140, 28);
        _recordType.DropDownStyle = ComboBoxStyle.DropDownList;
        _recordType.Items.AddRange(new object[]
        {
            LanguageManager.T("All"),
            LanguageManager.T("Birth"),
            LanguageManager.T("Death"),
            LanguageManager.T("Marriage")
        });
        _recordType.SelectedIndex = 0;

        _status.Location = new Point(435, 74);
        _status.Size = new Size(120, 28);
        _status.DropDownStyle = ComboBoxStyle.DropDownList;
        _status.Items.AddRange(new object[]
        {
            LanguageManager.T("All"),
            "ACTIVE",
            "CANCELLED"
        });
        _status.SelectedIndex = 0;

        _searchButton.Text = LanguageManager.T("Search");
        _searchButton.Location = new Point(565, 73);
        _searchButton.Size = new Size(90, 30);

        _registerButton.Text = "+ " + LanguageManager.T("Register civil status");
        _registerButton.Location = new Point(665, 73);
        _registerButton.Size = new Size(170, 30);
        _registerButton.BackColor = UiTheme.Accent;
        _registerButton.ForeColor = Color.White;
        _registerButton.FlatStyle = FlatStyle.Flat;

        _refreshButton.Text = LanguageManager.T("Refresh");
        _refreshButton.Location = new Point(845, 73);
        _refreshButton.Size = new Size(95, 30);

        _top.Controls.AddRange(new Control[]
        {
            _title, _search, _recordType, _status,
            _searchButton, _registerButton, _refreshButton
        });

        _bottom.Dock = DockStyle.Bottom;
        _bottom.Height = 65;
        _bottom.BackColor = Color.Transparent;

        _detailButton.Text = LanguageManager.T("View detail");
        _detailButton.Location = new Point(25, 12);
        _detailButton.Size = new Size(120, 36);
        _detailButton.BackColor = Color.SteelBlue;
        _detailButton.ForeColor = Color.White;
        _detailButton.FlatStyle = FlatStyle.Flat;

        _cancelButton.Text = LanguageManager.T("Cancel registration");
        _cancelButton.Location = new Point(160, 12);
        _cancelButton.Size = new Size(150, 36);
        _cancelButton.BackColor = Color.IndianRed;
        _cancelButton.ForeColor = Color.White;
        _cancelButton.FlatStyle = FlatStyle.Flat;

        _bottom.Controls.AddRange(new Control[] { _detailButton, _cancelButton });

        _center.Dock = DockStyle.Fill;
        _center.Padding = new Padding(25, 0, 25, 5);
        _center.BackColor = Color.Transparent;

        _grid.Dock = DockStyle.Fill;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.ReadOnly = true;
        _grid.MultiSelect = false;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        _grid.BackgroundColor = Color.White;
        _grid.ScrollBars = ScrollBars.Both;
        _grid.CellDoubleClick += (_, _) => DetailButton_Click(this, EventArgs.Empty);
        _grid.CellFormatting += Grid_CellFormatting;

        _center.Controls.Add(_grid);

        Controls.Add(_center);
        Controls.Add(_bottom);
        Controls.Add(_top);

        _searchButton.Click += async (_, _) => await LoadDataAsync();
        _refreshButton.Click += async (_, _) => await LoadDataAsync();
        _recordType.SelectedIndexChanged += async (_, _) => await LoadDataAsync();
        _status.SelectedIndexChanged += async (_, _) => await LoadDataAsync();
        _registerButton.Click += RegisterButton_Click;
        _detailButton.Click += DetailButton_Click;
        _cancelButton.Click += CancelButton_Click;

        _search.KeyDown += async (_, e) => {
            if (e.KeyCode == Keys.Enter) {
                e.SuppressKeyPress = true;
                await LoadDataAsync();
            }
        };
    }

    private async Task LoadDataAsync() {
        if (_loading || IsDisposed) {
            return;
        }

        _loading = true;
        SetBusy(true);

        try {
            var criteria = new CivilStatusSearchCriteria(
                SearchText: string.IsNullOrWhiteSpace(_search.Text)
                    ? null
                    : _search.Text.Trim(),
                RecordType: SelectedRecordType(),
                Status: SelectedStatus());

            var result = await _service.SearchAsync(criteria);
            if (result.IsFailure) {
                MessageBox.Show(
                    string.Join(Environment.NewLine, result.Errors),
                    LanguageManager.T("Cannot load civil status records"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            _grid.DataSource = result.Value?.ToList()
                ?? new List<CivilStatusRecordDto>();
            ConfigureGridColumns();
        } catch (Exception ex) {
            MessageBox.Show(
                ex.Message,
                LanguageManager.T("Error"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        } finally {
            _loading = false;
            SetBusy(false);
        }
    }

    private string? SelectedRecordType() {
        return _recordType.SelectedIndex switch {
            1 => "BIRTH",
            2 => "DEATH",
            3 => "MARRIAGE",
            _ => null
        };
    }

    private string? SelectedStatus() {
        return _status.SelectedIndex switch {
            1 => "ACTIVE",
            2 => "CANCELLED",
            _ => null
        };
    }

    private void ConfigureGridColumns() {
        if (_grid.Columns.Count == 0) {
            return;
        }

        if (_grid.Columns[nameof(CivilStatusRecordDto.CivilStatusId)] is { } id) {
            id.HeaderText = LanguageManager.T("ID");
            id.Width = 70;
        }

        if (_grid.Columns[nameof(CivilStatusRecordDto.RecordType)] is { } type) {
            type.HeaderText = LanguageManager.T("Record type");
            type.Width = 110;
        }

        if (_grid.Columns[nameof(CivilStatusRecordDto.CitizenId)] is { } cid) {
            cid.Visible = false;
        }

        if (_grid.Columns[nameof(CivilStatusRecordDto.CitizenNationalId)] is { } nid) {
            nid.HeaderText = LanguageManager.T("National ID");
            nid.Width = 120;
        }

        if (_grid.Columns[nameof(CivilStatusRecordDto.CitizenFullName)] is { } name) {
            name.HeaderText = LanguageManager.T("Full Name");
            name.Width = 170;
        }

        if (_grid.Columns[nameof(CivilStatusRecordDto.RelatedCitizenId)] is { } rid) {
            rid.Visible = false;
        }

        if (_grid.Columns[nameof(CivilStatusRecordDto.RelatedNationalId)] is { } rnid) {
            rnid.Visible = false;
        }

        if (_grid.Columns[nameof(CivilStatusRecordDto.RelatedFullName)] is { } rname) {
            rname.HeaderText = LanguageManager.T("Related citizen");
            rname.Width = 170;
        }

        if (_grid.Columns[nameof(CivilStatusRecordDto.AddressId)] is { } aid) {
            aid.Visible = false;
        }

        if (_grid.Columns[nameof(CivilStatusRecordDto.AddressText)] is { } addr) {
            addr.HeaderText = LanguageManager.T("Address");
            addr.Width = 220;
        }

        if (_grid.Columns[nameof(CivilStatusRecordDto.EventDate)] is { } ed) {
            ed.HeaderText = LanguageManager.T("Event date");
            ed.Width = 105;
            ed.DefaultCellStyle.Format = "dd/MM/yyyy";
        }

        if (_grid.Columns[nameof(CivilStatusRecordDto.RegistrationDate)] is { } rd) {
            rd.HeaderText = LanguageManager.T("Registration date");
            rd.Width = 120;
            rd.DefaultCellStyle.Format = "dd/MM/yyyy";
        }

        if (_grid.Columns[nameof(CivilStatusRecordDto.DocumentNumber)] is { } doc) {
            doc.HeaderText = LanguageManager.T("Document number");
            doc.Width = 120;
        }

        if (_grid.Columns[nameof(CivilStatusRecordDto.Status)] is { } st) {
            st.HeaderText = LanguageManager.T("Status");
            st.Width = 100;
        }

        if (_grid.Columns[nameof(CivilStatusRecordDto.Notes)] is { } notes) {
            notes.Visible = false;
        }

        if (_grid.Columns[nameof(CivilStatusRecordDto.CancelReason)] is { } reason) {
            reason.Visible = false;
        }

        if (_grid.Columns[nameof(CivilStatusRecordDto.CreatedAt)] is { } ca) {
            ca.HeaderText = LanguageManager.T("Created At");
            ca.Width = 140;
            ca.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
        }

        if (_grid.Columns[nameof(CivilStatusRecordDto.CreatedByUsername)] is { } cb) {
            cb.HeaderText = LanguageManager.T("Created By");
            cb.Width = 110;
        }

        if (_grid.Columns[nameof(CivilStatusRecordDto.CancelledAt)] is { } xa) {
            xa.Visible = false;
        }

        if (_grid.Columns[nameof(CivilStatusRecordDto.CancelledByUsername)] is { } xb) {
            xb.Visible = false;
        }
    }

    private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e) {
        if (e.ColumnIndex < 0 || e.Value is not string code) {
            return;
        }

        if (_grid.Columns[e.ColumnIndex].Name != nameof(CivilStatusRecordDto.RecordType)) {
            return;
        }

        e.Value = code switch {
            "BIRTH" => LanguageManager.T("Birth"),
            "DEATH" => LanguageManager.T("Death"),
            "MARRIAGE" => LanguageManager.T("Marriage"),
            _ => code
        };
    }

    private void SetBusy(bool busy) {
        _searchButton.Enabled = !busy;
        _registerButton.Enabled = !busy;
        _refreshButton.Enabled = !busy;
        _detailButton.Enabled = !busy;
        _cancelButton.Enabled = !busy;
        _recordType.Enabled = !busy;
        _status.Enabled = !busy;
    }

    private CivilStatusRecordDto? GetSelected()
        => _grid.CurrentRow?.DataBoundItem as CivilStatusRecordDto;

    private async void RegisterButton_Click(object? sender, EventArgs e) {
        using var form = new RegisterCivilStatusForm(
            _service, _addressService, _citizenService, _session);

        if (form.ShowDialog(this) == DialogResult.OK) {
            await LoadDataAsync();
        }
    }

    private void DetailButton_Click(object? sender, EventArgs e) {
        var selected = GetSelected();
        if (selected is null) {
            MessageBox.Show(
                LanguageManager.T("Select a record first."),
                LanguageManager.T("Civil status"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        using var form = new CivilStatusDetailForm(selected);
        form.ShowDialog(this);
    }

    private async void CancelButton_Click(object? sender, EventArgs e) {
        var selected = GetSelected();
        if (selected is null) {
            MessageBox.Show(
                LanguageManager.T("Select a record first."),
                LanguageManager.T("Civil status"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        if (selected.Status != "ACTIVE") {
            MessageBox.Show(
                LanguageManager.T("Only an active record can be cancelled."),
                LanguageManager.T("Cannot cancel"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        if (selected.RecordType == "DEATH") {
            MessageBox.Show(
                LanguageManager.T("A death registration cannot be cancelled. Correct it through the administrative correction procedure."),
                LanguageManager.T("Cannot cancel"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        using var form = new CancelCivilStatusForm(selected);
        if (form.ShowDialog(this) != DialogResult.OK) {
            return;
        }

        var result = await _service.CancelAsync(
            selected.CivilStatusId,
            new CancelCivilStatusDto(form.Reason),
            _session.UserId ?? 1);

        if (result.IsFailure) {
            MessageBox.Show(
                string.Join(Environment.NewLine, result.Errors),
                LanguageManager.T("Cannot cancel"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        MessageBox.Show(
            LanguageManager.T("Civil status registration cancelled."),
            LanguageManager.T("Success"),
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);

        await LoadDataAsync();
    }
}