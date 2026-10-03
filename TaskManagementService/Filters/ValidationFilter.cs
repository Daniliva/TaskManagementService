using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TaskManagementService.Filters;

/// <summary>
/// TODO (spec): validates every action argument that has an IValidator&lt;T&gt; registered in DI, before the action runs.
/// On failure the action is not executed and the answer is 400 application/problem+json (ValidationProblemDetails, Status 400,
/// Title "One or more validation errors occurred."): errors grouped by property name in camelCase ("title"); several problems of several
/// arguments are reported at once; a null argument gives the error key "" with "A request body is required.".
/// Use context.HttpContext.RequestAborted for the validation call.
/// </summary>
public sealed class ValidationFilter : IAsyncActionFilter
{
    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) => throw new NotImplementedException("TODO");
}
