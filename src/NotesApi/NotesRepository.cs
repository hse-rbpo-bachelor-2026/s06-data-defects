using Microsoft.Data.Sqlite;

namespace NotesApi;

// Слой доступа к данным учебного API "Заметки".
//
// ВНИМАНИЕ (учебный объект). В этом файле СОДЕРЖАТСЯ НАМЕРЕННЫЕ ДЕФЕКТЫ
// БЕЗОПАСНОСТИ для семинара С06. Это не образец: так писать нельзя.
// Задача студента - самостоятельно найти, воспроизвести, классифицировать,
// исправить и доказать исправление. Маркеры "REVIEW-n" лишь отмечают места,
// которые стоит рассмотреть; НИ класс дефекта, НИ способ исправления в коде
// не подписаны сознательно - это и есть работа семинара.

public sealed class NotesRepository
{
    private readonly string _connectionString;

    public NotesRepository(string connectionString)
    {
        _connectionString = connectionString;
        Initialize();
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    private void Initialize()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS notes (
                id       INTEGER PRIMARY KEY AUTOINCREMENT,
                owner    TEXT NOT NULL,
                tag      TEXT NOT NULL,
                text     TEXT NOT NULL
            );
        """;
        cmd.ExecuteNonQuery();

        // Демонстрационные данные: две заметки принадлежат "alice", одна - "bob".
        // Ими показывается, что инъекция раскрывает чужие записи.
        using var check = conn.CreateCommand();
        check.CommandText = "SELECT COUNT(*) FROM notes;";
        var count = Convert.ToInt64(check.ExecuteScalar());
        if (count == 0)
        {
            Seed(conn, "alice", "personal", "alice private note one");
            Seed(conn, "alice", "work", "alice private note two");
            Seed(conn, "bob", "personal", "bob secret note");
        }
    }

    private static void Seed(SqliteConnection conn, string owner, string tag, string text)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "INSERT INTO notes (owner, tag, text) VALUES ($o, $g, $t);";
        cmd.Parameters.AddWithValue("$o", owner);
        cmd.Parameters.AddWithValue("$g", tag);
        cmd.Parameters.AddWithValue("$t", text);
        cmd.ExecuteNonQuery();
    }

    // Поиск заметок пользователя по подстроке.
    //
    // REVIEW-1: рассмотрите, как параметры owner и q попадают в текст запроса.
    public List<Note> Search(string owner, string q)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT id, owner, tag, text FROM notes " +
            "WHERE owner = '" + owner + "' AND text LIKE '%" + q + "%';";
        return Read(cmd);
    }

    // Список заметок пользователя по тегу.
    //
    // REVIEW-4: рассмотрите построение запроса здесь (по аналогии с REVIEW-1).
    public List<Note> ByTag(string owner, string tag)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT id, owner, tag, text FROM notes " +
            "WHERE owner = '" + owner + "' AND tag = '" + tag + "';";
        return Read(cmd);
    }

    // Создание заметки - здесь параметризация сделана правильно.
    // Оставлено корректным намеренно: показывает контраст с дефектными методами
    // и служит источником ложного срабатывания SAST на позиции 9 (правило
    // может пометить любой доступ к БД, а этот вызов безопасен).
    public Note Add(string owner, string tag, string text)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO notes (owner, tag, text) VALUES ($o, $g, $t);
            SELECT last_insert_rowid();
        """;
        cmd.Parameters.AddWithValue("$o", owner);
        cmd.Parameters.AddWithValue("$g", tag);
        cmd.Parameters.AddWithValue("$t", text);
        var id = Convert.ToInt32(cmd.ExecuteScalar());
        return new Note(id, owner, tag, text);
    }

    private static List<Note> Read(SqliteCommand cmd)
    {
        var result = new List<Note>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new Note(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3)));
        }
        return result;
    }
}

public record Note(int Id, string Owner, string Tag, string Text);
