using Aris.Application.SystemConfiguration;

using System;
using System.Windows.Forms;

namespace Aris.UI;

public partial class SystemConfigurationControl : UserControl
{
    private readonly SystemConfigurationService _service;

    public SystemConfigurationControl(SystemConfigurationService service)
    {
        InitializeComponent();

        _service = service;

        this.Load += SystemConfigurationControl_Load;
        dgvConfigurations.SelectionChanged += dgvConfigurations_SelectionChanged;
        btnSaveChanges.Click += btnSaveChanges_Click;
    }

    private async void SystemConfigurationControl_Load(
        object? sender, EventArgs e)
    {
        try
        {
            var configurations = await _service.GetAllAsync();

            dgvConfigurations.DataSource = configurations;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Configuration Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }
    private void dgvConfigurations_SelectionChanged(
    object? sender, EventArgs e)
    {
        if (dgvConfigurations.CurrentRow?.DataBoundItem
            is not Aris.Domain.Entities.SystemConfiguration config)
        {
            txtConfigValue.Clear();
            btnSaveChanges.Enabled = false;
            return;
        }

        txtConfigValue.Text = config.ConfigValue;
        btnSaveChanges.Enabled = true;
    }

    private async void btnSaveChanges_Click(object? sender, EventArgs e)
    {
        if (dgvConfigurations.CurrentRow?.DataBoundItem
            is not Aris.Domain.Entities.SystemConfiguration config)
        {
            MessageBox.Show(
                "Please select a configuration.",
                "Validation",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        string newValue = txtConfigValue.Text.Trim();

        if (string.IsNullOrWhiteSpace(newValue))
        {
            MessageBox.Show(
                "Configuration value cannot be empty.",
                "Validation",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        btnSaveChanges.Enabled = false;

        try
        {
            bool updated = await _service.UpdateAsync(
                config.ConfigKey,
                newValue);

            if (!updated)
            {
                MessageBox.Show(
                    "Configuration not found.",
                    "Update Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            dgvConfigurations.DataSource = null;
            dgvConfigurations.DataSource = await _service.GetAllAsync();

            MessageBox.Show(
                "Configuration updated successfully.",
                "Success",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Configuration Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            btnSaveChanges.Enabled =
                dgvConfigurations.CurrentRow?.DataBoundItem
                is Aris.Domain.Entities.SystemConfiguration;
        }
    }
}