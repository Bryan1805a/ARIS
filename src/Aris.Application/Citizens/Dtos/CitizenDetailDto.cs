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
}
