using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using SiemensTrend.Communication.TIA;
using SiemensTrend.Core.Logging;
using SiemensTrend.ViewModels;

namespace SiemensTrend.Views
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly Logger _logger;

        public MainWindow()
        {
            InitializeComponent();

            // Создаем логер
            // Создаем логер
            _logger = new Logger();

            // Создаем и устанавливаем модель представления
            _viewModel = new MainViewModel(_logger);
            DataContext = _viewModel;

            // Инициализируем TagBrowserViewModel
            _viewModel.InitializeTagBrowser();

            // Устанавливаем DataContext для TagBrowserView
            tagBrowser.DataContext = _viewModel.TagBrowserViewModel;

            // Инициализируем начальное состояние
            UpdateConnectionState();
        }

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
                    _logger.Info($"Выбран проект для открытия: {projectPath}");

                    // Запускаем процесс открытия проекта и подключения к нему
                    _viewModel.StatusMessage = $"Открытие проекта: {Path.GetFileNameWithoutExtension(projectPath)}...";
                    _ = _viewModel.OpenTiaProject(projectPath);
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
        /// Обновление состояния подключения в интерфейсе
        /// </summary>
        private void UpdateConnectionState()
        {
            try
            {
                // Обновляем статус подключения
                statusConnectionState.Text = _viewModel.IsConnected ? "Подключено" : "Отключено";
                statusConnectionState.Foreground = _viewModel.IsConnected ?
                    System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;

                // Обновляем название проекта
                statusProjectName.Text = _viewModel.CurrentProjectName;

                // Обновляем доступность кнопок
                btnConnect.IsEnabled = !_viewModel.IsConnected;
                btnDisconnect.IsEnabled = _viewModel.IsConnected;
                btnGetPlcs.IsEnabled = _viewModel.IsConnected;
                btnGetPlcTags.IsEnabled = _viewModel.IsConnected;
                btnGetDbs.IsEnabled = _viewModel.IsConnected;
                btnGetDbTags.IsEnabled = _viewModel.IsConnected;
                btnStartMonitoring.IsEnabled = _viewModel.IsConnected && _viewModel.MonitoredTags.Count > 0;
                btnStopMonitoring.IsEnabled = _viewModel.IsConnected;
                btnExportTags.IsEnabled = _viewModel.IsConnected &&
                    (_viewModel.PlcTags.Count > 0 || _viewModel.DbTags.Count > 0);
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при обновлении состояния подключения: {ex.Message}");
            }
        }

        /// <summary>
        /// Обновление индикатора загрузки
        /// </summary>
        private void UpdateLoadingIndicator()
        {
            // Показываем или скрываем индикатор загрузки
            progressRing.Visibility = _viewModel.IsLoading ?
                Visibility.Visible : Visibility.Collapsed;

            // Показываем или скрываем прогресс-бар в статусной строке
            statusProgressBar.Visibility = _viewModel.IsLoading ?
                Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Подключиться"
        /// </summary>
        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Временно отключаем кнопку, чтобы избежать повторных нажатий
                btnConnect.IsEnabled = false;

                // Пытаемся подключиться к TIA Portal синхронно
                bool connected = _viewModel.ConnectToTiaPortal();

                // Обновляем состояние интерфейса
                UpdateConnectionState();

                if (!connected)
                {
                    // Проверяем, есть ли список проектов для выбора
                    if (_viewModel.TiaProjects != null && _viewModel.TiaProjects.Count > 0)
                    {
                        // Есть несколько открытых проектов, показываем диалог выбора
                        ShowProjectSelectionDialog(_viewModel.TiaProjects);
                    }
                    else
                    {
                        // Нет открытых проектов, предлагаем открыть файл проекта
                        ShowOpenProjectDialog();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при подключении: {ex.Message}");
                MessageBox.Show($"Ошибка при подключении: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Восстанавливаем кнопку в любом случае
                btnConnect.IsEnabled = !_viewModel.IsConnected;
            }
        }

        /// <summary>
        /// Показ диалога выбора проекта TIA Portal
        /// </summary>
        private void ShowProjectSelectionDialog(List<TiaProjectInfo> projects)
        {
            try
            {
                // Создаем экземпляр диалога выбора проекта
                var dialog = new ProjectSelectionDialog(projects);

                // Настраиваем владельца диалога, чтобы он был модальным
                dialog.Owner = this;

                // Показываем диалог
                bool? result = dialog.ShowDialog();

                if (result == true && dialog.SelectedProject != null)
                {
                    // Пользователь выбрал проект, подключаемся к нему
                    _viewModel.StatusMessage = $"Подключение к выбранному проекту: {dialog.SelectedProject.Name}...";

                    // Выполняем подключение синхронно (не используем await!)
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
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при выборе проекта: {ex.Message}");
                MessageBox.Show($"Ошибка при выборе проекта: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Отключиться"
        /// </summary>
        private void BtnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Выполняем отключение
                _viewModel.Disconnect();

                // Обновляем состояние интерфейса
                UpdateConnectionState();
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при отключении: {ex.Message}");
                MessageBox.Show($"Ошибка при отключении: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Получить ПЛК"
        /// </summary>
        private async void BtnGetPlcs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("Запрос списка доступных ПЛК");

                // Здесь будет вызов соответствующего метода ViewModel
                // Например: await _viewModel.GetAvailablePlcsAsync();

                // Пока просто выводим сообщение
                _viewModel.StatusMessage = "Получение списка ПЛК...";
                await Task.Delay(500); // Имитация работы
                _viewModel.StatusMessage = "Список ПЛК получен";

                _logger.Info("Список ПЛК получен");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении списка ПЛК: {ex.Message}");
                MessageBox.Show($"Ошибка при получении списка ПЛК: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Получить теги ПЛК"
        /// </summary>
        private async void BtnGetPlcTags_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("Запрос тегов ПЛК");

                // Проверяем соединение перед запросом тегов
                if (!_viewModel.IsConnected)
                {
                    _logger.Warn("Попытка получить теги без установленного соединения");
                    MessageBox.Show("Необходимо сначала подключиться к TIA Portal.",
                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Отключаем кнопку на время загрузки
                btnGetPlcTags.IsEnabled = false;
                _viewModel.IsLoading = true;
                _viewModel.StatusMessage = "Получение тегов ПЛК...";
                _viewModel.ProgressValue = 10;

                await _viewModel.GetPlcTagsAsync();

                _logger.Info($"Получено {_viewModel.PlcTags.Count} тегов ПЛК");
                _viewModel.StatusMessage = $"Получено {_viewModel.PlcTags.Count} тегов ПЛК";
                _viewModel.ProgressValue = 100;

                // Проверяем состояние соединения после получения тегов
                if (!_viewModel.IsConnected)
                {
                    _logger.Warn("Соединение было потеряно после получения тегов");
                    MessageBox.Show("Соединение с TIA Portal было потеряно. Пожалуйста, подключитесь снова.",
                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении тегов ПЛК: {ex.Message}");
                MessageBox.Show($"Ошибка при получении тегов ПЛК: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                _viewModel.StatusMessage = "Ошибка при получении тегов ПЛК";
                _viewModel.ProgressValue = 0;
            }
            finally
            {
                btnGetPlcTags.IsEnabled = _viewModel.IsConnected;
                _viewModel.IsLoading = false;
                UpdateConnectionState();
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Получить теги DB"
        /// </summary>
        private async void BtnGetDbTags_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("Запрос тегов DB");

                // Отключаем кнопку на время загрузки
                btnGetDbTags.IsEnabled = false;
                // Показываем индикатор загрузки
                _viewModel.IsLoading = true;
                _viewModel.StatusMessage = "Получение тегов DB...";
                _viewModel.ProgressValue = 10;

                await _viewModel.GetDbTagsAsync();

                _logger.Info($"Получено {_viewModel.DbTags.Count} тегов DB");
                _viewModel.StatusMessage = $"Получено {_viewModel.DbTags.Count} тегов DB";
                _viewModel.ProgressValue = 100;
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении тегов DB: {ex.Message}");
                MessageBox.Show($"Ошибка при получении тегов DB: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                _viewModel.StatusMessage = "Ошибка при получении тегов DB";
                _viewModel.ProgressValue = 0;
            }
            finally
            {
                // Включаем кнопку обратно
                btnGetDbTags.IsEnabled = _viewModel.IsConnected;
                // Скрываем индикатор загрузки
                _viewModel.IsLoading = false;
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Получить DB"
        /// </summary>
        private async void BtnGetDbs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("Запрос списка блоков данных");

                // Отключаем кнопку на время загрузки
                btnGetDbs.IsEnabled = false;
                // Показываем индикатор загрузки
                _viewModel.IsLoading = true;
                _viewModel.StatusMessage = "Получение блоков данных...";
                _viewModel.ProgressValue = 10;

                // Здесь в реальном проекте будет вызов к модели представления
                await Task.Delay(800); // Имитация работы

                _viewModel.StatusMessage = "Блоки данных получены";
                _viewModel.ProgressValue = 100;

                _logger.Info("Блоки данных получены");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении блоков данных: {ex.Message}");
                MessageBox.Show($"Ошибка при получении блоков данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                _viewModel.StatusMessage = "Ошибка при получении блоков данных";
                _viewModel.ProgressValue = 0;
            }
            finally
            {
                // Включаем кнопку обратно
                btnGetDbs.IsEnabled = _viewModel.IsConnected;
                // Скрываем индикатор загрузки
                _viewModel.IsLoading = false;
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Запустить мониторинг"
        /// </summary>
        private async void BtnStartMonitoring_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("Запуск мониторинга тегов");

                await _viewModel.StartMonitoringAsync();

                _logger.Info("Мониторинг запущен");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при запуске мониторинга: {ex.Message}");
                MessageBox.Show($"Ошибка при запуске мониторинга: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Остановить мониторинг"
        /// </summary>
        private async void BtnStopMonitoring_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("Остановка мониторинга тегов");

                await _viewModel.StopMonitoringAsync();

                _logger.Info("Мониторинг остановлен");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при остановке мониторинга: {ex.Message}");
                MessageBox.Show($"Ошибка при остановке мониторинга: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Экспорт тегов"
        /// </summary>
        private async void BtnExportTags_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Создаем диалог сохранения файла
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "CSV файлы (*.csv)|*.csv",
                    Title = "Экспорт тегов",
                    DefaultExt = ".csv",
                    AddExtension = true
                };

                // Если пользователь выбрал файл
                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;
                    _logger.Info($"Экспорт тегов в файл: {filePath}");

                    // Здесь будет вызов соответствующего метода ViewModel
                    // Например: await _viewModel.ExportTagsAsync(filePath);

                    // Пока просто выводим сообщение
                    _viewModel.StatusMessage = "Экспорт тегов...";
                    await Task.Delay(1000); // Имитация работы
                    _viewModel.StatusMessage = "Теги экспортированы";

                    _logger.Info("Теги успешно экспортированы");
                    MessageBox.Show("Теги успешно экспортированы",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при экспорте тегов: {ex.Message}");
                MessageBox.Show($"Ошибка при экспорте тегов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Сохранить лог"
        /// </summary>
        private void BtnSaveLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Создаем диалог сохранения файла
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Текстовые файлы (*.txt)|*.txt",
                    Title = "Сохранение лога",
                    DefaultExt = ".txt",
                    AddExtension = true
                };

                // Если пользователь выбрал файл
                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;
                    _logger.Info($"Сохранение лога в файл: {filePath}");

                    // Сохраняем содержимое лога в файл
                    File.WriteAllText(filePath, txtLog.Text);

                    _logger.Info("Лог успешно сохранен");
                    MessageBox.Show("Лог успешно сохранен",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при сохранении лога: {ex.Message}");
                MessageBox.Show($"Ошибка при сохранении лога: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Очистить лог"
        /// </summary>
        private void BtnClearLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("Очистка лога");

                // Очищаем содержимое лога
                txtLog.Clear();

                _logger.Info("Лог очищен");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при очистке лога: {ex.Message}");
                MessageBox.Show($"Ошибка при очистке лога: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}