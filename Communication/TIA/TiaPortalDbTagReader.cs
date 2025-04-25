using System;
using System.Collections.Generic;
using Siemens.Engineering;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;
using SiemensTrend.Core.Logging;
using SiemensTrend.Core.Models;

namespace SiemensTrend.Communication.TIA
{
    /// <summary>
    /// Класс для чтения блоков данных из проекта TIA Portal
    /// </summary>
    public class TiaPortalDbTagReader : TiaPortalTagReaderBase
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="logger">Логгер</param>
        /// <param name="tiaService">Сервис коммуникации с TIA Portal</param>
        public TiaPortalDbTagReader(Logger logger, TiaPortalCommunicationService tiaService)
            : base(logger, tiaService)
        {
        }

        /// <summary>
        /// Пустая реализация метода чтения тегов ПЛК (не используется в этом классе)
        /// </summary>
        public override int ReadPlcTags(PlcData plcData)
        {
            // Этот метод не используется в данном классе, 
            // он реализован в TiaPortalPlcTagReader
            return 0;
        }

        /// <summary>
        /// Чтение блоков данных из проекта
        /// </summary>
        /// <param name="plcData">Объект для сохранения тегов</param>
        /// <returns>Количество прочитанных блоков данных</returns>
        public override int ReadDataBlocks(PlcData plcData)
        {
            int dbCount = 0;
            int dbTagCount = 0;

            try
            {
                _logger.Info("Чтение блоков данных (безопасный режим)...");

                // Получаем программное обеспечение ПЛК
                var plcSoftware = GetPlcSoftware();
                if (plcSoftware == null)
                {
                    return 0;
                }

                // Получаем список всех блоков данных напрямую, без рекурсивной обработки групп
                var allDataBlocks = new List<DataBlock>();

                // Рекурсивно собираем только ссылки на блоки данных
                CollectDataBlocks(plcSoftware.BlockGroup, allDataBlocks);

                _logger.Info($"Найдено {allDataBlocks.Count} блоков данных для обработки");

                // Обрабатываем каждый блок данных по отдельности с восстановлением после ошибок
                foreach (var db in allDataBlocks)
                {
                    try
                    {
                        _logger.Info($"Обработка блока данных: {db.Name}");

                        // Запрашиваем свойства каждого блока данных в отдельном try-catch
                        bool isOptimized = false;
                        bool isUDT = false;
                        bool isSafety = false;

                        try { isOptimized = db.MemoryLayout == MemoryLayout.Optimized; }
                        catch (Exception ex) { _logger.Debug($"Ошибка при определении оптимизации: {ex.Message}"); }

                        try { isUDT = db.Name.Contains("UDT") || db.Name.Contains("Type"); }
                        catch (Exception ex) { _logger.Debug($"Ошибка при определении UDT: {ex.Message}"); }

                        try
                        {
                            var programmingLanguage = db.GetAttribute("ProgrammingLanguage")?.ToString();
                            isSafety = programmingLanguage == "F_DB";
                        }
                        catch (Exception ex) { _logger.Debug($"Ошибка при определении Safety: {ex.Message}"); }

                        // Осторожно обрабатываем члены блока данных
                        ProcessDbMembersSafe(db, plcData, isOptimized, isUDT, isSafety, ref dbTagCount);

                        // Важно: удерживаем COM-объект в памяти до завершения работы с ним
                        GC.KeepAlive(db);

                        dbCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Ошибка при обработке блока данных {db.Name}: {ex.Message}");
                        // Пропускаем проблемный блок и продолжаем с другими
                    }

                    // Проверяем соединение после каждого блока
                    if (!CheckConnection())
                    {
                        break;
                    }
                }

                _logger.Info($"Обработано {dbCount} блоков данных, найдено {dbTagCount} тегов DB");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при безопасном чтении блоков данных: {ex.Message}");
            }

            return dbCount;
        }

        /// <summary>
        /// Рекурсивный сбор блоков данных из группы
        /// </summary>
        private void CollectDataBlocks(PlcBlockGroup group, List<DataBlock> dataBlocks)
        {
            try
            {
                // Проверка аргументов
                if (group == null)
                {
                    _logger.Warn("CollectDataBlocks: group не может быть null");
                    return;
                }

                // Логгируем группу
                _logger.Debug($"CollectDataBlocks: Обработка группы блоков: {group.Name}");

                // Собираем блоки данных из текущей группы
                try
                {
                    // Проверка наличия доступа к Blocks
                    if (group.Blocks == null)
                    {
                        _logger.Warn($"CollectDataBlocks: Blocks равен null в группе {group.Name}");
                        return;
                    }

                    _logger.Debug($"CollectDataBlocks: Количество блоков в группе {group.Name}: {group.Blocks.Count}");

                    foreach (var block in group.Blocks)
                    {
                        try
                        {
                            if (block is DataBlock db)
                            {
                                dataBlocks.Add(db);
                                _logger.Debug($"CollectDataBlocks: Добавлен DB: {db.Name}");

                                // Важно: удерживаем COM-объект в памяти до завершения работы с ним
                                GC.KeepAlive(db);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error($"CollectDataBlocks: Ошибка при обработке блока: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"CollectDataBlocks: Ошибка при доступе к блокам группы {group.Name}: {ex.Message}");
                }

                // Рекурсивно обрабатываем подгруппы
                try
                {
                    // Проверка наличия доступа к Groups
                    if (group.Groups == null)
                    {
                        _logger.Warn($"CollectDataBlocks: Groups равен null в группе {group.Name}");
                        return;
                    }

                    _logger.Debug($"CollectDataBlocks: Количество подгрупп в группе {group.Name}: {group.Groups.Count}");

                    foreach (var subgroup in group.Groups)
                    {
                        try
                        {
                            CollectDataBlocks(subgroup as PlcBlockGroup, dataBlocks);
                        }
                        catch (Exception ex)
                        {
                            _logger.Debug($"CollectDataBlocks: Пропуск подгруппы из-за ошибки: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"CollectDataBlocks: Ошибка при доступе к подгруппам группы {group.Name}: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"CollectDataBlocks: Ошибка при сборе блоков данных: {ex.Message}");
            }
        }

        /// <summary>
        /// Обработка членов блока данных
        /// </summary>
        private void ProcessDbMembersSafe(DataBlock db, PlcData plcData, bool isOptimized, bool isUDT, bool isSafety, ref int tagCount)
        {
            try
            {
                // Проверка аргументов
                if (db == null)
                {
                    _logger.Warn("ProcessDbMembersSafe: db не может быть null");
                    return;
                }

                // Безопасная обработка интерфейса блока данных
                if (db.Interface == null)
                {
                    _logger.Warn($"ProcessDbMembersSafe: Блок данных {db.Name} не имеет интерфейса");
                    return;
                }

                // Ограничиваем глубину обработки для снижения вероятности ошибок
                ExtractFlattenedDbTags(db, plcData, isOptimized, isUDT, isSafety, ref tagCount);
            }
            catch (Exception ex)
            {
                _logger.Error($"ProcessDbMembersSafe: Ошибка при обработке членов блока данных {db?.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Извлечение тегов DB
        /// </summary>
        private void ExtractFlattenedDbTags(DataBlock db, PlcData plcData, bool isOptimized, bool isUDT, bool isSafety, ref int tagCount)
        {
            try
            {
                // Проверяем, что интерфейс и члены доступны
                var members = db.Interface.Members;
                if (members == null || members.Count == 0)
                {
                    _logger.Warn($"ExtractFlattenedDbTags: Интерфейс блока данных {db.Name} не содержит членов");
                    return;
                }

                // Перебираем каждый член
                foreach (var member in members)
                {
                    try
                    {
                        if (member == null) continue;

                        // Получаем имя члена
                        string name = member.Name;

                        // Определяем тип данных
                        string dataTypeString = "Unknown";
                        try
                        {
                            // Пробуем получить тип через свойство или аттрибут
                            var dataTypeObj = member.GetAttribute("DataTypeName");
                            dataTypeString = dataTypeObj?.ToString() ?? "Unknown";
                        }
                        catch (Exception ex)
                        {
                            _logger.Debug($"ExtractFlattenedDbTags: Ошибка при получении типа данных для {name}: {ex.Message}");
                        }

                        // Формируем полное имя тега с префиксом блока данных
                        string fullName = $"{db.Name}.{name}";

                        // Конвертируем строковый тип данных в TagDataType
                        TagDataType dataType = TiaTagTypeUtility.ConvertToTagDataType(dataTypeString);

                        // Проверяем, поддерживается ли тип данных
                        if (!TiaTagTypeUtility.IsSupportedTagType(dataType))
                        {
                            _logger.Debug($"ExtractFlattenedDbTags: Пропущен тег DB {fullName} с неподдерживаемым типом данных {dataTypeString}");
                            continue;
                        }

                        // Получаем комментарий
                        string comment = "";
                        try
                        {
                            var commentObj = member.GetAttribute("Comment");
                            comment = commentObj?.ToString() ?? "";
                        }
                        catch (Exception ex)
                        {
                            _logger.Debug($"ExtractFlattenedDbTags: Ошибка при получении комментария для {name}: {ex.Message}");
                        }

                        // Создаем и добавляем тег в плакдату
                        plcData.DbTags.Add(new TagDefinition
                        {
                            Name = fullName,
                            Address = isOptimized ? "Optimized" : "Standard",
                            DataType = dataType,
                            Comment = comment,
                            GroupName = db.Name,
                            IsOptimized = isOptimized,
                            IsUDT = isUDT,
                            IsSafety = isSafety
                        });

                        tagCount++;
                        _logger.Debug($"ExtractFlattenedDbTags: Добавлен тег DB: {fullName} ({dataTypeString})");

                        // Важно: удерживаем COM-объект в памяти до завершения работы с ним
                        GC.KeepAlive(member);
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug($"ExtractFlattenedDbTags: Ошибка при обработке члена: {ex.Message}");
                        // Продолжаем с другими членами
                    }
                }

                // Важно: удерживаем COM-объект в памяти до завершения работы с ним
                GC.KeepAlive(db);
                GC.KeepAlive(db.Interface);
                GC.KeepAlive(members);
            }
            catch (Exception ex)
            {
                _logger.Error($"ExtractFlattenedDbTags: Ошибка при извлечении тегов DB: {ex.Message}");
            }
        }
    }
}