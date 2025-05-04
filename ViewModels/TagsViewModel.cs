using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using SiemensTrend.Communication;
using SiemensTrend.Core.Commands;
using SiemensTrend.Core.Logging;
using SiemensTrend.Core.Models;
using SiemensTrend.Storage.TagManagement;
using SiemensTrend.Views.Dialogs;

namespace SiemensTrend.ViewModels
{
    /// <summary>
    /// ViewModel для работы со списком тегов
    /// </summary>
    public class TagsViewModel : ViewModelBase
    {
        private readonly Logger _logger;
        private readonly ICommunicationService _communicationService;
        private readonly TagManager _tagManager;

        // Коллекции тегов
        private ObservableCollection<TagViewModel> _allTags;
        private ObservableCollection<TagViewModel> _monitoredTags;
        private ObservableCollection<TagViewModel> _filteredTags;

        // Параметры фильтрации
        private string _filterText;
        private TagType? _tagTypeFilter;

        /// <summary>
        /// Все доступные теги
        /// </summary>
        public ObservableCollection<TagViewModel> AllTags
        {
            get => _allTags;
            private set => SetProperty(ref _allTags, value);
        }

        /// <summary>
        /// Теги для мониторинга
        /// </summary>
        public ObservableCollection<TagViewModel> MonitoredTags
        {
            get => _monitoredTags;
            private set => SetProperty(ref _monitoredTags, value);
        }

        /// <summary>
        /// Отфильтрованные теги для отображения
        /// </summary>
        public ObservableCollection<TagViewModel> FilteredTags
        {
            get => _filteredTags;
            private set => SetProperty(ref _filteredTags, value);
        }

        /// <summary>
        /// Текст для фильтрации
        /// </summary>
        public string FilterText
        {
            get => _filterText;
            set
            {
                if (SetProperty(ref _filterText, value))
                {
                    ApplyFilter();
                }
            }
        }

        /// <summary>
        /// Фильтр по типу тега
        /// </summary>
        public TagType? TagTypeFilter
        {
            get => _tagTypeFilter;
            set
            {
                if (SetProperty(ref _tagTypeFilter, value))
                {
                    ApplyFilter();
                }
            }
        }

        /// <summary>
        /// Команда для добавления тегов
        /// </summary>
        public ICommand AddTagsCommand { get; }

        /// <summary>
        /// Команда для удаления тега
        /// </summary>
        public ICommand RemoveTagCommand { get; }

        /// <summary>
        /// Команда для очистки фильтра
        /// </summary>
        public ICommand ClearFilterCommand { get; }

        /// <summary>
        /// Команда для сохранения тегов
        /// </summary>
        public ICommand SaveTagsCommand { get; }

        /// <summary>
        /// Команда для загрузки тегов
        /// </summary>
        public ICommand LoadTagsCommand { get; }

        /// <summary>
        /// Событие добавления тега в мониторинг
        /// </summary>
        public event EventHandler<TagViewModel> TagAddedToMonitoring;

        /// <summary>
        /// Событие удаления тега из мониторинга
        /// </summary>
        public event EventHandler<TagViewModel> TagRemovedFromMonitoring;

        /// <summary>
        /// Максимальное количество тегов для мониторинга
        /// </summary>
        public int MaxMonitoredTags => 10;

        /// <summary>
        /// Конструктор
        /// </summary>
        public TagsViewModel(Logger logger, ICommunicationService communicationService, TagManager tagManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _communicationService = communicationService ?? throw new ArgumentNullException(nameof(communicationService));
            _tagManager = tagManager ?? throw new ArgumentNullException(nameof(tagManager));

            // Инициализация коллекций
            AllTags = new ObservableCollection<TagViewModel>();
            MonitoredTags = new ObservableCollection<TagViewModel>();
            FilteredTags = new ObservableCollection<TagViewModel>();

            // Инициализация команд
            AddTagsCommand = new RelayCommand(AddTags);
            RemoveTagCommand = new RelayCommand<TagViewModel>(RemoveTag);
            ClearFilterCommand = new RelayCommand(ClearFilter);
            SaveTagsCommand = new RelayCommand(SaveTags);
            LoadTagsCommand = new RelayCommand(LoadTags);

            // Загрузка сохраненных тегов
            LoadTags();
        }

        /// <summary>
        /// Метод для открытия диалога добавления тегов
        /// </summary>
        private void AddTags()
        {
            try
            {
                // Создаем и показываем диалог
                var dialog = new AddTagsDialog(_logger, _communicationService);
                dialog.Owner = Application.Current.MainWindow;

                if (dialog.ShowDialog() == true)
                {
                    // Получаем найденные теги
                    var foundTags = dialog.FoundTags;

                    if (foundTags != null && foundTags.Count > 0)
                    {
                        // Добавляем теги в коллекцию
                        foreach (var tag in foundTags)
                        {
                            // Проверяем, нет ли уже тега с таким именем
                            if (!AllTags.Any(t => t.FullName == GetFullTagName(tag)))
                            {
                                // Создаем ViewModel для тега
                                var tagViewModel = new TagViewModel(tag);

                                // Задаем команды
                                tagViewModel.AddToMonitoringCommand =
                                    new RelayCommand(() => AddTagToMonitoring(tagViewModel));
                                tagViewModel.RemoveFromMonitoringCommand =
                                    new RelayCommand(() => RemoveTagFromMonitoring(tagViewModel));

                                // Добавляем в коллекцию
                                AllTags.Add(tagViewModel);
                            }
                        }

                        // Сохраняем теги
                        SaveTags();

                        // Применяем фильтр
                        ApplyFilter();

                        _logger.Info($"Добавлено {foundTags.Count} тегов");
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
        /// Получение полного имени тега
        /// </summary>
        private string GetFullTagName(TagDefinition tag)
        {
            if (tag.IsDbTag)
            {
                return $"\"{tag.GroupName}\".{tag.Name}";
            }
            else
            {
                return $"\"{tag.GroupName}\"";
            }
        }

        /// <summary>
        /// Метод для удаления тега
        /// </summary>
        private void RemoveTag(TagViewModel tag)
        {
            if (tag == null)
                return;

            try
            {
                // Удаляем тег из коллекций
                AllTags.Remove(tag);

                // Если тег был в мониторинге, удаляем и оттуда
                if (MonitoredTags.Contains(tag))
                {
                    RemoveTagFromMonitoring(tag);
                }

                // Обновляем фильтрованную коллекцию
                ApplyFilter();

                // Сохраняем изменения
                SaveTags();

                _logger.Info($"Тег {tag.FullName} удален");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при удалении тега: {ex.Message}");
                MessageBox.Show($"Ошибка при удалении тега: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Метод для добавления тега в мониторинг
        /// </summary>
        private void AddTagToMonitoring(TagViewModel tag)
        {
            if (tag == null)
                return;

            try
            {
                // Проверяем, не превышен ли лимит тегов для мониторинга
                if (MonitoredTags.Count >= MaxMonitoredTags)
                {
                    MessageBox.Show($"Достигнут максимальный лимит тегов для мониторинга ({MaxMonitoredTags})",
                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверяем, нет ли уже этого тега в мониторинге
                if (!MonitoredTags.Contains(tag))
                {
                    // Добавляем тег в мониторинг
                    MonitoredTags.Add(tag);

                    // Обновляем статус тега
                    tag.IsMonitored = true;

                    // Обновляем фильтрованную коллекцию
                    ApplyFilter();

                    // Вызываем событие
                    TagAddedToMonitoring?.Invoke(this, tag);

                    _logger.Info($"Тег {tag.FullName} добавлен в мониторинг");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при добавлении тега в мониторинг: {ex.Message}");
                MessageBox.Show($"Ошибка при добавлении тега в мониторинг: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Метод для удаления тега из мониторинга
        /// </summary>
        private void RemoveTagFromMonitoring(TagViewModel tag)
        {
            if (tag == null)
                return;

            try
            {
                // Удаляем тег из мониторинга
                if (MonitoredTags.Remove(tag))
                {
                    // Обновляем статус тега
                    tag.IsMonitored = false;

                    // Обновляем фильтрованную коллекцию
                    ApplyFilter();

                    // Вызываем событие
                    TagRemovedFromMonitoring?.Invoke(this, tag);

                    _logger.Info($"Тег {tag.FullName} удален из мониторинга");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при удалении тега из мониторинга: {ex.Message}");
                MessageBox.Show($"Ошибка при удалении тега из мониторинга: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Метод для применения фильтра
        /// </summary>
        private void ApplyFilter()
        {
            try
            {
                // Создаем новую коллекцию
                var filtered = new List<TagViewModel>();

                // Фильтруем теги
                foreach (var tag in AllTags)
                {
                    bool isMatch = true;

                    // Фильтр по тексту
                    if (!string.IsNullOrEmpty(FilterText))
                    {
                        string searchText = FilterText.ToLower();

                        isMatch = tag.Group?.ToLower().Contains(searchText) == true ||
                                 tag.Name?.ToLower().Contains(searchText) == true ||
                                 tag.Comment?.ToLower().Contains(searchText) == true;
                    }

                    // Фильтр по типу тега
                    if (isMatch && TagTypeFilter.HasValue)
                    {
                        isMatch = tag.TagType == TagTypeFilter.Value;
                    }

                    // Добавляем тег, если он соответствует фильтрам
                    if (isMatch)
                    {
                        filtered.Add(tag);
                    }
                }

                // Обновляем коллекцию
                Application.Current.Dispatcher.Invoke(() =>
                {
                    FilteredTags.Clear();
                    foreach (var tag in filtered)
                    {
                        FilteredTags.Add(tag);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при применении фильтра: {ex.Message}");
            }
        }

        /// <summary>
        /// Метод для очистки фильтра
        /// </summary>
        private void ClearFilter()
        {
            FilterText = null;
            TagTypeFilter = null;
        }

        /// <summary>
        /// Метод для сохранения тегов
        /// </summary>
        private void SaveTags()
        {
            try
            {
                // Конвертируем ViewModels в модели
                var tags = AllTags.Select(vm => vm.Tag).ToList();

                // Сохраняем теги
                _tagManager.SaveTags(tags);

                _logger.Info($"Сохранено {tags.Count} тегов");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при сохранении тегов: {ex.Message}");
                MessageBox.Show($"Ошибка при сохранении тегов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Метод для загрузки тегов
        /// </summary>
        private void LoadTags()
        {
            try
            {
                // Загружаем теги из хранилища
                var tags = _tagManager.LoadTags();

                // Очищаем текущие коллекции
                AllTags.Clear();
                MonitoredTags.Clear();

                // Добавляем загруженные теги
                foreach (var tag in tags)
                {
                    // Создаем ViewModel для тега
                    var tagViewModel = new TagViewModel(tag);

                    // Задаем команды
                    tagViewModel.AddToMonitoringCommand =
                        new RelayCommand(() => AddTagToMonitoring(tagViewModel));
                    tagViewModel.RemoveFromMonitoringCommand =
                        new RelayCommand(() => RemoveTagFromMonitoring(tagViewModel));

                    // Добавляем в коллекцию
                    AllTags.Add(tagViewModel);
                }

                // Применяем фильтр
                ApplyFilter();

                _logger.Info($"Загружено {tags.Count} тегов");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при загрузке тегов: {ex.Message}");
                MessageBox.Show($"Ошибка при загрузке тегов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}