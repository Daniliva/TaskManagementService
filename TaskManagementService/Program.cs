using TaskManagementService.Repositories;
using TaskManagementService.Validators;

namespace TaskManagementService;

/// <summary>
/// TODO (spec): this is the ORIGINAL composition root. Required by the integration tests (ApiTests), see the reference branch:
/// - JsonStringEnumConverter (statuses travel as strings, unknown text -> 400) and ApiBehaviorOptions.InvalidModelStateResponseFactory
///   returning application/problem+json (ValidationProblemDetails);
/// - AddProblemDetails + UseExceptionHandler that answers 500 application/problem+json WITHOUT any exception text;
/// - validators registered as IValidator&lt;T&gt; (CreateTaskDto, UpdateTaskDto, UpdateStatusDto, TaskQuery) + ValidationFilter registered;
/// - TimeProvider.System singleton; /health (AddHealthChecks + MapHealthChecks);
/// - only ONE OpenAPI generator: remove AddOpenApi/MapOpenApi, keep Swashbuckle.
/// </summary>
public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();
        builder.Services.AddSingleton<ITaskRepository, InMemoryTaskRepository>();

        builder.Services.AddScoped<CreateTaskDtoValidator>();
        builder.Services.AddScoped<UpdateStatusDtoValidator>();
        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}
