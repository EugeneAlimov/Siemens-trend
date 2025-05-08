using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using SiemensTrend.Communication;
using SiemensTrend.Communication.TIA;
using SiemensTrend.Core.Commands;
using SiemensTrend.Core.Logging;
using SiemensTrend.Core.Models;
using SiemensTrend.Storage.TagManagement;

namespace SiemensTrend.ViewModels
{
    /// <summary>
    /// Основная модель представления
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        // Общие поля

        /// <summary>
        /// Логгер для записи событий
        /// </summary>
        protected readonly Logger _logger;

        /// <summary>
        /// Сервис для коммуникации с ПЛК
        /// </summary>
        protected readonly ICommunicationService _communicationService;

        /// <summary>
        /// Адаптер для коммуникации с TIA Portal
        /// </summary>
        private readonly TiaPortalServiceAdapter _tiaAdapter;

        /// <summary>
        /// Свойство для доступа к адаптеру TIA Portal
        /// </summary>
        public TiaPortalServiceAdapter TiaPortalService => _tiaAdapter;

        /// <summary>
        /// Управление тегами
        /// </summary>
        protected readonly TagManager _tagManager;

        /// <summary>
        /// Объединенный список тегов
        /// </summary>
        private ObservableCollection<TagDefinition> _allTags;

        private bool _isConnected;
        private bool _isLoading;
        private string _statusMessage;
        private int _progressValue;

        /// <summary>
        /// Команда для добавления тега в мониторинг
        /// </summary>
        public ICommand AddTagToMonitoringCommand { get; private set; }

        /// <summary>
        /// Событие добавления тега на график
        /// </summary>
        public event EventHandler<TagDefinition> TagAddedToMonitoring;

        /// <summary>
        /// Событие удаления тега с графика
        /// </summary>
        public event EventHandler<TagDefinition> TagRemovedFromMonitoring;

        /// <summary>
        /// Команда для удаления тега из мониторинга
        /// </summary>
        public ICommand RemoveTagFromMonitoringCommand { get; private set; }

        private ObservableCollection<TagDefinition> _availableTags;
        private ObservableCollection<TagDefinition> _monitoredTags;
        private ObservableCollection<TagDefinition> _plcTags;
        private ObservableCollection<TagDefinition> _dbTags;

        /// <summary>
        /// Статус подключения
        /// </summary>
        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        /// <summary>
        /// Флаг загрузки (операция в процессе)
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// Статусное сообщение
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// Значение прогресса (0-100)
        /// </summary>
        public int ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        /// <summary>
        /// Доступные теги
        /// </summary>
        public ObservableCollection<TagDefinition> AvailableTags
        {
            get => _availableTags;
            private set => SetProperty(ref _availableTags, value);
        }

        /// <summary>
        /// Мониторируемые теги
        /// </summary>
        public ObservableCollection<TagDefinition> MonitoredTags
        {
            get => _monitoredTags;
            private set => SetProperty(ref _monitoredTags, value);
        }

        /// <summary>
        /// Теги ПЛК
        /// </summary>
        public ObservableCollection<TagDefinition> PlcTags
        {
            get => _plcTags;
            private set => SetProperty(ref _plcTags, value);
        }

        /// <summary>
        /// Теги DB
        /// </summary>
        public ObservableCollection<TagDefinition> DbTags
        {
            get => _dbTags;
            private set => SetProperty(ref _dbTags, value);
        }

        /// <summary>
        /// Все доступные теги (объединение PlcTags и DbTags)
        /// </summary>
        public ObservableCollection<TagDefinition> AllTags
        {
            get => _allTags;
            private set => SetProperty(ref _allTags, value);
        }

        /// <summary>
        /// Список проектов TIA Portal для выбора
        /// </summary>
        public List<TiaProjectInfo> TiaProjects { get; private set; } = new List<TiaProjectInfo>();

        /// <summary>
        /// Модель представления графика
        /// </summary>
        public ChartViewModel ChartViewModel { get; }

        /// <summary>
        /// Модель представления для работы с тегами
        /// </summary>
        public TagsViewModel TagsViewModel { get; }

        /// <summary>
        /// Конструктор
        /// </summary>
        public MainViewModel(
            Logger logger,
            ICommunicationService communicationService,
            TiaPortalServiceAdapter tiaAdapter,
            TagManager tagManager,
            ChartViewModel chartViewModel)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _communicationService = communicationService ?? throw new ArgumentNullException(nameof(communicationService));
            _tiaAdapter = tiaAdapter ?? throw new ArgumentNullException(nameof(tiaAdapter));
            _tagManager = tagManager ?? throw new ArgumentNullException(nameof(tagManager));
            ChartViewModel = chartViewModel ?? throw new ArgumentNullException(nameof(chartViewModel));

            // Логируем информацию о типах сервисов
            _logger.Info($"MainViewModel: Инициализирован с коммуникационным сервисом типа {_communicationService.GetType().FullName}");

            // Инициализируем коллекции
            AvailableTags = new ObservableCollection<TagDefinition>();
            MonitoredTags = new ObservableCollection<TagDefinition>();
            PlcTags = new ObservableCollection<TagDefinition>();
            DbTags = new ObservableCollection<TagDefinition>();
            AllTags = new ObservableCollection<TagDefinition>();

            // Инициализируем команды
            AddTagToMonitoringCommand = new RelayCommand<TagDefinition>(AddTagToMonitoring);
            RemoveTagFromMonitoringCommand = new RelayCommand<TagDefinition>(RemoveTagFromMonitoring);

            // Инициализируем начальные значения
            IsConnected = false;
            IsLoading = false;
            StatusMessage = "Готов к работе";
            ProgressValue = 0;

            // Инициализируем TagsViewModel
            TagsViewModel = new TagsViewModel(_logger, _communicationService, _tagManager);

            // Подписка на события
            SubscribeToTagsEvents();
            SubscribeToConnectionEvents();

            _logger.Info("MainViewModel инициализирован успешно");
        }

        /// <summary>
        /// Подписка на события изменения состояния подключения
        /// </summary>
        private void SubscribeToConnectionEvents()
        {
            // Подписываемся на событие изменения состояния подключения
            _communicationService.ConnectionStateChanged += (sender, isConnected) =>
            {
                IsConnected = isConnected;
                _logger.Info($"Состояние подключения изменилось: {isConnected}");
            };
        }

        /// <summary>
        /// Инициализация приложения
        /// </summary>
        public void Initialize()
        {
            try
            {
                _logger.Info("Initialize: Инициализация приложения");

                // Загружаем теги из хранилища
                LoadTagsFromStorage();

                _logger.Info("Initialize: Инициализация завершена");
            }
            catch (Exception ex)
            {
                _logger.Error($"Initialize: Ошибка при инициализации: {ex.Message}");
            }
        }

        /// <summary>
        /// Загружает теги из хранилища
        /// </summary>
        private void LoadTagsFromStorage()
        {
            try
            {
                _logger.Info("LoadTagsFromStorage: Загрузка тегов из хранилища");

                var tags = _tagManager.LoadTags();

                // Очищаем текущие коллекции
                PlcTags.Clear();
                DbTags.Clear();
                AvailableTags.Clear();

                // Распределяем теги по коллекциям
                foreach (var tag in tags)
                {
                    AvailableTags.Add(tag);

                    if (tag.IsDbTag)
                    {
                        DbTags.Add(tag);
                    }
                    else
                    {
                        PlcTags.Add(tag);
                    }
                }

                // Обновляем объединенный список тегов
                UpdateAllTags();

                _logger.Info($"LoadTagsFromStorage: Загружено {PlcTags.Count} тегов PLC и {DbTags.Count} тегов DB");
            }
            catch (Exception ex)
            {
                _logger.Error($"LoadTagsFromStorage: Ошибка загрузки тегов: {ex.Message}");
            }
        }

        /// <summary>
        /// Сохраняет теги в хранилище
        /// </summary>
        public void SaveTagsToStorage()
        {
            try
            {
                _logger.Info("SaveTagsToStorage: Сохранение тегов в хранилище");

                var allTags = new List<TagDefinition>();
                allTags.AddRange(PlcTags);
                allTags.AddRange(DbTags);

                _tagManager.SaveTags(allTags);

                _logger.Info($"SaveTagsToStorage: Сохранено {allTags.Count} тегов");
                StatusMessage = "Теги сохранены";
            }
            catch (Exception ex)
            {
                _logger.Error($"SaveTagsToStorage: Ошибка сохранения тегов: {ex.Message}");
                StatusMessage = "Ошибка сохранения тегов";
            }
        }

        /// <summary>
        /// Метод для обновления списка всех тегов
        /// </summary>
        private void UpdateAllTags()
        {
            try
            {
                if (AllTags == null)
                {
                    AllTags = new ObservableCollection<TagDefinition>();
                }

                AllTags.Clear();

                // Добавляем PLC теги
                foreach (var tag in PlcTags)
                {
                    AllTags.Add(tag);
                }

                // Добавляем DB теги
                foreach (var tag in DbTags)
                {
                    AllTags.Add(tag);
                }

                // Уведомляем представление об изменении
                OnPropertyChanged(nameof(AllTags));
            }
            catch (Exception ex)
            {
                _logger.Error($"UpdateAllTags: Ошибка при обновлении списка всех тегов: {ex.Message}");
            }
        }

        /// <summary>
        /// Добавляет новый тег
        /// </summary>
        public void AddNewTag(TagDefinition tag)
        {
            if (tag == null)
                return;

            try
            {
                _logger.Info($"AddNewTag: Добавление нового тега: {tag.Name}");

                // Проверяем, существует ли уже тег с таким именем
                if (TagExists(tag.Name))
                {
                    _logger.Warn($"AddNewTag: Тег с именем {tag.Name} уже существует");
                    StatusMessage = $"Тег с именем {tag.Name} уже существует";
                    return;
                }

                // Добавляем тег в соответствующие коллекции
                AvailableTags.Add(tag);

                // Используем явно установленное свойство IsDbTag, а не вычисленное
                if (tag.IsDbTag)
                {
                    DbTags.Add(tag);
                    _logger.Info($"AddNewTag: Добавлен DB тег: {tag.Name}");
                }
                else
                {
                    PlcTags.Add(tag);
                    _logger.Info($"AddNewTag: Добавлен PLC тег: {tag.Name}");
                }

                // Обновляем объединенный список тегов
                UpdateAllTags();

                // Сохраняем изменения
                SaveTagsToStorage();

                StatusMessage = $"Тег {tag.Name} добавлен";
            }
            catch (Exception ex)
            {
                _logger.Error($"AddNewTag: Ошибка при добавлении тега: {ex.Message}");
                StatusMessage = "Ошибка при добавлении тега";
            }
        }

        /// <summary>
        /// Редактирует тег
        /// </summary>
        public void EditTag(TagDefinition originalTag, TagDefinition updatedTag)
        {
            if (originalTag == null || updatedTag == null)
                return;

            try
            {
                _logger.Info($"EditTag: Редактирование тега: {originalTag.Name} -> {updatedTag.Name}");

                // Проверяем, существует ли тег с новым именем, если оно отличается
                if (!originalTag.Name.Equals(updatedTag.Name, StringComparison.OrdinalIgnoreCase) &&
                    TagExists(updatedTag.Name))
                {
                    _logger.Warn($"EditTag: Тег с именем {updatedTag.Name} уже существует");
                    StatusMessage = $"Тег с именем {updatedTag.Name} уже существует";
                    return;
                }

                // Удаляем старый тег
                RemoveTag(originalTag);

                // Добавляем обновленный тег
                AddNewTag(updatedTag);

                _logger.Info($"EditTag: Тег отредактирован успешно");
                StatusMessage = $"Тег {updatedTag.Name} отредактирован";
            }
            catch (Exception ex)
            {
                _logger.Error($"EditTag: Ошибка при редактировании тега: {ex.Message}");
                StatusMessage = "Ошибка при редактировании тега";
            }
        }

        /// <summary>
        /// Удаляет тег
        /// </summary>
        public void RemoveTag(TagDefinition tag)
        {
            if (tag == null)
                return;

            try
            {
                _logger.Info($"RemoveTag: Удаление тега: {tag.Name}");

                // Удаляем тег из всех коллекций
                AvailableTags.Remove(tag);

                if (tag.IsDbTag)
                {
                    DbTags.Remove(tag);
                }
                else
                {
                    PlcTags.Remove(tag);
                }

                // Удаляем из мониторинга, если присутствует
                if (MonitoredTags.Contains(tag))
                {
                    RemoveTagFromMonitoring(tag);
                }

                // Обновляем объединенный список тегов
                UpdateAllTags();

                // Сохраняем изменения
                SaveTagsToStorage();

                _logger.Info($"RemoveTag: Тег {tag.Name} удален");
                StatusMessage = $"Тег {tag.Name} удален";
            }
            catch (Exception ex)
            {
                _logger.Error($"RemoveTag: Ошибка при удалении тега: {ex.Message}");
                StatusMessage = "Ошибка при удалении тега";
            }
        }

        /// <summary>
        /// Проверяет, существует ли тег с указанным именем
        /// </summary>
        private bool TagExists(string tagName)
        {
            if (string.IsNullOrEmpty(tagName))
                return false;

            // Проверяем в PLC тегах
            if (PlcTags.Any(t => t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase)))
                return true;

            // Проверяем в DB тегах
            if (DbTags.Any(t => t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase)))
                return true;

            return false;
        }

        /// <summary>
        /// Добавляет тег в список мониторинга
        /// </summary>
        private void AddTagToMonitoring(TagDefinition tag)
        {
            if (tag == null)
                return;

            try
            {
                // Проверяем лимит тегов для мониторинга
                if (MonitoredTags.Count >= MaxMonitoredTags)
                {
                    _logger.Warn($"AddTagToMonitoring: Достигнут лимит тегов для мониторинга ({MaxMonitoredTags})");
                    StatusMessage = $"Достигнут лимит тегов для мониторинга ({MaxMonitoredTags})";
                    return;
                }

                // Проверяем, не добавлен ли тег уже
                if (MonitoredTags.Any(t => t.Id == tag.Id))
                {
                    _logger.Warn($"AddTagToMonitoring: Тег {tag.Name} уже добавлен в мониторинг");
                    StatusMessage = $"Тег {tag.Name} уже добавлен в мониторинг";
                    return;
                }

                // Добавляем тег в мониторинг
                MonitoredTags.Add(tag);
                _logger.Info($"AddTagToMonitoring: Тег {tag.Name} добавлен в мониторинг");
                StatusMessage = $"Тег {tag.Name} добавлен в мониторинг";

                // Вызываем событие для обновления графика
                TagAddedToMonitoring?.Invoke(this, tag);
            }
            catch (Exception ex)
            {
                _logger.Error($"AddTagToMonitoring: Ошибка при добавлении тега в мониторинг: {ex.Message}");
                StatusMessage = "Ошибка при добавлении тега в мониторинг";
            }
        }

        /// <summary>
        /// Удаляет тег из списка мониторинга
        /// </summary>
        private void RemoveTagFromMonitoring(TagDefinition tag)
        {
            if (tag == null)
                return;

            try
            {
                if (MonitoredTags.Remove(tag))
                {
                    _logger.Info($"RemoveTagFromMonitoring: Тег {tag.Name} удален из мониторинга");
                    StatusMessage = $"Тег {tag.Name} удален из мониторинга";

                    // Вызываем событие для обновления графика
                    TagRemovedFromMonitoring?.Invoke(this, tag);
                }
                else
                {
                    _logger.Warn($"RemoveTagFromMonitoring: Тег {tag.Name} не найден в списке мониторинга");
                    StatusMessage = $"Тег {tag.Name} не найден в списке мониторинга";
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"RemoveTagFromMonitoring: Ошибка при удалении тега из мониторинга: {ex.Message}");
                StatusMessage = "Ошибка при удалении тега из мониторинга";
            }
        }

        /// <summary>
        /// Метод для подписки на события TagsViewModel
        /// </summary>
        private void SubscribeToTagsEvents()
        {
            // Подписываемся на события добавления/удаления тегов из мониторинга
            TagsViewModel.TagAddedToMonitoring += OnTagAddedToMonitoring;
            TagsViewModel.TagRemovedFromMonitoring += OnTagRemovedFromMonitoring;

            // Подписываемся на изменение списка мониторинга для обновления команд
            TagsViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(TagsViewModel.MonitoredTags))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            };
        }

        /// <summary>
        /// Обработчик события добавления тега в мониторинг
        /// </summary>
        private void OnTagAddedToMonitoring(object sender, TagViewModel tag)
        {
            if (tag?.Tag != null && ChartViewModel != null)
            {
                // Добавляем тег на график
                ChartViewModel.AddTag(tag.Tag);
            }
        }

        /// <summary>
        /// Обработчик события удаления тега из мониторинга
        /// </summary>
        private void OnTagRemovedFromMonitoring(object sender, TagViewModel tag)
        {
            if (tag?.Tag != null && ChartViewModel != null)
            {
                // Удаляем тег с графика
                ChartViewModel.RemoveTag(tag.Tag);
            }
        }

        /// <summary>
        /// Соединение с TIA Portal
        /// </summary>
        /// <returns>True если подключение успешно</returns>
        public bool ConnectToTiaPortal()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Проверка запущенных экземпляров TIA Portal...";
                ProgressValue = 10;

                _logger.Info("Запрос подключения к TIA Portal через адаптер");

                // Получаем список открытых проектов
                StatusMessage = "Получение списка открытых проектов...";
                _logger.Info("Запрос списка открытых проектов");

                // ВАЖНО: используем синхронные вызовы для TIA Portal API через адаптер
                List<TiaProjectInfo> openProjects = _tiaAdapter.GetOpenProjects();
                _logger.Info($"Получено {openProjects.Count} открытых проектов");
                ProgressValue = 50;

                // Сохраняем список проектов для последующего выбора
                TiaProjects = openProjects;

                // Для MainWindow - возвращаем false, чтобы показать диалог выбора проекта
                // Даже если есть только один проект, мы всё равно покажем диалог
                if (openProjects.Count > 0)
                {
                    StatusMessage = $"Найдено {openProjects.Count} открытых проектов TIA Portal";
                    _logger.Info($"Найдено {openProjects.Count} открытых проектов. Возвращаем список для выбора.");
                    ProgressValue = 60;
                    IsLoading = false;
                    return false;
                }
                else
                {
                    // Нет открытых проектов
                    StatusMessage = "Открытые проекты не найдены. Выберите файл проекта...";
                    _logger.Info("Открытые проекты не найдены. Потребуется выбрать файл.");
                    ProgressValue = 60;
                    IsLoading = false;
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при поиске проектов TIA Portal: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.Error($"Внутренняя ошибка: {ex.InnerException.Message}");
                }
                StatusMessage = "Ошибка при поиске проектов TIA Portal";
                ProgressValue = 0;
                IsConnected = false;
                IsLoading = false;
                return false;
            }
        }

        /// <summary>
        /// Подключение к конкретному проекту TIA Portal
        /// </summary>
        /// <param name="projectInfo">Информация о проекте</param>
        /// <returns>True если подключение успешно</returns>
        public bool ConnectToSpecificTiaProject(TiaProjectInfo projectInfo)
        {
            try
            {
                if (projectInfo == null)
                {
                    _logger.Error("ConnectToSpecificTiaProject: projectInfo не может быть null");
                    return false;
                }

                IsLoading = true;
                StatusMessage = $"Подключение к проекту {projectInfo.Name}...";
                _logger.Info($"ConnectToSpecificTiaProject: Подключение к проекту {projectInfo.Name}");
                ProgressValue = 60;

                // Используем адаптер для подключения к проекту
                bool result = _tiaAdapter.ConnectToSpecificTiaProject(projectInfo);

                if (result)
                {
                    // Успешное подключение
                    StatusMessage = $"Подключено к проекту: {projectInfo.Name}";
                    _logger.Info($"ConnectToSpecificTiaProject: Успешное подключение к проекту: {projectInfo.Name}");
                    ProgressValue = 100;
                    IsConnected = true;

                    return true;
                }
                else
                {
                    // Ошибка подключения
                    StatusMessage = "Ошибка при подключении к TIA Portal";
                    _logger.Error("ConnectToSpecificTiaProject: Ошибка при подключении к TIA Portal");
                    ProgressValue = 0;
                    IsConnected = false;
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"ConnectToSpecificTiaProject: Ошибка при подключении к проекту {projectInfo?.Name}: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.Error($"ConnectToSpecificTiaProject: Внутренняя ошибка: {ex.InnerException.Message}");
                }
                StatusMessage = ($"Ошибка при подключении к проекту {projectInfo?.Name}");
                ProgressValue = 0;
                IsConnected = false;
                return false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Открытие проекта TIA Portal
        /// </summary>
        /// <param name="projectPath">Путь к файлу проекта TIA Portal</param>
        /// <returns>True если проект успешно открыт</returns>
        public bool OpenTiaProject(string projectPath)
        {
            try
            {
                if (string.IsNullOrEmpty(projectPath))
                {
                    _logger.Error("OpenTiaProject: путь к проекту не может быть пустым");
                    return false;
                }

                IsLoading = true;
                StatusMessage = $"Открытие проекта TIA Portal...";
                _logger.Info($"OpenTiaProject: Открытие проекта {projectPath}");
                ProgressValue = 20;

                // Предупреждаем пользователя о длительной операции
                StatusMessage = "Открытие проекта TIA Portal. Это может занять некоторое время...";
                ProgressValue = 30;

                // Открытие проекта через адаптер
                _logger.Info("OpenTiaProject: Начинаем открытие проекта");
                bool result = _tiaAdapter.OpenTiaProject(projectPath);
                _logger.Info($"OpenTiaProject: Результат открытия проекта: {result}");

                // Обрабатываем результат
                if (result)
                {
                    // Успешное открытие
                    string projectName = System.IO.Path.GetFileNameWithoutExtension(projectPath);
                    StatusMessage = $"Проект TIA Portal открыт успешно: {projectName}";
                    _logger.Info($"OpenTiaProject: Проект успешно открыт: {projectName}");
                    ProgressValue = 100;
                    IsConnected = true;  // Важно: явно устанавливаем этот флаг!
                }
                else
                {
                    StatusMessage = "Ошибка при открытии проекта TIA Portal";
                    _logger.Error("OpenTiaProject: Ошибка при открытии проекта TIA Portal");
                    ProgressValue = 0;
                    IsConnected = false;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.Error($"OpenTiaProject: Ошибка при открытии проекта TIA Portal: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.Error($"OpenTiaProject: Внутренняя ошибка: {ex.InnerException.Message}");
                }
                StatusMessage = "Ошибка при открытии проекта TIA Portal";
                ProgressValue = 0;
                IsConnected = false;
                return false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Отключение от ПЛК
        /// </summary>
        public void Disconnect()
        {
            if (IsLoading || !IsConnected)
                return;

            try
            {
                _communicationService.Disconnect();
                StatusMessage = "Отключено от ПЛК";

                // Очищаем списки тегов
                AvailableTags.Clear();
                MonitoredTags.Clear();
                PlcTags.Clear();
                DbTags.Clear();

                IsConnected = false;
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при отключении: {ex.Message}");
                StatusMessage = "Ошибка при отключении";
            }
        }

        /// <summary>
        /// Начало мониторинга выбранных тегов
        /// </summary>
        public async Task StartMonitoringAsync()
        {
            if (!IsConnected || MonitoredTags.Count == 0)
                return;

            try
            {
                // Останавливаем мониторинг, если он уже запущен
                await StopMonitoringAsync();

                StatusMessage = "Запуск мониторинга...";

                // Устанавливаем интервал опроса в миллисекундах
                // (можно добавить настройку в UI)
                _communicationService.PollingIntervalMs = 1000;

                // Запускаем мониторинг
                await _communicationService.StartMonitoringAsync(MonitoredTags);
                StatusMessage = $"Мониторинг запущен для {MonitoredTags.Count} тегов";
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при запуске мониторинга: {ex.Message}");
                StatusMessage = "Ошибка при запуске мониторинга";
            }
        }

        /// <summary>
        /// Остановка мониторинга
        /// </summary>
        public async Task StopMonitoringAsync()
        {
            if (!IsConnected)
                return;

            try
            {
                StatusMessage = "Остановка мониторинга...";
                await _communicationService.StopMonitoringAsync();
                StatusMessage = "Мониторинг остановлен";
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при остановке мониторинга: {ex.Message}");
                StatusMessage = "Ошибка при остановке мониторинга";
            }
        }

        /// <summary>
        /// Текущее имя проекта
        /// </summary>
        public string CurrentProjectName
        {
            get
            {
                if (_tiaAdapter != null && _tiaAdapter.CurrentProject != null)
                    return _tiaAdapter.CurrentProject.Name;
                return "Нет проекта";
            }
        }

        /// <summary>
        /// Максимальное количество тегов для мониторинга
        /// </summary>
        public int MaxMonitoredTags => 10;
    }
}