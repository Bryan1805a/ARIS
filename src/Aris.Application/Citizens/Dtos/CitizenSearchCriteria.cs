using System;
using System.Collections.Generic;
using System.Text;

namespace Aris.Application.Citizens.Dtos;

public sealed record CitizenSearchCriteria
{
    public string? NationalId { get; init; }
    public string? FullName { get; init; }
    public string? Status { get; init; }
}