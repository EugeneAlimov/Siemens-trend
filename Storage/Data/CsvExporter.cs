using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using SiemensTrend.Core.Logging;
using SiemensTrend.Core.Models;

namespace SiemensTrend.Storage.Data
{
    /// <summary>
    /// Класс для экспорта данных в CSV
    /// </summary>
    public class CsvExporter
    {
        private readonly Logger _logger;

        /// <summary>
        /// Конструктор
        /// </summary>
        public CsvExporter(Logger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Экспорт данных временных рядов в CSV
        /// </summary>
        public async Task<bool> ExportTimeSeriesAsync(IEnumerable<TagDataPoint> dataPoints, string filePath,
            string delimiter = ",", bool includeHeader = true)
        {
            try
            {
                _logger.Info($"Экспорт данных в CSV: {filePath}");

                // Создаем директорию, если она не существует
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Открываем файл для записи
                using (var streamWriter = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    // Записываем заголовок, если нужно
                    if (includeHeader)
                    {
                        await streamWriter.WriteLineAsync(
                            $"Timestamp{delimiter}TagName{delimiter}TagAddress{delimiter}Value");
                    }

                    // Записываем данные
                    foreach (var dataPoint in dataPoints)
                    {
                        // Формируем строку CSV
                        string timestamp = dataPoint.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
                        string tagName = dataPoint.Tag?.Name ?? "Unknown";
                        string tagAddress = dataPoint.Tag?.Address ?? "Unknown";
                        string value = dataPoint.Value?.ToString() ?? "null";

                        // Экранируем значения, если они содержат разделитель
                        if (tagName.Contains(delimiter)) tagName = $"\"{tagName}\"";
                        if (tagAddress.Contains(delimiter)) tagAddress = $"\"{tagAddress}\"";
                        if (value.Contains(delimiter)) value = $"\"{value}\"";

                        // Записываем строку в файл
                        await streamWriter.WriteLineAsync(
                            $"{timestamp}{delimiter}{tagName}{delimiter}{tagAddress}{delimiter}{value}");
                    }
                }

                _logger.Info("Экспорт данных в CSV успешно завершен");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при экспорте данных в CSV: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Экспорт списка тегов в CSV
        /// </summary>
        public async Task<bool> ExportTagsAsync(IEnumerable<TagDefinition> tags, string filePath,
            string delimiter = ",", bool includeHeader = true)
        {
            try
            {
                _logger.Info($"Экспорт тегов в CSV: {filePath}");

                // Создаем директорию, если она не существует
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Открываем файл для записи
                using (var streamWriter = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    // Записываем заголовок, если нужно
                    if (includeHeader)
                    {
                        await streamWriter.WriteLineAsync(
                            $"Group{delimiter}Name{delimiter}Address{delimiter}DataType{delimiter}Comment");
                    }

                    // Записываем данные
                    foreach (var tag in tags)
                    {
                        // Формируем строку CSV
                        string group = tag.GroupName ?? "";
                        string name = tag.Name ?? "Unknown";
                        string address = tag.Address ?? "Unknown";
                        string dataType = tag.DataType.ToString();
                        string comment = tag.Comment ?? "";

                        // Экранируем значения, если они содержат разделитель
                        if (group.Contains(delimiter)) group = $"\"{group}\"";
                        if (name.Contains(delimiter)) name = $"\"{name}\"";
                        if (address.Contains(delimiter)) address = $"\"{address}\"";
                        if (comment.Contains(delimiter)) comment = $"\"{comment}\"";

                        // Записываем строку в файл
                        await streamWriter.WriteLineAsync(
                            $"{group}{delimiter}{name}{delimiter}{address}{delimiter}{dataType}{delimiter}{comment}");
                    }
                }

                _logger.Info("Экспорт тегов в CSV успешно завершен");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при экспорте тегов в CSV: {ex.Message}");
                return false;
            }
        }
    }
}