using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using SiemensTrend.Communication.TIA;

namespace SiemensTrend.Views
{
    /// <summary>
    /// Часть класса MainWindow для управления проектами
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Показ диалога открытия проекта TIA Portal
        /// </summary>
        private void ShowOpenProjectDialog()
        {
            try
            {
                // Создаем диалог открытия файла
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "TIA Portal Проекты (*.ap*)|*.ap*",
                    Title = "Открыть проект TIA Portal",
                    CheckFileExists = true
                };

                // Показываем диалог
                if (openFileDialog.ShowDialog() == true)
                {
                    string projectPath = openFileDialog.FileName;
                    string projectName = Path.GetFileNameWithoutExtension(projectPath);
                    _logger.Info($"Выбран проект для открытия: {projectPath}");

                    // Проверяем наличие кэшированных данных
                    //bool hasCachedData = _viewModel.CheckCachedProjectData(projectName);

                    //if (hasCachedData)
                    //{
                    //    // Спрашиваем пользователя, хочет ли он использовать кэш
                    //    var dialogResult = MessageBox.Show(
                    //        $"Найдены кэшированные данные для проекта {projectName}. Хотите использовать их вместо открытия проекта?",
                    //        "Кэшированные данные",
                    //        MessageBoxButton.YesNoCancel,
                    //        MessageBoxImage.Question);

                    //    if (dialogResult == MessageBoxResult.Yes)
                    //    {
                    //        // Загружаем данные из кэша
                    //        _logger.Info($"Использование кэшированных данных для проекта {projectName}");
                    //        _viewModel.LoadCachedProjectDataAsync(projectName);
                    //        UpdateConnectionState();
                    //        return;
                    //    }
                    //    else if (dialogResult == MessageBoxResult.Cancel)
                    //    {
                    //        // Отменяем операцию
                    //        _logger.Info("Операция отменена пользователем");
                    //        return;
                    //    }
                    //    // Для MessageBoxResult.No - продолжаем открытие проекта
                    //}

                    // Запускаем процесс открытия проекта и подключения к нему
                    _viewModel.StatusMessage = $"Открытие проекта: {projectName}...";
                    bool openResult = _viewModel.OpenTiaProject(projectPath);

                    // После открытия, обновляем состояние интерфейса
                    UpdateConnectionState();

                    // Если проект открыт успешно, но подключения нет,
                    // попробуем явно подключиться ещё раз
                    if (openResult && !_viewModel.IsConnected)
                    {
                        _logger.Info("Проект открыт, но подключение не установлено. Попытка подключения...");
                        _viewModel.ConnectToTiaPortal();
                        UpdateConnectionState();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при открытии проекта: {ex.Message}");
                MessageBox.Show($"Ошибка при открытии проекта: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Показ диалога выбора проекта TIA Portal
        /// </summary>
        private void ShowProjectChoiceDialog(List<TiaProjectInfo> projects)
        {
            try
            {
                if (projects == null || projects.Count == 0)
                {
                    _logger.Warn("ShowProjectChoiceDialog: Список проектов пуст или равен null");
                    return;
                }

                // Создаем экземпляр нового диалога выбора проекта
                var dialog = new TiaProjectChoiceDialog(projects);

                // Настраиваем владельца диалога, чтобы он был модальным
                dialog.Owner = this;

                // Показываем диалог
                bool? result = dialog.ShowDialog();

                if (result == true)
                {
                    if (dialog.OpenNewProject)
                    {
                        // Пользователь выбрал открытие нового проекта
                        _logger.Info("Пользователь выбрал открытие нового проекта");
                        ShowOpenProjectDialog();
                    }
                    else if (dialog.SelectedProject != null)
                    {
                        // Пользователь выбрал проект, подключаемся к нему
                        _viewModel.StatusMessage = $"Подключение к выбранному проекту: {dialog.SelectedProject.Name}...";

                        // Выполняем подключение (не используем Task.Run!)
                        bool success = _viewModel.ConnectToSpecificTiaProject(dialog.SelectedProject);

                        // Обновляем интерфейс
                        UpdateConnectionState();

                        if (success)
                        {
                            _logger.Info($"Успешное подключение к проекту: {dialog.SelectedProject.Name}");
                        }
                        else
                        {
                            _logger.Error($"Не удалось подключиться к проекту: {dialog.SelectedProject.Name}");
                            MessageBox.Show($"Не удалось подключиться к проекту: {dialog.SelectedProject.Name}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                else
                {
                    _logger.Info("Пользователь отменил выбор проекта");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при выборе проекта: {ex.Message}");
                MessageBox.Show($"Ошибка при выборе проекта: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}