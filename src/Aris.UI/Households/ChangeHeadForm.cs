using Aris.Application.Common;
using Aris.Application.Households;
using Aris.Application.Households.Dtos;

namespace Aris.UI.Households;

/// <summary>Change the household head with close-and-open tenures (<c>HH-04</c>).</summary>
public sealed class ChangeHeadForm : Form
{
    private readonly IHouseholdService _householdService;
    private readonly IUserSession _session;
    private readonly int _householdId;

    private readonly ComboBox _newHead = new();
    private readonly DateTimePicker _effectiveDate = new();
    private readonly Button _changeButton = new();
    private readonly Button _cancelButton = new();

    public ChangeHeadForm(
        IHouseholdService householdService,
        IUserSession session,
        int householdId,
        IReadOnlyList<HouseholdMemberDto> members)
    {
        _householdService = householdService;
        _session = session;
        _householdId = householdId;

        Text = "ARIS - Change household head";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(500, 245);

        BuildLayout(members);
    }

    private void BuildLayout(IReadOnlyList<HouseholdMemberDto> members)
    {
        var title = new Label
        {
            Text = $"Change the head of household #{_householdId}",
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = Color.DarkSlateGray,
            AutoSize = true,
            Location = new Point(20, 15)
        };

        var headLabel = new Label
        {
            Text = "New head",
            AutoSize = true,
            Location = new Point(20, 70)
        };

        _newHead.Location = new Point(165, 67);
        _newHead.Size = new Size(300, 23);
        _newHead.DropDownStyle = ComboBoxStyle.DropDownList;
        _newHead.DisplayMember = nameof(HouseholdMemberDto.FullName);
        _newHead.ValueMember = nameof(HouseholdMemberDto.CitizenId);
        _newHead.DataSource = members
            .Where(m => m.Status == "ACTIVE" && m.Role == "MEMBER")
            .ToList();

        var dateLabel = new Label
        {
            Text = "Effective date",
            AutoSize = true,
            Location = new Point(20, 110)
        };

        _effectiveDate.Location = new Point(165, 107);
        _effectiveDate.Size = new Size(300, 23);
        _effectiveDate.Format = DateTimePickerFormat.Custom;
        _effectiveDate.CustomFormat = "yyyy-MM-dd";

        _changeButton.Text = "Change head";
        _changeButton.Location = new Point(250, 175);
        _changeButton.Size = new Size(125, 32);
        _changeButton.BackColor = Color.SteelBlue;
        _changeButton.ForeColor = Color.White;
        _changeButton.FlatStyle = FlatStyle.Flat;
        _changeButton.Click += ChangeButton_Click;

        _cancelButton.Text = "Cancel";
        _cancelButton.Location = new Point(385, 175);
        _cancelButton.Size = new Size(90, 32);
        _cancelButton.DialogResult = DialogResult.Cancel;

        if (_newHead.Items.Count == 0)
        {
            _changeButton.Enabled = false;
            title.Text += " - no active MEMBER available";
        }

        AcceptButton = _changeButton;
        CancelButton = _cancelButton;

        Controls.AddRange(new Control[]
        {
            title, headLabel, _newHead, dateLabel, _effectiveDate,
            _changeButton, _cancelButton
        });
    }

    private async void ChangeButton_Click(object? sender, EventArgs e)
    {
        if (_session.UserId is not int userId)
        {
            MessageBox.Show(
                "Your session has expired. Sign in again.",
                "Session expired",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        if (_newHead.SelectedValue is not int newHeadCitizenId)
        {
            MessageBox.Show(
                "Select the new head.",
                "Missing information",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        _changeButton.Enabled = false;

        try
        {
            var result = await _householdService.ChangeHeadAsync(new ChangeHeadDto
            {
                HouseholdId = _householdId,
                NewHeadCitizenId = newHeadCitizenId,
                EffectiveDate = DateOnly.FromDateTime(_effectiveDate.Value)
            }, userId);

            if (result.IsFailure)
            {
                MessageBox.Show(
                    string.Join(Environment.NewLine, result.Errors),
                    "Cannot change head",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show(
                "The household head was changed.",
                "Success",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Unexpected error while changing the head: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            _changeButton.Enabled = true;
        }
    }
}
