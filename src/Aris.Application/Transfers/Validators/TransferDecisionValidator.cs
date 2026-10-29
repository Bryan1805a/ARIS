using Aris.Application.Transfers.Dtos;
using FluentValidation;

namespace Aris.Application.Transfers.Validators;

public sealed class TransferDecisionValidator : AbstractValidator<TransferDecisionDto>
{
    public TransferDecisionValidator()
    {
        RuleFor(x => x.RequestId)
            .GreaterThan(0).WithMessage("A transfer request is required.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Decide at least one item.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.ItemId)
                .GreaterThan(0).WithMessage("Item id is required.");

            item.RuleFor(x => x.RejectReason)
                .NotEmpty()
                .When(x => !x.Approve)
                .WithMessage("A rejection reason is required (CK_TRI_Reject).");

            item.RuleFor(x => x.RejectReason)
                .MaximumLength(300)
                .WithMessage("Reject reason must be at most 300 characters.");
        });

        RuleFor(x => x.DecisionNote)
            .MaximumLength(500)
            .WithMessage("Decision note must be at most 500 characters.");
    }
}
