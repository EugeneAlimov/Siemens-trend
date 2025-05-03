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
        /// Загрузка тегов из XML файла по умолчанию
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
        /// Сохранение тегов в XML файл по умолчанию
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
        /// Загрузка тегов из XML файла по указанному пути
        /// </summary>
        /// <param name="filePath">Путь к XML файлу</param>
        /// <returns>Список тегов</returns>
        public List<TagDefinition> LoadTagsFromXml(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.Info($"LoadTagsFromXml: Файл не существует: {filePath}");
                    return new List<TagDefinition>();
                }

                _logger.Info($"LoadTagsFromXml: Загрузка тегов из файла {filePath}");

                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    var serializer = new XmlSerializer(typeof(List<TagDefinition>));
                    var tags = (List<TagDefinition>)serializer.Deserialize(fileStream);

                    _logger.Info($"LoadTagsFromXml: Успешно загружено {tags.Count} тегов");
                    return tags;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"LoadTagsFromXml: Ошибка загрузки тегов: {ex.Message}");
                return new List<TagDefinition>();
            }
        }

        /// <summary>
        /// Сохранение тегов в XML файл по указанному пути
        /// </summary>
        /// <param name="tags">Список тегов</param>
        /// <param name="filePath">Путь к файлу</param>
        /// <returns>True если сохранение успешно</returns>
        public bool SaveTagsToXml(List<TagDefinition> tags, string filePath)
        {
            try
            {
                _logger.Info($"SaveTagsToXml: Сохранение {tags.Count} тегов в файл {filePath}");

                // Создаем директорию, если она не существует
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    var serializer = new XmlSerializer(typeof(List<TagDefinition>));
                    serializer.Serialize(fileStream, tags);
                }

                _logger.Info("SaveTagsToXml: Теги сохранены успешно");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"SaveTagsToXml: Ошибка сохранения тегов: {ex.Message}");
                return false;
            }
        }
    }
}