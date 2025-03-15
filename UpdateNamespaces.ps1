# CreateFolderStructure.ps1
# Скрипт для создания структуры папок проекта SiemensTrend

# Конфигурация
$projectPath = "." # Путь к проекту, по умолчанию текущая директория

# Функция для логирования
function Write-Log {
    param (
        [string]$Message,
        [string]$Color = "White"
    )
    
    Write-Host $Message -ForegroundColor $Color
}

# Вывод заголовка
Write-Log "=====================================================================" "Cyan"
Write-Log "  Создание структуры папок для проекта SiemensTrend" "Cyan"
Write-Log "=====================================================================" "Cyan"
Write-Log "Директория проекта: $projectPath" "Yellow"
Write-Log "====================================================================="

# Определяем структуру папок в соответствии с архитектурой
$directories = @(
    "Core\Models", 
    "Core\Interfaces", 
    "Core\Extensions",
    "Communication\TIA", 
    "Communication\S7", 
    "Communication\OpcUa",
    "TagManagement\Models", 
    "TagManagement\Services",
    "Visualization\Charts", 
    "Visualization\Controls",
    "Alerts\Models", 
    "Alerts\Services",
    "Storage\Project", 
    "Storage\Data",
    "ViewModels",
    "Views"
)

# Счетчики для статистики
$createdFolders = 0
$existingFolders = 0

# Создаем каждую директорию, если она не существует
foreach ($dir in $directories) {
    $fullPath = Join-Path -Path $projectPath -ChildPath $dir
    
    if (-not (Test-Path -Path $fullPath)) {
        New-Item -Path $fullPath -ItemType Directory | Out-Null
        Write-Log "✅ Создана директория: $dir" -Color "Green"
        $createdFolders++
    } else {
        Write-Log "⏩ Директория уже существует: $dir" -Color "Gray"
        $existingFolders++
    }
}

Write-Log "====================================================================="
Write-Log "Готово! Создано директорий: $createdFolders, уже существует: $existingFolders" "Cyan"
Write-Log "====================================================================="

# Дополнительные инструкции
Write-Log "`nСледующие шаги:" "Yellow"
Write-Log "1. Переместите файлы в соответствующие директории" "White"
Write-Log "2. Обновите пространства имен в файлах в соответствии с их новым расположением" "White"
Write-Log "   Например, для файлов в Core/Models используйте namespace SiemensTrend.Core.Models" "White"
Write-Log "3. Обновите ссылки в .csproj файле" "White"
Write-Log "4. Выполните сборку проекта для проверки изменений" "White"
