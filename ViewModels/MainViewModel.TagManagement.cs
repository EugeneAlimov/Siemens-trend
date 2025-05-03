using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SiemensTrend.Communication.TIA;
using SiemensTrend.Core.Models;

namespace SiemensTrend.ViewModels
{
    /// <summary>
    /// Часть MainViewModel для управления тегами
    /// </summary>
    public partial class MainViewModel
    {
        /// <summary>
        /// Загрузка тегов
        /// </summary>
        private async Task LoadTagsAsync()
        {
            if (!IsConnected)
                return;

            try
            {
                IsLoading = true;
                StatusMessage = "Загрузка тегов...";
                ProgressValue = 0;

                // Для демонстрации просто добавляем тестовые теги
                AvailableTags.Clear();

                // Эмулируем задержку загрузки тегов
                await Task.Delay(1000);

                // Добавляем тестовые теги
                AvailableTags.Add(new TagDefinition { Name = "Motor1_Speed", Address = "DB1.DBD0", DataType = TagDataType.Real, GroupName = "Motors" });
                AvailableTags.Add(new TagDefinition { Name = "Motor1_Running", Address = "DB1.DBX4.0", DataType = TagDataType.Bool, GroupName = "Motors" });
                AvailableTags.Add(new TagDefinition { Name = "Motor2_Speed", Address = "DB1.DBD8", DataType = TagDataType.Real, GroupName = "Motors" });
                AvailableTags.Add(new TagDefinition { Name = "Motor2_Running", Address = "DB1.DBX12.0", DataType = TagDataType.Bool, GroupName = "Motors" });
                AvailableTags.Add(new TagDefinition { Name = "Temperature", Address = "DB2.DBD0", DataType = TagDataType.Real, GroupName = "Sensors" });
                AvailableTags.Add(new TagDefinition { Name = "Pressure", Address = "DB2.DBD4", DataType = TagDataType.Real, GroupName = "Sensors" });
                AvailableTags.Add(new TagDefinition { Name = "Level", Address = "DB2.DBD8", DataType = TagDataType.Real, GroupName = "Sensors" });
                AvailableTags.Add(new TagDefinition { Name = "Alarm", Address = "DB3.DBX0.0", DataType = TagDataType.Bool, GroupName = "System" });

                StatusMessage = $"Загружено {AvailableTags.Count} тегов";
                ProgressValue = 100;
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при загрузке тегов: {ex.Message}");
                StatusMessage = "Ошибка при загрузке тегов";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Добавление тега в мониторинг
        /// </summary>
        public void AddTagToMonitoring(TagDefinition tag)
        {
            if (tag == null || MonitoredTags.Contains(tag))
                return;

            MonitoredTags.Add(tag);
            _logger.Info($"Тег {tag.Name} добавлен в мониторинг");
        }

        /// <summary>
        /// Удаление тега из мониторинга
        /// </summary>
        public void RemoveTagFromMonitoring(TagDefinition tag)
        {
            if (tag == null || !MonitoredTags.Contains(tag))
                return;

            MonitoredTags.Remove(tag);
            _logger.Info($"Тег {tag.Name} удален из мониторинга");
        }
    }
}