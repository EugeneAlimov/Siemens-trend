using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
        // В MainWindow.xaml.cs, метод ShowOpenProjectDialog()
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
                    bool hasCachedData = _viewModel.CheckCachedProjectData(projectName);

                    if (hasCachedData)
                    {
                        // Спрашиваем пользователя, хочет ли он использовать кэш
                        var dialogResult = MessageBox.Show(
                            $"Найдены кэшированные данные для проекта {projectName}. Хотите использовать их вместо открытия проекта?",
                            "Кэшированные данные",
                            MessageBoxButton.YesNoCancel,
                            MessageBoxImage.Question);

                        if (dialogResult == MessageBoxResult.Yes)
                        {
                            // Загружаем данные из кэша
                            _logger.Info($"Использование кэшированных данных для проекта {projectName}");
                            _viewModel.LoadCachedProjectDataAsync(projectName);
                            UpdateConnectionState();
                            return;
                        }
                        else if (dialogResult == MessageBoxResult.Cancel)
                        {
                            // Отменяем операцию
                            _logger.Info("Операция отменена пользователем");
                            return;
                        }
                        // Для MessageBoxResult.No - продолжаем открытие проекта
                    }

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
        /// Новая функция для сохранения тегов текущего проекта
        /// </summary>
        private async void SaveTagsToCache()
        {
            if (!_viewModel.IsConnected)
            {
                MessageBox.Show("Необходимо сначала подключиться к проекту TIA Portal",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _logger.Info("Сохранение тегов в кэш...");
                _viewModel.StatusMessage = "Сохранение тегов в кэш...";
                _viewModel.IsLoading = true;
                _viewModel.ProgressValue = 20;

                // Экспортируем теги в XML
                await _viewModel.ExportTagsToXml();

                _viewModel.ProgressValue = 100;
                _viewModel.StatusMessage = "Теги успешно сохранены в кэш";
                _logger.Info("Теги успешно сохранены в кэш");

                MessageBox.Show("Теги успешно сохранены в кэш и будут доступны при следующем открытии проекта",
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при сохранении тегов в кэш: {ex.Message}");
                MessageBox.Show($"Ошибка при сохранении тегов в кэш: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _viewModel.IsLoading = false;
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

                // Получаем список проектов TIA Portal
                bool connected = _viewModel.ConnectToTiaPortal();

                // ConnectToTiaPortal всегда вернет false, чтобы показать диалог выбора
                // В список TiaProjects будут загружены найденные проекты (если есть)
                var projects = _viewModel.TiaProjects;

                if (projects != null && projects.Count > 0)
                {
                    // Есть открытые проекты, показываем диалог выбора
                    ShowProjectChoiceDialog(projects);
                }
                else
                {
                    // Нет открытых проектов, предлагаем открыть файл проекта
                    ShowOpenProjectDialog();
                }

                // Обновляем состояние интерфейса
                UpdateConnectionState();
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
        /// Обработчик для кнопки "Сохранить в кэш"
        /// </summary>
        private void BtnSaveToCache_Click(object sender, RoutedEventArgs e)
        {
            SaveTagsToCache();
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

        /// <summary>
        /// Обработчик кнопки "Очистить кэш"
        /// </summary>
        private void BtnClearCache_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Если подключено - очищаем текущий проект
                if (_viewModel.IsConnected)
                {
                    ClearCurrentCache();
                }
                else
                {
                    // Если не подключено - всегда показываем диалог выбора проекта из кэша
                    ShowClearCacheDialog();
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"BtnClearCache_Click: Ошибка: {ex.Message}");
                MessageBox.Show($"Ошибка при очистке кэша: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Диалог для выбора проекта из кэша для очистки
        /// </summary>
        private void ShowClearCacheDialog()
        {
            try
            {
                // Получаем список проектов в кэше
                var cachedProjects = _viewModel.GetCachedProjects();

                if (cachedProjects.Count == 0)
                {
                    MessageBox.Show("В кэше нет сохраненных проектов",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Создаем диалог выбора проекта
                var dialog = new Window
                {
                    Title = "Выбор проекта для очистки кэша",
                    Width = 400,
                    Height = 300,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    ResizeMode = ResizeMode.NoResize
                };

                // Создаем панель с элементами управления
                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                // Добавляем заголовок
                var label = new TextBlock
                {
                    Text = "Выберите проект для очистки кэша:",
                    Margin = new Thickness(10),
                    TextWrapping = TextWrapping.Wrap
                };
                Grid.SetRow(label, 0);
                grid.Children.Add(label);

                // Добавляем список проектов
                var listBox = new ListBox
                {
                    Margin = new Thickness(10),
                    ItemsSource = cachedProjects,
                    SelectedIndex = cachedProjects.Count > 0 ? 0 : -1
                };
                Grid.SetRow(listBox, 1);
                grid.Children.Add(listBox);

                // Добавляем кнопки
                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(10)
                };
                Grid.SetRow(buttonPanel, 2);

                var cancelButton = new Button
                {
                    Content = "Отмена",
                    Width = 80,
                    Margin = new Thickness(0, 0, 10, 0)
                };
                cancelButton.Click += (s, e) => dialog.DialogResult = false;
                buttonPanel.Children.Add(cancelButton);

                var okButton = new Button
                {
                    Content = "Очистить",
                    Width = 80
                };
                okButton.Click += (s, e) =>
                {
                    if (listBox.SelectedItem != null)
                    {
                        string selectedProject = listBox.SelectedItem as string;

                        // Спрашиваем подтверждение
                        var result = MessageBox.Show($"Вы действительно хотите удалить кэш проекта {selectedProject}?",
                            "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            dialog.DialogResult = true;
                        }
                        else
                        {
                            dialog.DialogResult = false;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Выберите проект из списка",
                            "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                };
                buttonPanel.Children.Add(okButton);

                grid.Children.Add(buttonPanel);

                // Устанавливаем контент и показываем диалог
                dialog.Content = grid;
                bool? result = dialog.ShowDialog();

                // Обрабатываем результат
                if (result == true && listBox.SelectedItem != null)
                {
                    string selectedProject = listBox.SelectedItem as string;
                    bool success = _viewModel.ClearProjectCache(selectedProject);

                    if (success)
                    {
                        MessageBox.Show($"Кэш проекта {selectedProject} успешно очищен",
                            "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Не удалось очистить кэш проекта {selectedProject}",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"ShowClearCacheDialog: Ошибка: {ex.Message}");
                MessageBox.Show($"Ошибка при работе с диалогом очистки кэша: {ex.Message}",
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

        /// <summary>
        /// Очистка кэша текущего проекта
        /// </summary>
        private void ClearCurrentCache()
        {
            try
            {
                string projectName = _viewModel.CurrentProjectName;

                if (string.IsNullOrEmpty(projectName) || projectName == "Нет проекта")
                {
                    MessageBox.Show("Нет активного проекта для очистки кэша",
                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Спрашиваем подтверждение
                var result = MessageBox.Show($"Вы действительно хотите удалить кэш проекта {projectName}?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                // Очищаем кэш
                bool success = _viewModel.ClearCurrentProjectCache();

                if (success)
                {
                    MessageBox.Show($"Кэш проекта {projectName} успешно очищен",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Не удалось очистить кэш проекта {projectName}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"ClearCurrentCache: Ошибка: {ex.Message}");
                MessageBox.Show($"Ошибка при очистке кэша: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}