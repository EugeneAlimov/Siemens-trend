using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SiemensTrend.Communication;
using SiemensTrend.Communication.S7;
using SiemensTrend.Core.Logging;
using SiemensTrend.Core.Models;
using SiemensTrend.Storage.TagManagement;

using SiemensTrend.Storage.TagManagement;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
namespace SiemensTrend.ViewModels
{
    /// <summary>
    /// Main view model
    /// </summary>
    public partial class MainViewModel : ViewModelBase
    {
        /// <summary>
        /// Logger for event recording
        /// </summary>
        protected readonly Logger _logger;

        /// <summary>
        /// Service for communication with PLC
        /// </summary>
        protected ICommunicationService _communicationService;

        /// <summary>
        /// Tag manager for manual tag management
        /// </summary>
        protected TagManager _tagManager;

        private bool _isConnected;
        private bool _isLoading;
        private string _statusMessage;
        private int _progressValue;
        /// <summary>
        /// Команда для добавления тега в мониторинг
        /// </summary>
        public ICommand AddTagToMonitoringCommand { get; private set; }

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
        /// Constructor
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <summary>
        /// ÐšÐ¾Ð½ÑÑ‚Ñ€ÑƒÐºÑ‚Ð¾Ñ€
        /// </summary>
        /// <param name="logger">Ð›Ð¾Ð³Ð³ÐµÑ€</param>
        public MainViewModel(Logger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.Info("Initializing MainViewModel");

            // Ð˜Ð½Ð¸Ñ†Ð¸Ð°Ð»Ð¸Ð·Ð¸Ñ€ÑƒÐµÐ¼ ÐºÐ¾Ð»Ð»ÐµÐºÑ†Ð¸Ð¸
            AvailableTags = new ObservableCollection<TagDefinition>();
            MonitoredTags = new ObservableCollection<TagDefinition>();
            PlcTags = new ObservableCollection<TagDefinition>();
            DbTags = new ObservableCollection<TagDefinition>();

            // Ð˜Ð½Ð¸Ñ†Ð¸Ð°Ð»Ð¸Ð·Ð¸Ñ€ÑƒÐµÐ¼ Ð½Ð°Ñ‡Ð°Ð»ÑŒÐ½Ñ‹Ðµ Ð·Ð½Ð°Ñ‡ÐµÐ½Ð¸Ñ
            IsConnected = false;
            IsLoading = false;
            StatusMessage = "Ready to work";
            ProgressValue = 0;

            // Ð˜Ð½Ð¸Ñ†Ð¸Ð°Ð»Ð¸Ð·Ð¸Ñ€ÑƒÐµÐ¼ Ð¼ÐµÐ½ÐµÐ´Ð¶ÐµÑ€ Ñ‚ÐµÐ³Ð¾Ð²
            _tagManager = new TagManager(_logger);
            
            // Ð—Ð°Ð³Ñ€ÑƒÐ¶Ð°ÐµÐ¼ Ñ‚ÐµÐ³Ð¸ Ð¸Ð· Ñ…Ñ€Ð°Ð½Ð¸Ð»Ð¸Ñ‰Ð°
            LoadTagsFromStorage();

            // Fix for the ambiguous constructor call
            AddTagToMonitoringCommand = new RelayCommand<TagDefinition>(AddTagToMonitoring, null);

            RemoveTagFromMonitoringCommand = new RelayCommand<TagDefinition>(RemoveTagFromMonitoring);
            
            _logger.Info("MainViewModel Ð¸Ð½Ð¸Ñ†Ð¸Ð°Ð»Ð¸Ð·Ð¸Ñ€Ð¾Ð²Ð°Ð½ ÑƒÑÐ¿ÐµÑˆÐ½Ð¾");
            
            // ÐÐ²Ñ‚Ð¾Ð¼Ð°Ñ‚Ð¸Ñ‡ÐµÑÐºÐ¸ Ð·Ð°Ð³Ñ€ÑƒÐ¶Ð°ÐµÐ¼ ÑÐ¾Ñ…Ñ€Ð°Ð½ÐµÐ½Ð½Ñ‹Ðµ Ñ‚ÐµÐ³Ð¸
            try
            {
                LoadTagsFromXmlAsync().Wait();
            }
            catch (Exception ex)
            {
                _logger.Warn($"ÐÐµ ÑƒÐ´Ð°Ð»Ð¾ÑÑŒ Ð·Ð°Ð³Ñ€ÑƒÐ·Ð¸Ñ‚ÑŒ ÑÐ¾Ñ…Ñ€Ð°Ð½ÐµÐ½Ð½Ñ‹Ðµ Ñ‚ÐµÐ³Ð¸: {ex.Message}");
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
        /// Добавляет новый тег
        /// </summary>
        /// <param name="tag">Тег для добавления</param>
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
        /// Удаляет тег
        /// </summary>
        /// <param name="tag">Тег для удаления</param>
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
                    MonitoredTags.Remove(tag);
                }

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
        /// Редактирует тег
        /// </summary>
        /// <param name="originalTag">Исходный тег</param>
        /// <param name="updatedTag">Обновленный тег</param>
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
        /// Создает тестовые теги
        /// </summary>
        public void CreateTestTags()
        {
            try
            {
                _logger.Info("CreateTestTags: Создание тестовых тегов");

                // Очищаем текущие коллекции
                PlcTags.Clear();
                DbTags.Clear();
                AvailableTags.Clear();

                // Создаем тестовые теги PLC
                AddNewTag(new TagDefinition { Name = "Motor1_Start", Address = "M0.0", DataType = TagDataType.Bool, GroupName = "Motors", Comment = "Start motor 1" });
                AddNewTag(new TagDefinition { Name = "Motor1_Stop", Address = "M0.1", DataType = TagDataType.Bool, GroupName = "Motors", Comment = "Stop motor 1" });
                AddNewTag(new TagDefinition { Name = "Temperature", Address = "MW10", DataType = TagDataType.Int, GroupName = "Sensors", Comment = "Temperature sensor" });
                AddNewTag(new TagDefinition { Name = "Pressure", Address = "MD20", DataType = TagDataType.Real, GroupName = "Sensors", Comment = "Pressure sensor" });

                // Создаем тестовые теги DB
                AddNewTag(new TagDefinition { Name = "DB1.Start", Address = "DB1.DBX0.0", DataType = TagDataType.Bool, GroupName = "DB1", Comment = "Start command", IsDbTag = true });
                AddNewTag(new TagDefinition { Name = "DB1.Speed", Address = "DB1.DBD2", DataType = TagDataType.Real, GroupName = "DB1", Comment = "Speed setpoint", IsDbTag = true });
                AddNewTag(new TagDefinition { Name = "DB2.Level", Address = "DB2.DBW4", DataType = TagDataType.Int, GroupName = "DB2", Comment = "Tank level", IsDbTag = true });
                AddNewTag(new TagDefinition { Name = "DB2.Status", Address = "DB2.DBX6.0", DataType = TagDataType.Bool, GroupName = "DB2", Comment = "Tank status", IsDbTag = true });

                _logger.Info("CreateTestTags: Тестовые теги созданы успешно");
                StatusMessage = "Тестовые теги созданы";
            }
            catch (Exception ex)
            {
                _logger.Error($"CreateTestTags: Ошибка при создании тестовых тегов: {ex.Message}");
                StatusMessage = "Ошибка при создании тестовых тегов";
            }
        }

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

    /// <summary>
    /// Команда с параметром
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T> _canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action<T> execute, Predicate<T> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke((T)parameter) ?? true;

        public void Execute(object parameter) => _execute((T)parameter);
    }

    /// <summary>
    /// Команда без параметров
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object parameter) => _execute();
    }

        /// <summary>
        /// ï¿½ï¿½ï¿½à ­ï¿½ï¿½ï¿½ï¿½ â¥£ï¿½ï¿½ ï¿½ XML-ä ©ï¿½
        /// </summary>
        /// <param name="filePath">ï¿½ï¿½ï¿½ï¿½ ï¿½ ä ©ï¿½ï¿½ (ï¿½á«¨ null, ï¿½ã¤¥ï¿½ ï¿½á¯®ï¿½ì§®ï¿½ï¿½ï¿½ ï¿½ï¿½ï¿½ï¿½ ï¿½ï¿½ ã¬®ï¿½ç ­ï¿½ï¿½)</param>
        /// <returns>Task</returns>
        public async Task SaveTagsToXmlAsync(string filePath = null)
        {
            try
            {
                _logger.Info("SaveTagsToXmlAsync: ï¿½ï¿½ï¿½à ­ï¿½ï¿½ï¿½ï¿½ â¥£ï¿½ï¿½ ï¿½ XML");
                
                // ï¿½á«¨ ï¿½ï¿½ï¿½ï¿½ ï¿½ï¿½ ãª ï¿½ï¿½ï¿½, ï¿½á¯®ï¿½ï¿½ã¥¬ ï¿½ï¿½ï¿½ï¿½ ï¿½ï¿½ ã¬®ï¿½ç ­ï¿½ï¿½
                if (string.IsNullOrEmpty(filePath))
                {
                    string appDataPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "SiemensTrend");
                        
                    // ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ ï¿½ï¿½à¥ªï¿½ï¿½ï¿½, ï¿½á«¨ ï¿½ã¦­ï¿½
                    if (!Directory.Exists(appDataPath))
                    {
                        Directory.CreateDirectory(appDataPath);
                    }
                    
                    filePath = Path.Combine(appDataPath, "UserTags.xml");
                }
                
                // ï¿½ï¿½ï¿½ï¿½à ¥ï¿½ ï¿½ï¿½ â¥£ï¿½
                var allTags = new List<TagDefinition>();
                allTags.AddRange(PlcTags);
                allTags.AddRange(DbTags);
                
                // ï¿½ï¿½ï¿½à ­ï¥¬ â¥£ï¿½
                await Task.Run(() => _tagManager.SaveTagsToXml(allTags, filePath));
                
                _logger.Info($"SaveTagsToXmlAsync: ï¿½ï¿½ï¿½à ­ï¿½ï¿½ï¿½ {allTags.Count} â¥£ï¿½ï¿½ ï¿½ {filePath}");
                StatusMessage = $"ï¿½ï¿½ï¿½à ­ï¿½ï¿½ï¿½ {allTags.Count} â¥£ï¿½ï¿½";
            }
            catch (Exception ex)
            {
                _logger.Error($"SaveTagsToXmlAsync: ï¿½è¨¡ï¿½ï¿½: {ex.Message}");
                StatusMessage = "ï¿½è¨¡ï¿½ï¿½ ï¿½ï¿½ ï¿½ï¿½à ­ï¿½ï¿½ï¿½ï¿½ â¥£ï¿½ï¿½ ï¿½ XML";
                throw;
            }
        }

        /// <summary>
        /// ï¿½ï¿½ï¿½ï¿½ã§ªï¿½ â¥£ï¿½ï¿½ ï¿½ï¿½ XML-ä ©ï¿½ï¿½
        /// </summary>
        /// <param name="filePath">ï¿½ï¿½ï¿½ï¿½ ï¿½ ä ©ï¿½ï¿½ (ï¿½á«¨ null, ï¿½ã¤¥ï¿½ ï¿½á¯®ï¿½ì§®ï¿½ï¿½ï¿½ ï¿½ï¿½ï¿½ï¿½ ï¿½ï¿½ ã¬®ï¿½ç ­ï¿½ï¿½)</param>
        /// <returns>Task</returns>
        public async Task LoadTagsFromXmlAsync(string filePath = null)
        {
            try
            {
                _logger.Info("LoadTagsFromXmlAsync: ï¿½ï¿½ï¿½ï¿½ã§ªï¿½ â¥£ï¿½ï¿½ ï¿½ï¿½ XML");
                
                // ï¿½á«¨ ï¿½ï¿½ï¿½ï¿½ ï¿½ï¿½ ãª ï¿½ï¿½ï¿½, ï¿½á¯®ï¿½ï¿½ã¥¬ ï¿½ï¿½ï¿½ï¿½ ï¿½ï¿½ ã¬®ï¿½ç ­ï¿½ï¿½
                if (string.IsNullOrEmpty(filePath))
                {
                    string appDataPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "SiemensTrend");
                    
                    filePath = Path.Combine(appDataPath, "UserTags.xml");
                }
                
                // ï¿½à®¢ï¿½ï¿½ï¥¬ ï¿½ï¿½ï¿½ï¿½â¢®ï¿½ï¿½ï¿½ï¿½ï¿½ ä ©ï¿½ï¿½
                if (!File.Exists(filePath))
                {
                    _logger.Warn($"LoadTagsFromXmlAsync: ï¿½ï¿½ï¿½ï¿½ ï¿½ï¿½ ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½: {filePath}");
                    StatusMessage = "ï¿½ï¿½ï¿½ï¿½ ï¿½ â¥£ï¿½ï¿½ï¿½ ï¿½ï¿½ ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½";
                    return;
                }
                
                // ï¿½ï¿½ï¿½ï¿½ã¦ ï¿½ï¿½ â¥£ï¿½
                var tags = await Task.Run(() => _tagManager.LoadTagsFromXml(filePath));
                
                // ï¿½ï¿½é ¥ï¿½ â¥ªï¿½é¨¥ ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½æ¨¨
                PlcTags.Clear();
                DbTags.Clear();
                AvailableTags.Clear();
                
                // ï¿½ï¿½ï¿½à¥¤ï¿½ï¿½ï¥¬ â¥£ï¿½ ï¿½ï¿½ ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½
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
                
                _logger.Info($"LoadTagsFromXmlAsync: ï¿½ï¿½ï¿½ï¿½ã¦¥ï¿½ï¿½ {tags.Count} â¥£ï¿½ï¿½");
                StatusMessage = $"ï¿½ï¿½ï¿½ï¿½ã¦¥ï¿½ï¿½ {tags.Count} â¥£ï¿½ï¿½";
            }
            catch (Exception ex)
            {
                _logger.Error($"LoadTagsFromXmlAsync: ï¿½è¨¡ï¿½ï¿½: {ex.Message}");
                StatusMessage = "ï¿½è¨¡ï¿½ï¿½ ï¿½ï¿½ ï¿½ï¿½ï¿½ï¿½ã§ªï¿½ â¥£ï¿½ï¿½ ï¿½ï¿½ XML";
                throw;
            }
        }
}

