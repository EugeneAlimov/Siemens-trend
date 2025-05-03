using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SiemensTrend.Communication.TIA;
using SiemensTrend.Core.Logging;
using SiemensTrend.Core.Models;

namespace SiemensTrend.ViewModels
{
    /// <summary>
    /// ViewModel для обозревателя тегов
    /// </summary>
    public class TagBrowserViewModel : ViewModelBase
    {
        private readonly Logger _logger;
        private readonly TiaPortalCommunicationService _tiaService;

        private ObservableCollection<TreeViewItemViewModel> _rootItems;
        private string _searchText;
        private bool _isLoading;
        private PlcData _plcData; // Хранилище данных ПЛК и DB

        /// <summary>
        /// Корневые элементы дерева
        /// </summary>
        public ObservableCollection<TreeViewItemViewModel> RootItems
        {
            get => _rootItems;
            set => SetProperty(ref _rootItems, value);
        }

        /// <summary>
        /// Текст поиска
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value) && !_isLoading)
                {
                    SearchTags(); // Вызываем поиск при изменении текста
                }
            }
        }

        /// <summary>
        /// Индикатор загрузки
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// Команда обновления данных
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// Команда добавления тега в мониторинг
        /// </summary>
        public ICommand AddTagCommand { get; }

        /// <summary>
        /// Команда поиска
        /// </summary>
        public ICommand SearchCommand { get; }

        /// <summary>
        /// Событие выбора тега для мониторинга
        /// </summary>
        public event EventHandler<TagDefinition> TagSelected;

        /// <summary>
        /// Инициализация дерева тегов
        /// </summary>
        private void InitializeTree()
        {
            RootItems.Clear();

            // Добавляем корневые узлы
            var plcTagsNode = new TreeViewItemViewModel
            {
                Header = "ПЛК Теги",
                IsExpanded = true
            };

            var dbTagsNode = new TreeViewItemViewModel
            {
                Header = "Блоки данных",
                IsExpanded = true
            };

            RootItems.Add(plcTagsNode);
            RootItems.Add(dbTagsNode);
        }

        /// <summary>
        /// Обновление списка тегов
        /// </summary>
        public async void RefreshTags()
        {
            try
            {
                IsLoading = true;
                _logger.Info("Обновление дерева тегов...");

                InitializeTree(); // Очищаем и создаем базовую структуру

                var plcTagsNode = RootItems[0];
                var dbTagsNode = RootItems[1];

                try
                {
                    // Получаем данные из TIA Portal
                    //_plcData = await _tiaService.GetAllProjectTagsAsync();
                    _logger.Info($"Получено {_plcData.PlcTags.Count} тегов ПЛК и {_plcData.DbTags.Count} тегов DB");

                    // Добавляем теги ПЛК в дерево
                    AddPlcTagsToTree(_plcData.PlcTags, plcTagsNode);

                    // Добавляем теги DB в дерево
                    AddDbTagsToTree(_plcData.DbTags, dbTagsNode);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Ошибка при получении тегов из TIA Portal: {ex.Message}");
                    // Падать не надо, просто показываем ошибку в логе
                }

                _logger.Info("Дерево тегов обновлено успешно");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при обновлении дерева тегов: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Добавить теги ПЛК в дерево
        /// </summary>
        private void AddPlcTagsToTree(List<TagDefinition> plcTags, TreeViewItemViewModel plcTagsNode)
        {
            // Группируем ПЛК теги по таблицам
            var plcTagsByTable = plcTags
                .GroupBy(t => t.GroupName)
                .OrderBy(g => g.Key);

            foreach (var group in plcTagsByTable)
            {
                var tableNode = new TreeViewItemViewModel
                {
                    Header = group.Key ?? "Неизвестная таблица",
                    IsExpanded = false
                };

                // Добавляем теги в дерево
                foreach (var tag in group.OrderBy(t => t.Name))
                {
                    var tagNode = new TreeViewItemViewModel
                    {
                        Header = tag.Name,
                        IsExpanded = false,
                        Tag = tag
                    };

                    tableNode.Children.Add(tagNode);
                }

                // Добавляем узел таблицы только если есть теги
                if (tableNode.Children.Count > 0)
                {
                    plcTagsNode.Children.Add(tableNode);
                }
            }
        }

        /// <summary>
        /// Добавить теги DB в дерево
        /// </summary>
        private void AddDbTagsToTree(List<TagDefinition> dbTags, TreeViewItemViewModel dbTagsNode)
        {
            // Группируем DB теги по блокам данных
            var dbTagsByBlock = dbTags
                .GroupBy(t => t.GroupName)
                .OrderBy(g => g.Key);

            foreach (var group in dbTagsByBlock)
            {
                var isOptimized = group.Any(t => t.IsOptimized);
                var isSafety = group.Any(t => t.IsSafety);
                var isUdt = group.Any(t => t.IsUDT);

                var dbNode = new TreeViewItemViewModel
                {
                    Header = (group.Key ?? "Неизвестный DB") +
                             (isOptimized ? " (Optimized)" : "") +
                             (isSafety ? " (Safety)" : "") +
                             (isUdt ? " (UDT)" : ""),
                    IsExpanded = false
                };

                // Добавляем теги в дерево
                foreach (var tag in group.OrderBy(t => t.Name))
                {
                    // Получаем только имя переменной (без имени DB)
                    string displayName = GetTagDisplayName(tag.Name, group.Key);

                    var tagNode = new TreeViewItemViewModel
                    {
                        Header = displayName,
                        IsExpanded = false,
                        Tag = tag
                    };

                    dbNode.Children.Add(tagNode);
                }

                // Добавляем узел DB только если есть теги
                if (dbNode.Children.Count > 0)
                {
                    dbTagsNode.Children.Add(dbNode);
                }
            }
        }

        /// <summary>
        /// Получить отображаемое имя тега (без префикса DB)
        /// </summary>
        private string GetTagDisplayName(string fullName, string dbName)
        {
            if (string.IsNullOrEmpty(fullName))
                return "Unknown";

            // Если имя начинается с имени DB и точки, удаляем эту часть
            if (!string.IsNullOrEmpty(dbName) && fullName.StartsWith(dbName + "."))
            {
                return fullName.Substring(dbName.Length + 1);
            }

            // Иначе возвращаем как есть
            return fullName;
        }

        /// <summary>
        /// Поиск тегов по тексту
        /// </summary>
        public void SearchTags()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                RefreshTags(); // Если строка пуста, восстанавливаем полное дерево
                return;
            }

            try
            {
                IsLoading = true;
                _logger.Info($"Поиск тегов по запросу: {SearchText}");

                string searchTextLower = SearchText.ToLower();

                // Сбрасываем дерево
                InitializeTree();

                var plcTagsNode = RootItems[0];
                var dbTagsNode = RootItems[1];

                // Фильтруем по строке поиска
                var filteredPlcTags = _plcData.PlcTags.Where(tag =>
                    tag.Name.ToLower().Contains(searchTextLower) ||
                    tag.Address.ToLower().Contains(searchTextLower) ||
                    (tag.Comment != null && tag.Comment.ToLower().Contains(searchTextLower))).ToList();

                var filteredDbTags = _plcData.DbTags.Where(tag =>
                    tag.Name.ToLower().Contains(searchTextLower) ||
                    tag.Address.ToLower().Contains(searchTextLower) ||
                    (tag.Comment != null && tag.Comment.ToLower().Contains(searchTextLower))).ToList();

                // Добавляем отфильтрованные теги в дерево
                AddPlcTagsToTree(filteredPlcTags, plcTagsNode);
                AddDbTagsToTree(filteredDbTags, dbTagsNode);

                // Разворачиваем узлы для результатов поиска
                foreach (var node in plcTagsNode.Children)
                {
                    node.IsExpanded = true;
                }
                foreach (var node in dbTagsNode.Children)
                {
                    node.IsExpanded = true;
                }

                _logger.Info($"Найдено {filteredPlcTags.Count + filteredDbTags.Count} тегов по запросу");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при поиске тегов: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    /// <summary>
    /// ViewModel для элемента дерева
    /// </summary>
    public class TreeViewItemViewModel : ViewModelBase
    {
        private string _header;
        private bool _isExpanded;
        private bool _isSelected;
        private object _tag;

        /// <summary>
        /// Заголовок элемента
        /// </summary>
        public string Header
        {
            get => _header;
            set => SetProperty(ref _header, value);
        }

        /// <summary>
        /// Флаг развернутого узла
        /// </summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        /// <summary>
        /// Флаг выбранного узла
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        /// <summary>
        /// Тег (произвольные данные)
        /// </summary>
        public object Tag
        {
            get => _tag;
            set => SetProperty(ref _tag, value);
        }

        /// <summary>
        /// Дочерние элементы
        /// </summary>
        public ObservableCollection<TreeViewItemViewModel> Children { get; set; }

        public TreeViewItemViewModel()
        {
            Children = new ObservableCollection<TreeViewItemViewModel>();
        }
    }

    /// <summary>
    /// Простая реализация команды
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
    /// Реализация команды с параметром
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
}