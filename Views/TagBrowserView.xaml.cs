using System.Collections.ObjectModel;
using System.Windows.Controls;
using SiemensTrend.Communication.TIA;
using SiemensTrend.Core.Logging;
using SiemensTrend.Core.Models;
using System.Windows.Input;

namespace SiemensTrend.ViewModels
{
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

        public TagBrowserViewModel(Logger logger, TiaPortalCommunicationService tiaService)
        {
            _logger = logger;
            _tiaService = tiaService;

            RootItems = new ObservableCollection<TreeViewItemViewModel>();

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

                // Получаем все таблицы тегов
                await Task.Run(() => {
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

                        plcTagsNode.Children.Add(tableNode);
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
                                    Address = tag.Address,
                                    DataType = tag.DataType,
                                    GroupName = tag.DbName
                                }
                            };

                            dbNode.Children.Add(tagNode);
                        }

                        dbTagsNode.Children.Add(dbNode);
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