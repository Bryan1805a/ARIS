using Aris.Application.Common;
using Aris.Application.Transfers;
using Aris.Application.Transfers.Dtos;
using Aris.UI.Localization;

namespace Aris.UI.Transfers;

/// <summary>Checker inbox detail; supports partial approval and maker cancellation.</summary>
public sealed class TransferDecisionForm : Form
{
    private readonly ITransferService _transferService;
    private readonly IUserSession _session;
    private readonly int _requestId;

    private readonly Label _header = new();
    private readonly Label _note = new();
    private readonly DataGridView _items = new();
    private readonly Button _submitButton = new();
    private readonly Button _cancelRequestButton = new();
    private readonly Button _closeButton = new();

    private TransferRequestDetailDto? _detail;
    private List<TransferItemDto> _itemList = new();

    public TransferDecisionForm(
        ITransferService transferService,
        IUserSession session,
        int requestId)
    {
        _transferService = transferService;
        _session = session;
        _requestId = requestId;

        Text = $"{LanguageManager.T("ARIS - Transfer request")} #{requestId}";
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(920, 620);

        BuildLayout();
        Load += async (_, _) => await LoadDetailAsync();
    }

    private void BuildLayout()
    {
        var title = new Label
        {
            Text = $"Transfer request #{_requestId}",
            Font = new Font("Segoe UI", 15F, FontStyle.Bold),
            ForeColor = Color.DarkSlateGray,
            AutoSize = true,
            Location = new Point(20, 15)
        };

        _header.AutoSize = false;
        _header.Size = new Size(870, 80);
        _header.Location = new Point(20, 55);
        _header.ForeColor = Color.DimGray;

        _note.AutoSize = false;
        _note.Size = new Size(870, 22);
        _note.Location = new Point(20, 140);
        _note.ForeColor = Color.Firebrick;

        _items.Location = new Point(20, 170);
        _items.Size = new Size(870, 300);
        _items.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _items.AllowUserToAddRows = false;
        _items.AllowUserToDeleteRows = false;
        _items.RowHeadersVisible = false;
        _items.SelectionMode = DataGridViewSelectionMode.CellSelect;
        _items.MultiSelect = false;
        _items.BackgroundColor = Color.White;
        _items.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        _items.Columns.Add(new DataGridViewCheckBoxColumn
        {
            Name = "Approve",
            HeaderText = "Approve",
            FillWeight = 12,
            ReadOnly = false
        });
        _items.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Citizen",
            HeaderText = "Citizen",
            FillWeight = 28,
            ReadOnly = true
        });
        _items.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "NationalId",
            HeaderText = "National ID",
            FillWeight = 16,
            ReadOnly = true
        });
        _items.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "ItemStatus",
            HeaderText = "Status",
            FillWeight = 14,
            ReadOnly = true
        });
        _items.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "RejectReason",
            HeaderText = "Reject reason (required when not approved)",
            FillWeight = 30,
            ReadOnly = false
        });

        _submitButton.Text = "Submit decision";
        _submitButton.Location = new Point(550, 545);
        _submitButton.Size = new Size(140, 36);
        _submitButton.BackColor = Color.SteelBlue;
        _submitButton.ForeColor = Color.White;
        _submitButton.FlatStyle = FlatStyle.Flat;
        _submitButton.Click += SubmitButton_Click;

        _cancelRequestButton.Text = "Cancel request";
        _cancelRequestButton.Location = new Point(400, 545);
        _cancelRequestButton.Size = new Size(140, 36);
        _cancelRequestButton.BackColor = Color.Firebrick;
        _cancelRequestButton.ForeColor = Color.White;
        _cancelRequestButton.FlatStyle = FlatStyle.Flat;
        _cancelRequestButton.Click += CancelRequestButton_Click;

        _closeButton.Text = "Close";
        _closeButton.Location = new Point(790, 545);
        _closeButton.Size = new Size(100, 36);
        _closeButton.DialogResult = DialogResult.Cancel;

        Controls.AddRange(new Control[]
        {
            title, _header, _note, _items, _submitButton, _cancelRequestButton, _closeButton
        });
    }

    private async Task LoadDetailAsync()
    {
        try
        {
            var result = await _transferService.GetByIdAsync(_requestId);

            if (result.IsFailure || result.Value is null)
            {
                MessageBox.Show(
                    string.Join(Environment.NewLine, result.Errors.DefaultIfEmpty("Request not found.")),
                    "Cannot load request",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                Close();
                return;
            }

            _detail = result.Value;
            _itemList = _detail.Items.ToList();

            var statusLabel = LanguageManager.T("Status:");
            var sourceLabel = LanguageManager.T("Source:");
            var destinationLabel = LanguageManager.T("Destination:");
            var makerLabel = LanguageManager.T("Maker:");
            var requestedLabel = LanguageManager.T("Requested:");
            var householdLabel = LanguageManager.T("Household");
            var noAddress = LanguageManager.T("no address");
            var none = LanguageManager.T("(none)");

            _header.Text =
                $"{_detail.RequestType}    {statusLabel} {_detail.Status}    " +
                $"{LanguageManager.T("Effective date")}: {_detail.EffectiveDate:yyyy-MM-dd}" +
                Environment.NewLine +
                $"{sourceLabel} {householdLabel} #{_detail.SourceHouseholdId} ({_detail.SourceAddressText ?? noAddress})" +
                Environment.NewLine +
                (_detail.DestinationHouseholdId is int destinationHousehold
                    ? $"{destinationLabel} {householdLabel} #{destinationHousehold}"
                    : $"{destinationLabel} {_detail.DestinationAddressText ?? none}") +
                Environment.NewLine +
                $"{makerLabel} {_detail.RequestedByName ?? _detail.RequestedBy.ToString()}    " +
                $"{requestedLabel} {_detail.RequestedAt:yyyy-MM-dd HH:mm}";

            _items.Rows.Clear();

            foreach (var item in _itemList)
            {
                var index = _items.Rows.Add(
                    item.Status == "APPROVED" || item.Status == "PENDING",
                    $"{item.FullName}",
                    item.NationalId,
                    item.Status,
                    item.RejectReason ?? string.Empty);

                if (_detail.Status != "PENDING")
                {
                    _items.Rows[index].ReadOnly = true;
                }
            }

            var isPending = _detail.Status == "PENDING";
            var isMaker = _detail.RequestedBy == _session.UserId;

            _submitButton.Enabled = isPending && !isMaker;
            _cancelRequestButton.Enabled = isPending && isMaker;
            _note.Text = !isPending
                ? $"{LanguageManager.T("Decided by")} {_detail.DecidedByName ?? _detail.DecidedBy?.ToString() ?? "(unknown)"} " +
                  $"{LanguageManager.T("at")} {_detail.DecidedAt?.ToString("yyyy-MM-dd HH:mm") ?? "-"}."
                : isMaker
                    ? LanguageManager.T("You made this request. Another officer must approve or reject it.")
                    : LanguageManager.T("Approve or reject each item. A rejection needs a reason.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Unexpected error while loading the request: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private async void SubmitButton_Click(object? sender, EventArgs e)
    {
        if (_detail is null)
        {
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

        var decisions = new List<TransferItemDecisionDto>();

        for (var i = 0; i < _itemList.Count; i++)
        {
            var approve = Convert.ToBoolean(_items.Rows[i].Cells["Approve"].Value ?? false);
            var reason = Convert.ToString(_items.Rows[i].Cells["RejectReason"].Value)?.Trim();

            if (!approve && string.IsNullOrWhiteSpace(reason))
            {
                MessageBox.Show(
                    $"Enter a rejection reason for {_itemList[i].FullName}.",
                    "Rejection reason required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                _items.CurrentCell = _items.Rows[i].Cells["RejectReason"];
                _items.BeginEdit(true);
                return;
            }

            decisions.Add(new TransferItemDecisionDto
            {
                ItemId = _itemList[i].ItemId,
                Approve = approve,
                RejectReason = approve ? null : reason
            });
        }

        _submitButton.Enabled = false;

        try
        {
            var result = await _transferService.DecideAsync(new TransferDecisionDto
            {
                RequestId = _requestId,
                Items = decisions,
                DecisionNote = "Decision recorded from the transfer inbox."
            }, userId);

            if (result.IsFailure)
            {
                MessageBox.Show(
                    string.Join(Environment.NewLine, result.Errors),
                    "Cannot execute decision",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show(
                "The decision was recorded and the transfer was executed.",
                "Decision saved",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Unexpected error while saving the decision: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            _submitButton.Enabled = true;
        }
    }

    private async void CancelRequestButton_Click(object? sender, EventArgs e)
    {
        if (_detail is null || _session.UserId is not int userId)
        {
            return;
        }

        var confirmation = MessageBox.Show(
            "Cancel this pending transfer request?",
            "Cancel request",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirmation != DialogResult.Yes)
        {
            return;
        }

        var result = await _transferService.CancelAsync(_requestId, userId, "Cancelled by maker.");

        if (result.IsFailure)
        {
            MessageBox.Show(
                string.Join(Environment.NewLine, result.Errors),
                "Cannot cancel request",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        MessageBox.Show(
            "The request was cancelled.",
            "Cancelled",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);

        DialogResult = DialogResult.OK;
        Close();
    }
}
