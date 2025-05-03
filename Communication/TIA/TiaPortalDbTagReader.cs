using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Siemens.Engineering;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.SW.Blocks.Interface;
using Siemens.Engineering.SW.ExternalSources;
using Siemens.Engineering.SW.Tags;
using SiemensTrend.Core.Logging;
using SiemensTrend.Core.Models;

namespace SiemensTrend.Communication.TIA
{
    /// <summary>
    /// Улучшенный класс для чтения блоков данных из проекта TIA Portal
    /// с защитой от сбоев и частичной обработкой структуры блоков
    /// </summary>
    public class TiaPortalDbTagReader : TiaPortalTagReaderBase
    {
        // Максимальное количество блоков данных для обработки
        private readonly int _maxDbCount = 1000;
        // Максимальное количество членов DB для обработки в одном блоке
        private readonly int _maxMembersPerDb = 200;
        // Максимальная глубина иерархии блоков данных
        private readonly int _maxHierarchyDepth = 3;

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
            // Этот метод не используется в данном классе
            return 0;
        }

        /// <summary>
        /// Чтение блоков данных из проекта с защитой от сбоев
        /// </summary>
        /// <param name="plcData">Объект для сохранения тегов</param>
        /// <returns>Количество прочитанных блоков данных</returns>
        public override int ReadDataBlocks(PlcData plcData)
        {
            int dbCount = 0;
            int dbTagCount = 0;

            try
            {
                _logger.Info("ReadDataBlocks: Начало безопасного чтения блоков данных...");

                // Получаем программное обеспечение ПЛК
                var plcSoftware = GetPlcSoftware();
                if (plcSoftware == null)
                {
                    return 0;
                }

                // Получаем список всех блоков данных без рекурсивной обработки групп
                var allDataBlocks = new List<DataBlock>();

                // Ограничиваем количество собираемых блоков данных
                _logger.Info($"ReadDataBlocks: Начало сбора блоков данных (лимит: {_maxDbCount})...");
                CollectDataBlocks(plcSoftware.BlockGroup, allDataBlocks, _maxDbCount);

                _logger.Info($"ReadDataBlocks: Найдено {allDataBlocks.Count} блоков данных для обработки");

                // Обрабатываем каждый блок данных по отдельности с восстановлением после ошибок
                int processedCount = 0;
                foreach (var db in allDataBlocks)
                {
                    try
                    {
                        if (db == null) continue;

                        processedCount++;
                        _logger.Info($"ReadDataBlocks: Обработка блока данных: {db.Name} ({processedCount}/{allDataBlocks.Count})");

                        // Используем таймаут для предотвращения зависаний
                        var propertiesTask = System.Threading.Tasks.Task.Run(() => GetDbProperties(db));
                        bool timeoutOccurred = !propertiesTask.Wait(5000); // Ждем не более 5 секунд

                        if (timeoutOccurred)
                        {
                            _logger.Warn($"ReadDataBlocks: Таймаут при получении свойств блока данных {db.Name}");
                            continue;
                        }

                        var properties = propertiesTask.Result;
                        bool isOptimized = properties.Item1;
                        bool isUDT = properties.Item2;
                        bool isSafety = properties.Item3;

                        // Создаем запись для самого блока данных
                        var dbDefinition = new TagDefinition
                        {
                            Id = Guid.NewGuid(),
                            Name = db.Name,
                            Address = isOptimized ? "Optimized" : "Standard",
                            DataType = TagDataType.UDT,
                            Comment = "Data Block",
                            GroupName = "DB",
                            IsOptimized = isOptimized,
                            IsUDT = isUDT,
                            IsSafety = isSafety
                        };

                        plcData.DbTags.Add(dbDefinition);
                        dbCount++;

                        // Обрабатываем члены блока данных безопасным способом
                        int membersProcessed = ProcessDbMembersSafe(db, plcData, isOptimized, isUDT, isSafety, ref dbTagCount);
                        _logger.Info($"ReadDataBlocks: В блоке {db.Name} обработано {membersProcessed} членов");

                        // Важно: удерживаем COM-объект в памяти до завершения работы с ним
                        GC.KeepAlive(db);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"ReadDataBlocks: Ошибка при обработке блока данных {db?.Name}: {ex.Message}");
                        // Пропускаем проблемный блок и продолжаем с другими
                    }

                    // Проверяем соединение после каждого блока
                    if (!CheckConnection())
                    {
                        _logger.Error("ReadDataBlocks: Потеряно соединение с TIA Portal");
                        break;
                    }

                    // Принудительно запускаем сборщик мусора каждые 10 блоков
                    if (processedCount % 10 == 0)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                }

                _logger.Info($"ReadDataBlocks: Обработано {dbCount} блоков данных, найдено {dbTagCount} тегов DB");
            }
            catch (Exception ex)
            {
                _logger.Error($"ReadDataBlocks: Ошибка при безопасном чтении блоков данных: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.Error($"ReadDataBlocks: Внутренняя ошибка: {ex.InnerException.Message}");
                }
            }
            finally
            {
                // Очищаем память после завершения
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            return dbCount;
        }

        /// <summary>
        /// Получение свойств блока данных безопасным способом
        /// </summary>
        private Tuple<bool, bool, bool> GetDbProperties(DataBlock db)
        {
            bool isOptimized = false;
            bool isUDT = false;
            bool isSafety = false;

            try
            {
                // Пытаемся определить оптимизацию
                try
                {
                    isOptimized = db.MemoryLayout == MemoryLayout.Optimized;
                }
                catch (Exception ex)
                {
                    _logger.Debug($"GetDbProperties: Ошибка при определении оптимизации: {ex.Message}");
                }

                // Определяем UDT по имени
                try
                {
                    isUDT = db.Name.Contains("UDT") || db.Name.Contains("Type");
                }
                catch (Exception ex)
                {
                    _logger.Debug($"GetDbProperties: Ошибка при определении UDT: {ex.Message}");
                }

                // Пытаемся определить Safety
                try
                {
                    var programmingLanguage = db.GetAttribute("ProgrammingLanguage")?.ToString();
                    isSafety = programmingLanguage == "F_DB";
                }
                catch (Exception ex)
                {
                    _logger.Debug($"GetDbProperties: Ошибка при определении Safety: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"GetDbProperties: Общая ошибка при получении свойств блока {db?.Name}: {ex.Message}");
            }

            return new Tuple<bool, bool, bool>(isOptimized, isUDT, isSafety);
        }

        /// <summary>
        /// Рекурсивный сбор блоков данных из группы
        /// </summary>
        private void CollectDataBlocks(PlcBlockGroup group, List<DataBlock> dataBlocks, int maxCount)
        {
            try
            {
                // Проверка аргументов
                if (group == null)
                {
                    _logger.Warn("CollectDataBlocks: group не может быть null");
                    return;
                }

                // Если достигли максимального количества блоков, прекращаем сбор
                if (dataBlocks.Count >= maxCount)
                {
                    _logger.Info($"CollectDataBlocks: Достигнут лимит количества блоков данных ({maxCount})");
                    return;
                }

                // Логгируем группу
                string groupName = "Unknown";
                try { groupName = group.Name; } catch { }
                _logger.Debug($"CollectDataBlocks: Обработка группы блоков: {groupName}");

                // Собираем блоки данных из текущей группы с защитой от сбоев
                try
                {
                    // Проверка наличия доступа к Blocks
                    if (group.Blocks != null)
                    {
                        int blockCount = 0;
                        try { blockCount = group.Blocks.Count; } catch { }
                        _logger.Debug($"CollectDataBlocks: Количество блоков в группе {groupName}: {blockCount}");

                        // Создаем защищенную копию списка блоков
                        var blockList = new List<PlcBlock>();
                        foreach (var block in group.Blocks)
                        {
                            if (block != null)
                            {
                                blockList.Add(block);
                                if (blockList.Count >= 1000) break; // Защита от слишком большого количества блоков
                            }
                        }

                        // Проходим по копии списка
                        foreach (var block in blockList)
                        {
                            try
                            {
                                if (block is DataBlock db)
                                {
                                    dataBlocks.Add(db);
                                    _logger.Debug($"CollectDataBlocks: Добавлен DB: {db.Name}");

                                    // Если достигли максимального количества блоков, прекращаем сбор
                                    if (dataBlocks.Count >= maxCount)
                                    {
                                        _logger.Info($"CollectDataBlocks: Достигнут лимит количества блоков данных ({maxCount})");
                                        return;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.Debug($"CollectDataBlocks: Ошибка при обработке блока: {ex.Message}");
                                // Продолжаем с другими блоками
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"CollectDataBlocks: Ошибка при доступе к блокам группы {groupName}: {ex.Message}");
                }

                // Рекурсивно обрабатываем подгруппы
                try
                {
                    // Проверка наличия доступа к Groups
                    if (group.Groups != null)
                    {
                        int groupCount = 0;
                        try { groupCount = group.Groups.Count; } catch { }
                        _logger.Debug($"CollectDataBlocks: Количество подгрупп в группе {groupName}: {groupCount}");

                        // Создаем защищенную копию списка подгрупп
                        var subgroupList = new List<PlcBlockGroup>();
                        foreach (var subgroup in group.Groups)
                        {
                            if (subgroup is PlcBlockGroup pbg)
                            {
                                subgroupList.Add(pbg);
                                if (subgroupList.Count >= 100) break; // Защита от слишком большого количества подгрупп
                            }
                        }

                        // Проходим по копии списка
                        foreach (var subgroup in subgroupList)
                        {
                            try
                            {
                                CollectDataBlocks(subgroup, dataBlocks, maxCount);

                                // Если достигли максимального количества блоков, прекращаем сбор
                                if (dataBlocks.Count >= maxCount)
                                {
                                    return;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.Debug($"CollectDataBlocks: Ошибка при обработке подгруппы: {ex.Message}");
                                // Продолжаем с другими подгруппами
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"CollectDataBlocks: Ошибка при доступе к подгруппам группы {groupName}: {ex.Message}");
                }

                // Важно: удерживаем COM-объект в памяти до завершения работы с ним
                GC.KeepAlive(group);
            }
            catch (Exception ex)
            {
                _logger.Error($"CollectDataBlocks: Общая ошибка при сборе блоков данных: {ex.Message}");
            }
        }

        /// <summary>
        /// Обработка членов блока данных с ограничением по количеству и глубине
        /// </summary>
        private int ProcessDbMembersSafe(DataBlock db, PlcData plcData, bool isOptimized, bool isUDT, bool isSafety, ref int tagCount)
        {
            int processedMembers = 0;

            try
            {
                // Проверка аргументов
                if (db == null)
                {
                    _logger.Warn("ProcessDbMembersSafe: db не может быть null");
                    return 0;
                }

                string dbName = "Unknown";
                try { dbName = db.Name; } catch { }

                // Безопасно получаем интерфейс блока данных
                if (db.Interface == null)
                {
                    _logger.Warn($"ProcessDbMembersSafe: Блок данных {dbName} не имеет интерфейса");
                    return 0;
                }

                // Проверяем наличие членов
                if (db.Interface.Members == null)
                {
                    _logger.Warn($"ProcessDbMembersSafe: Интерфейс блока данных {dbName} не имеет членов");
                    return 0;
                }

                int memberCount = 0;
                try { memberCount = db.Interface.Members.Count; } catch { }
                _logger.Info($"ProcessDbMembersSafe: В блоке {dbName} найдено {memberCount} членов");

                // Ограничиваем количество обрабатываемых членов
                var membersTasks = new List<System.Threading.Tasks.Task>();
                var membersList = new List<Member>();

                // Создаем защищенную копию списка членов
                foreach (var member in db.Interface.Members)
                {
                    if (member != null)
                    {
                        membersList.Add(member);
                        if (membersList.Count >= _maxMembersPerDb) break; // Защита от слишком большого количества членов
                    }
                }

                // Обрабатываем каждый член параллельно
                foreach (var member in membersList)
                {
                    try
                    {
                        ProcessDbMember(member, dbName, plcData, isOptimized, isUDT, isSafety, 0, ref processedMembers, ref tagCount);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"ProcessDbMembersSafe: Ошибка при обработке члена блока данных: {ex.Message}");
                        // Продолжаем с другими членами
                    }
                }

                // Важно: удерживаем COM-объект в памяти до завершения работы с ним
                GC.KeepAlive(db);
                GC.KeepAlive(db.Interface);
                GC.KeepAlive(db.Interface.Members);
            }
            catch (Exception ex)
            {
                _logger.Error($"ProcessDbMembersSafe: Общая ошибка при обработке членов блока данных: {ex.Message}");
            }

            return processedMembers;
        }

        /// <summary>
        /// Обработка отдельного члена блока данных с ограничением глубины
        /// </summary>
        private void ProcessDbMember(Member member, string dbName, PlcData plcData, bool isOptimized,
                                     bool isUDT, bool isSafety, int depth, ref int processedMembers, ref int tagCount)
        {
            try
            {
                if (member == null) return;
                if (depth >= _maxHierarchyDepth) return; // Ограничение глубины иерархии

                string memberName = "Unknown";
                try { memberName = member.Name; } catch { }

                try
                {
                    // Пытаемся получить доступ к подчленам через Members
                    bool hasSubMembers = false;
                    List<Member> subMembers = new List<Member>();

                    // Сначала пробуем через прямой доступ (если свойство Members доступно)
                    try
                    {
                        if (member.GetType().GetProperty("Members") != null)
                        {
                            var members = member.GetType().GetProperty("Members").GetValue(member);
                            if (members != null && members is System.Collections.IEnumerable collection)
                            {
                                foreach (var item in collection)
                                {
                                    if (item is Member subMember)
                                    {
                                        subMembers.Add(subMember);
                                        hasSubMembers = true;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug($"ProcessDbMember: Не удалось получить подчлены через свойство Members: {ex.Message}");
                    }

                    // Затем пробуем через атрибут (если не удалось через свойство)
                    if (!hasSubMembers)
                    {
                        try
                        {
                            var membersAttribute = member.GetAttribute("Members");
                            if (membersAttribute != null && membersAttribute is System.Collections.IEnumerable collection)
                            {
                                foreach (var item in collection)
                                {
                                    if (item is Member subMember)
                                    {
                                        subMembers.Add(subMember);
                                        hasSubMembers = true;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Debug($"ProcessDbMember: Не удалось получить подчлены через атрибут Members: {ex.Message}");
                        }
                    }

                    // Обрабатываем найденные подчлены (если они есть)
                    if (hasSubMembers && subMembers.Count > 0)
                    {
                        _logger.Debug($"ProcessDbMember: Обнаружено {subMembers.Count} подчленов для {memberName}");

                        // Ограничиваем количество подчленов (для безопасности)
                        int maxSubMembers = Math.Min(subMembers.Count, 20);

                        for (int i = 0; i < maxSubMembers; i++)
                        {
                            // Рекурсивно обрабатываем подчлен с увеличением глубины
                            ProcessDbMember(subMembers[i], dbName, plcData, isOptimized,
                                           isUDT, isSafety, depth + 1, ref processedMembers, ref tagCount);
                        }
                    }
                    else
                    {
                        _logger.Debug($"ProcessDbMember: У члена {memberName} не найдено подчленов");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Debug($"ProcessDbMember: Ошибка при обработке подчленов {memberName}: {ex.Message}");
                }

                // Важно: удерживаем COM-объект в памяти до завершения работы с ним
                GC.KeepAlive(member);
            }
            catch (Exception ex)
            {
                _logger.Error($"ProcessDbMember: Ошибка при обработке члена {member?.Name}: {ex.Message}");
            }
        }
    }
}