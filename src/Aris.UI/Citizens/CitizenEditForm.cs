using Aris.Application.Citizens;
using Aris.Application.Citizens.Dtos;
using Aris.Application.Common;

namespace Aris.UI.Citizens;

/// <summary>Create or update a citizen's non-identifying demographics.</summary>
public sealed class CitizenEditForm : Form
{
    private readonly ICitizenService _citizenService;
    private readonly IUserSession _session;
    private readonly CitizenDetailDto? _existing;

    private readonly TextBox _nationalId = new();
    private readonly TextBox _fullName = new();
    private readonly DateTimePicker _dateOfBirth = new();
    private readonly ComboBox _gender = new();
    private readonly Button _saveButton = new();
    private readonly Button _cancelButton = new();

    public CitizenEditForm(
        ICitizenService citizenService,
        IUserSession session,
        CitizenDetailDto? existing = null)
    {
        _citizenService = citizenService;
        _session = session;
        _existing = existing;

        Text = existing is null ? "ARIS - Register citizen" : "ARIS - Update citizen";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(470, 315);

        BuildLayout();

        if (existing is not null)
        {
            _nationalId.Text = existing.NationalId;
            _nationalId.ReadOnly = true;
            _fullName.Text = existing.FullName;
            _dateOfBirth.Value = existing.DateOfBirth.ToDateTime(TimeOnly.MinValue);
            _gender.SelectedItem = existing.Gender;
        }
    }

    private void BuildLayout()
    {
        var title = new Label
        {
            Text = _existing is null ? "Register a citizen" : "Update citizen demographics",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = Color.DarkSlateGray,
            AutoSize = true,
            Location = new Point(20, 15)
        };

        var hint = new Label
        {
            Text = _existing is null
                ? "The citizen starts as UNASSIGNED until a household is registered."
                : "National ID is identifying data and cannot be changed.",
            ForeColor = Color.DimGray,
            AutoSize = false,
            Size = new Size(430, 35),
            Location = new Point(20, 50)
        };

        AddRow("National ID", _nationalId, 95);
        _nationalId.MaxLength = 12;

        AddRow("Full name", _fullName, 135);
        _fullName.MaxLength = 100;

        var dobLabel = new Label
        {
            Text = "Date of birth",
            AutoSize = true,
            Location = new Point(20, 178)
        };

        _dateOfBirth.Location = new Point(140, 175);
        _dateOfBirth.Size = new Size(310, 23);
        _dateOfBirth.Format = DateTimePickerFormat.Custom;
        _dateOfBirth.CustomFormat = "yyyy-MM-dd";
        _dateOfBirth.MaxDate = DateTime.Today.AddDays(-1);

        var genderLabel = new Label
        {
            Text = "Gender",
            AutoSize = true,
            Location = new Point(20, 218)
        };

        _gender.Location = new Point(140, 215);
        _gender.Size = new Size(310, 23);
        _gender.DropDownStyle = ComboBoxStyle.DropDownList;
        _gender.Items.AddRange(new object[] { "MALE", "FEMALE", "OTHER" });

        _saveButton.Text = _existing is null ? "Register" : "Save changes";
        _saveButton.Location = new Point(230, 270);
        _saveButton.Size = new Size(110, 30);
        _saveButton.BackColor = Color.SteelBlue;
        _saveButton.ForeColor = Color.White;
        _saveButton.FlatStyle = FlatStyle.Flat;
        _saveButton.Click += SaveButton_Click;

        _cancelButton.Text = "Cancel";
        _cancelButton.Location = new Point(350, 270);
        _cancelButton.Size = new Size(100, 30);
        _cancelButton.DialogResult = DialogResult.Cancel;

        AcceptButton = _saveButton;
        CancelButton = _cancelButton;

        Controls.AddRange(new Control[]
        {
            title, hint, dobLabel, _dateOfBirth, genderLabel, _gender,
            _saveButton, _cancelButton
        });
    }

    private void AddRow(string labelText, TextBox textBox, int top)
    {
        var label = new Label
        {
            Text = labelText,
            AutoSize = true,
            Location = new Point(20, top + 3)
        };

        textBox.Location = new Point(140, top);
        textBox.Size = new Size(310, 23);

        Controls.Add(label);
        Controls.Add(textBox);
    }

    private async void SaveButton_Click(object? sender, EventArgs e)
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

        var fullName = _fullName.Text.Trim();
        var gender = _gender.SelectedItem?.ToString();
        var dateOfBirth = DateOnly.FromDateTime(_dateOfBirth.Value);

        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(gender))
        {
            MessageBox.Show(
                "Full name and gender are required.",
                "Missing information",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        _saveButton.Enabled = false;

        try
        {
            Result result;

            if (_existing is null)
            {
                var created = await _citizenService.CreateCitizenAsync(
                    new CreateCitizenDto
                    {
                        NationalId = _nationalId.Text.Trim(),
                        FullName = fullName,
                        DateOfBirth = dateOfBirth,
                        Gender = gender
                    },
                    userId);

                result = created;
            }
            else
            {
                result = await _citizenService.UpdateCitizenAsync(
                    _existing.CitizenId,
                    new UpdateCitizenDto
                    {
                        FullName = fullName,
                        DateOfBirth = dateOfBirth,
                        Gender = gender
                    },
                    userId);
            }

            if (result.IsFailure)
            {
                MessageBox.Show(
                    string.Join(Environment.NewLine, result.Errors),
                    "Cannot save citizen",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Unexpected error while saving the citizen: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            _saveButton.Enabled = true;
        }
    }
}
