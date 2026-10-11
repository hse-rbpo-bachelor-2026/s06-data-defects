namespace NotesApi;

// Выдача файлов-вложений.
//
// ВНИМАНИЕ (учебный объект): здесь НАМЕРЕННЫЙ ДЕФЕКТ для семинара С06 (это
// артефакт-результат самостоятельной работы). Класс дефекта и способ
// исправления в коде НЕ подписаны сознательно - это задача студента.
// REVIEW-3: рассмотрите, как из параметра name собирается путь к файлу и что
// ограничивает результат каталогом вложений.

public sealed class AttachmentStore
{
    private readonly string _root;

    public AttachmentStore(string root)
    {
        _root = root;
        Directory.CreateDirectory(_root);
        // Демонстрационное вложение (внутри каталога - легальная цель).
        var demo = Path.Combine(_root, "welcome.txt");
        if (!File.Exists(demo))
            File.WriteAllText(demo, "attachment sample content");

        // Служебный файл вне каталога вложений (на уровень выше). Нужен как
        // детерминированная цель для проверки того, что выдача вложений не
        // выходит за пределы своего каталога. Содержимое помечено как приватное.
        var parent = Directory.GetParent(_root)?.FullName ?? _root;
        var secret = Path.Combine(parent, "private-demo.txt");
        if (!File.Exists(secret))
            File.WriteAllText(secret, "PRIVATE demo file outside attachments");
    }

    // Возвращает содержимое вложения по имени (REVIEW-3: см. выше).
    public string? Read(string name)
    {
        var path = Path.Combine(_root, name);
        if (!File.Exists(path))
            return null;
        return File.ReadAllText(path);
    }
}
