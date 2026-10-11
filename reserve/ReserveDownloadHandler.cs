namespace NotesApi.Reserve;

// Резервный тренировочный фрагмент (не подключен к приложению).
//
// ВНИМАНИЕ (учебный объект): НАМЕРЕННЫЙ ДЕФЕКТ того же класса, что REVIEW-3 в
// основном объекте. REVIEW-R1: рассмотрите, как из reportName собирается путь к
// файлу и что ограничивает результат каталогом отчетов. Пройдите полный цикл
// (воспроизведение - локализация - классификация - исправление - доказательство).

public sealed class ReserveReportStore
{
    private readonly string _root;

    public ReserveReportStore(string root)
    {
        _root = root;
        Directory.CreateDirectory(_root);
    }

    // REVIEW-R1: сборка пути к отчету из reportName.
    public string? ReadReport(string reportName)
    {
        var path = Path.Combine(_root, reportName + ".txt");
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }
}
