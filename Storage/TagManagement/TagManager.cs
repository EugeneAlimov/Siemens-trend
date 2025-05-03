using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using SiemensTrend.Core.Logging;
using SiemensTrend.Core.Models;

namespace SiemensTrend.Storage.TagManagement
{
    /// <summary>
    /// Класс для управления тегами (ручное добавление, редактирование, сохранение)
    /// </summary>
    public class TagManager
    {
        private readonly Logger _logger;
        private readonly string _tagsFilePath;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="logger">Логгер</param>
        public TagManager(Logger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Определяем путь к файлу хранения тегов
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SiemensTrend");
                
            // Создаем директорию, если она не существует
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }
            
            _tagsFilePath = Path.Combine(appDataPath, "Tags.xml");
            _logger.Info($"TagManager: Инициализирован с путем к файлу тегов: {_tagsFilePath}");
        }

        /// <summary>
        /// Загрузка тегов из XML файла
        /// </summary>
        /// <returns>Список тегов</returns>
        public List<TagDefinition> LoadTags()
        {
            try
            {
                if (!File.Exists(_tagsFilePath))
                {
                    _logger.Info("LoadTags: Файл тегов не существует, возвращаем пустой список");
                    return new List<TagDefinition>();
                }

                _logger.Info($"LoadTags: Загрузка тегов из файла {_tagsFilePath}");
                
                using (var fileStream = new FileStream(_tagsFilePath, FileMode.Open, FileAccess.Read))
                {
                    var serializer = new XmlSerializer(typeof(List<TagDefinition>));
                    var tags = (List<TagDefinition>)serializer.Deserialize(fileStream);
                    
                    _logger.Info($"LoadTags: Успешно загружено {tags.Count} тегов");
                    return tags;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"LoadTags: Ошибка загрузки тегов: {ex.Message}");
                return new List<TagDefinition>();
            }
        }

        /// <summary>
        /// Сохранение тегов в XML файл
        /// </summary>
        /// <param name="tags">Список тегов для сохранения</param>
        /// <returns>True если сохранение успешно</returns>
        public bool SaveTags(List<TagDefinition> tags)
        {
            try
            {
                _logger.Info($"SaveTags: Сохранение {tags.Count} тегов в файл {_tagsFilePath}");
                
                using (var fileStream = new FileStream(_tagsFilePath, FileMode.Create, FileAccess.Write))
                {
                    var serializer = new XmlSerializer(typeof(List<TagDefinition>));
                    serializer.Serialize(fileStream, tags);
                }
                
                _logger.Info("SaveTags: Теги сохранены успешно");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"SaveTags: Ошибка сохранения тегов: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Импорт тегов из CSV файла
        /// </summary>
        /// <param name="filePath">Путь к CSV файлу</param>
        /// <returns>Список импортированных тегов</returns>
        public List<TagDefinition> ImportTagsFromCsv(string filePath)
        {
            try
            {
                _logger.Info($"ImportTagsFromCsv: Импорт тегов из файла {filePath}");
                
                if (!File.Exists(filePath))
                {
                    _logger.Error($"ImportTagsFromCsv: Файл не существует: {filePath}");
                    return new List<TagDefinition>();
                }
                
                var tags = new List<TagDefinition>();
                var lines = File.ReadAllLines(filePath);
                
                // Пропускаем заголовок, если он есть
                bool hasHeader = lines.Length > 0 && 
                    (lines[0].Contains("Name") || lines[0].Contains("Address") || 
                     lines[0].Contains("DataType") || lines[0].Contains("Group"));
                
                int startLine = hasHeader ? 1 : 0;
                
                for (int i = startLine; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line))
                        continue;
                    
                    string[] parts = line.Split(',');
                    if (parts.Length < 3)
                        continue; // Недостаточно данных
                    
                    string name = parts[0].Trim();
                    string address = parts[1].Trim();
                    string dataTypeStr = parts.Length > 2 ? parts[2].Trim() : "Unknown";
                    string groupName = parts.Length > 3 ? parts[3].Trim() : string.Empty;
                    string comment = parts.Length > 4 ? parts[4].Trim() : string.Empty;
                    
                    // Определяем тип данных
                    TagDataType dataType;
                    switch (dataTypeStr.ToLower())
                    {
                        case "bool":
                            dataType = TagDataType.Bool;
                            break;
                        case "int":
                            dataType = TagDataType.Int;
                            break;
                        case "dint":
                            dataType = TagDataType.DInt;
                            break;
                        case "real":
                            dataType = TagDataType.Real;
                            break;
                        default:
                            dataType = TagDataType.Other;
                            break;
                    }
                    
                    // Создаем новый тег
                    var tag = new TagDefinition
                    {
                        Id = Guid.NewGuid(),
                        Name = name,
                        Address = address,
                        DataType = dataType,
                        GroupName = groupName,
                        Comment = comment,
                        IsDbTag = address.Contains("DB") || name.Contains("DB")
                    };
                    
                    tags.Add(tag);
                }
                
                _logger.Info($"ImportTagsFromCsv: Успешно импортировано {tags.Count} тегов");
                return tags;
            }
            catch (Exception ex)
            {
                _logger.Error($"ImportTagsFromCsv: Ошибка импорта тегов: {ex.Message}");
                return new List<TagDefinition>();
            }
        }

        /// <summary>
        /// Экспорт тегов в CSV файл
        /// </summary>
        /// <param name="tags">Список тегов</param>
        /// <param name="filePath">Путь к файлу</param>
        /// <returns>True если экспорт успешен</returns>
        public bool ExportTagsToCsv(List<TagDefinition> tags, string filePath)
        {
            try
            {
                _logger.Info($"ExportTagsToCsv: Экспорт {tags.Count} тегов в файл {filePath}");
                
                // Создаем заголовок
                var lines = new List<string>
                {
                    "Name,Address,DataType,Group,Comment"
                };
                
                // Добавляем данные
                foreach (var tag in tags)
                {
                    // Экранируем поля с запятыми
                    string name = EscapeCsvField(tag.Name);
                    string address = EscapeCsvField(tag.Address);
                    string dataType = tag.DataType.ToString();
                    string group = EscapeCsvField(tag.GroupName ?? string.Empty);
                    string comment = EscapeCsvField(tag.Comment ?? string.Empty);
                    
                    string line = $"{name},{address},{dataType},{group},{comment}";
                    lines.Add(line);
                }
                
                // Сохраняем файл
                File.WriteAllLines(filePath, lines);
                
                _logger.Info("ExportTagsToCsv: Теги экспортированы успешно");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"ExportTagsToCsv: Ошибка экспорта тегов: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Экранирование поля CSV
        /// </summary>
        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return string.Empty;
                
            // Если поле содержит запятую или кавычку, оборачиваем его в кавычки
            if (field.Contains(",") || field.Contains("\""))
            {
                // Экранируем внутренние кавычки
                field = field.Replace("\"", "\"\"");
                return $"\"{field}\"";
            }
            
            return field;
        }
    }
}