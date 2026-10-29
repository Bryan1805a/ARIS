using Aris.Application.Transfers.Dtos;
using Aris.Application.Transfers.Validators;
using Xunit;

namespace Aris.UnitTests.Transfers;

public sealed class TransferValidatorTests
{
    private static CreateTransferRequestDto Join() => new()
    {
        RequestType = "JOIN_EXISTING",
        SourceHouseholdId = 1,
        CitizenIds = new[] { 10 },
        DestinationHouseholdId = 2,
        EffectiveDate = new DateOnly(2026, 1, 1)
    };

    private static CreateTransferRequestDto NewHousehold() => new()
    {
        RequestType = "NEW_HOUSEHOLD",
        SourceHouseholdId = 1,
        CitizenIds = new[] { 10, 11 },
        DestinationAddressId = 5,
        NewHeadCitizenId = 10,
        EffectiveDate = new DateOnly(2026, 1, 1)
    };

    private static CreateTransferRequestDto Move() => new()
    {
        RequestType = "HOUSEHOLD_MOVE",
        SourceHouseholdId = 1,
        CitizenIds = new[] { 10, 11 },
        DestinationAddressId = 5,
        EffectiveDate = new DateOnly(2026, 1, 1)
    };

    [Fact]
    public void Valid_shapes_pass()
    {
        var validator = new CreateTransferRequestValidator();

        Assert.True(validator.Validate(Join()).IsValid);
        Assert.True(validator.Validate(NewHousehold()).IsValid);
        Assert.True(validator.Validate(Move()).IsValid);
    }

    [Fact]
    public void Unknown_request_type_fails()
    {
        var dto = Join() with { RequestType = "MOVE_SOMEWHERE" };

        var result = new CreateTransferRequestValidator().Validate(dto);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Join_requires_a_different_destination_household()
    {
        var validator = new CreateTransferRequestValidator();

        Assert.False(validator.Validate(Join() with
        {
            DestinationHouseholdId = null
        }).IsValid);

        Assert.False(validator.Validate(Join() with
        {
            DestinationHouseholdId = 1
        }).IsValid);
    }

    [Fact]
    public void New_household_requires_an_address_and_a_new_head()
    {
        var validator = new CreateTransferRequestValidator();

        Assert.False(validator.Validate(NewHousehold() with
        {
            DestinationAddressId = null
        }).IsValid);

        Assert.False(validator.Validate(NewHousehold() with
        {
            NewHeadCitizenId = null
        }).IsValid);

        Assert.False(validator.Validate(NewHousehold() with
        {
            DestinationHouseholdId = 2
        }).IsValid);
    }

    [Fact]
    public void House_move_cannot_have_a_new_or_successor_head()
    {
        var validator = new CreateTransferRequestValidator();

        Assert.False(validator.Validate(Move() with
        {
            NewHeadCitizenId = 10
        }).IsValid);

        Assert.False(validator.Validate(Move() with
        {
            SuccessorHeadCitizenId = 11
        }).IsValid);
    }

    [Fact]
    public void Invalid_move_missing_address_or_citizens_fails()
    {
        var validator = new CreateTransferRequestValidator();

        Assert.False(validator.Validate(Move() with
        {
            DestinationAddressId = null
        }).IsValid);

        Assert.False(validator.Validate(Move() with
        {
            CitizenIds = Array.Empty<int>()
        }).IsValid);
    }

    [Fact]
    public void Decision_requires_a_reason_for_each_rejection()
    {
        var result = new TransferDecisionValidator().Validate(new TransferDecisionDto
        {
            RequestId = 1,
            Items = new[]
            {
                new TransferItemDecisionDto
                {
                    ItemId = 1,
                    Approve = false
                }
            }
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("rejection reason", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Valid_decision_passes()
    {
        var result = new TransferDecisionValidator().Validate(new TransferDecisionDto
        {
            RequestId = 1,
            Items = new[]
            {
                new TransferItemDecisionDto { ItemId = 1, Approve = true },
                new TransferItemDecisionDto
                {
                    ItemId = 2,
                    Approve = false,
                    RejectReason = "Not eligible"
                }
            },
            DecisionNote = "Checked"
        });

        Assert.True(result.IsValid);
    }
}
