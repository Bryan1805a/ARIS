using System;
using System.Collections.Generic;
using System.Text;

namespace Aris.Application.Citizens.Dtos;

public sealed record CitizenDetailDto
{
    public required int CitizenId { get; init; }
    public required string NationalId { get; init; }
    public required string FullName { get; init; }
    public required DateOnly DateOfBirth { get; init; }
    public required string Gender { get; init; }
    public required string Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? DeactivatedAt { get; init; }

    /// <summary>Current household, when the citizen has an active membership.</summary>
    public int? HouseholdId { get; init; }

    /// <summary><c>HEAD</c> or <c>MEMBER</c> in the current household.</summary>
    public string? HouseholdRole { get; init; }

    /// <summary>Active members of the current household, including this citizen.</summary>
    public IReadOnlyList<CitizenDto> HouseholdMembers { get; init; }
        = Array.Empty<CitizenDto>();

    /// <summary>Canonical address of the current household, resolved through address lineage.</summary>
    public string? CurrentAddressText { get; init; }

    /// <summary>Residency history, newest first. Voided rows are excluded.</summary>
    public IReadOnlyList<ResidencyPeriodDto> ResidencyTimeline { get; init; }
        = Array.Empty<ResidencyPeriodDto>();
}
