using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
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
            set => SetProperty(ref _searchText, value);
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
        /// Событие выбора тега для мониторинга
        /// </summary>
        public event EventHandler<TagDefinition> TagSelected;

        /// <summary>
        /// Конструктор
        /// </summary>
        public TagBrowserViewModel(Logger logger, TiaPortalCommunicationService tiaService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tiaService = tiaService ?? throw new ArgumentNullException(nameof(tiaService));

            RootItems = new ObservableCollection<TreeViewItemViewModel>();

            // Инициализируем команды
            RefreshCommand = new RelayCommand(RefreshTags);
            AddTagCommand = new RelayCommand<TreeViewItemViewModel>(AddTagToMonitoring);

            // Инициализируем дерево
            InitializeTree();
        }

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
        private async void RefreshTags()
        {
            try
            {
                IsLoading = true;

                InitializeTree(); // Очищаем и создаем базовую структуру

                var plcTagsNode = RootItems[0];
                var dbTagsNode = RootItems[1];

                // Выполняем в отдельном потоке, чтобы не блокировать UI
                await Task.Run(() => {
                    // Получаем все таблицы тегов
                    var tagTables = _tiaService.GetAllTagTables();

                    // Группируем таблицы тегов
                    foreach (var tagTable in tagTables)
                    {
                        var tableNode = new TreeViewItemViewModel
                        {
                            Header = tagTable.Name,
                            IsExpanded = false
                        };

                        // Читаем теги из таблицы
                        var plcTags = _tiaService.ReadPlcTagTable(tagTable);

                        // Добавляем теги в дерево
                        foreach (var tag in plcTags.Tags)
                        {
                            var tagNode = new TreeViewItemViewModel
                            {
                                Header = tag.Name,
                                IsExpanded = false,
                                Tag = new TagDefinition
                                {
                                    Name = tag.Name,
                                    Address = tag.Address,
                                    DataType = tag.DataType,
                                    GroupName = tag.TableName
                                }
                            };

                            tableNode.Children.Add(tagNode);
                        }

                        // Добавляем узел таблицы только если есть теги
                        if (tableNode.Children.Count > 0)
                        {
                            plcTagsNode.Children.Add(tableNode);
                        }
                    }

                    // Получаем все блоки данных
                    var dataBlocks = _tiaService.GetAllDataBlocks();

                    // Группируем блоки данных
                    foreach (var dataBlock in dataBlocks)
                    {
                        var dbNode = new TreeViewItemViewModel
                        {
                            Header = dataBlock.Name,
                            IsExpanded = false
                        };

                        // Читаем теги из блока данных
                        var dbTags = _tiaService.ReadDataBlockTags(dataBlock);

                        // Добавляем теги в дерево
                        foreach (var tag in dbTags.Tags)
                        {
                            var tagNode = new TreeViewItemViewModel
                            {
                                Header = tag.Name,
                                IsExpanded = false,
                                Tag = new TagDefinition
                                {
                                    Name = tag.FullName,
                                    Address = tag.IsOptimized ? "Optimized" : tag.Address,
                                    DataType = tag.DataType,
                                    GroupName = tag.DbName
                                }
                            };

                            dbNode.Children.Add(tagNode);
                        }

                        // Добавляем узел DB только если есть теги
                        if (dbNode.Children.Count > 0)
                        {
                            dbTagsNode.Children.Add(dbNode);
                        }
                    }
                });

                _logger.Info("Дерево тегов обновлено");
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
        /// Добавление тега в мониторинг
        /// </summary>
        private void AddTagToMonitoring(TreeViewItemViewModel item)
        {
            if (item?.Tag is TagDefinition tagDef)
            {
                TagSelected?.Invoke(this, tagDef);
                _logger.Info($"Выбран тег для мониторинга: {tagDef.Name}");
            }
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
                _logger.Info($"Поиск тегов по запросу: {SearchText}");

                string searchTextLower = SearchText.ToLower();

                // Временно отключаем обновление, чтобы не вызывать множественные перерисовки
                InitializeTree(); // Очищаем и создаем базовую структуру

                var plcTagsNode = RootItems[0];
                var dbTagsNode = RootItems[1];

                // Получаем все теги (для демо просто используем то, что есть)
                var allTags = _tiaService.GetTagsFromProjectAsync().Result;

                // Фильтруем по строке поиска
                var filteredTags = allTags.Where(tag =>
                    tag.Name.ToLower().Contains(searchTextLower) ||
                    tag.Address.ToLower().Contains(searchTextLower) ||
                    (tag.Comment != null && tag.Comment.ToLower().Contains(searchTextLower))).ToList();

                // Группируем отфильтрованные теги
                var plcTags = filteredTags.Where(t => !t.GroupName.StartsWith("DB")).ToList();
                var dbTags = filteredTags.Where(t => t.GroupName.StartsWith("DB")).ToList();

                // Группируем PLC теги по таблицам
                var plcTagsByTable = plcTags.GroupBy(t => t.GroupName);
                foreach (var group in plcTagsByTable)
                {
                    var tableNode = new TreeViewItemViewModel
                    {
                        Header = group.Key,
                        IsExpanded = true
                    };

                    foreach (var tag in group)
                    {
                        tableNode.Children.Add(new TreeViewItemViewModel
                        {
                            Header = tag.Name,
                            IsExpanded = false,
                            Tag = tag
                        });
                    }

                    plcTagsNode.Children.Add(tableNode);
                }

                // Группируем DB теги по блокам данных
                var dbTagsByBlock = dbTags.GroupBy(t => t.GroupName);
                foreach (var group in dbTagsByBlock)
                {
                    var dbNode = new TreeViewItemViewModel
                    {
                        Header = group.Key,
                        IsExpanded = true
                    };

                    foreach (var tag in group)
                    {
                        dbNode.Children.Add(new TreeViewItemViewModel
                        {
                            Header = tag.Name,
                            IsExpanded = false,
                            Tag = tag
                        });
                    }

                    dbTagsNode.Children.Add(dbNode);
                }

                _logger.Info($"Найдено {filteredTags.Count} тегов");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при поиске тегов: {ex.Message}");
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