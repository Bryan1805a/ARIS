using System;
using System.Collections.Generic;
using System.Text;

namespace Aris.Application.Citizens.Dtos;

public sealed record CreateCitizenDto
{
    public required string NationalId { get; init; }
    public required string FullName { get; init; }
    public required DateOnly DateOfBirth { get; init; }
    public required string Gender { get; init; }
}