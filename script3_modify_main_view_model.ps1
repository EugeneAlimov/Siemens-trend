# script3_modify_main_view_model.ps1
# Script to modify MainViewModel.cs

# Project path
$projectPath = Get-Location

# Path to MainViewModel.cs
$mainViewModelPath = Join-Path -Path $projectPath -ChildPath "ViewModels\MainViewModel.cs"

# Content of the new MainViewModel.cs file
$mainViewModelContent = @"
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
                
                _logger.Info(\$"Loaded {PlcTags.Count} PLC tags and {DbTags.Count} DB tags");
            }
            catch (Exception ex)
            {
                _logger.Error(\$"Error loading tags from storage: {ex.Message}");
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
                
                _logger.Info(\$"Saved {allTags.Count} tags");
                StatusMessage = "Tags saved";
            }
            catch (Exception ex)
            {
                _logger.Error(\$"Error saving tags: {ex.Message}");
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
                _logger.Info(\$"Adding new tag: {tag.Name}");
                
                // Check if tag already exists
                if (AvailableTags.Any(t => t.Name == tag.Name))
                {
                    _logger.Warn(\$"Tag with name {tag.Name} already exists");
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
                
                _logger.Info(\$"Tag {tag.Name} added successfully");
                StatusMessage = \$"Tag {tag.Name} added";
            }
            catch (Exception ex)
            {
                _logger.Error(\$"Error adding tag: {ex.Message}");
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
                _logger.Info(\$"Removing tag: {tag.Name}");
                
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
                
                _logger.Info(\$"Tag {tag.Name} removed successfully");
                StatusMessage = \$"Tag {tag.Name} removed";
            }
            catch (Exception ex)
            {
                _logger.Error(\$"Error removing tag: {ex.Message}");
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
                _logger.Info(\$"Editing tag: {originalTag.Name}");
                
                // Remove old tag
                RemoveTag(originalTag);
                
                // Add updated tag
                AddNewTag(updatedTag);
                
                _logger.Info(\$"Tag {updatedTag.Name} edited successfully");
                StatusMessage = \$"Tag {updatedTag.Name} edited";
            }
            catch (Exception ex)
            {
                _logger.Error(\$"Error editing tag: {ex.Message}");
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
}
"@
Set-Content -Path $mainViewModelPath -Value $mainViewModelContent

Write-Host "Script 3 completed successfully. MainViewModel class modified."