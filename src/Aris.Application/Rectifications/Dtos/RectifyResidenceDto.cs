namespace Aris.Application.Rectifications.Dtos;

/// <summary>
/// Corrects an erroneous <c>Residence</c> row by voiding it and opening a linked
/// replacement (<c>HIST-01</c>).
/// </summary>
/// <remarks>
/// The replacement keeps the source row's citizen and lifecycle status. When the source
/// is <c>ACTIVE</c> only <see cref="StartDate"/> may change; address and end-date changes
/// on current state belong to a transfer (<c>UC-06</c>) or deactivation (<c>CIT-04</c>).
/// </remarks>
public sealed record RectifyResidenceDto {
    /// <summary>The row being corrected. It must not already be <c>VOID</c>.</summary>
    public required int ResidenceId { get; init; }

    /// <summary>The corrected address. Must equal the source address for an active row.</summary>
    public required int AddressId { get; init; }

    public required DateOnly StartDate { get; init; }

    /// <summary>Required for an <c>ENDED</c> source; must be null for an <c>ACTIVE</c> source.</summary>
    public DateOnly? EndDate { get; init; }

    /// <summary>Why the row is wrong. Stored in the audit details.</summary>
    public required string Reason { get; init; }
}