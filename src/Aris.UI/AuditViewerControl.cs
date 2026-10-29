using System;
using System.Windows.Forms;

using Aris.Application.Auditing;
using Aris.Application.Interfaces;

namespace Aris.UI;

public partial class AuditViewerControl : UserControl
{
    private readonly IAuditQueryService _auditQueryService;

    public AuditViewerControl(IAuditQueryService auditQueryService)
    {
        InitializeComponent();

        _auditQueryService = auditQueryService;

        dgvAuditLogs.ReadOnly = true;
        dgvAuditLogs.AutoGenerateColumns = true;
        dgvAuditLogs.AllowUserToAddRows = false;
        dgvAuditLogs.AllowUserToDeleteRows = false;
    }

    public async Task LoadAuditLogsAsync()
    {
        try
        {
            var auditLogs = await _auditQueryService.GetRecentAsync(100);

            dgvAuditLogs.DataSource = auditLogs;

            dgvAuditLogs.Columns["AuditLogId"]?.HeaderText = "ID";
            dgvAuditLogs.Columns["Username"]?.HeaderText = "Username";
            dgvAuditLogs.Columns["EntityType"]?.HeaderText = "Entity";
            dgvAuditLogs.Columns["OccurredAt"]?.HeaderText = "Time";
            dgvAuditLogs.Columns["Details"]?.HeaderText = "Details";

            var occurredAt = dgvAuditLogs.Columns["OccurredAt"];
            if (occurredAt is not null)
            {
                occurredAt.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm:ss";
            }
        }
        catch (Exception)
        {
            MessageBox.Show(
                "Unable to load audit logs. Please try again.",
                "Audit Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}