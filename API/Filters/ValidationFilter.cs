using Application.DTOs;
using InvoiceHub.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace InvoiceHub.API.Filters;

/// <summary>
/// Intercepts ModelState validation errors before the action runs.
/// Returns a consistent ApiResponse 400 instead of the default ProblemDetails.
/// </summary>
public class ValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .ToList();

            context.Result = new BadRequestObjectResult(
                ApiResponse<object>.Fail("Validation failed.", errors));
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}