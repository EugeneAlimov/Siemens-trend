using System;
using System.Windows;
using SiemensTrend.Communication;
using SiemensTrend.Core.Models;
using SiemensTrend.Views.Dialogs;

namespace SiemensTrend.Views
{
    /// <summary>
    /// Часть класса MainWindow для управления тегами
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Обработчик нажатия кнопки "Добавить тег"
        /// </summary>
        /// <summary>
        /// Обработчик нажатия кнопки "Добавить тег"
        /// </summary>
        private void BtnAddTag_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("Вызов диалога добавления тегов");

                // Проверяем подключение к TIA Portal
                if (!_viewModel.IsConnected || _viewModel.TiaPortalService == null)
                {
                    _logger.Warn("Попытка добавления тегов без подключения к TIA Portal");
                    MessageBox.Show("Необходимо сначала подключиться к TIA Portal",
                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Создаем диалог для добавления тегов
                var dialog = new AddTagsDialog(_logger, _communicationService);
                dialog.Owner = this;

                // Показываем диалог
                if (dialog.ShowDialog() == true)
                {
                    // Получаем найденные теги
                    var foundTags = dialog.FoundTags;

                    if (foundTags != null && foundTags.Count > 0)
                    {
                        // Добавляем теги в модель
                        foreach (var tag in foundTags)
                        {
                            _viewModel.AddNewTag(tag);
                        }

                        _logger.Info($"Добавлено {foundTags.Count} тегов");
                        _viewModel.StatusMessage = $"Добавлено {foundTags.Count} тегов";
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
        /// Обработчик нажатия кнопки "Редактировать тег"
        /// </summary>
        private void BtnEditTag_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("Вызов диалога редактирования тега");

                // Получаем выбранный тег из таблицы тегов
                TagDefinition selectedTag = dgTags.SelectedItem as TagDefinition;

                if (selectedTag == null)
                {
                    _logger.Warn("Не выбран тег для редактирования");
                    MessageBox.Show("Пожалуйста, выберите тег для редактирования",
                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Создаем диалог для редактирования тега
                var dialog = new TagEditorDialog(selectedTag);
                dialog.Owner = this;

                // Показываем диалог
                if (dialog.ShowDialog() == true)
                {
                    // Получаем отредактированный тег
                    var updatedTag = dialog.Tag;

                    // Обновляем тег в модели
                    _viewModel.EditTag(selectedTag, updatedTag);

                    _logger.Info($"Отредактирован тег: {updatedTag.Name}");
                    _viewModel.StatusMessage = $"Тег {updatedTag.Name} отредактирован";
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при редактировании тега: {ex.Message}");
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
                _logger.Info("Удаление тега");

                // Получаем выбранный тег из таблицы тегов
                TagDefinition selectedTag = dgTags.SelectedItem as TagDefinition;

                if (selectedTag == null)
                {
                    _logger.Warn("Не выбран тег для удаления");
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

                    _logger.Info($"Удален тег: {selectedTag.Name}");
                    _viewModel.StatusMessage = $"Тег {selectedTag.Name} удален";
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при удалении тега: {ex.Message}");
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
                _logger.Info("Сохранение тегов");

                // Сохраняем теги
                _viewModel.SaveTagsToStorage();

                _logger.Info("Теги сохранены успешно");
                _viewModel.StatusMessage = "Теги сохранены успешно";
                MessageBox.Show("Теги сохранены успешно",
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при сохранении тегов: {ex.Message}");
                MessageBox.Show($"Ошибка при сохранении тегов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Запустить мониторинг"
        /// </summary>
        private async void BtnStartMonitoring_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("Запуск мониторинга тегов");

                await _viewModel.StartMonitoringAsync();

                _logger.Info("Мониторинг запущен");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при запуске мониторинга: {ex.Message}");
                MessageBox.Show($"Ошибка при запуске мониторинга: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Остановить мониторинг"
        /// </summary>
        private async void BtnStopMonitoring_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("Остановка мониторинга тегов");

                await _viewModel.StopMonitoringAsync();

                _logger.Info("Мониторинг остановлен");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при остановке мониторинга: {ex.Message}");
                MessageBox.Show($"Ошибка при остановке мониторинга: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}