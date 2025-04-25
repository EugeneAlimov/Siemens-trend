using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace SiemensTrend.Views
{
    /// <summary>
    /// Часть класса MainWindow для работы с кэшем проектов
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Обработчик кнопки "Очистить кэш"
        /// </summary>
        private void BtnClearCache_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Если подключено - очищаем текущий проект
                if (_viewModel.IsConnected)
                {
                    ClearCurrentCache();
                }
                else
                {
                    // Если не подключено - всегда показываем диалог выбора проекта из кэша
                    ShowClearCacheDialog();
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"BtnClearCache_Click: Ошибка: {ex.Message}");
                MessageBox.Show($"Ошибка при очистке кэша: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Диалог для выбора проекта из кэша для очистки
        /// </summary>
        private void ShowClearCacheDialog()
        {
            try
            {
                // Получаем список проектов в кэше
                var cachedProjects = _viewModel.GetCachedProjects();

                if (cachedProjects.Count == 0)
                {
                    MessageBox.Show("В кэше нет сохраненных проектов",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Создаем диалог выбора проекта
                var dialog = new Window
                {
                    Title = "Выбор проекта для очистки кэша",
                    Width = 400,
                    Height = 300,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    ResizeMode = ResizeMode.NoResize
                };

                // Создаем панель с элементами управления
                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                // Добавляем заголовок
                var label = new TextBlock
                {
                    Text = "Выберите проект для очистки кэша:",
                    Margin = new Thickness(10),
                    TextWrapping = TextWrapping.Wrap
                };
                Grid.SetRow(label, 0);
                grid.Children.Add(label);

                // Добавляем список проектов
                var listBox = new ListBox
                {
                    Margin = new Thickness(10),
                    ItemsSource = cachedProjects,
                    SelectedIndex = cachedProjects.Count > 0 ? 0 : -1
                };
                Grid.SetRow(listBox, 1);
                grid.Children.Add(listBox);

                // Добавляем кнопки
                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(10)
                };
                Grid.SetRow(buttonPanel, 2);

                var cancelButton = new Button
                {
                    Content = "Отмена",
                    Width = 80,
                    Margin = new Thickness(0, 0, 10, 0)
                };
                cancelButton.Click += (s, e) => dialog.DialogResult = false;
                buttonPanel.Children.Add(cancelButton);

                var okButton = new Button
                {
                    Content = "Очистить",
                    Width = 80
                };
                okButton.Click += (s, e) =>
                {
                    if (listBox.SelectedItem != null)
                    {
                        string selectedProject = listBox.SelectedItem as string;

                        // Спрашиваем подтверждение
                        var result = MessageBox.Show($"Вы действительно хотите удалить кэш проекта {selectedProject}?",
                            "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            dialog.DialogResult = true;
                        }
                        else
                        {
                            dialog.DialogResult = false;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Выберите проект из списка",
                            "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                };
                buttonPanel.Children.Add(okButton);

                grid.Children.Add(buttonPanel);

                // Устанавливаем контент и показываем диалог
                dialog.Content = grid;
                bool? result = dialog.ShowDialog();

                // Обрабатываем результат
                if (result == true && listBox.SelectedItem != null)
                {
                    string selectedProject = listBox.SelectedItem as string;
                    bool success = _viewModel.ClearProjectCache(selectedProject);

                    if (success)
                    {
                        MessageBox.Show($"Кэш проекта {selectedProject} успешно очищен",
                            "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Не удалось очистить кэш проекта {selectedProject}",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"ShowClearCacheDialog: Ошибка: {ex.Message}");
                MessageBox.Show($"Ошибка при работе с диалогом очистки кэша: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Очистка кэша текущего проекта
        /// </summary>
        private void ClearCurrentCache()
        {
            try
            {
                string projectName = _viewModel.CurrentProjectName;

                if (string.IsNullOrEmpty(projectName) || projectName == "Нет проекта")
                {
                    MessageBox.Show("Нет активного проекта для очистки кэша",
                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Спрашиваем подтверждение
                var result = MessageBox.Show($"Вы действительно хотите удалить кэш проекта {projectName}?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                // Очищаем кэш
                bool success = _viewModel.ClearCurrentProjectCache();

                if (success)
                {
                    MessageBox.Show($"Кэш проекта {projectName} успешно очищен",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Не удалось очистить кэш проекта {projectName}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"ClearCurrentCache: Ошибка: {ex.Message}");
                MessageBox.Show($"Ошибка при очистке кэша: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Показ информации о кэше проекта
        /// </summary>
        private void ShowCacheInfo()
        {
            try
            {
                string projectName = _viewModel.CurrentProjectName;

                if (string.IsNullOrEmpty(projectName) || projectName == "Нет проекта")
                {
                    MessageBox.Show("Нет активного проекта для просмотра информации о кэше",
                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Получаем информацию о кэше
                var cacheInfo = _viewModel.GetCacheInfo(projectName);

                if (cacheInfo.ContainsKey("error"))
                {
                    MessageBox.Show($"Ошибка при получении информации о кэше: {cacheInfo["error"]}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Формируем текст сообщения
                string message = $"Информация о кэше проекта {projectName}:\n\n";

                // Добавляем основную информацию
                if (cacheInfo.ContainsKey("exists") && (bool)cacheInfo["exists"])
                {
                    message += "Кэш существует: Да\n";

                    if (cacheInfo.ContainsKey("hasTags"))
                    {
                        bool hasTags = (bool)cacheInfo["hasTags"];
                        message += $"Теги ПЛК: {(hasTags ? "Есть" : "Нет")}\n";
                    }

                    if (cacheInfo.ContainsKey("hasDBs"))
                    {
                        bool hasDBs = (bool)cacheInfo["hasDBs"];
                        message += $"Блоки данных: {(hasDBs ? "Есть" : "Нет")}\n";
                    }

                    if (cacheInfo.ContainsKey("tagCount"))
                        message += $"Количество тегов: {cacheInfo["tagCount"]}\n";

                    if (cacheInfo.ContainsKey("dbCount"))
                        message += $"Количество блоков данных: {cacheInfo["dbCount"]}\n";

                    if (cacheInfo.ContainsKey("tagsLastUpdated"))
                        message += $"Последнее обновление тегов: {cacheInfo["tagsLastUpdated"]}\n";

                    if (cacheInfo.ContainsKey("dbsLastUpdated"))
                        message += $"Последнее обновление блоков данных: {cacheInfo["dbsLastUpdated"]}\n";
                }
                else
                {
                    message += "Кэш не существует для этого проекта";
                }

                // Показываем информацию
                MessageBox.Show(message, "Информация о кэше", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.Error($"ShowCacheInfo: Ошибка: {ex.Message}");
                MessageBox.Show($"Ошибка при получении информации о кэше: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}