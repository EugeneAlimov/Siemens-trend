using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Siemens.Engineering.SW.Tags;
using SiemensTrend.Core.Models;

namespace SiemensTrend.Helpers
{
    /// <summary>
    /// Методы TiaPortalXmlManager для работы с тегами ПЛК
    /// </summary>
    public partial class TiaPortalXmlManager
    {
        /// <summary>
        /// Экспорт только таблиц тегов ПЛК
        /// </summary>
        public void ExportTagTablesToXml(PlcTagTableGroup tagTableGroup)
        {
            if (tagTableGroup == null)
            {
                _logger.Error("ExportTagTablesToXml: TagTableGroup не может быть null");
                return;
            }

            if (string.IsNullOrEmpty(_currentProjectName))
            {
                _logger.Error("ExportTagTablesToXml: Не установлен текущий проект");
                return;
            }

            try
            {
                _logger.Info("ExportTagTablesToXml: Начало экспорта таблиц тегов");

                // Создаем директорию для экспорта, если она не существует
                Directory.CreateDirectory(_plcTagsPath);

                // Собираем все таблицы тегов для постепенной обработки
                var allTagTables = new List<PlcTagTable>();
                CollectTagTables(tagTableGroup, allTagTables);

                _logger.Info($"ExportTagTablesToXml: Найдено {allTagTables.Count} таблиц тегов");

                // Обрабатываем каждую таблицу отдельно
                int processedCount = 0;
                int successCount = 0;

                foreach (var tagTable in allTagTables)
                {
                    // Проверяем отмену операции
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        _logger.Info("ExportTagTablesToXml: Операция отменена пользователем");
                        break;
                    }

                    try
                    {
                        if (tagTable == null) continue;

                        processedCount++;
                        string tableName = tagTable.Name;

                        _logger.Info($"ExportTagTablesToXml: Обработка таблицы {tableName} ({processedCount}/{allTagTables.Count})");

                        // Создаем поддиректорию, если нужно
                        string exportFolder = _plcTagsPath;
                        Directory.CreateDirectory(exportFolder);

                        // Экспортируем таблицу тегов
                        string exportPath = Path.Combine(exportFolder, $"{tableName}.xml");
                        ExportSingleTagTableToXml(tagTable, exportPath);

                        successCount++;
                        _logger.Info($"ExportTagTablesToXml: Таблица {tableName} успешно экспортирована");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"ExportTagTablesToXml: Ошибка при обработке таблицы тегов: {ex.Message}");
                        // Продолжаем с другими таблицами
                    }
                }

                _logger.Info($"ExportTagTablesToXml: Экспорт таблиц тегов завершен. Успешно: {successCount}/{allTagTables.Count}");
            }
            catch (Exception ex)
            {
                _logger.Error($"ExportTagTablesToXml: Общая ошибка: {ex.Message}");
            }
        }

        /// <summary>
        /// Рекурсивный сбор таблиц тегов из группы
        /// </summary>
        private void CollectTagTables(PlcTagTableGroup group, List<PlcTagTable> tagTables)
        {
            if (group == null) return;

            try
            {
                // Собираем таблицы на текущем уровне
                if (group.TagTables != null)
                {
                    foreach (var table in group.TagTables)
                    {
                        if (table is PlcTagTable tagTable)
                        {
                            tagTables.Add(tagTable);
                        }
                    }
                }

                // Рекурсивно обрабатываем подгруппы
                if (group.Groups != null)
                {
                    foreach (var subgroup in group.Groups)
                    {
                        if (subgroup is PlcTagTableUserGroup userGroup)
                        {
                            CollectTagTables(userGroup, tagTables);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"CollectTagTables: Ошибка при сборе таблиц тегов: {ex.Message}");
            }
        }

        /// <summary>
        /// Экспорт отдельной таблицы тегов в XML
        /// </summary>
        private void ExportSingleTagTableToXml(PlcTagTable tagTable, string exportPath)
        {
            if (tagTable == null)
            {
                _logger.Error("ExportSingleTagTableToXml: tagTable не может быть null");
                return;
            }

            try
            {
                // Проверяем существование директории для экспорта
                string directory = Path.GetDirectoryName(exportPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Ограничиваем количество тегов для обработки
                int maxTags = 5000;
                var tagElements = new List<XElement>();
                int tagCount = 0;

                if (tagTable.Tags == null)
                {
                    _logger.Warn($"ExportSingleTagTableToXml: Таблица {tagTable.Name} не содержит тегов (Tags == null)");
                    // Создаем пустой XML для таблицы
                    XDocument emptyDoc = new XDocument(
                        new XElement("TagTable",
                            new XAttribute("Name", tagTable.Name),
                            new XElement("Tags")
                        )
                    );
                    emptyDoc.Save(exportPath);
                    return;
                }

                foreach (var tag in tagTable.Tags)
                {
                    // Проверяем отмену операции
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        _logger.Info("ExportSingleTagTableToXml: Операция отменена пользователем");
                        return;
                    }

                    if (tagCount >= maxTags)
                    {
                        _logger.Warn($"ExportSingleTagTableToXml: Достигнут лимит тегов ({maxTags}) для таблицы {tagTable.Name}");
                        break;
                    }

                    try
                    {
                        if (tag == null) continue;

                        string name = tag.Name;
                        string dataType = "Unknown";
                        string address = "";
                        string comment = "";

                        try { dataType = tag.GetAttribute("DataTypeName")?.ToString() ?? "Unknown"; }
                        catch (Exception ex) { _logger.Debug($"Ошибка при получении типа данных: {ex.Message}"); }

                        try { address = tag.LogicalAddress; }
                        catch (Exception ex) { _logger.Debug($"Ошибка при получении адреса: {ex.Message}"); }

                        try { comment = tag.GetAttribute("Comment")?.ToString() ?? ""; }
                        catch (Exception ex) { _logger.Debug($"Ошибка при получении комментария: {ex.Message}"); }

                        tagElements.Add(new XElement("Tag",
                            new XAttribute("Name", name),
                            new XAttribute("DataType", dataType),
                            new XAttribute("Address", address),
                            new XAttribute("Comment", comment)
                        ));

                        tagCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug($"ExportSingleTagTableToXml: Ошибка при обработке тега: {ex.Message}");
                        // Продолжаем с другими тегами
                    }
                }

                // Создаем XML документ
                XDocument doc = new XDocument(
                    new XElement("TagTable",
                        new XAttribute("Name", tagTable.Name),
                        new XElement("Tags", tagElements)
                    )
                );

                // Сохраняем документ
                doc.Save(exportPath);
                _logger.Info($"ExportSingleTagTableToXml: Таблица {tagTable.Name} экспортирована: {exportPath}");
            }
            catch (Exception ex)
            {
                string tableName = "unknown";
                try { tableName = tagTable.Name; } catch { }

                _logger.Error($"ExportSingleTagTableToXml: Ошибка при экспорте таблицы {tableName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Загрузка тегов ПЛК из XML-файлов
        /// </summary>
        public List<TagDefinition> LoadPlcTagsFromXml()
        {
            if (string.IsNullOrEmpty(_currentProjectName))
            {
                _logger.Error("LoadPlcTagsFromXml: Не установлен текущий проект");
                return new List<TagDefinition>();
            }

            var tagList = new List<TagDefinition>();

            try
            {
                if (!Directory.Exists(_plcTagsPath))
                {
                    _logger.Error($"LoadPlcTagsFromXml: Директория {_plcTagsPath} не найдена");
                    return tagList;
                }

                string[] xmlFiles = Directory.GetFiles(_plcTagsPath, "*.xml", SearchOption.AllDirectories);
                _logger.Info($"LoadPlcTagsFromXml: Найдено {xmlFiles.Length} XML-файлов с тегами");

                foreach (string file in xmlFiles)
                {
                    if (!File.Exists(file))
                    {
                        _logger.Warn($"LoadPlcTagsFromXml: Файл {file} не существует");
                        continue;
                    }

                    string tableName = Path.GetFileNameWithoutExtension(file);
                    _logger.Info($"LoadPlcTagsFromXml: Обработка файла {tableName}");

                    try
                    {
                        XDocument doc = XDocument.Load(file);
                        var tagElements = doc.Descendants("Tag").ToList();

                        if (tagElements.Count == 0)
                        {
                            _logger.Warn($"LoadPlcTagsFromXml: Файл {file} не содержит тегов");
                            continue;
                        }

                        foreach (var tagElement in tagElements)
                        {
                            try
                            {
                                string name = tagElement.Attribute("Name")?.Value ?? "Unknown";
                                string dataTypeStr = tagElement.Attribute("DataType")?.Value ?? "Unknown";
                                string address = tagElement.Attribute("Address")?.Value ?? "";
                                string comment = tagElement.Attribute("Comment")?.Value ?? "";

                                TagDataType dataType = ConvertStringToTagDataType(dataTypeStr);

                                tagList.Add(new TagDefinition
                                {
                                    Name = name,
                                    DataType = dataType,
                                    Address = address,
                                    Comment = comment,
                                    GroupName = tableName
                                });

                                _logger.Debug($"LoadPlcTagsFromXml: Загружен тег {name}, тип: {dataType}");
                            }
                            catch (Exception tagEx)
                            {
                                _logger.Debug($"LoadPlcTagsFromXml: Ошибка при обработке тега: {tagEx.Message}");
                                // Продолжаем с другими тегами
                            }
                        }
                    }
                    catch (Exception fileEx)
                    {
                        _logger.Error($"LoadPlcTagsFromXml: Ошибка при обработке файла {file}: {fileEx.Message}");
                        // Продолжаем с другими файлами
                    }
                }

                _logger.Info($"LoadPlcTagsFromXml: Всего загружено {tagList.Count} тегов");
            }
            catch (Exception ex)
            {
                _logger.Error($"LoadPlcTagsFromXml: Ошибка: {ex.Message}");
            }

            return tagList;
        }
    }
}