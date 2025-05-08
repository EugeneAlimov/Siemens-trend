using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using SiemensTrend.Communication;
using SiemensTrend.Communication.TIA;
using SiemensTrend.Core.Logging;
using SiemensTrend.Core.Models;
using SiemensTrend.ViewModels;
using SiemensTrend.Views.Dialogs;

namespace SiemensTrend.Views
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Модель представления
        /// </summary>
        private readonly MainViewModel _viewModel;

        /// <summary>
        /// Логгер
        /// </summary>
        private readonly Logger _logger;

        /// <summary>
        /// Коммуникационный сервис
        /// </summary>
        private readonly ICommunicationService _communicationService;

        /// <summary>
        /// Конструктор
        /// </summary>
        public MainWindow(Logger logger, MainViewModel viewModel, ICommunicationService communicationService)
        {
            InitializeComponent();

            // Используем инжектированные зависимости
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _communicationService = communicationService ?? throw new ArgumentNullException(nameof(communicationService));

            // Логируем тип коммуникационного сервиса
            _logger.Info($"MainWindow: Инициализирован с коммуникационным сервисом типа {_communicationService.GetType().FullName}");

            // Устанавливаем контекст данных
            DataContext = _viewModel;

            // Инициализируем интерфейс
            InitializeUI();

            // Загружаем теги при запуске
            _viewModel.Initialize();

            // Обновляем состояние интерфейса
            UpdateConnectionState();

            // Подписываемся на события
            _communicationService.DataReceived += CommunicationService_DataReceived;
        }

        /// <summary>
        /// Инициализирует компоненты пользовательского интерфейса
        /// </summary>
        private void InitializeUI()
        {
            // Добавьте логику инициализации UI здесь.
            // Например, установка значений по умолчанию, привязка данных или настройка элементов управления.
            _logger.Info("UI успешно инициализирован.");
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
                    Brushes.Green : Brushes.Red;

                // Обновляем название проекта
                statusProjectName.Text = _viewModel.CurrentProjectName;

                // Обновляем доступность кнопок
                btnConnect.IsEnabled = !_viewModel.IsConnected;
                btnDisconnect.IsEnabled = _viewModel.IsConnected;
                btnStartMonitoring.IsEnabled = _viewModel.IsConnected && _viewModel.MonitoredTags.Count > 0;
                btnStopMonitoring.IsEnabled = _viewModel.IsConnected;
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при обновлении состояния подключения: {ex.Message}");
            }
        }

        /// <summary>
        /// Обработчик события получения новых данных
        /// </summary>
        private void CommunicationService_DataReceived(object sender, TagDataReceivedEventArgs e)
        {
            try
            {
                // Добавляем данные на график
                Dispatcher.Invoke(() =>
                {
                    // Проверяем, что компонент графика существует
                    if (chartView != null)
                    {
                        chartView.AddDataPoints(e.DataPoints);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при обработке новых данных: {ex.Message}");
            }
        }

        /// <summary>
        /// Переопределяем OnContentRendered для инициализации графика после загрузки UI
        /// </summary>
        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            InitializeChart();
        }

        /// <summary>
        /// Метод инициализации графика
        /// </summary>
        private void InitializeChart()
        {
            try
            {
                // Подписываемся на событие изменения временного интервала
                chartView.TimeRangeChanged += (s, interval) =>
                {
                    _logger.Info($"Изменен интервал графика: {interval} сек");
                };

                // Подписываемся на события добавления и удаления тегов
                _viewModel.TagAddedToMonitoring += (s, tag) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        chartView.AddTagToChart(tag);
                    });
                };

                _viewModel.TagRemovedFromMonitoring += (s, tag) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        chartView.RemoveTagFromChart(tag);
                    });
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при инициализации графика: {ex.Message}");
            }
        }

        /// <summary>
        /// Переопределяем OnClosing для отписки от событий
        /// </summary>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);

            // Отписываемся от событий
            if (_communicationService != null)
            {
                _communicationService.DataReceived -= CommunicationService_DataReceived;
            }
        }

        #region Обработчики событий для кнопок в MainWindow.xaml

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
        /// Обработчик нажатия кнопки "Добавить тег"
        /// </summary>
        private void BtnAddTag_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("Вызов диалога добавления тегов");

                // Проверяем подключение к TIA Portal
                if (!_viewModel.IsConnected)
                {
                    _logger.Warn("Попытка добавления тегов без подключения к TIA Portal");
                    MessageBox.Show("Необходимо сначала подключиться к TIA Portal",
                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Создаем диалог для добавления тегов
                var dialog = new AddTagsDialog(_logger, _communicationService);
                dialog.Owner = this;

                // Показываем диалог
                if (dialog.ShowDialog() == true)
                {
                    // Получаем найденные теги
                    var foundTags = dialog.FoundTags;

                    if (foundTags != null && foundTags.Count > 0)
                    {
                        // Добавляем теги в модель
                        foreach (var tag in foundTags)
                        {
                            _viewModel.AddNewTag(tag);
                        }

                        _logger.Info($"Добавлено {foundTags.Count} тегов");
                        _viewModel.StatusMessage = $"Добавлено {foundTags.Count} тегов";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при добавлении тегов: {ex.Message}");
                MessageBox.Show($"Ошибка при добавлении тегов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Редактировать тег"
        /// </summary>
        private void BtnEditTag_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("Вызов диалога редактирования тега");

                // Получаем выбранный тег из таблицы тегов
                TagDefinition selectedTag = dgTags.SelectedItem as TagDefinition;

                if (selectedTag == null)
                {
                    _logger.Warn("Не выбран тег для редактирования");
                    MessageBox.Show("Пожалуйста, выберите тег для редактирования",
                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Создаем диалог для редактирования тега
                var dialog = new TagEditorDialog(selectedTag);
                dialog.Owner = this;

                // Показываем диалог
                if (dialog.ShowDialog() == true)
                {
                    // Получаем отредактированный тег
                    var updatedTag = dialog.Tag;

                    // Обновляем тег в модели
                    _viewModel.EditTag(selectedTag, updatedTag);

                    _logger.Info($"Отредактирован тег: {updatedTag.Name}");
                    _viewModel.StatusMessage = $"Тег {updatedTag.Name} отредактирован";
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при редактировании тега: {ex.Message}");
                MessageBox.Show($"Ошибка при редактировании тега: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Удалить тег"
        /// </summary>
        private void BtnRemoveTag_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("Удаление тега");

                // Получаем выбранный тег из таблицы тегов
                TagDefinition selectedTag = dgTags.SelectedItem as TagDefinition;

                if (selectedTag == null)
                {
                    _logger.Warn("Не выбран тег для удаления");
                    MessageBox.Show("Пожалуйста, выберите тег для удаления",
                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Запрос подтверждения
                var result = MessageBox.Show($"Вы уверены, что хотите удалить тег {selectedTag.Name}?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Удаляем тег из модели
                    _viewModel.RemoveTag(selectedTag);

                    _logger.Info($"Удален тег: {selectedTag.Name}");
                    _viewModel.StatusMessage = $"Тег {selectedTag.Name} удален";
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при удалении тега: {ex.Message}");
                MessageBox.Show($"Ошибка при удалении тега: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Сохранить теги"
        /// </summary>
        private void BtnSaveTags_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("Сохранение тегов");

                // Сохраняем теги
                _viewModel.SaveTagsToStorage();

                _logger.Info("Теги сохранены успешно");
                _viewModel.StatusMessage = "Теги сохранены успешно";
                MessageBox.Show("Теги сохранены успешно",
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при сохранении тегов: {ex.Message}");
                MessageBox.Show($"Ошибка при сохранении тегов: {ex.Message}",
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

        #endregion

        #region Методы для работы с проектами TIA Portal

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

        #endregion
    }
}