using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Aris.UI.Citizens;
namespace Aris.UI.Citizens;

public partial class CitizenDetailForm : Form
{
    private readonly CitizenDetailViewModel _viewModel;

    public CitizenDetailForm(CitizenDetailViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
    }

    private async void CitizenDetailForm_Load(object sender, EventArgs e)
    {
        var detail = await _viewModel.LoadCitizenDetailsAsync();
        if (detail != null)
        {
            txtNationalId.Text = detail.NationalId;
            txtNationalId.ReadOnly = true;
            txtFullName.Text = detail.FullName;
            dtpDateOfBirth.Value = detail.DateOfBirth.ToDateTime(TimeOnly.MinValue);
            cmbGender.SelectedItem = detail.Gender;
            txtStatus.Text = detail.Status;
            txtStatus.ReadOnly = true;
        }
    }
}
