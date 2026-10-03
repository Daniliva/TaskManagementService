using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskManagementService.Repositories;
using Task = System.Threading.Tasks.Task;

namespace TaskManagementService.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Development");
}

public class ApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory factory;

    public ApiTests(ApiFactory factory) => this.factory = factory;

    private HttpClient Client() => this.factory.CreateClient();

    private static StringContent Json(string json) => new(json, Encoding.UTF8, "application/json");

    private static async Task<JsonElement> Body(HttpResponseMessage r)
    {
        using var doc = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
        return doc.RootElement.Clone();
    }

    private static async Task<int> Create(HttpClient c, string title = "Task", string description = "Desc")
    {
        var response = await c.PostAsJsonAsync("/api/tasks", new { title, description });
        response.EnsureSuccessStatusCode();
        return (await Body(response)).GetProperty("id").GetInt32();
    }

    // ---- create ------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Create_returns_201_with_location_and_a_string_status()
    {
        using var c = this.Client();

        var response = await c.PostAsJsonAsync("/api/tasks", new { title = "  Write docs  ", description = " text " });
        var body = await Body(response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Backlog", body.GetProperty("status").GetString());
        Assert.Equal("Write docs", body.GetProperty("title").GetString());
        Assert.Equal("text", body.GetProperty("description").GetString());
        Assert.EndsWith("/api/Tasks/" + body.GetProperty("id").GetInt32(), response.Headers.Location!.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.True(body.TryGetProperty("createdAt", out _));
        Assert.True(body.TryGetProperty("updatedAt", out _));
    }

    [Fact]
    public async Task Create_ignores_a_status_and_id_sent_by_the_client()
    {
        using var c = this.Client();

        var response = await c.PostAsync("/api/tasks", Json("""{"id":999,"title":"x","status":"Done"}"""));
        var body = await Body(response);

        Assert.Equal("Backlog", body.GetProperty("status").GetString());
        Assert.NotEqual(999, body.GetProperty("id").GetInt32());
    }

    [Theory]
    [InlineData("""{"title":"","description":"d"}""", "title")]
    [InlineData("""{"title":"   ","description":"d"}""", "title")]
    [InlineData("""{"description":"d"}""", "title")]
    public async Task Create_with_a_bad_title_is_a_validation_problem(string json, string field)
    {
        using var c = this.Client();

        var response = await c.PostAsync("/api/tasks", Json(json));
        var body = await Body(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        Assert.True(body.GetProperty("errors").TryGetProperty(field, out var messages));
        Assert.Equal("Title is required.", messages[0].GetString());
    }

    [Fact]
    public async Task Create_reports_every_problem_at_once()
    {
        using var c = this.Client();

        var response = await c.PostAsJsonAsync("/api/tasks", new { title = new string('a', 101), description = new string('b', 1001) });
        var errors = (await Body(response)).GetProperty("errors");

        Assert.True(errors.TryGetProperty("title", out _));
        Assert.True(errors.TryGetProperty("description", out _));
    }

    [Fact]
    public async Task Create_with_broken_json_is_a_400_problem_and_does_not_create()
    {
        using var c = this.Client();
        var before = (await Body(await c.GetAsync("/api/tasks"))).GetProperty("totalCount").GetInt32();

        var response = await c.PostAsync("/api/tasks", Json("{ not json"));
        var after = (await Body(await c.GetAsync("/api/tasks"))).GetProperty("totalCount").GetInt32();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(before, after);
    }

    [Fact]
    public async Task Create_with_a_null_body_is_a_400()
    {
        using var c = this.Client();

        var response = await c.PostAsync("/api/tasks", Json("null"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_with_the_wrong_content_type_is_415()
    {
        using var c = this.Client();

        var response = await c.PostAsync("/api/tasks", new StringContent("title=x", Encoding.UTF8, "text/plain"));

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    // ---- read --------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Get_by_id_returns_the_task_and_404_problem_for_unknown()
    {
        using var c = this.Client();
        var id = await Create(c, "Find me");

        var found = await c.GetAsync("/api/tasks/" + id);
        var missing = await c.GetAsync("/api/tasks/987654");

        Assert.Equal("Find me", (await Body(found)).GetProperty("title").GetString());
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("application/problem+json", missing.Content.Headers.ContentType!.MediaType);
        Assert.Contains("987654", (await Body(missing)).GetProperty("detail").GetString());
    }

    [Theory]
    [InlineData("/api/tasks/abc")]
    [InlineData("/api/tasks/1.5")]
    [InlineData("/api/tasks/99999999999")]
    public async Task Non_numeric_ids_do_not_reach_the_actions(string url)
    {
        using var c = this.Client();

        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync(url)).StatusCode);
    }

    [Fact]
    public async Task List_is_paged_and_filtered()
    {
        using var c = this.Client();
        var marker = "zzfilter" + Guid.NewGuid().ToString("N")[..6];
        var ids = new List<int>();
        for (var i = 0; i < 5; i++)
        {
            ids.Add(await Create(c, marker + i));
        }

        await c.PatchAsync($"/api/tasks/{ids[0]}/status", Json("""{"status":"InWork"}"""));

        var page = await Body(await c.GetAsync($"/api/tasks?search={marker}&pageSize=2&page=2"));
        var inWork = await Body(await c.GetAsync($"/api/tasks?search={marker}&status=InWork"));

        Assert.Equal(5, page.GetProperty("totalCount").GetInt32());
        Assert.Equal(3, page.GetProperty("totalPages").GetInt32());
        Assert.Equal(2, page.GetProperty("items").GetArrayLength());
        Assert.Equal(2, page.GetProperty("page").GetInt32());
        Assert.Equal(ids[0], inWork.GetProperty("items")[0].GetProperty("id").GetInt32());
        Assert.Equal(1, inWork.GetProperty("totalCount").GetInt32());
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    [InlineData("status=Nope")]
    [InlineData("status=42")]
    [InlineData("page=abc")]
    public async Task List_with_invalid_query_is_a_400_problem(string query)
    {
        using var c = this.Client();

        var response = await c.GetAsync("/api/tasks?" + query);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
    }

    // ---- update / delete ---------------------------------------------------------------------------------------

    [Fact]
    public async Task Put_updates_the_text()
    {
        using var c = this.Client();
        var id = await Create(c, "old");

        var response = await c.PutAsJsonAsync("/api/tasks/" + id, new { title = " new ", description = "d2" });
        var body = await Body(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("new", body.GetProperty("title").GetString());
        Assert.Equal("Backlog", body.GetProperty("status").GetString());
        Assert.Equal("new", (await Body(await c.GetAsync("/api/tasks/" + id))).GetProperty("title").GetString());
    }

    [Fact]
    public async Task Put_cannot_change_the_status()
    {
        using var c = this.Client();
        var id = await Create(c);

        await c.PutAsync("/api/tasks/" + id, Json("""{"title":"t","description":"","status":"Done"}"""));

        Assert.Equal("Backlog", (await Body(await c.GetAsync("/api/tasks/" + id))).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Put_validates_and_404s()
    {
        using var c = this.Client();
        var id = await Create(c);

        Assert.Equal(HttpStatusCode.BadRequest, (await c.PutAsJsonAsync("/api/tasks/" + id, new { title = "", description = "" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.PutAsJsonAsync("/api/tasks/987654", new { title = "t", description = "" })).StatusCode);
    }

    [Fact]
    public async Task Delete_gives_204_then_404()
    {
        using var c = this.Client();
        var id = await Create(c);

        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync("/api/tasks/" + id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.DeleteAsync("/api/tasks/" + id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync("/api/tasks/" + id)).StatusCode);
    }

    // ---- status ------------------------------------------------------------------------------------------------

    [Fact]
    public async Task The_whole_life_of_a_task_with_history()
    {
        using var c = this.Client();
        var id = await Create(c);

        foreach (var status in new[] { "InWork", "Testing", "Done" })
        {
            Assert.Equal(HttpStatusCode.NoContent, (await c.PatchAsync($"/api/tasks/{id}/status", Json($$"""{"status":"{{status}}"}"""))).StatusCode);
        }

        var task = await Body(await c.GetAsync("/api/tasks/" + id));
        var history = await Body(await c.GetAsync($"/api/tasks/{id}/history"));

        Assert.Equal("Done", task.GetProperty("status").GetString());
        Assert.Equal(new[] { "InWork", "Testing", "Done" }, history.EnumerateArray().Select(h => h.GetProperty("to").GetString()));
        Assert.Equal(new[] { "Backlog", "InWork", "Testing" }, history.EnumerateArray().Select(h => h.GetProperty("from").GetString()));
        Assert.NotEqual(task.GetProperty("createdAt").GetString(), task.GetProperty("updatedAt").GetString());
    }

    [Fact]
    public async Task A_refused_status_change_is_409_with_the_allowed_next_statuses()
    {
        using var c = this.Client();
        var id = await Create(c);

        var response = await c.PatchAsync($"/api/tasks/{id}/status", Json("""{"status":"Done"}"""));
        var body = await Body(response);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal("Invalid status transition from Backlog to Done.", body.GetProperty("detail").GetString());
        Assert.Equal(new[] { "InWork" }, body.GetProperty("allowedNext").EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public async Task Leaving_Done_is_refused_with_nothing_allowed()
    {
        using var c = this.Client();
        var id = await Create(c);
        foreach (var s in new[] { "InWork", "Testing", "Done" })
        {
            await c.PatchAsync($"/api/tasks/{id}/status", Json($$"""{"status":"{{s}}"}"""));
        }

        var response = await c.PatchAsync($"/api/tasks/{id}/status", Json("""{"status":"Backlog"}"""));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Empty((await Body(response)).GetProperty("allowedNext").EnumerateArray());
    }

    [Theory]
    [InlineData("""{"status":"Nope"}""")]
    [InlineData("""{"status":99}""")]
    [InlineData("""{"status":""}""")]
    [InlineData("""{}""")]
    [InlineData("null")]
    public async Task Invalid_status_values_are_400(string json)
    {
        using var c = this.Client();
        var id = await Create(c);

        var response = await c.PatchAsync($"/api/tasks/{id}/status", Json(json));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Status_change_and_history_of_an_unknown_task_are_404()
    {
        using var c = this.Client();

        Assert.Equal(HttpStatusCode.NotFound, (await c.PatchAsync("/api/tasks/987654/status", Json("""{"status":"InWork"}"""))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync("/api/tasks/987654/history")).StatusCode);
    }

    [Fact]
    public async Task Status_is_accepted_case_insensitively()
    {
        using var c = this.Client();
        var id = await Create(c);

        Assert.Equal(HttpStatusCode.NoContent, (await c.PatchAsync($"/api/tasks/{id}/status", Json("""{"status":"inwork"}"""))).StatusCode);
    }

    // ---- concurrency and platform ------------------------------------------------------------------------------

    [Fact]
    public async Task Parallel_creates_over_http_get_unique_ids()
    {
        using var c = this.Client();

        var ids = await Task.WhenAll(Enumerable.Range(0, 60).Select(i => Create(c, "p" + i)));

        Assert.Equal(60, ids.Distinct().Count());
    }

    [Fact]
    public async Task Parallel_identical_status_changes_over_http_let_exactly_one_through()
    {
        using var c = this.Client();
        var id = await Create(c);

        var codes = await Task.WhenAll(Enumerable.Range(0, 30).Select(async _ => (await c.PatchAsync($"/api/tasks/{id}/status", Json("""{"status":"InWork"}"""))).StatusCode));

        Assert.Equal(1, codes.Count(x => x == HttpStatusCode.NoContent));
        Assert.Equal(29, codes.Count(x => x == HttpStatusCode.Conflict));
    }

    [Fact]
    public async Task Health_is_ok()
    {
        using var c = this.Client();

        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/health")).StatusCode);
    }

    [Fact]
    public async Task There_is_exactly_one_openapi_document_generator()
    {
        using var c = this.Client();

        var swagger = await c.GetAsync("/swagger/v1/swagger.json");
        var duplicate = await c.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, swagger.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, duplicate.StatusCode);
        var paths = (await Body(swagger)).GetProperty("paths").EnumerateObject().Select(p => p.Name).ToList();
        Assert.Contains("/api/Tasks", paths);
        Assert.Contains("/api/Tasks/{id}", paths);
        Assert.Contains("/api/Tasks/{id}/status", paths);
        Assert.Contains("/api/Tasks/{id}/history", paths);
    }

    [Fact]
    public async Task An_unexpected_failure_is_a_500_problem_without_internals()
    {
        using var broken = this.factory.WithWebHostBuilder(b => b.ConfigureServices(s =>
        {
            s.RemoveAll<ITaskRepository>();
            s.AddSingleton<ITaskRepository, ExplodingRepository>();
        }));
        using var c = broken.CreateClient();

        var response = await c.GetAsync("/api/tasks/1");
        var text = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        Assert.DoesNotContain("secret connection string", text, StringComparison.Ordinal);
        Assert.DoesNotContain("   at ", text, StringComparison.Ordinal);
    }

    private sealed class ExplodingRepository : ITaskRepository
    {
        private static InvalidOperationException Boom() => new("secret connection string leaked");

        public Task<Models.Task> CreateAsync(Models.Task task) => throw Boom();

        public Task<IEnumerable<Models.Task>> GetAllAsync() => throw Boom();

        public Task<Models.PagedResult<Models.Task>> QueryAsync(Models.TaskQuery query) => throw Boom();

        public Task<Models.Task?> GetByIdAsync(int id) => throw Boom();

        public Task<Models.Task> UpdateAsync(int id, string title, string description) => throw Boom();

        public Task UpdateStatusAsync(int id, Models.TaskStatus newStatus) => throw Boom();

        public Task<bool> DeleteAsync(int id) => throw Boom();

        public Task<IReadOnlyList<Models.TaskHistoryEntry>> GetHistoryAsync(int id) => throw Boom();
    }
}
