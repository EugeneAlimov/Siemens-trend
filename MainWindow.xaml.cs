using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using S7.Net;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WPF;
using Siemens_trend.Helpers;

namespace Siemens_trend
{
    public partial class MainWindow : Window
    {
        private Plc? plc;
        private TiaPortalHelper tiaHelper;
        private string? projectPath;
        private HashSet<string> selectedTags = new HashSet<string>();
        private System.Timers.Timer updateTimer;
        private Dictionary<string, ObservableCollection<double>> tagData;

        public ISeries[] Series { get; set; }
        public Axis[] XAxes { get; set; }
        public Axis[] YAxes { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            tiaHelper = new TiaPortalHelper();
            tagData = new Dictionary<string, ObservableCollection<double>>();

            // 🔹 Делаем кнопку неактивной при старте
            StartMonitoringButton.IsEnabled = false;

            // 🔹 Загружаем теги и DB при старте, если они есть
            if (tiaHelper.AreTagsAvailable())
            {
                tiaHelper.GetTagsAndDB();
                UpdateTagTreeView();
            }

            // 🔹 Назначаем обработчики кнопок
            ConnectButton.Click += ConnectButton_Click;
            DisconnectButton.Click += DisconnectButton_Click;
            LoadTagsButton.Click += LoadTagsButton_Click;
            ExportFromTIAButton.Click += ExportFromTIAButton_Click;
            StartMonitoringButton.Click += StartMonitoringButton_Click;

            // Инициализация осей
            XAxes = new[] { new Axis { Name = "Time" } };
            YAxes = new[] { new Axis { Name = "Value" } };

            // Инициализация пустого графика
            Series = new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = new ObservableCollection<double>(),
                    Fill = null
                }
            };

            DataContext = this;

            updateTimer = new System.Timers.Timer(1000);
            updateTimer.Elapsed += UpdateChart;
        }

        private void StartMonitoringButton_Click(object sender, RoutedEventArgs e)
        {
            updateTimer.Start();
        }

        private void UpdateChart(object sender, System.Timers.ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                bool dataUpdated = false; // Флаг, чтобы обновлять UI только при изменении данных

                foreach (var tag in selectedTags)
                {
                    try
                    {
                        object value = tiaHelper.ReadTagValue(tag);

                        if (value == null || value.ToString() == "N/A" || value.ToString().Contains("Ошибка"))
                        {
                            LogError($"⚠ Тег {tag} не найден или ошибка чтения.");
                            continue; // Пропускаем этот тег, не добавляя 0
                        }

                        // ✅ Пробуем преобразовать в double
                        double newValue;
                        if (value is double doubleValue)
                        {
                            newValue = doubleValue;
                        }
                        else if (!double.TryParse(value.ToString(), out newValue))
                        {
                            LogError($"⚠ Ошибка преобразования значения тега {tag}: {value}");
                            continue; // Пропускаем этот тег, если не удалось преобразовать
                        }

                        // ✅ Проверяем, есть ли тег в `tagData`
                        if (!tagData.ContainsKey(tag))
                        {
                            tagData[tag] = new ObservableCollection<double>();
                        }

                        // ✅ Обновляем данные
                        tagData[tag].Add(newValue);
                        if (tagData[tag].Count > 50) tagData[tag].RemoveAt(0);

                        dataUpdated = true; // Отмечаем, что данные изменились
                    }
                    catch (Exception ex)
                    {
                        LogError($"❌ Ошибка чтения {tag}: {ex.Message}");
                    }
                }

                // 🔹 Обновляем график только если были изменения
                if (dataUpdated)
                {
                    Series = selectedTags
                        .Where(tag => tagData.ContainsKey(tag)) // Только если тег в `tagData`
                        .Select(tag => new LineSeries<double>
                        {
                            Name = tag,
                            Values = tagData[tag]
                        }).ToArray();

                    DataContext = null;
                    DataContext = this;
                }
            });
        }

        private object ReadOptimizedTag(string tagName)
        {
            try
            {
                var software = tiaHelper.GetPlcSoftware();
                if (software == null)
                {
                    throw new Exception("PLC Software не найден.");
                }

                var tagTable = software.TagTableGroup.TagTables.FirstOrDefault(t => t.Tags.Any(tag => tag.Name == tagName));
                if (tagTable == null)
                {
                    throw new Exception($"Тег {tagName} не найден в TIA Portal.");
                }

                var tag = tagTable.Tags.FirstOrDefault(t => t.Name == tagName);
                if (tag == null)
                {
                    throw new Exception($"Тег {tagName} отсутствует в таблице тегов.");
                }

                return tag.GetAttribute("Value")?.ToString() ?? "0"; // ✅ Читаем значение
            }
            catch (Exception ex)
            {
                LogError($"Ошибка Symbolic Access для {tagName}: {ex.Message}");
                return "0";
            }
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string ip = IpTextBox.Text.Trim();
                if (!short.TryParse(RackTextBox.Text.Trim(), out short rack) ||
                    !short.TryParse(SlotTextBox.Text.Trim(), out short slot))
                {
                    MessageBox.Show("Некорректные значения Rack или Slot.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                plc = new Plc(CpuType.S71200, ip, rack, slot);

                try
                {
                    plc.Open();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка подключения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (plc.IsConnected)
                {
                    StatusTextBlock.Text = "Статус: Подключено";
                    LogMessage($"✅ Успешное подключение к ПЛК {ip} (Rack: {rack}, Slot: {slot})");

                    // 🔹 Включаем кнопку мониторинга, если есть выбранные теги
                    StartMonitoringButton.IsEnabled = selectedTags.Count > 0;
                }
                else
                {
                    MessageBox.Show("❌ Не удалось подключиться к ПЛК.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                LogError($"Ошибка подключения: {ex.Message}");
                StatusTextBlock.Text = "Статус: Ошибка подключения";
            }
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (plc != null && plc.IsConnected)
                {
                    plc.Close();
                    StatusTextBlock.Text = "Статус: Отключено";
                    LogMessage("🔌 Отключение от ПЛК.");
                }
            }
            catch (Exception ex)
            {
                LogError($"Ошибка отключения: {ex.Message}");
            }
        }

        private void TagsTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem selectedItem)
            {
                string tagName = selectedItem.Header.ToString();

                if (!selectedTags.Contains(tagName))
                {
                    selectedTags.Add(tagName);
                    tagData[tagName] = new ObservableCollection<double>();
                }
            }
        }

        private void UpdateTagTreeView()
        {
            Dispatcher.Invoke(() =>
            {
                TagsTreeView.Items.Clear();

                if (tiaHelper.PlcData.Tags.Count == 0 && tiaHelper.PlcData.DataBlocks.Count == 0)
                {
                    LogMessage("⚠ Нет загруженных тегов.");
                    return;
                }

                // 🔹 Группируем теги PLC по таблицам
                TreeViewItem globalTagsGroup = new TreeViewItem { Header = $"Глобальные теги ({tiaHelper.PlcData.Tags.Count})" };

                var tagGroups = tiaHelper.PlcData.Tags
                    .GroupBy(t => t.TableName)
                    .OrderBy(g => g.Key); // 🔹 Сортируем по имени группы

                foreach (var group in tagGroups)
                {
                    TreeViewItem groupNode = new TreeViewItem { Header = $"{group.Key} ({group.Count()} тегов)" };

                    foreach (var tag in group)
                    {
                        //var checkBox = new CheckBox { Content = $"{tag.Name} ({tag.DataType})" };
                        var checkBox = new CheckBox { Content = tag.Name }; // Используем полное имя из XML
                        checkBox.Checked += TagSelectionChanged;
                        checkBox.Unchecked += TagSelectionChanged;
                        groupNode.Items.Add(checkBox);
                    }

                    globalTagsGroup.Items.Add(groupNode);
                }

                TagsTreeView.Items.Add(globalTagsGroup);

                // 🔹 Группируем DB
                TreeViewItem dbTagsGroup = new TreeViewItem { Header = $"Data Blocks ({tiaHelper.PlcData.DataBlocks.Count})" };

                foreach (var db in tiaHelper.PlcData.DataBlocks)
                {
                    TreeViewItem dbNode = new TreeViewItem { Header = $"{db.Name} ({db.Variables.Count} переменных)" };

                    foreach (var variable in db.Variables)
                    {
                        var checkBox = new CheckBox { Content = $"{variable.Name}: {variable.DataType}" };
                        checkBox.Checked += TagSelectionChanged;
                        checkBox.Unchecked += TagSelectionChanged;
                        dbNode.Items.Add(checkBox);
                    }

                    dbTagsGroup.Items.Add(dbNode);
                }

                TagsTreeView.Items.Add(dbTagsGroup);
            });
        }

        private void TagSelectionChanged(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                string tagName = checkBox.Content.ToString();

                if (checkBox.IsChecked == true)
                {
                    if (!selectedTags.Contains(tagName))
                    {
                        selectedTags.Add(tagName);
                        tagData[tagName] = new ObservableCollection<double>();
                    }
                }
                else
                {
                    if (selectedTags.Contains(tagName))
                    {
                        selectedTags.Remove(tagName);
                        tagData.Remove(tagName);
                    }
                }
            }

            // ✅ Делаем кнопку активной, если есть выбранные теги
            StartMonitoringButton.IsEnabled = selectedTags.Count > 0;
        }

        private async void ExportFromTIAButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(projectPath))
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Title = "Выберите проект TIA Portal",
                    Filter = "TIA Portal Projects (*.ap19)|*.ap19",
                    InitialDirectory = @"D:\Projects\"
                };

                if (openFileDialog.ShowDialog() != true)
                {
                    MessageBox.Show("Файл проекта не выбран!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                projectPath = openFileDialog.FileName;
                ProjectNameTextBlock.Text = Path.GetFileName(projectPath);
            }

            // 🔹 Отключаем кнопки во время экспорта
            ProgressBar.Visibility = Visibility.Visible;
            ExportFromTIAButton.IsEnabled = false;
            LoadTagsButton.IsEnabled = false;
            StartMonitoringButton.IsEnabled = false;

            try
            {
                await tiaHelper.ExportProjectToXmlAsync(projectPath);
                MessageBox.Show("Экспорт завершен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                // 🔹 После экспорта загружаем теги
                await Task.Run(() => tiaHelper.GetTagsAndDB());
                UpdateTagTreeView();
            }
            finally
            {
                // 🔹 Включаем кнопки после завершения
                ProgressBar.Visibility = Visibility.Hidden;
                ExportFromTIAButton.IsEnabled = true;
                LoadTagsButton.IsEnabled = true;
                StartMonitoringButton.IsEnabled = selectedTags.Count > 0;
            }
        }

        private async void LoadTagsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!tiaHelper.AreTagsAvailable())
            {
                MessageBox.Show("Теги отсутствуют. Сначала экспортируйте их из TIA.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 🔹 Отключаем кнопки во время загрузки
            ProgressBar.Visibility = Visibility.Visible;
            LoadTagsButton.IsEnabled = false;
            ExportFromTIAButton.IsEnabled = false;
            StartMonitoringButton.IsEnabled = false;
            TagsTreeView.Items.Clear();

            try
            {
                await Task.Run(() => tiaHelper.GetTagsAndDB()); // Загружаем теги и DB
                UpdateTagTreeView();

                StatusTextBlock.Text = $"Статус: Загружено {tiaHelper.PlcData.Tags.Count} тегов, {tiaHelper.PlcData.DataBlocks.Count} DB";
                LogMessage($"Загружено {tiaHelper.PlcData.Tags.Count} тегов, {tiaHelper.PlcData.DataBlocks.Count} DB.");
            }
            finally
            {
                // 🔹 Включаем кнопки после загрузки
                ProgressBar.Visibility = Visibility.Hidden;
                LoadTagsButton.IsEnabled = true;
                ExportFromTIAButton.IsEnabled = true;
                StartMonitoringButton.IsEnabled = selectedTags.Count > 0;
            }
        }

        private void LogMessage(string message) => File.AppendAllText("log.txt", $"{DateTime.Now}: {message}\n");

        private void LogError(string errorMessage) => File.AppendAllText("log.txt", $"{DateTime.Now} [ERROR]: {errorMessage}\n");
    }
}
