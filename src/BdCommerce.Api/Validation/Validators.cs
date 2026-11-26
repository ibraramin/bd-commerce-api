using BdCommerce.Api.Contracts;
using FluentValidation;

namespace BdCommerce.Api.Validation;

public sealed class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.CustomerName)
            .NotEmpty().WithMessage("CustomerName is required.");
        RuleFor(x => x.CustomerEmail)
            .NotEmpty().WithMessage("CustomerEmail is required.")
            .EmailAddress().WithMessage("CustomerEmail must be a valid email address.");
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Items must contain at least one item.");
        RuleForEach(x => x.Items!).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .GreaterThan(0).WithMessage("ProductId must be greater than 0.");
            item.RuleFor(i => i.Quantity)
                .GreaterThanOrEqualTo(1).WithMessage("Quantity must be at least 1.");
        });
    }
}

public sealed class InitiatePaymentRequestValidator : AbstractValidator<InitiatePaymentRequest>
{
    public InitiatePaymentRequestValidator()
    {
        RuleFor(x => x.OrderId)
            .GreaterThan(0).WithMessage("OrderId must be greater than 0.");
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than 0.");
    }
}

public sealed class PaymentCallbackRequestValidator : AbstractValidator<PaymentCallbackRequest>
{
    public PaymentCallbackRequestValidator()
    {
        RuleFor(x => x.PaymentIntentId)
            .NotEmpty().WithMessage("PaymentIntentId is required.");
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.");
    }
}
