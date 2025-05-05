using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
    /// Main view model
    /// </summary>
    public partial class MainViewModel : ViewModelBase
    {
        // Общие поля для всех частичных классов MainViewModel

        /// <summary>
        /// Logger for event recording
        /// </summary>
        protected readonly Logger _logger;

        /// <summary>
        /// Service for communication with PLC
        /// </summary>
        public ICommunicationService _communicationService;

        /// <summary>
        /// Tag manager for manual tag management
        /// </summary>
        protected TagManager _tagManager;

        /// Свойство для объединенного списка тегов
        private ObservableCollection<TagDefinition> _allTags;

        // Поле для работы с TIA Portal - оставляем только в этом файле,
        public TiaPortalCommunicationService TiaPortalService => _tiaPortalService;

        private TiaPortalCommunicationService _tiaPortalService;

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
        /// Connection status
        /// </summary>
        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        /// <summary>
        /// Loading flag (operation in progress)
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// Status message
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// Progress value (0-100)
        /// </summary>
        public int ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        /// <summary>
        /// Available tags
        /// </summary>
        public ObservableCollection<TagDefinition> AvailableTags
        {
            get => _availableTags;
            private set => SetProperty(ref _availableTags, value);
        }

        /// <summary>
        /// Monitored tags
        /// </summary>
        public ObservableCollection<TagDefinition> MonitoredTags
        {
            get => _monitoredTags;
            private set => SetProperty(ref _monitoredTags, value);
        }

        /// <summary>
        /// PLC tags
        /// </summary>
        public ObservableCollection<TagDefinition> PlcTags
        {
            get => _plcTags;
            private set => SetProperty(ref _plcTags, value);
        }

        /// <summary>
        /// DB tags
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
        /// Constructor
        /// </summary>
        /// <param name="logger">Logger</param>
        public MainViewModel(Logger logger, ICommunicationService communicationService,
                           TagManager tagManager, ChartViewModel chartViewModel)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _communicationService = communicationService ?? throw new ArgumentNullException(nameof(communicationService));
            _tagManager = tagManager ?? throw new ArgumentNullException(nameof(tagManager));
            ChartViewModel = chartViewModel ?? throw new ArgumentNullException(nameof(chartViewModel));

            // Инициализируем коллекции
            AvailableTags = new ObservableCollection<TagDefinition>();
            MonitoredTags = new ObservableCollection<TagDefinition>();
            PlcTags = new ObservableCollection<TagDefinition>();
            DbTags = new ObservableCollection<TagDefinition>();
            TiaProjects = new List<TiaProjectInfo>();

            // Инициализируем команды
            AddTagToMonitoringCommand = new RelayCommand<TagDefinition>(AddTagToMonitoring);
            RemoveTagFromMonitoringCommand = new RelayCommand<TagDefinition>(RemoveTagFromMonitoring);

            // Инициализируем начальные значения
            IsConnected = false;
            IsLoading = false;
            StatusMessage = "Готов к работе";
            ProgressValue = 0;

            // Получаем сервис TIA Portal, если он доступен
            if (communicationService is Communication.TIA.TiaPortalCommunicationService tiaService)
            {
                _tiaPortalService = tiaService;
            }

            // Инициализируем TagsViewModel
            TagsViewModel = new TagsViewModel(_logger, communicationService, tagManager);

            // Подписка на события
            SubscribeToTagsEvents();

            _logger.Info("MainViewModel инициализирован успешно");
        }

        /// <summary>
        /// Метод для обновления списка всех тегов
        /// </summary>
        //private void UpdateAllTags()
        //{
        //    if (AllTags == null)
        //    {
        //        AllTags = new ObservableCollection<TagDefinition>();
        //    }
            
        //    AllTags.Clear();
            
        //    // Добавляем PLC теги
        //    foreach (var tag in PlcTags)
        //    {
        //        AllTags.Add(tag);
        //    }
            
        //    // Добавляем DB теги
        //    foreach (var tag in DbTags)
        //    {
        //        AllTags.Add(tag);
        //    }
            
        //    // Уведомляем представление об изменении
        //    OnPropertyChanged(nameof(AllTags));
        //}

        /// <summary>
        /// Инициализация приложения
        /// </summary>
        //public void Initialize()
        //{
        //    try
        //    {
        //        _logger.Info("Initialize: Инициализация приложения");

        //        // Загружаем теги из хранилища
        //        LoadTagsFromStorage();

        //        _logger.Info("Initialize: Инициализация завершена");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.Error($"Initialize: Ошибка при инициализации: {ex.Message}");
        //    }
        //}

        ///// <summary>
        ///// Загружает теги из хранилища
        ///// </summary>
        //private void LoadTagsFromStorage()
        //{
        //    try
        //    {
        //        _logger.Info("LoadTagsFromStorage: Загрузка тегов из хранилища");

        //        var tags = _tagManager.LoadTags();

        //        // Очищаем текущие коллекции
        //        PlcTags.Clear();
        //        DbTags.Clear();
        //        AvailableTags.Clear();

        //        // Распределяем теги по коллекциям
        //        foreach (var tag in tags)
        //        {
        //            AvailableTags.Add(tag);

        //            if (tag.IsDbTag)
        //            {
        //                DbTags.Add(tag);
        //            }
        //            else
        //            {
        //                PlcTags.Add(tag);
        //            }
        //        }
                
        //        // Обновляем объединенный список тегов
        //        UpdateAllTags();

        //        _logger.Info($"LoadTagsFromStorage: Загружено {PlcTags.Count} тегов PLC и {DbTags.Count} тегов DB");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.Error($"LoadTagsFromStorage: Ошибка загрузки тегов: {ex.Message}");
        //    }
        //}

        ///// <summary>
        ///// Сохраняет теги в хранилище
        ///// </summary>
        //public void SaveTagsToStorage()
        //{
        //    try
        //    {
        //        _logger.Info("SaveTagsToStorage: Сохранение тегов в хранилище");

        //        var allTags = new List<TagDefinition>();
        //        allTags.AddRange(PlcTags);
        //        allTags.AddRange(DbTags);

        //        _tagManager.SaveTags(allTags);

        //        _logger.Info($"SaveTagsToStorage: Сохранено {allTags.Count} тегов");
        //        StatusMessage = "Теги сохранены";
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.Error($"SaveTagsToStorage: Ошибка сохранения тегов: {ex.Message}");
        //        StatusMessage = "Ошибка сохранения тегов";
        //    }
        //}

        /// <summary>
        /// Добавляет новый тег
        /// </summary>
        /// <param name="tag">Тег для добавления</param>
        /// 
        //public void AddNewTag(TagDefinition tag)
        //{
        //    if (tag == null)
        //        return;

        //    try
        //    {
        //        _logger.Info($"AddNewTag: Добавление нового тега: {tag.Name}");

        //        // Проверяем, существует ли уже тег с таким именем
        //        if (TagExists(tag.Name))
        //        {
        //            _logger.Warn($"AddNewTag: Тег с именем {tag.Name} уже существует");
        //            StatusMessage = $"Тег с именем {tag.Name} уже существует";
        //            return;
        //        }

        //        // Добавляем тег в соответствующие коллекции
        //        AvailableTags.Add(tag);

        //        // Используем явно установленное свойство IsDbTag, а не вычисленное
        //        if (tag.IsDbTag)
        //        {
        //            DbTags.Add(tag);
        //            _logger.Info($"AddNewTag: Добавлен DB тег: {tag.Name}");
        //        }
        //        else
        //        {
        //            PlcTags.Add(tag);
        //            _logger.Info($"AddNewTag: Добавлен PLC тег: {tag.Name}");
        //        }

        //        // Обновляем объединенный список тегов
        //        UpdateAllTags();

        //        // Сохраняем изменения
        //        SaveTagsToStorage();

        //        StatusMessage = $"Тег {tag.Name} добавлен";
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.Error($"AddNewTag: Ошибка при добавлении тега: {ex.Message}");
        //        StatusMessage = "Ошибка при добавлении тега";
        //    }
        //}
        

        /// <summary>
        /// Инициализация метода в конструкторе
        /// </summary>
        //private void InitializeTags()
        //{
        //    // Инициализируем коллекции
        //    AvailableTags = new ObservableCollection<TagDefinition>();
        //    MonitoredTags = new ObservableCollection<TagDefinition>();
        //    PlcTags = new ObservableCollection<TagDefinition>();
        //    DbTags = new ObservableCollection<TagDefinition>();
        //    AllTags = new ObservableCollection<TagDefinition>();
            
        //    // Загружаем теги из хранилища
        //    LoadTagsFromStorage();
        //}

        /// <summary>
        /// Редактирует тег
        /// </summary>
        /// <param name="originalTag">Исходный тег</param>
        /// <param name="updatedTag">Обновленный тег</param>
        //public void EditTag(TagDefinition originalTag, TagDefinition updatedTag)
        //{
        //    if (originalTag == null || updatedTag == null)
        //        return;

        //    try
        //    {
        //        _logger.Info($"EditTag: Редактирование тега: {originalTag.Name} -> {updatedTag.Name}");

        //        // Проверяем, существует ли тег с новым именем, если оно отличается
        //        if (!originalTag.Name.Equals(updatedTag.Name, StringComparison.OrdinalIgnoreCase) &&
        //            TagExists(updatedTag.Name))
        //        {
        //            _logger.Warn($"EditTag: Тег с именем {updatedTag.Name} уже существует");
        //            StatusMessage = $"Тег с именем {updatedTag.Name} уже существует";
        //            return;
        //        }

        //        // Удаляем старый тег
        //        RemoveTag(originalTag);

        //        // Добавляем обновленный тег
        //        AddNewTag(updatedTag);

        //        _logger.Info($"EditTag: Тег отредактирован успешно");
        //        StatusMessage = $"Тег {updatedTag.Name} отредактирован";
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.Error($"EditTag: Ошибка при редактировании тега: {ex.Message}");
        //        StatusMessage = "Ошибка при редактировании тега";
        //    }
        //}

        /// <summary>
        /// Удаляет тег
        /// </summary>
        /// <param name="tag">Тег для удаления</param>
        /// 
        //public void RemoveTag(TagDefinition tag)
        //{
        //    if (tag == null)
        //        return;

        //    try
        //    {
        //        _logger.Info($"RemoveTag: Удаление тега: {tag.Name}");

        //        // Удаляем тег из всех коллекций
        //        AvailableTags.Remove(tag);

        //        if (tag.IsDbTag)
        //        {
        //            DbTags.Remove(tag);
        //        }
        //        else
        //        {
        //            PlcTags.Remove(tag);
        //        }

        //        // Удаляем из мониторинга, если присутствует
        //        if (MonitoredTags.Contains(tag))
        //        {
        //            MonitoredTags.Remove(tag);
        //        }

        //        // Обновляем объединенный список тегов
        //        UpdateAllTags();

        //        // Сохраняем изменения
        //        SaveTagsToStorage();

        //        _logger.Info($"RemoveTag: Тег {tag.Name} удален");
        //        StatusMessage = $"Тег {tag.Name} удален";
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.Error($"RemoveTag: Ошибка при удалении тега: {ex.Message}");
        //        StatusMessage = "Ошибка при удалении тега";
        //    }
        //}

        /// <summary>
        /// Проверяет, существует ли тег с указанным именем
        /// </summary>
        //private bool TagExists(string tagName)
        //{
        //    if (string.IsNullOrEmpty(tagName))
        //        return false;

        //    // Проверяем в PLC тегах
        //    if (PlcTags.Any(t => t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase)))
        //        return true;

        //    // Проверяем в DB тегах
        //    if (DbTags.Any(t => t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase)))
        //        return true;

        //    return false;
        //}

        ///// <summary>
        ///// Добавляет тег в список мониторинга
        ///// </summary>
        //private void AddTagToMonitoring(TagDefinition tag)
        //{
        //    if (tag == null)
        //        return;

        //    try
        //    {
        //        // Проверяем лимит тегов для мониторинга
        //        if (MonitoredTags.Count >= MaxMonitoredTags)
        //        {
        //            _logger.Warn($"AddTagToMonitoring: Достигнут лимит тегов для мониторинга ({MaxMonitoredTags})");
        //            StatusMessage = $"Достигнут лимит тегов для мониторинга ({MaxMonitoredTags})";
        //            return;
        //        }

        //        // Проверяем, не добавлен ли тег уже
        //        if (MonitoredTags.Any(t => t.Id == tag.Id))
        //        {
        //            _logger.Warn($"AddTagToMonitoring: Тег {tag.Name} уже добавлен в мониторинг");
        //            StatusMessage = $"Тег {tag.Name} уже добавлен в мониторинг";
        //            return;
        //        }

        //        // Добавляем тег в мониторинг
        //        MonitoredTags.Add(tag);
        //        _logger.Info($"AddTagToMonitoring: Тег {tag.Name} добавлен в мониторинг");
        //        StatusMessage = $"Тег {tag.Name} добавлен в мониторинг";

        //        // Вызываем событие для обновления графика
        //        TagAddedToMonitoring?.Invoke(this, tag);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.Error($"AddTagToMonitoring: Ошибка при добавлении тега в мониторинг: {ex.Message}");
        //        StatusMessage = "Ошибка при добавлении тега в мониторинг";
        //    }
        //}

        ///// <summary>
        ///// Удаляет тег из списка мониторинга
        ///// </summary>
        //private void RemoveTagFromMonitoring(TagDefinition tag)
        //{
        //    if (tag == null)
        //        return;

        //    try
        //    {
        //        if (MonitoredTags.Remove(tag))
        //        {
        //            _logger.Info($"RemoveTagFromMonitoring: Тег {tag.Name} удален из мониторинга");
        //            StatusMessage = $"Тег {tag.Name} удален из мониторинга";

        //            // Вызываем событие для обновления графика
        //            TagRemovedFromMonitoring?.Invoke(this, tag);
        //        }
        //        else
        //        {
        //            _logger.Warn($"RemoveTagFromMonitoring: Тег {tag.Name} не найден в списке мониторинга");
        //            StatusMessage = $"Тег {tag.Name} не найден в списке мониторинга";
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.Error($"RemoveTagFromMonitoring: Ошибка при удалении тега из мониторинга: {ex.Message}");
        //        StatusMessage = "Ошибка при удалении тега из мониторинга";
        //    }
        //}

        /// <summary>
        /// Current project name
        /// </summary>
        public string CurrentProjectName
        {
            get
            {
                if (_tiaPortalService != null && _tiaPortalService.CurrentProject != null)
                    return _tiaPortalService.CurrentProject.Name;
                return "No project";
            }
        }

        /// <summary>
        /// Maximum number of tags for monitoring
        /// </summary>
        public int MaxMonitoredTags => 10;
    }
}