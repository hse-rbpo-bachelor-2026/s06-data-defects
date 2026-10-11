using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Data.Sqlite;
using Xunit;

namespace NotesApi.Tests;

// Каждый тест получает собственную базу и файлы. Запущенное на 8080 API
// не используется: WebApplicationFactory создает отдельный тестовый сервер.
public sealed class NotesTests : IDisposable
{
    private readonly DemoFactory _factory = new();

    private HttpClient ClientAs(string user)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User", user);
        return client;
    }

    [Fact]
    public async Task Health_Ok()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Search_Own_Notes_Works()
    {
        using var client = ClientAs("alice");
        var notes = await client.GetFromJsonAsync<List<Note>>("/notes/search?q=private");
        Assert.NotNull(notes);
        // Проверка владельца пустого списка сама по себе ничего не подтверждает.
        Assert.Equal(2, notes!.Count);
        Assert.All(notes, n => Assert.Equal("alice", n.Owner));
        Assert.Contains(notes, n => n.Text == "alice private note one");
        Assert.Contains(notes, n => n.Text == "alice private note two");
    }

    [Fact]
    public async Task Create_Note_Works()
    {
        using var client = ClientAs("alice");
        var response = await client.PostAsJsonAsync("/notes", new { text = "seminar-created-note", tag = "work" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<Note>();
        Assert.NotNull(created);
        Assert.Equal("seminar-created-note", created!.Text);
        Assert.Equal("alice", created.Owner);
        var notes = await client.GetFromJsonAsync<List<Note>>("/notes/search?q=seminar-created-note");
        Assert.Single(notes!);
        Assert.Equal(created.Id, notes![0].Id);
    }

    [Fact]
    public async Task Create_Empty_Text_Is_Rejected()
    {
        using var client = ClientAs("alice");
        var response = await client.PostAsJsonAsync("/notes", new { text = "", tag = "work" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Attachment_Valid_File_Is_Served()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/attachments?name=welcome.txt");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("attachment sample content", await response.Content.ReadAsStringAsync());
    }

    // Д1: сначала снимите Skip и наблюдайте падение, затем исправляйте.
    [Fact(Skip = "Д1. Снимите Skip во время первого разбора.")]
    public async Task Search_Injection_Does_Not_Leak_Foreign_Notes()
    {
        using var bob = ClientAs("bob");
        var target = await bob.GetFromJsonAsync<List<Note>>("/notes/search?q=secret");
        Assert.Single(target!); // мишень проверки существует
        using var alice = ClientAs("alice");
        var payload = "%' OR 1=1 -- ";
        var notes = await alice.GetFromJsonAsync<List<Note>>(
            $"/notes/search?q={Uri.EscapeDataString(payload)}");
        Assert.NotNull(notes);
        Assert.All(notes!, n => Assert.Equal("alice", n.Owner));
    }

    [Fact(Skip = "Д2. Дополните тест и снимите Skip во втором блоке.")]
    public async Task Create_Invalid_Json_Is_Rejected_Without_Internal_Details()
    {
        using var client = ClientAs("alice");
        using var content = new StringContent("{ not valid json", System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/notes", content);
        var body = await response.Content.ReadAsStringAsync();
        // TODO: ожидаемый статус для поврежденного JSON.
        // TODO: признаки внутренних деталей, которые не должны уходить клиенту.
        throw new NotImplementedException("Добавьте проверки статуса и содержимого ответа.");
    }

    [Fact(Skip = "Д3. Напишите негативную проверку и снимите Skip.")]
    public async Task Attachment_Outside_Root_Is_Not_Returned()
    {
        using var client = _factory.CreateClient();
        // TODO: запрос, воспроизводящий найденное нарушение.
        // TODO: проверка отказа и отсутствия защищаемого содержимого.
        // Штатный файл проверяет Attachment_Valid_File_Is_Served.
        await Task.CompletedTask;
        throw new NotImplementedException("Добавьте сценарий и проверяемый результат.");
    }

    public void Dispose() => _factory.Dispose();
}

internal sealed class DemoFactory : WebApplicationFactory<Program>
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "rbpo-s06-tests", Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(_root);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<NotesRepository>();
            services.RemoveAll<AttachmentStore>();
            var connection = new SqliteConnectionStringBuilder { DataSource = Path.Combine(_root, "notes.db") }.ToString();
            services.AddSingleton(_ => new NotesRepository(connection));
            services.AddSingleton(_ => new AttachmentStore(Path.Combine(_root, "attachments")));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        }
    }
}
