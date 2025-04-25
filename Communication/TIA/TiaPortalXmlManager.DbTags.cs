using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Siemens.Engineering;
using Siemens.Engineering.SW.Blocks;
using SiemensTrend.Core.Models;

namespace SiemensTrend.Helpers
{
    /// <summary>
    /// Методы TiaPortalXmlManager для работы с тегами блоков данных (DB)
    /// </summary>
    public partial class TiaPortalXmlManager
    {
        /// <summary>
        /// Экспорт только блоков данных
        /// </summary>
        public void ExportDataBlocksToXml(PlcBlockGroup blockGroup)
        {
            if (blockGroup == null)
            {
                _logger.Error("ExportDataBlocksToXml: blockGroup не может быть null");
                return;
            }

            if (string.IsNullOrEmpty(_currentProjectName))
            {
                _logger.Error("ExportDataBlocksToXml: Не установлен текущий проект");
                return;
            }

            try
            {
                _logger.Info("ExportDataBlocksToXml: Начало экспорта блоков данных");

                // Проверяем существование директории для экспорта
                Directory.CreateDirectory(_dbExportsPath);

                // Собираем блоки данных
                var allDataBlocks = new List<DataBlock>();
                CollectDataBlocks(blockGroup, allDataBlocks);

                _logger.Info($"ExportDataBlocksToXml: Найдено {allDataBlocks.Count} блоков данных");

                // Обрабатываем каждый блок данных последовательно
                int processedCount = 0;
                int successCount = 0;

                foreach (var db in allDataBlocks)
                {
                    // Проверяем отмену операции
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        _logger.Info("ExportDataBlocksToXml: Операция отменена пользователем");
                        break;
                    }

                    try
                    {
                        if (db == null) continue;

                        processedCount++;
                        string dbName = db.Name;

                        _logger.Info($"ExportDataBlocksToXml: Обработка блока данных {dbName} ({processedCount}/{allDataBlocks.Count})");

                        ExportSingleDataBlockToXml(db);

                        successCount++;
                        _logger.Info($"ExportDataBlocksToXml: Блок данных {dbName} успешно экспортирован");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"ExportDataBlocksToXml: Ошибка при обработке блока данных: {ex.Message}");
                        // Продолжаем с другими блоками
                    }
                }

                _logger.Info($"ExportDataBlocksToXml: Экспорт блоков данных завершен. Успешно: {successCount}/{allDataBlocks.Count}");
            }
            catch (Exception ex)
            {
                _logger.Error($"ExportDataBlocksToXml: Ошибка: {ex.Message}");
            }
        }

        /// <summary>
        /// Рекурсивный сбор блоков данных из группы
        /// </summary>
        private void CollectDataBlocks(PlcBlockGroup group, List<DataBlock> dataBlocks)
        {
            if (group == null) return;

            try
            {
                // Собираем блоки на текущем уровне
                if (group.Blocks != null)
                {
                    foreach (var block in group.Blocks)
                    {
                        // Проверяем отмену операции
                        if (_cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            _logger.Info("CollectDataBlocks: Операция отменена пользователем");
                            return;
                        }

                        if (block is DataBlock db)
                        {
                            dataBlocks.Add(db);
                        }
                    }
                }

                // Рекурсивно обрабатываем подгруппы
                if (group.Groups != null)
                {
                    foreach (var subgroup in group.Groups)
                    {
                        if (subgroup as PlcBlockGroup != null)
                        {
                            CollectDataBlocks(subgroup as PlcBlockGroup, dataBlocks);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"CollectDataBlocks: Ошибка при сборе блоков данных: {ex.Message}");
            }
        }

        /// <summary>
        /// Экспорт отдельного блока данных в XML
        /// </summary>
        private void ExportSingleDataBlockToXml(DataBlock db)
        {
            if (db == null) return;

            try
            {
                string dbName = db.Name;
                string exportPath = Path.Combine(_dbExportsPath, $"{dbName}.xml");

                // Проверяем существование директории
                string directory = Path.GetDirectoryName(exportPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                bool isOptimized = false;

                try
                {
                    isOptimized = db.MemoryLayout == MemoryLayout.Optimized;
                }
                catch (Exception ex)
                {
                    _logger.Warn($"ExportSingleDataBlockToXml: Не удалось определить MemoryLayout для DB {dbName}: {ex.Message}");
                }

                // Безопасное извлечение переменных из блока данных
                var variables = new List<XElement>();

                if (db.Interface == null)
                {
                    _logger.Warn($"ExportSingleDataBlockToXml: Блок данных {dbName} не имеет интерфейса (Interface == null)");
                    // Создаем пустой XML для блока данных
                    XDocument emptyDoc = new XDocument(
                        new XElement("DataBlock",
                            new XAttribute("Name", dbName),
                            new XAttribute("Optimized", isOptimized),
                            new XElement("Variables")
                        )
                    );
                    emptyDoc.Save(exportPath);
                    return;
                }

                if (db.Interface.Members == null)
                {
                    _logger.Warn($"ExportSingleDataBlockToXml: Блок данных {dbName} не имеет членов (Members == null)");
                    // Создаем пустой XML для блока данных
                    XDocument emptyDoc = new XDocument(
                        new XElement("DataBlock",
                            new XAttribute("Name", dbName),
                            new XAttribute("Optimized", isOptimized),
                            new XElement("Variables")
                        )
                    );
                    emptyDoc.Save(exportPath);
                    return;
                }

                try
                {
                    foreach (var member in db.Interface.Members)
                    {
                        // Проверяем отмену операции
                        if (_cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            _logger.Info("ExportSingleDataBlockToXml: Операция отменена пользователем");
                            return;
                        }

                        if (member == null) continue;

                        string varName = member.Name;
                        string dataType = "Unknown";
                        string comment = "";

                        try
                        {
                            dataType = member.GetAttribute("DataTypeName")?.ToString() ?? "Unknown";
                        }
                        catch (Exception ex)
                        {
                            _logger.Debug($"Ошибка при получении типа данных для {varName}: {ex.Message}");
                        }

                        try
                        {
                            comment = member.GetAttribute("Comment")?.ToString() ?? "";
                        }
                        catch (Exception ex)
                        {
                            _logger.Debug($"Ошибка при получении комментария для {varName}: {ex.Message}");
                        }

                        variables.Add(new XElement("Variable",
                            new XAttribute("Name", varName),
                            new XAttribute("DataType", dataType),
                            new XAttribute("Comment", comment)
                        ));
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"ExportSingleDataBlockToXml: Ошибка при обработке переменных DB {dbName}: {ex.Message}");
                }

                // Создаем XML документ
                XDocument doc = new XDocument(
                    new XElement("DataBlock",
                        new XAttribute("Name", dbName),
                        new XAttribute("Optimized", isOptimized),
                        new XElement("Variables", variables)
                    )
                );

                // Сохраняем документ
                doc.Save(exportPath);
                _logger.Info($"ExportSingleDataBlockToXml: DB {dbName} экспортирован: {exportPath}");
            }
            catch (Exception ex)
            {
                string dbName = "unknown";
                try { dbName = db.Name; } catch { }

                _logger.Error($"ExportSingleDataBlockToXml: Ошибка при экспорте DB {dbName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Загрузка блоков данных из XML-файлов
        /// </summary>
        public List<TagDefinition> LoadDbTagsFromXml()
        {
            if (string.IsNullOrEmpty(_currentProjectName))
            {
                _logger.Error("LoadDbTagsFromXml: Не установлен текущий проект");
                return new List<TagDefinition>();
            }

            var dbTags = new List<TagDefinition>();

            try
            {
                if (!Directory.Exists(_dbExportsPath))
                {
                    _logger.Error($"LoadDbTagsFromXml: Директория {_dbExportsPath} не найдена");
                    return dbTags;
                }

                string[] xmlFiles = Directory.GetFiles(_dbExportsPath, "*.xml", SearchOption.TopDirectoryOnly);
                _logger.Info($"LoadDbTagsFromXml: Найдено {xmlFiles.Length} XML-файлов с DB");

                foreach (string file in xmlFiles)
                {
                    if (!File.Exists(file))
                    {
                        _logger.Warn($"LoadDbTagsFromXml: Файл {file} не существует");
                        continue;
                    }

                    try
                    {
                        XDocument doc = XDocument.Load(file);

                        if (doc.Root == null)
                        {
                            _logger.Warn($"LoadDbTagsFromXml: Файл {file} имеет некорректную структуру XML (Root = null)");
                            continue;
                        }

                        string dbName = doc.Root.Attribute("Name")?.Value ?? Path.GetFileNameWithoutExtension(file);
                        bool isOptimized = false;

                        try
                        {
                            isOptimized = bool.TryParse(doc.Root.Attribute("Optimized")?.Value ?? "false", out bool opt) && opt;
                        }
                        catch (Exception ex)
                        {
                            _logger.Debug($"LoadDbTagsFromXml: Ошибка при получении атрибута Optimized: {ex.Message}");
                        }

                        // Добавляем информацию о блоке данных
                        var dbTag = new TagDefinition
                        {
                            Name = dbName,
                            Address = isOptimized ? "Optimized" : "Standard",
                            DataType = TagDataType.Bool, // Используем существующий тип вместо Struct
                            GroupName = "DataBlocks",
                            IsOptimized = isOptimized
                        };

                        dbTags.Add(dbTag);

                        // Если нужно также добавлять переменные блоков данных:
                        /*
                        foreach (var varElement in doc.Descendants("Variable"))
                        {
                            try
                            {
                                string varName = varElement.Attribute("Name")?.Value ?? "Unknown";
                                string dataTypeStr = varElement.Attribute("DataType")?.Value ?? "Unknown";
                                
                                // Формируем полное имя переменной с префиксом DB
                                string fullName = $"{dbName}.{varName}";
                                
                                TagDataType dataType = ConvertStringToTagDataType(dataTypeStr);
                                
                                dbTags.Add(new TagDefinition
                                {
                                    Name = fullName,
                                    DataType = dataType,
                                    Address = isOptimized ? "Optimized" : "Standard",
                                    GroupName = dbName,
                                    IsOptimized = isOptimized
                                });
                            }
                            catch (Exception varEx)
                            {
                                _logger.Debug($"LoadDbTagsFromXml: Ошибка при обработке переменной: {varEx.Message}");
                                // Продолжаем с другими переменными
                            }
                        }
                        */

                        _logger.Info($"LoadDbTagsFromXml: Загружен DB {dbName}");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"LoadDbTagsFromXml: Ошибка при обработке файла {Path.GetFileName(file)}: {ex.Message}");
                        // Продолжаем с другими файлами
                    }
                }

                _logger.Info($"LoadDbTagsFromXml: Всего загружено {dbTags.Count} блоков данных");
            }
            catch (Exception ex)
            {
                _logger.Error($"LoadDbTagsFromXml: Ошибка: {ex.Message}");
            }

            return dbTags;
        }
    }
}