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
        /// Initializes the UI components and sets up any required configurations.
        /// </summary>
        private void InitializeUI()
        {
            // Add your UI initialization logic here.
            // For example, setting up default values, binding data, or configuring controls.
            _logger.Info("UI initialized successfully.");
        }

        /// <summary>
        /// иии?? ии?? и?
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            // Инициализируем логгер
            _logger = new Logger();

            // Создаем и инициализируем ViewModel
            _viewModel = new MainViewModel(_logger);
            DataContext = _viewModel;

            // Инициализируем интерфейс
            InitializeUI();

            // Загружаем теги при запуске
            _viewModel.Initialize();

            // Обновляем состояние интерфейса
            UpdateConnectionState();
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

