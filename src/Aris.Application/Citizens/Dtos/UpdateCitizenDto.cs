using System;
using System.Collections.Generic;
using System.Text;

namespace Aris.Application.Citizens.Dtos;

public sealed record UpdateCitizenDto
{
    public required string FullName { get; init; }
    public required DateOnly DateOfBirth { get; init; }
    public required string Gender { get; init; }
}