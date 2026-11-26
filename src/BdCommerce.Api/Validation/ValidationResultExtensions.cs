using FluentValidation.Results;
using Microsoft.AspNetCore.Http;

namespace BdCommerce.Api.Validation;

public static class ValidationResultExtensions
{
    /// <summary>Turns validation failures into a 400 with per-field, human-readable errors.</summary>
    public static IResult ToValidationProblem(this ValidationResult result)
    {
        var errors = result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray(),
                StringComparer.OrdinalIgnoreCase);

        return Results.BadRequest(new { errors });
    }
}
