using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SiemensTrend.Communication;
using SiemensTrend.Communication.S7;
using SiemensTrend.Core.Logging;
using SiemensTrend.Core.Models;
using SiemensTrend.Helpers;
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
        public MainViewModel(Logger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.Info("Initializing MainViewModel");

            // Initialize collections
            AvailableTags = new ObservableCollection<TagDefinition>();
            MonitoredTags = new ObservableCollection<TagDefinition>();
            PlcTags = new ObservableCollection<TagDefinition>();
            DbTags = new ObservableCollection<TagDefinition>();

            // Initialize initial values
            IsConnected = false;
            IsLoading = false;
            StatusMessage = "Ready to work";
            ProgressValue = 0;

            // Initialize tag manager
            _tagManager = new TagManager(_logger);
            LoadTagsFromStorage();

            
            // Инициализация команд
            AddTagToMonitoringCommand = new RelayCommand<TagDefinition>(AddTagToMonitoring);
            RemoveTagFromMonitoringCommand = new RelayCommand<TagDefinition>(RemoveTagFromMonitoring);
            _logger.Info("MainViewModel initialized successfully");
        }

        /// <summary>
        /// Load tags from storage
        /// </summary>
        private void LoadTagsFromStorage()
        {
            try
            {
                _logger.Info("Loading tags from storage");
                
                var tags = _tagManager.LoadTags();
                
                // Clear current collections
                PlcTags.Clear();
                DbTags.Clear();
                AvailableTags.Clear();
                
                // Distribute tags to collections
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
                
                _logger.Info($"Loaded {PlcTags.Count} PLC tags and {DbTags.Count} DB tags");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error loading tags from storage: {ex.Message}");
            }
        }

        /// <summary>
        /// Save tags to storage
        /// </summary>
        public void SaveTagsToStorage()
        {
            try
            {
                _logger.Info("Saving tags to storage");
                
                var allTags = new List<TagDefinition>();
                allTags.AddRange(PlcTags);
                allTags.AddRange(DbTags);
                
                _tagManager.SaveTags(allTags);
                
                _logger.Info($"Saved {allTags.Count} tags");
                StatusMessage = "Tags saved";
            }
            catch (Exception ex)
            {
                _logger.Error($"Error saving tags: {ex.Message}");
                StatusMessage = "Error saving tags";
            }
        }

        /// <summary>
        /// Add new tag
        /// </summary>
        public void AddNewTag(TagDefinition tag)
        {
            if (tag == null)
                return;
                
            try
            {
                _logger.Info($"Adding new tag: {tag.Name}");
                
                // Check if tag already exists
                if (AvailableTags.Any(t => t.Name == tag.Name))
                {
                    _logger.Warn($"Tag with name {tag.Name} already exists");
                    return;
                }
                
                // Add to appropriate collections
                AvailableTags.Add(tag);
                
                if (tag.IsDbTag)
                {
                    DbTags.Add(tag);
                }
                else
                {
                    PlcTags.Add(tag);
                }
                
                // Save changes
                SaveTagsToStorage();
                
                _logger.Info($"Tag {tag.Name} added successfully");
                StatusMessage = $"Tag {tag.Name} added";
            }
            catch (Exception ex)
            {
                _logger.Error($"Error adding tag: {ex.Message}");
                StatusMessage = "Error adding tag";
            }
        }

        /// <summary>
        /// Remove tag
        /// </summary>
        public void RemoveTag(TagDefinition tag)
        {
            if (tag == null)
                return;
                
            try
            {
                _logger.Info($"Removing tag: {tag.Name}");
                
                // Remove from all collections
                AvailableTags.Remove(tag);
                
                if (tag.IsDbTag)
                {
                    DbTags.Remove(tag);
                }
                else
                {
                    PlcTags.Remove(tag);
                }
                
                // Remove from monitoring if present
                if (MonitoredTags.Contains(tag))
                {
                    MonitoredTags.Remove(tag);
                }
                
                // Save changes
                SaveTagsToStorage();
                
                _logger.Info($"Tag {tag.Name} removed successfully");
                StatusMessage = $"Tag {tag.Name} removed";
            }
            catch (Exception ex)
            {
                _logger.Error($"Error removing tag: {ex.Message}");
                StatusMessage = "Error removing tag";
            }
        }

        /// <summary>
        /// Edit tag
        /// </summary>
        public void EditTag(TagDefinition originalTag, TagDefinition updatedTag)
        {
            if (originalTag == null || updatedTag == null)
                return;
                
            try
            {
                _logger.Info($"Editing tag: {originalTag.Name}");
                
                // Remove old tag
                RemoveTag(originalTag);
                
                // Add updated tag
                AddNewTag(updatedTag);
                
                _logger.Info($"Tag {updatedTag.Name} edited successfully");
                StatusMessage = $"Tag {updatedTag.Name} edited";
            }
            catch (Exception ex)
            {
                _logger.Error($"Error editing tag: {ex.Message}");
                StatusMessage = "Error editing tag";
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
}
