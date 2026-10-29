using System.Text;

using Aris.Application.Citizens;
using Aris.Application.Citizens.Dtos;
using Aris.Application.Common;

namespace Aris.UI.Citizens;

/// <summary>Citizen profile with household, canonical address and residency timeline.</summary>
public sealed partial class CitizenDetailForm : Form
{
    private readonly ICitizenService _citizenService;
    private readonly IUserSession _session;
    private readonly int _citizenId;
    private readonly TextBox _summary = new();

    private CitizenDetailDto? _detail;

    public CitizenDetailForm(
        ICitizenService citizenService,
        IUserSession session,
        int citizenId)
    {
        _citizenService = citizenService;
        _session = session;
        _citizenId = citizenId;

        InitializeComponent();

        Text = "ARIS - Citizen detail";
        label6.Text = "Citizen Detail";
        cmbGender.Items.Clear();
        cmbGender.Items.AddRange(new object[] { "MALE", "FEMALE", "OTHER" });

        _summary.Location = new Point(420, 70);
        _summary.Size = new Size(355, 300);
        _summary.Multiline = true;
        _summary.ReadOnly = true;
        _summary.ScrollBars = ScrollBars.Vertical;
        _summary.BackColor = Color.White;
        Controls.Add(_summary);

        btnSave.Text = "Update information";
        btnDeactivate.Text = "Deactivate citizen";

        btnSave.Click += SaveButton_Click;
        btnDeactivate.Click += DeactivateButton_Click;

        Load += async (_, _) => await LoadCitizenAsync();
    }

    private async Task LoadCitizenAsync()
    {
        var result = await _citizenService.GetByIdAsync(_citizenId);

        if (result.IsFailure || result.Value is null)
        {
            MessageBox.Show(
                string.Join(Environment.NewLine, result.Errors.DefaultIfEmpty("Citizen not found.")),
                "Cannot load citizen",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            Close();
            return;
        }

        _detail = result.Value;

        txtNationalId.Text = _detail.NationalId;
        txtNationalId.ReadOnly = true;
        txtFullName.Text = _detail.FullName;
        dtpDateOfBirth.Value = _detail.DateOfBirth.ToDateTime(TimeOnly.MinValue);
        cmbGender.SelectedItem = _detail.Gender;
        txtStatus.Text = _detail.Status;
        txtStatus.ReadOnly = true;

        _summary.Text = BuildSummary(_detail);
        Text = $"ARIS - {_detail.FullName}";

        var isInactive = _detail.Status == "INACTIVE";
        btnSave.Enabled = !isInactive;
        btnDeactivate.Enabled = !isInactive;
    }

    private static string BuildSummary(CitizenDetailDto detail)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"Created:    {detail.CreatedAt:yyyy-MM-dd HH:mm}");
        builder.AppendLine($"Updated:    {detail.UpdatedAt:yyyy-MM-dd HH:mm}");
        builder.AppendLine($"Deactivated:{detail.DeactivatedAt?.ToString("yyyy-MM-dd HH:mm") ?? " -"}");
        builder.AppendLine();
        builder.AppendLine($"Household:  {detail.HouseholdId?.ToString() ?? "none"}");
        builder.AppendLine($"Role:       {detail.HouseholdRole ?? " -"}");
        builder.AppendLine($"Address:    {detail.CurrentAddressText ?? "none"}");
        builder.AppendLine();

        if (detail.HouseholdMembers.Count > 0)
        {
            builder.AppendLine("Household members:");
            foreach (var member in detail.HouseholdMembers)
            {
                builder.AppendLine($"  {member.FullName} ({member.NationalId}) {member.Status}");
            }

            builder.AppendLine();
        }

        builder.AppendLine("Residency timeline:");
        if (detail.ResidencyTimeline.Count == 0)
        {
            builder.AppendLine("  (no residences)");
        }
        else
        {
            foreach (var period in detail.ResidencyTimeline)
            {
                var end = period.EndDate?.ToString("yyyy-MM-dd") ?? "present";
                builder.AppendLine(
                    $"  {period.StartDate:yyyy-MM-dd} -> {end}  {period.Status}");
                builder.AppendLine($"    {period.AddressText}");
            }
        }

        return builder.ToString();
    }

    private async void SaveButton_Click(object? sender, EventArgs e)
    {
        if (_detail is null)
        {
            return;
        }

        using var form = new CitizenEditForm(_citizenService, _session, _detail);

        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await LoadCitizenAsync();
        }
    }

    private async void DeactivateButton_Click(object? sender, EventArgs e)
    {
        if (_detail is null || _detail.Status == "INACTIVE")
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

        int? successorId = null;
        var otherMembers = _detail.HouseholdMembers
            .Where(m => m.CitizenId != _detail.CitizenId)
            .ToList();

        if (_detail.HouseholdRole == "HEAD" && otherMembers.Count > 0)
        {
            using var picker = new SuccessorHeadForm(otherMembers);

            if (picker.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            successorId = picker.SelectedCitizenId;
        }

        var consequence = _detail.HouseholdRole == "HEAD" && otherMembers.Count == 0
            ? " This is the last member, so the household and its address will be inactivated."
            : string.Empty;

        var confirmation = MessageBox.Show(
            $"Deactivate {_detail.FullName} ({_detail.NationalId})?{consequence}",
            "Deactivate citizen",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirmation != DialogResult.Yes)
        {
            return;
        }

        btnDeactivate.Enabled = false;

        try
        {
            var result = await _citizenService.DeactivateCitizenAsync(
                _detail.CitizenId, userId, successorId);

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

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Unexpected error while deactivating the citizen: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            btnDeactivate.Enabled = true;
        }
    }
}
