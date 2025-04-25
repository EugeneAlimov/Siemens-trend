using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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
        /// Экспорт только блоков данных (минимизированная версия)
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

                // Вместо глубокого обхода всех блоков, создадим простую информацию о блоках данных
                // сразу создаем минимальные XML-файлы
                int dbCounter = ExportDataBlocksBasicInfo(blockGroup, 0);

                _logger.Info($"ExportDataBlocksToXml: Экспорт базовой информации о блоках данных завершен. Обработано {dbCounter} блоков");
            }
            catch (Exception ex)
            {
                _logger.Error($"ExportDataBlocksToXml: Ошибка: {ex.Message}");
            }
            finally
            {
                // Принудительно очищаем память
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        /// <summary>
        /// Экспорт базовой информации о блоках данных
        /// </summary>
        private int ExportDataBlocksBasicInfo(PlcBlockGroup group, int currentCount)
        {
            int dbCounter = currentCount;

            if (group == null) return dbCounter;

            try
            {
                // Обрабатываем блоки текущей группы
                if (group.Blocks != null)
                {
                    // Создаем копию списка блоков для безопасной итерации
                    var blocks = new List<PlcBlock>();
                    foreach (var block in group.Blocks)
                    {
                        if (block != null)
                        {
                            blocks.Add(block);
                        }
                    }

                    // Обрабатываем копию списка
                    foreach (var block in blocks)
                    {
                        try
                        {
                            // Проверяем отмену операции
                            if (_cancellationTokenSource.Token.IsCancellationRequested)
                            {
                                _logger.Info("ExportDataBlocksBasicInfo: Операция отменена пользователем");
                                return dbCounter;
                            }

                            if (block is DataBlock)
                            {
                                // Быстро получаем только базовую информацию и записываем в файл
                                if (CreateBasicDbInfoFile(block as DataBlock))
                                {
                                    dbCounter++;
                                }

                                // Добавляем паузу между блоками для снижения нагрузки на TIA Portal
                                Thread.Sleep(50);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error($"ExportDataBlocksBasicInfo: Ошибка при обработке блока: {ex.Message}");
                            // Продолжаем с другими блоками
                        }
                    }
                }

                // Рекурсивно обрабатываем подгруппы
                if (group.Groups != null)
                {
                    // Создаем копию списка подгрупп для безопасной итерации
                    var subgroups = new List<PlcBlockGroup>();
                    foreach (var subgroup in group.Groups)
                    {
                        if (subgroup is PlcBlockGroup)
                        {
                            subgroups.Add(subgroup as PlcBlockGroup);
                        }
                    }

                    // Обрабатываем копию списка
                    foreach (var subgroup in subgroups)
                    {
                        try
                        {
                            // Проверяем отмену операции
                            if (_cancellationTokenSource.Token.IsCancellationRequested)
                            {
                                _logger.Info("ExportDataBlocksBasicInfo: Операция отменена пользователем");
                                return dbCounter;
                            }

                            dbCounter = ExportDataBlocksBasicInfo(subgroup, dbCounter);

                            // Проверяем, не произошло ли отключение от TIA Portal
                            if (!IsConnectedToTiaPortal(group))
                            {
                                _logger.Error("ExportDataBlocksBasicInfo: Потеряно соединение с TIA Portal");
                                return dbCounter;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error($"ExportDataBlocksBasicInfo: Ошибка при обработке подгруппы: {ex.Message}");
                            // Продолжаем с другими подгруппами
                        }
                    }
                }

                // Важно: удерживаем COM-объект group до конца метода
                GC.KeepAlive(group);
                return dbCounter;
            }
            catch (Exception ex)
            {
                _logger.Error($"ExportDataBlocksBasicInfo: Ошибка при экспорте блоков данных: {ex.Message}");
                return dbCounter;
            }
        }

        /// <summary>
        /// Проверка, подключены ли мы все еще к TIA Portal
        /// </summary>
        private bool IsConnectedToTiaPortal(PlcBlockGroup group)
        {
            try
            {
                // Пытаемся выполнить простую операцию с объектом
                string name = group.Name;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Создает базовый XML-файл с информацией о блоке данных
        /// </summary>
        private bool CreateBasicDbInfoFile(DataBlock db)
        {
            if (db == null) return false;

            try
            {
                string dbName = db.Name;
                _logger.Info($"CreateBasicDbInfoFile: Создание базовой информации для DB {dbName}");

                string exportPath = Path.Combine(_dbExportsPath, $"{dbName}.xml");

                // Проверяем существование директории
                string directory = Path.GetDirectoryName(exportPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                bool isOptimized = false;

                // Безопасно определяем оптимизацию
                try
                {
                    isOptimized = db.MemoryLayout == MemoryLayout.Optimized;
                }
                catch (Exception ex)
                {
                    _logger.Warn($"CreateBasicDbInfoFile: Не удалось определить MemoryLayout для DB {dbName}: {ex.Message}");
                }

                // Создаем минимальный XML с основной информацией
                XDocument doc = new XDocument(
                    new XElement("DataBlock",
                        new XAttribute("Name", dbName),
                        new XAttribute("Optimized", isOptimized),
                        new XElement("Variables")
                    )
                );

                // Создаем фиктивную переменную-заглушку
                // Так мы гарантируем, что загрузчик будет видеть блок данных
                doc.Root.Element("Variables").Add(
                    new XElement("Variable",
                        new XAttribute("Name", "INFO"),
                        new XAttribute("DataType", "String"),
                        new XAttribute("Comment", "Simplified export to prevent TIA Portal crashes")
                    )
                );

                // Сохраняем документ
                doc.Save(exportPath);
                _logger.Info($"CreateBasicDbInfoFile: Базовая информация для DB {dbName} сохранена: {exportPath}");

                // Важно: удерживаем COM-объект до конца метода
                GC.KeepAlive(db);
                return true;
            }
            catch (Exception ex)
            {
                string dbName = "unknown";
                try { dbName = db.Name; } catch { }

                _logger.Error($"CreateBasicDbInfoFile: Ошибка при создании XML для DB {dbName}: {ex.Message}");
                return false;
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
                            DataType = TagDataType.Bool, // Используем существующий тип
                            GroupName = "DataBlocks",
                            IsOptimized = isOptimized
                        };

                        dbTags.Add(dbTag);

                        // Добавляем переменные из XML, если они есть
                        foreach (var varElement in doc.Descendants("Variable"))
                        {
                            try
                            {
                                string varName = varElement.Attribute("Name")?.Value ?? "Unknown";
                                string dataTypeStr = varElement.Attribute("DataType")?.Value ?? "Unknown";

                                // Пропускаем служебную переменную-заглушку
                                if (varName == "INFO" && dataTypeStr == "String")
                                    continue;

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