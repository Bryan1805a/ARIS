using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

using Aris.Application.Auditing;
using Aris.UI.Localization;

namespace Aris.UI;

/// <summary>
/// Audit viewer with date/actor/entity/action/result filters and row detail (<c>AUD-01</c>).
/// </summary>
public partial class AuditViewerControl : UserControl {
    private readonly IAuditQueryService _auditQueryService;

    private readonly FlowLayoutPanel _filters = new();
    private readonly TextBox _username = new();
    private readonly TextBox _entityType = new();
    private readonly TextBox _action = new();
    private readonly ComboBox _result = new();
    private readonly CheckBox _useFrom = new();
    private readonly CheckBox _useTo = new();
    private readonly DateTimePicker _from = new();
    private readonly DateTimePicker _to = new();
    private readonly Button _searchButton = new();
    private readonly Button _resetButton = new();
    private readonly Label _status = new();
    private readonly TextBox _detail = new();

    private bool _loading;

    public AuditViewerControl(IAuditQueryService auditQueryService) {
        InitializeComponent();
        _auditQueryService = auditQueryService;

        ConfigureGrid();
        BuildLayout();
        UiTheme.ApplyScreen(this);

        // Selection changes update the detail pane without requiring another
        // database request because the full DTO is already bound to the row.
        dgvAuditLogs.SelectionChanged += (_, _) => ShowSelectedDetail();
    }

    public async Task LoadAuditLogsAsync() {
        if (_loading) {
            // Ignore overlapping searches so a slow request cannot be replaced
            // by an older result completing out of order.
            return;
        }

        _loading = true;
        _searchButton.Enabled = false;
        _resetButton.Enabled = false;
        _status.Text = LanguageManager.T("Loading...");

        try {
            // Build criteria at execution time so the request reflects the
            // current controls rather than a stale previous search.
            var result = await _auditQueryService.SearchAsync(BuildCriteria());

            if (result.IsFailure) {
                dgvAuditLogs.DataSource = null;
                _status.Text = string.Empty;
                _detail.Text = string.Join(Environment.NewLine, result.Errors);

                MessageBox.Show(
                    string.Join(Environment.NewLine, result.Errors),
                    LanguageManager.T("Cannot load audit logs"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var logs = result.Value ?? new List<AuditLogDto>();
            // Bind the service result directly; the grid remains read-only and
            // the detail pane uses the selected DTO for inspection.
            dgvAuditLogs.DataSource = logs;
            _status.Text = $"{LanguageManager.T("Rows:")} {logs.Count}";

            if (dgvAuditLogs.Rows.Count > 0 && dgvAuditLogs.Columns.Count > 0) {
                // Select the first result to make the detail pane useful
                // immediately after a successful search.
                dgvAuditLogs.CurrentCell = dgvAuditLogs.Rows[0].Cells[0];
            }

            ShowSelectedDetail();
        } catch (Exception ex) {
            dgvAuditLogs.DataSource = null;
            _status.Text = string.Empty;
            _detail.Text = LanguageManager.T("Select an audit row to inspect it.");

            MessageBox.Show(
                $"Unexpected error while loading audit logs: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        } finally {
            // Always restore controls, including after validation or transport
            // failures, so the user can retry the operation.
            _loading = false;
            _searchButton.Enabled = true;
            _resetButton.Enabled = true;
        }
    }

    private void ConfigureGrid() {
        // Audit rows are evidence, not editable application data; all grid
        // editing operations are disabled.
        dgvAuditLogs.Dock = DockStyle.Fill;
        dgvAuditLogs.Margin = new Padding(8, 4, 8, 4);
        dgvAuditLogs.ReadOnly = true;
        dgvAuditLogs.AutoGenerateColumns = false;
        dgvAuditLogs.AllowUserToAddRows = false;
        dgvAuditLogs.AllowUserToDeleteRows = false;
        dgvAuditLogs.MultiSelect = false;
        dgvAuditLogs.RowHeadersVisible = false;
        dgvAuditLogs.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvAuditLogs.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvAuditLogs.BackgroundColor = Color.White;
        dgvAuditLogs.Columns.Clear();

        AddColumn(nameof(AuditLogDto.AuditLogId), "ID", 55);
        AddColumn(nameof(AuditLogDto.OccurredAt), "Time (UTC)", 140, "yyyy-MM-dd HH:mm:ss");
        AddColumn(nameof(AuditLogDto.Username), "Actor", 130);
        AddColumn(nameof(AuditLogDto.Action), "Action", 170);
        AddColumn(nameof(AuditLogDto.EntityType), "Entity", 130);
        AddColumn(nameof(AuditLogDto.EntityId), "Entity ID", 70);
        AddColumn(nameof(AuditLogDto.Result), "Result", 75);
        AddColumn(nameof(AuditLogDto.CorrelationId), "Correlation", 210);
    }

    private void AddColumn(
        string propertyName,
        string header,
        int fillWeight,
        string? format = null) {
        // Bind columns explicitly to keep the visible audit surface stable if
        // AuditLogDto later gains additional properties.
        var column = new DataGridViewTextBoxColumn {
            DataPropertyName = propertyName,
            HeaderText = LanguageManager.T(header),
            FillWeight = fillWeight,
            SortMode = DataGridViewColumnSortMode.NotSortable
        };

        if (format is not null) {
            column.DefaultCellStyle.Format = format;
        }

        dgvAuditLogs.Columns.Add(column);
    }

    private void BuildLayout() {
        Controls.Remove(dgvAuditLogs);

        ConfigureFilters();

        var detailPanel = new Panel {
            Dock = DockStyle.Fill,
            Padding = new Padding(8, 4, 8, 8)
        };

        var detailLabel = new Label {
            Text = LanguageManager.T("Audit detail"),
            Dock = DockStyle.Top,
            Height = 24,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };

        _detail.Dock = DockStyle.Fill;
        _detail.Multiline = true;
        _detail.ReadOnly = true;
        _detail.ScrollBars = ScrollBars.Both;
        _detail.WordWrap = false;
        _detail.Font = new Font("Consolas", 9F);
        _detail.BackColor = Color.White;
        _detail.Text = LanguageManager.T("Select an audit row to inspect it.");

        detailPanel.Controls.Add(_detail);
        detailPanel.Controls.Add(detailLabel);

        var layout = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3
        };

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 116F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));

        layout.Controls.Add(_filters, 0, 0);
        layout.Controls.Add(dgvAuditLogs, 0, 1);
        layout.Controls.Add(detailPanel, 0, 2);

        Controls.Add(layout);
    }

    private void ConfigureFilters() {
        // Optional date controls are paired with checkboxes so an unchecked
        // date does not accidentally constrain the search.
        _filters.Dock = DockStyle.Fill;
        _filters.AutoScroll = true;
        _filters.WrapContents = true;
        _filters.Padding = new Padding(8, 6, 8, 2);
        _filters.Margin = new Padding(0);

        _useFrom.Text = LanguageManager.T("From (UTC)");
        _useFrom.AutoSize = true;
        _useFrom.Margin = new Padding(8, 8, 2, 4);
        _useFrom.CheckedChanged += (_, _) => _from.Enabled = _useFrom.Checked;

        _from.Format = DateTimePickerFormat.Custom;
        _from.CustomFormat = "yyyy-MM-dd";
        _from.Width = 120;
        _from.Enabled = false;
        _from.Value = DateTime.Today.AddDays(-7);
        _from.Margin = new Padding(2, 4, 8, 4);

        _useTo.Text = LanguageManager.T("To (UTC)");
        _useTo.AutoSize = true;
        _useTo.Margin = new Padding(8, 8, 2, 4);
        _useTo.CheckedChanged += (_, _) => _to.Enabled = _useTo.Checked;

        _to.Format = DateTimePickerFormat.Custom;
        _to.CustomFormat = "yyyy-MM-dd";
        _to.Width = 120;
        _to.Enabled = false;
        _to.Value = DateTime.Today;
        _to.Margin = new Padding(2, 4, 8, 4);

        _username.Width = 190;
        _username.PlaceholderText = LanguageManager.T("Actor / attempted username");
        _username.Margin = new Padding(2, 4, 8, 4);

        _entityType.Width = 150;
        _entityType.PlaceholderText = LanguageManager.T("Entity type");
        _entityType.Margin = new Padding(2, 4, 8, 4);

        _action.Width = 180;
        _action.PlaceholderText = LanguageManager.T("Action (contains)");
        _action.Margin = new Padding(2, 4, 8, 4);

        _result.Width = 110;
        _result.DropDownStyle = ComboBoxStyle.DropDownList;
        _result.Items.AddRange(new object[] {
            LanguageManager.T("All results"),
            "SUCCESS",
            "FAILURE"
        });
        _result.SelectedIndex = 0;
        _result.Margin = new Padding(2, 4, 8, 4);

        _searchButton.Text = LanguageManager.T("Search");
        _searchButton.Width = 90;
        _searchButton.Height = 28;
        _searchButton.Margin = new Padding(8, 4, 4, 4);
        _searchButton.Click += async (_, _) => await LoadAuditLogsAsync();

        _resetButton.Text = LanguageManager.T("Reset filters");
        _resetButton.Width = 110;
        _resetButton.Height = 28;
        _resetButton.Margin = new Padding(4, 4, 8, 4);
        _resetButton.Click += async (_, _) => await ResetFiltersAsync();

        _status.AutoSize = true;
        _status.ForeColor = Color.DimGray;
        _status.Margin = new Padding(8, 10, 4, 4);

        AddFilterLabel("Actor");
        _filters.Controls.Add(_username);
        AddFilterLabel("Entity");
        _filters.Controls.Add(_entityType);
        AddFilterLabel("Action");
        _filters.Controls.Add(_action);
        AddFilterLabel("Result");
        _filters.Controls.Add(_result);

        _filters.Controls.Add(_useFrom);
        _filters.Controls.Add(_from);
        _filters.Controls.Add(_useTo);
        _filters.Controls.Add(_to);
        _filters.Controls.Add(_searchButton);
        _filters.Controls.Add(_resetButton);
        _filters.Controls.Add(_status);

        foreach (var box in new[] { _username, _entityType, _action }) {
            box.KeyDown += async (_, e) => {
                if (e.KeyCode == Keys.Enter) {
                    // Enter provides a quick search shortcut while suppressing
                    // the default control beep.
                    e.SuppressKeyPress = true;
                    await LoadAuditLogsAsync();
                }
            };
        }
    }

    private void AddFilterLabel(string text) {
        _filters.Controls.Add(new Label {
            Text = LanguageManager.T(text),
            AutoSize = true,
            Margin = new Padding(8, 8, 2, 4)
        });
    }

    private async Task ResetFiltersAsync() {
        // Reset both values and enabled date flags before issuing a fresh query.
        _username.Clear();
        _entityType.Clear();
        _action.Clear();
        _result.SelectedIndex = 0;
        _useFrom.Checked = false;
        _useTo.Checked = false;
        _from.Value = DateTime.Today.AddDays(-7);
        _to.Value = DateTime.Today;

        await LoadAuditLogsAsync();
    }

    private AuditSearchCriteria BuildCriteria() {
        // The end date is exclusive and advances by one day so a selected date
        // includes the entire UTC calendar day.
        var result = _result.SelectedIndex <= 0
            ? null
            : _result.SelectedItem?.ToString();

        return new AuditSearchCriteria {
            FromUtc = _useFrom.Checked
                ? DateTime.SpecifyKind(_from.Value.Date, DateTimeKind.Utc)
                : null,
            ToUtcExclusive = _useTo.Checked
                ? DateTime.SpecifyKind(_to.Value.Date.AddDays(1), DateTimeKind.Utc)
                : null,
            Username = NullIfBlank(_username.Text),
            EntityType = NullIfBlank(_entityType.Text),
            Action = NullIfBlank(_action.Text),
            Result = result,
            Limit = 500
        };
    }

    private static string? NullIfBlank(string value) {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private void ShowSelectedDetail() {
        if (dgvAuditLogs.CurrentRow?.DataBoundItem is not AuditLogDto log) {
            // Avoid displaying details from a row that is no longer selected
            // after rebinding or clearing the data source.
            _detail.Text = LanguageManager.T("Select an audit row to inspect it.");
            return;
        }

        var entity = log.EntityId is int entityId
            ? $"{log.EntityType} #{entityId}"
            : log.EntityType;

        var builder = new StringBuilder();
        builder.AppendLine($"{LanguageManager.T("ID:")} {log.AuditLogId}");
        builder.AppendLine(
            $"{LanguageManager.T("Time (UTC):")} {log.OccurredAt:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine($"{LanguageManager.T("Actor:")} {log.Username}");
        builder.AppendLine($"{LanguageManager.T("Action:")} {log.Action}");
        builder.AppendLine($"{LanguageManager.T("Entity:")} {entity}");
        builder.AppendLine($"{LanguageManager.T("Result:")} {log.Result}");
        builder.AppendLine(
            $"{LanguageManager.T("Correlation:")} {log.CorrelationId?.ToString() ?? "-"}");
        builder.AppendLine();
        builder.AppendLine(LanguageManager.T("Details:"));
        builder.AppendLine(PrettyPrint(log.Details));

        _detail.Text = builder.ToString();
        _detail.SelectionLength = 0;
    }

    private static string PrettyPrint(string? json) {
        if (string.IsNullOrWhiteSpace(json)) {
            return LanguageManager.T("No details");
        }

        try {
            // Format valid JSON for readability, but preserve non-JSON legacy
            // details rather than hiding information behind a parse error.
            using var document = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(
                document.RootElement,
                new JsonSerializerOptions { WriteIndented = true });
        } catch (JsonException) {
            return json;
        }
    }
}