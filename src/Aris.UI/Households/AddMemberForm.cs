using Aris.Application.Citizens;
using Aris.Application.Citizens.Dtos;
using Aris.Application.Common;
using Aris.Application.Households;
using Aris.Application.Households.Dtos;

namespace Aris.UI.Households;

/// <summary>Add an UNASSIGNED citizen to an active household (<c>HH-02</c>).</summary>
public sealed class AddMemberForm : Form
{
    private readonly IHouseholdService _householdService;
    private readonly ICitizenService _citizenService;
    private readonly IUserSession _session;
    private readonly int _householdId;

    private readonly ComboBox _citizen = new();
    private readonly DateTimePicker _effectiveDate = new();
    private readonly Button _addButton = new();
    private readonly Button _cancelButton = new();

    public AddMemberForm(
        IHouseholdService householdService,
        ICitizenService citizenService,
        IUserSession session,
        int householdId)
    {
        _householdService = householdService;
        _citizenService = citizenService;
        _session = session;
        _householdId = householdId;

        Text = "ARIS - Add household member";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(500, 245);

        BuildLayout();
        Shown += async (_, _) => await LoadCandidatesAsync();
    }

    private void BuildLayout()
    {
        var title = new Label
        {
            Text = $"Add a member to household #{_householdId}",
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = Color.DarkSlateGray,
            AutoSize = true,
            Location = new Point(20, 15)
        };

        var citizenLabel = new Label
        {
            Text = "UNASSIGNED citizen",
            AutoSize = true,
            Location = new Point(20, 70)
        };

        _citizen.Location = new Point(165, 67);
        _citizen.Size = new Size(300, 23);
        _citizen.DropDownStyle = ComboBoxStyle.DropDownList;
        _citizen.DisplayMember = nameof(CitizenDto.FullName);
        _citizen.ValueMember = nameof(CitizenDto.CitizenId);

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

        _addButton.Text = "Add member";
        _addButton.Location = new Point(265, 175);
        _addButton.Size = new Size(110, 32);
        _addButton.BackColor = Color.SteelBlue;
        _addButton.ForeColor = Color.White;
        _addButton.FlatStyle = FlatStyle.Flat;
        _addButton.Click += AddButton_Click;

        _cancelButton.Text = "Cancel";
        _cancelButton.Location = new Point(385, 175);
        _cancelButton.Size = new Size(90, 32);
        _cancelButton.DialogResult = DialogResult.Cancel;

        AcceptButton = _addButton;
        CancelButton = _cancelButton;

        Controls.AddRange(new Control[]
        {
            title, citizenLabel, _citizen, dateLabel, _effectiveDate,
            _addButton, _cancelButton
        });
    }

    private async Task LoadCandidatesAsync()
    {
        try
        {
            var result = await _citizenService.SearchAsync(new CitizenSearchCriteria
            {
                Status = "UNASSIGNED"
            });

            if (result.IsFailure)
            {
                MessageBox.Show(
                    string.Join(Environment.NewLine, result.Errors),
                    "Cannot load citizens",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            _citizen.DataSource = result.Value?.ToList() ?? new List<CitizenDto>();

            if (_citizen.Items.Count == 0)
            {
                _addButton.Enabled = false;
                MessageBox.Show(
                    "There are no UNASSIGNED citizens. Register a citizen first.",
                    "No candidates",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Unexpected error while loading citizens: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private async void AddButton_Click(object? sender, EventArgs e)
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

        if (_citizen.SelectedValue is not int citizenId)
        {
            MessageBox.Show(
                "Select a citizen.",
                "Missing information",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        _addButton.Enabled = false;

        try
        {
            var result = await _householdService.AddMemberAsync(new AddMemberDto
            {
                HouseholdId = _householdId,
                CitizenId = citizenId,
                EffectiveDate = DateOnly.FromDateTime(_effectiveDate.Value)
            }, userId);

            if (result.IsFailure)
            {
                MessageBox.Show(
                    string.Join(Environment.NewLine, result.Errors),
                    "Cannot add member",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show(
                "The member was added.",
                "Success",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Unexpected error while adding the member: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            _addButton.Enabled = true;
        }
    }
}
