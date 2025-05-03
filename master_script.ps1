# master_script.ps1
# Мастер-скрипт для запуска всех 5 скриптов последовательно

Write-Host "Начало модификации приложения SiemensTrend..." -ForegroundColor Green

# Запускаем скрипт 1: Создание класса TagManager
Write-Host "Шаг 1: Создание класса TagManager..." -ForegroundColor Cyan
. .\script1_create_tag_manager.ps1

# Запускаем скрипт 2: Создание диалогового окна для редактирования тегов
Write-Host "Шаг 2: Создание диалогового окна для редактирования тегов..." -ForegroundColor Cyan
. .\script2_create_tag_editor_dialog.ps1

# Запускаем скрипт 3: Модификация MainViewModel.cs
Write-Host "Шаг 3: Модификация MainViewModel.cs..." -ForegroundColor Cyan
. .\script3_modify_main_view_model.ps1

# Запускаем скрипт 4: Обновление MainWindow.xaml и добавление обработчиков
Write-Host "Шаг 4: Обновление MainWindow.xaml и добавление обработчиков..." -ForegroundColor Cyan
. .\script4_update_main_window.ps1

# Запускаем скрипт 5: Создание README с описанием изменений
Write-Host "Шаг 5: Создание README с описанием изменений..." -ForegroundColor Cyan
. .\script5_create_readme.ps1

Write-Host "Модификация приложения SiemensTrend завершена успешно!" -ForegroundColor Green
Write-Host "Подробности внесенных изменений можно найти в файле README_CHANGES.md" -ForegroundColor Green