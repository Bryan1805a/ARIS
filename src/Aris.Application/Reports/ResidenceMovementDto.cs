namespace Aris.Application.Reports;

public sealed class ResidenceMovementDto
{
    public DateOnly ReportDate { get; set; }

    public int NewRegistrations { get; set; }

    public int Transfers { get; set; }

    public int EndedResidences { get; set; }
}