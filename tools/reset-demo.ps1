# Сначала остановите API (Ctrl+C). Код и результаты тестов сохраняются.
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$binaryRoot = Join-Path $repoRoot 'src/NotesApi/bin'
if (-not (Test-Path -LiteralPath $binaryRoot)) {
    Write-Host 'Данных запуска пока нет. Приложение создаст их само.'
    exit 0
}
$targets = @(Get-ChildItem -LiteralPath $binaryRoot -Directory -Recurse -Filter 'lab-data')
foreach ($target in $targets) {
    if ($target.Attributes -band [IO.FileAttributes]::ReparsePoint) {
        throw "Каталог является ссылкой; сброс остановлен: $($target.FullName)"
    }
    Remove-Item -LiteralPath $target.FullName -Recurse -Force
    Write-Host "Удалены учебные данные: $($target.FullName)"
}
Write-Host 'Сброс завершен. Следующий запуск восстановит данные.'
