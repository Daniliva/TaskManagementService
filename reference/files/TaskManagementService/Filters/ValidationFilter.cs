using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace TaskManagementService.Filters;

/// <summary>
/// Validates every action argument that has an IValidator registered in DI, before the action runs.
/// On failure the action is not executed and the answer is 400 application/problem+json (ValidationProblemDetails):
/// errors grouped by property name (camelCase, e.g. "title"), a null body gives the error key "" with "A request body is required.".
/// </summary>
public sealed class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var errors = new Dictionary<string, List<string>>();
        foreach (var parameter in context.ActionDescriptor.Parameters)
        {
            context.ActionArguments.TryGetValue(parameter.Name, out var argument);
            var validatorType = typeof(IValidator<>).MakeGenericType(parameter.ParameterType);
            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            if (argument is null)
            {
                Add(errors, string.Empty, "A request body is required.");
                continue;
            }

            var result = await validator.ValidateAsync(new ValidationContext<object>(argument), context.HttpContext.RequestAborted);
            foreach (var failure in result.Errors)
            {
                Add(errors, CamelCase(failure.PropertyName), failure.ErrorMessage);
            }
        }

        if (errors.Count > 0)
        {
            var problem = new ValidationProblemDetails(errors.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray()))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
            };
            context.Result = new ObjectResult(problem) { StatusCode = StatusCodes.Status400BadRequest, ContentTypes = { "application/problem+json" } };
            return;
        }

        _ = await next();
    }

    private static void Add(Dictionary<string, List<string>> errors, string key, string message)
    {
        if (!errors.TryGetValue(key, out var list))
        {
            list = new List<string>();
            errors[key] = list;
        }

        list.Add(message);
    }

    private static string CamelCase(string name) => string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];
}
