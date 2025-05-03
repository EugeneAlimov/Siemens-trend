using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SiemensTrend.Core.Logging;
using SiemensTrend.Core.Models;
using SiemensTrend.ViewModels;

using SiemensTrend.Core.Models;
using SiemensTrend.Storage.TagManagement;
using System.Collections.Generic;
using SiemensTrend.Communication.TIA;
namespace SiemensTrend.Views
{
    /// <summary>
    /// ии ииии?? и MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// ии ииии?
        /// </summary>
        private readonly MainViewModel _viewModel;

        /// <summary>
        /// ии и ии ии?
        /// </summary>
        private readonly Logger _logger;

        /// <summary>
        /// иии?? ии?? и?
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            // ии? и??
            _logger = new Logger();

            // ии? ? ииии? ии ииии?
            _viewModel = new MainViewModel(_logger);
            DataContext = _viewModel;

            // ии?? иии ии и ииии? иии? UI-иии
            InitializeUI();

            // ииии?? иии иии
            UpdateConnectionState();
        }
        private void InitializeUI()
        {
            try
            {
                // иии ии?? ии и ии?
                AddTestButton();

                _logger.Info("MainWindow: UI иииии ? иии?? иии? и ии ? DB");
            }
            catch (Exception ex)
            {
                _logger.Error($"InitializeUI: ии и ииии? иии? иии UI: {ex.Message}");
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Создать тестовые теги"
        /// </summary>
        private void BtnCreateTestTags_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("BtnCreateTestTags_Click: Создание тестовых тегов");

                // Запрос подтверждения
                var result = MessageBox.Show("Это заменит все существующие теги на тестовые. Продолжить?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Создаем тестовые теги
                    _viewModel.CreateTestTags();

                    _logger.Info("BtnCreateTestTags_Click: Тестовые теги созданы");
                    MessageBox.Show("Тестовые теги созданы успешно",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"BtnCreateTestTags_Click: Ошибка: {ex.Message}");
                MessageBox.Show($"Ошибка при создании тестовых тегов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// иии? ии? ии "иии и"
        /// </summary>
        private void BtnSaveLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // ии? ии иии? и??
                Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "иии и?? (*.txt)|*.txt",
                    Title = "иии? и?",
                    DefaultExt = ".txt",
                    AddExtension = true
                };

                // и? ииии ии и?
                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;
                    _logger.Info($"иии? и? ? и?: {filePath}");

                    // иии иии? и? ? и?
                    System.IO.File.WriteAllText(filePath, txtLog.Text);

                    _logger.Info("и ии? ии??");
                    MessageBox.Show("и ии? ии??",
                        "иии?", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"ии и иии? и?: {ex.Message}");
                MessageBox.Show($"ии и иии? и?: {ex.Message}",
                    "ии", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// иии? ии? ии "ии?? и"
        /// </summary>
        private void BtnClearLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("ии? и?");

                // ии? иии? и?
                txtLog.Clear();

                _logger.Info("и ии");
            }
            catch (Exception ex)
            {
                _logger.Error($"ии и ии? и?: {ex.Message}");
                MessageBox.Show($"ии и ии? и?: {ex.Message}",
                    "ии", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddTestButton()
        {
            try
            {
                ToolBarTray toolbarTray = null;

                // и?? ?? и??
                toolbarTray = FindName("toolBarTray") as ToolBarTray;

                // и? ?? и?? ?? и??, ?? ии ?? и? ? иии? ии
                if (toolbarTray == null)
                {
                    var toolbarTrays = FindVisualChildren<ToolBarTray>(this);
                    if (toolbarTrays.Count > 0)
                    {
                        toolbarTray = toolbarTrays[0];
                    }
                }

                if (toolbarTray != null && toolbarTray.ToolBars.Count > 0)
                {
                    var toolbar = toolbarTray.ToolBars[0];

                    // ии? ии?? ии
                    var btnTest = new Button
                    {
                        Content = "и? DB (ии?)",
                        Margin = new Thickness(3),
                        Padding = new Thickness(5, 3, 5, 3),
                        Background = Brushes.LightYellow
                    };

                    btnTest.Click += (s, e) => TestDbTagsLoading();

                    // иии иии?? ? ии
                    toolbar.Items.Add(new Separator());
                    toolbar.Items.Add(btnTest);

                    _logger.Info("AddTestButton: ии?? ии иии");
                }
                else
                {
                    _logger.Warn("AddTestButton: ии ииии ?? ии?");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"AddTestButton: ии и иии? ии?? ии: {ex.Message}");
            }
        }

        /// <summary>
        /// ии?? ии?? и?? DB и ии?
        /// </summary>
        private List<TagDefinition> CreateTestDbTags()
        {
            var tags = new List<TagDefinition>();

            // ии? иии ии?? и??
            tags.Add(new TagDefinition
            {
                Id = Guid.NewGuid(),
                Name = "DB1",
                Address = "Standard",
                DataType = TagDataType.UDT,
                Comment = "ии?? и? ии",
                GroupName = "DB",
                IsOptimized = false,
                IsUDT = true
            });

            tags.Add(new TagDefinition
            {
                Id = Guid.NewGuid(),
                Name = "DB2",
                Address = "Optimized",
                DataType = TagDataType.UDT,
                Comment = "ии?? иииии? и? ии",
                GroupName = "DB",
                IsOptimized = true,
                IsUDT = true
            });

            tags.Add(new TagDefinition
            {
                Id = Guid.NewGuid(),
                Name = "DB_Motor",
                Address = "Standard",
                DataType = TagDataType.UDT,
                Comment = "ии?? и? ии и иии? иии?",
                GroupName = "DB",
                IsOptimized = false,
                IsUDT = true
            });

            // иии иии? и ии ии
            tags.Add(new TagDefinition
            {
                Id = Guid.NewGuid(),
                Name = "DB1.Value",
                Address = "DB1.DBD0",
                DataType = TagDataType.Real,
                Comment = "ии??",
                GroupName = "DB1",
                IsOptimized = false
            });

            tags.Add(new TagDefinition
            {
                Id = Guid.NewGuid(),
                Name = "DB1.Status",
                Address = "DB1.DBW4",
                DataType = TagDataType.Int,
                Comment = "ии",
                GroupName = "DB1",
                IsOptimized = false
            });

            tags.Add(new TagDefinition
            {
                Id = Guid.NewGuid(),
                Name = "DB2.Value",
                Address = "DB2.Value",
                DataType = TagDataType.Real,
                Comment = "ии?? (иииии? и?)",
                GroupName = "DB2",
                IsOptimized = true
            });

            tags.Add(new TagDefinition
            {
                Id = Guid.NewGuid(),
                Name = "DB_Motor.Speed",
                Address = "DB_Motor.DBD0",
                DataType = TagDataType.Real,
                Comment = "ии?? иии",
                GroupName = "DB_Motor",
                IsOptimized = false
            });

            tags.Add(new TagDefinition
            {
                Id = Guid.NewGuid(),
                Name = "DB_Motor.Start",
                Address = "DB_Motor.DBX4.0",
                DataType = TagDataType.Bool,
                Comment = "ии иии",
                GroupName = "DB_Motor",
                IsOptimized = false
            });

            tags.Add(new TagDefinition
            {
                Id = Guid.NewGuid(),
                Name = "DB_Motor.Stop",
                Address = "DB_Motor.DBX4.1",
                DataType = TagDataType.Bool,
                Comment = "ии? иии",
                GroupName = "DB_Motor",
                IsOptimized = false
            });

            return tags;
        }

        /// <summary>
        /// иии? и?? и ииии ии и?? DB
        /// </summary>
        private async void TestDbTagsLoading()
        {
            try
            {
                _logger.Info("TestDbTagsLoading: ии ииии ии?? и?? DB");
                _viewModel.StatusMessage = "ииии ии?? и?? DB...";
                _viewModel.IsLoading = true;

                // иии иии?? ? TIA Portal
                if (!_viewModel.IsConnected)
                {
                    _logger.Warn("TestDbTagsLoading: и иии?? ? TIA Portal");
                    MessageBox.Show("иии? ии? ииии ? TIA Portal.",
                        "ииии??", MessageBoxButton.OK, MessageBoxImage.Warning);
                    _viewModel.IsLoading = false;
                    return;
                }

                // ии? ии?? и? DB и иии??
                var testTags = CreateTestDbTags();
                _logger.Info($"TestDbTagsLoading: ии? {testTags.Count} ии?? и?? DB");

                // ии? ? иии иии
                _viewModel.DbTags.Clear();
                foreach (var tag in testTags)
                {
                    _viewModel.DbTags.Add(tag);
                }

                _logger.Info("TestDbTagsLoading: ии?? и? DB иии ии?");
                _viewModel.StatusMessage = $"иии {testTags.Count} ии?? и?? DB";
            }
            catch (Exception ex)
            {
                _logger.Error($"TestDbTagsLoading: ии: {ex.Message}");
                _viewModel.StatusMessage = "ии и ииии ии?? и?? DB";
            }
            finally
            {
                _viewModel.IsLoading = false;
            }
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
        /// Обработчик нажатия кнопки "Добавить тег"
        /// </summary>
        private void BtnAddTag_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("BtnAddTag_Click: Вызов диалога добавления тега");

                // Создаем диалог для добавления тега
                var dialog = new Dialogs.TagEditorDialog();
                dialog.Owner = this;

                // Показываем диалог
                if (dialog.ShowDialog() == true)
                {
                    // Получаем созданный тег
                    var newTag = dialog.Tag;

                    // Добавляем тег в модель
                    _viewModel.AddNewTag(newTag);

                    _logger.Info($"BtnAddTag_Click: Добавлен новый тег: {newTag.Name}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"BtnAddTag_Click: Ошибка: {ex.Message}");
                MessageBox.Show($"Ошибка при добавлении тега: {ex.Message}",
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
                _logger.Info("BtnEditTag_Click: Вызов диалога редактирования тега");

                // Получаем выбранный тег из таблицы PLC или DB
                TagDefinition selectedTag = null;

                // Проверяем, есть ли выбранный тег в таблице PLC
                var plcDataGrid = this.FindName("dgPlcTags") as DataGrid;
                if (plcDataGrid != null && plcDataGrid.SelectedItem is TagDefinition)
                {
                    selectedTag = plcDataGrid.SelectedItem as TagDefinition;
                }

                // Если в таблице PLC ничего не выбрано, проверяем таблицу DB
                if (selectedTag == null)
                {
                    var dbDataGrid = this.FindName("dgDbTags") as DataGrid;
                    if (dbDataGrid != null && dbDataGrid.SelectedItem is TagDefinition)
                    {
                        selectedTag = dbDataGrid.SelectedItem as TagDefinition;
                    }
                }

                if (selectedTag == null)
                {
                    _logger.Warn("BtnEditTag_Click: Не выбран тег для редактирования");
                    MessageBox.Show("Пожалуйста, выберите тег для редактирования",
                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Создаем диалог для редактирования тега
                var dialog = new Dialogs.TagEditorDialog(selectedTag);
                dialog.Owner = this;

                // Показываем диалог
                if (dialog.ShowDialog() == true)
                {
                    // Получаем отредактированный тег
                    var updatedTag = dialog.Tag;

                    // Обновляем тег в модели
                    _viewModel.EditTag(selectedTag, updatedTag);

                    _logger.Info($"BtnEditTag_Click: Отредактирован тег: {updatedTag.Name}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"BtnEditTag_Click: Ошибка: {ex.Message}");
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
                _logger.Info("BtnRemoveTag_Click: Удаление тега");

                // Получаем выбранный тег из таблицы PLC или DB
                TagDefinition selectedTag = null;

                // Проверяем, есть ли выбранный тег в таблице PLC
                var plcDataGrid = this.FindName("dgPlcTags") as DataGrid;
                if (plcDataGrid != null && plcDataGrid.SelectedItem is TagDefinition)
                {
                    selectedTag = plcDataGrid.SelectedItem as TagDefinition;
                }

                // Если в таблице PLC ничего не выбрано, проверяем таблицу DB
                if (selectedTag == null)
                {
                    var dbDataGrid = this.FindName("dgDbTags") as DataGrid;
                    if (dbDataGrid != null && dbDataGrid.SelectedItem is TagDefinition)
                    {
                        selectedTag = dbDataGrid.SelectedItem as TagDefinition;
                    }
                }

                if (selectedTag == null)
                {
                    _logger.Warn("BtnRemoveTag_Click: Не выбран тег для удаления");
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

                    _logger.Info($"BtnRemoveTag_Click: Удален тег: {selectedTag.Name}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"BtnRemoveTag_Click: Ошибка: {ex.Message}");
                MessageBox.Show($"Ошибка при удалении тега: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Импорт тегов"
        /// </summary>
        private void BtnImportTags_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("BtnImportTags_Click: Импорт тегов из CSV");

                // Создаем диалог выбора файла
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "CSV файлы (*.csv)|*.csv|Все файлы (*.*)|*.*",
                    Title = "Выбор файла с тегами для импорта"
                };

                // Показываем диалог
                if (openFileDialog.ShowDialog() == true)
                {
                    string filePath = openFileDialog.FileName;
                    _logger.Info($"BtnImportTags_Click: Выбран файл: {filePath}");

                    // Показываем индикатор загрузки
                    _viewModel.IsLoading = true;
                    _viewModel.StatusMessage = "Импорт тегов...";
                    _viewModel.ProgressValue = 10;

                    // Импортируем теги
                    var tagManager = new Storage.TagManagement.TagManager(_logger);
                    var importedTags = tagManager.ImportTagsFromCsv(filePath);

                    // Спрашиваем, что делать с существующими тегами
                    var result = MessageBox.Show(
                        "Заменить существующие теги импортированными? " +
                        "Нажмите 'Да' для замены, 'Нет' для добавления к существующим.",
                        "Импорт тегов", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Cancel)
                    {
                        _logger.Info("BtnImportTags_Click: Импорт отменен пользователем");
                        _viewModel.IsLoading = false;
                        return;
                    }

                    _viewModel.ProgressValue = 30;

                    // Если выбрана замена, очищаем текущие теги
                    if (result == MessageBoxResult.Yes)
                    {
                        _viewModel.PlcTags.Clear();
                        _viewModel.DbTags.Clear();
                        _viewModel.AvailableTags.Clear();
                        _logger.Info("BtnImportTags_Click: Существующие теги очищены");
                    }

                    _viewModel.ProgressValue = 60;

                    // Добавляем импортированные теги
                    int addedCount = 0;
                    foreach (var tag in importedTags)
                    {
                        _viewModel.AddNewTag(tag);
                        addedCount++;
                    }

                    _viewModel.ProgressValue = 90;

                    // Сохраняем изменения
                    _viewModel.SaveTagsToStorage();

                    _viewModel.ProgressValue = 100;

                    _logger.Info($"BtnImportTags_Click: Импортировано {addedCount} тегов из {importedTags.Count}");
                    MessageBox.Show($"Импортировано {addedCount} тегов из {importedTags.Count}",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);

                    _viewModel.StatusMessage = $"Импортировано {addedCount} тегов";
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"BtnImportTags_Click: Ошибка: {ex.Message}");
                MessageBox.Show($"Ошибка при импорте тегов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                _viewModel.StatusMessage = "Ошибка при импорте тегов";
            }
            finally
            {
                _viewModel.IsLoading = false;
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Экспорт тегов"
        /// </summary>
        private void BtnExportTags_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("BtnExportTags_Click: Экспорт тегов в CSV");

                // Проверяем наличие тегов для экспорта
                int tagCount = _viewModel.PlcTags.Count + _viewModel.DbTags.Count;
                if (tagCount == 0)
                {
                    _logger.Warn("BtnExportTags_Click: Нет тегов для экспорта");
                    MessageBox.Show("Нет тегов для экспорта",
                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Создаем диалог сохранения файла
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV файлы (*.csv)|*.csv|Все файлы (*.*)|*.*",
                    Title = "Экспорт тегов",
                    DefaultExt = ".csv",
                    AddExtension = true
                };

                // Показываем диалог
                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;
                    _logger.Info($"BtnExportTags_Click: Выбран файл: {filePath}");

                    // Показываем индикатор загрузки
                    _viewModel.IsLoading = true;
                    _viewModel.StatusMessage = "Экспорт тегов...";
                    _viewModel.ProgressValue = 10;

                    // Собираем все теги для экспорта
                    var allTags = new List<TagDefinition>();
                    allTags.AddRange(_viewModel.PlcTags);
                    allTags.AddRange(_viewModel.DbTags);

                    _viewModel.ProgressValue = 50;

                    // Экспортируем теги
                    var tagManager = new Storage.TagManagement.TagManager(_logger);
                    bool success = tagManager.ExportTagsToCsv(allTags, filePath);

                    _viewModel.ProgressValue = 100;

                    if (success)
                    {
                        _logger.Info($"BtnExportTags_Click: Экспортировано {allTags.Count} тегов");
                        MessageBox.Show($"Экспортировано {allTags.Count} тегов",
                            "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                        _viewModel.StatusMessage = $"Экспортировано {allTags.Count} тегов";
                    }
                    else
                    {
                        _logger.Error("BtnExportTags_Click: Ошибка при экспорте тегов");
                        MessageBox.Show("Ошибка при экспорте тегов",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        _viewModel.StatusMessage = "Ошибка при экспорте тегов";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"BtnExportTags_Click: Ошибка: {ex.Message}");
                MessageBox.Show($"Ошибка при экспорте тегов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                _viewModel.StatusMessage = "Ошибка при экспорте тегов";
            }
            finally
            {
                _viewModel.IsLoading = false;
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Сохранить теги"
        /// </summary>
        private void BtnSaveTags_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("BtnSaveTags_Click: Сохранение тегов");

                // Сохраняем теги
                _viewModel.SaveTagsToStorage();

                _logger.Info("BtnSaveTags_Click: Теги сохранены успешно");
                MessageBox.Show("Теги сохранены успешно",
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.Error($"BtnSaveTags_Click: Ошибка: {ex.Message}");
                MessageBox.Show($"Ошибка при сохранении тегов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

