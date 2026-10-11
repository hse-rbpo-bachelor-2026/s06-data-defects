using NotesApi;

// Учебное API "Заметки" для семинара С06 (Guided Defect Lab).
//
// ВНИМАНИЕ (учебный объект): приложение СОДЕРЖИТ НАМЕРЕННЫЕ ДЕФЕКТЫ. Легенда:
// "вам передали чужой проект, поступили жалобы". Задача - пройти по каждому
// дефекту полный цикл: воспроизвести, найти причину, исправить, доказать.
//
// Роль owner передается заголовком X-User для простоты (аутентификация - тема
// Л07, здесь не моделируется). Это упрощение объекта, а не дефект.

var builder = WebApplication.CreateBuilder(args);

// Учебные данные отделены от кода; сброс — tools/reset-demo.ps1 после остановки.
var dataRoot = Path.Combine(AppContext.BaseDirectory, "lab-data");
Directory.CreateDirectory(dataRoot);
var dbConnection =
    Environment.GetEnvironmentVariable("NOTES_DB_CONNECTION")
    ?? $"Data Source={Path.Combine(dataRoot, "notes.db")}";
var attachmentRoot =
    Environment.GetEnvironmentVariable("NOTES_ATTACHMENT_ROOT")
    ?? Path.Combine(dataRoot, "attachments");

builder.Services.AddSingleton(_ => new NotesRepository(dbConnection));
builder.Services.AddSingleton(_ => new AttachmentStore(attachmentRoot));

var app = builder.Build();

static string CurrentUser(HttpRequest req) =>
    req.Headers["X-User"].FirstOrDefault() ?? "alice";

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Поиск заметок (см. REVIEW-1 в NotesRepository).
app.MapGet("/notes/search", (HttpRequest req, string q, NotesRepository repo) =>
{
    var user = CurrentUser(req);
    var found = repo.Search(user, q);
    return Results.Ok(found);
});

// Заметки по тегу (см. REVIEW-4 в NotesRepository).
app.MapGet("/notes/by-tag", (HttpRequest req, string tag, NotesRepository repo) =>
{
    var user = CurrentUser(req);
    return Results.Ok(repo.ByTag(user, tag));
});

// Создание заметки.
//
// REVIEW-2: рассмотрите, что возвращается клиенту и что пишется в лог при сбое
// (ветка catch ниже).
app.MapPost("/notes", async (HttpRequest req, NotesRepository repo, ILogger<Program> log) =>
{
    NoteInput? input = null;
    try
    {
        input = await req.ReadFromJsonAsync<NoteInput>();
        if (input is null || string.IsNullOrWhiteSpace(input.Text))
            return Results.BadRequest(new { error = "text is required" });

        var user = CurrentUser(req);
        var tag = string.IsNullOrWhiteSpace(input.Tag) ? "personal" : input.Tag;
        var note = repo.Add(user, tag, input.Text);
        return Results.Created($"/notes/{note.Id}", note);
    }
    catch (Exception ex)
    {
        // REVIEW-2: сюда попадает внутреннее исключение. Что из этого уходит
        // наружу (ответ) и что сохраняется (лог)?
        log.LogError("Failed to create note. Error: {Error}", ex.ToString());
        return Results.Problem(detail: ex.ToString(), statusCode: 500);
    }
});

// Выдача вложения (см. REVIEW-3 в AttachmentStore).
app.MapGet("/attachments", (string name, AttachmentStore store) =>
{
    var content = store.Read(name);
    return content is null
        ? Results.NotFound()
        : Results.Text(content);
});

app.Run();

public record NoteInput(string Text, string? Tag);

// Точка входа видима тестам.
public partial class Program;
