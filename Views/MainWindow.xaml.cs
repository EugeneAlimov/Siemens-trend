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
            _logger = new Logger();

            // Создаем и устанавливаем модель представления
            _viewModel = new MainViewModel(_logger);
            DataContext = _viewModel;

            // Подписываемся на изменение состояния подключения
            _viewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(_viewModel.IsConnected))
                {
                    UpdateConnectionState();
                }
                else if (args.PropertyName == nameof(_viewModel.IsLoading))
                {
                    UpdateLoadingIndicator();
                }
            };

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
                    _ = _viewModel.OpenTiaProjectAsync(projectPath);
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
            // Обновляем статус подключения
            statusConnectionState.Text = _viewModel.IsConnected ? "Подключено" : "Отключено";
            statusConnectionState.Foreground = _viewModel.IsConnected ?
                System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;

            // Обновляем доступность кнопок
            btnConnect.IsEnabled = !_viewModel.IsConnected;
            btnDisconnect.IsEnabled = _viewModel.IsConnected;
            btnGetPlcs.IsEnabled = _viewModel.IsConnected;
            btnGetPlcTags.IsEnabled = _viewModel.IsConnected;
            btnGetDbs.IsEnabled = _viewModel.IsConnected;
            btnGetDbTags.IsEnabled = _viewModel.IsConnected;
            btnStartMonitoring.IsEnabled = _viewModel.IsConnected;
            btnStopMonitoring.IsEnabled = _viewModel.IsConnected;
            btnExportTags.IsEnabled = _viewModel.IsConnected;
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
        private async void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Пытаемся подключиться к TIA Portal
                bool connected = await _viewModel.ConnectToTiaPortalAsync();

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
                    _ = _viewModel.ConnectToSpecificTiaProjectAsync(dialog.SelectedProject);
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

                // Здесь будет вызов соответствующего метода ViewModel
                // Например: await _viewModel.GetPlcTagsAsync();

                // Пока просто выводим сообщение
                _viewModel.StatusMessage = "Получение тегов ПЛК...";
                await Task.Delay(1000); // Имитация работы
                _viewModel.StatusMessage = "Теги ПЛК получены";

                _logger.Info("Теги ПЛК получены");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении тегов ПЛК: {ex.Message}");
                MessageBox.Show($"Ошибка при получении тегов ПЛК: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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

                // Здесь будет вызов соответствующего метода ViewModel
                // Например: await _viewModel.GetDataBlocksAsync();

                // Пока просто выводим сообщение
                _viewModel.StatusMessage = "Получение блоков данных...";
                await Task.Delay(800); // Имитация работы
                _viewModel.StatusMessage = "Блоки данных получены";

                _logger.Info("Блоки данных получены");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении блоков данных: {ex.Message}");
                MessageBox.Show($"Ошибка при получении блоков данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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

                // Здесь будет вызов соответствующего метода ViewModel
                // Например: await _viewModel.GetDbTagsAsync();

                // Пока просто выводим сообщение
                _viewModel.StatusMessage = "Получение тегов DB...";
                await Task.Delay(1200); // Имитация работы
                _viewModel.StatusMessage = "Теги DB получены";

                _logger.Info("Теги DB получены");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении тегов DB: {ex.Message}");
                MessageBox.Show($"Ошибка при получении тегов DB: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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