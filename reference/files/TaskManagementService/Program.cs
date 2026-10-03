using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TaskManagementService.DTOs;
using TaskManagementService.Filters;
using TaskManagementService.Models;
using TaskManagementService.Repositories;
using TaskManagementService.Validators;

namespace TaskManagementService;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services
            .AddControllers()
            .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddProblemDetails();
        builder.Services.AddHealthChecks();

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<ITaskRepository, InMemoryTaskRepository>();

        builder.Services.AddScoped<IValidator<CreateTaskDto>, CreateTaskDtoValidator>();
        builder.Services.AddScoped<IValidator<UpdateTaskDto>, UpdateTaskDtoValidator>();
        builder.Services.AddScoped<IValidator<UpdateStatusDto>, UpdateStatusDtoValidator>();
        builder.Services.AddScoped<IValidator<TaskQuery>, TaskQueryValidator>();
        builder.Services.AddScoped<ValidationFilter>();

        // Model binding errors (bad JSON, unknown enum text) must look like validation errors too.
        builder.Services.Configure<ApiBehaviorOptions>(o => o.InvalidModelStateResponseFactory = ctx =>
            new ObjectResult(new ValidationProblemDetails(ctx.ModelState) { Status = StatusCodes.Status400BadRequest })
            {
                StatusCode = StatusCodes.Status400BadRequest,
                ContentTypes = { "application/problem+json" },
            });

        var app = builder.Build();

        app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
        {
            // Never show the exception to the client; it is already logged by the framework.
            var problem = new ProblemDetails { Status = StatusCodes.Status500InternalServerError, Title = "An unexpected error occurred." };
            context.Response.StatusCode = problem.Status.Value;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(problem, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json");
        }));

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapHealthChecks("/health");
        app.MapControllers();
        app.Run();
    }
}

public partial class Program
{
}
