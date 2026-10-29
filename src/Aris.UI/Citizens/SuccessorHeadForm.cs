using Aris.Application.Citizens.Dtos;

namespace Aris.UI.Citizens;

/// <summary>Choose the successor head when the departing citizen is the head.</summary>
public sealed class SuccessorHeadForm : Form
{
    private readonly ComboBox _members = new();
    private readonly Button _okButton = new();
    private readonly Button _cancelButton = new();

    public SuccessorHeadForm(IReadOnlyList<CitizenDto> candidates)
    {
        Text = "ARIS - Choose successor head";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(440, 190);

        var label = new Label
        {
            Text = "The departing citizen is the household head. Choose a successor:",
            AutoSize = false,
            Size = new Size(400, 40),
            Location = new Point(20, 20)
        };

        _members.Location = new Point(20, 70);
        _members.Size = new Size(400, 23);
        _members.DropDownStyle = ComboBoxStyle.DropDownList;
        _members.DataSource = candidates.ToList();
        _members.DisplayMember = nameof(CitizenDto.FullName);
        _members.ValueMember = nameof(CitizenDto.CitizenId);

        _okButton.Text = "Use as head";
        _okButton.Location = new Point(200, 125);
        _okButton.Size = new Size(110, 30);
        _okButton.BackColor = Color.SteelBlue;
        _okButton.ForeColor = Color.White;
        _okButton.FlatStyle = FlatStyle.Flat;
        _okButton.Click += OkButton_Click;

        _cancelButton.Text = "Cancel";
        _cancelButton.Location = new Point(320, 125);
        _cancelButton.Size = new Size(100, 30);
        _cancelButton.DialogResult = DialogResult.Cancel;

        AcceptButton = _okButton;
        CancelButton = _cancelButton;

        Controls.AddRange(new Control[] { label, _members, _okButton, _cancelButton });
    }

    /// <summary>The chosen citizen id after <see cref="DialogResult.OK"/>.</summary>
    public int? SelectedCitizenId { get; private set; }

    private void OkButton_Click(object? sender, EventArgs e)
    {
        if (_members.SelectedValue is int citizenId)
        {
            SelectedCitizenId = citizenId;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
