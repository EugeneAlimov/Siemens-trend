using System;
using System.Collections.Generic;
using System.Threading;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;
using SiemensTrend.Core.Logging;
using SiemensTrend.Core.Models;

namespace SiemensTrend.Communication.TIA
{
    /// <summary>
    /// Улучшенный комбинированный класс для чтения всех типов тегов из проекта TIA Portal
    /// с поддержкой различных режимов чтения для обеспечения стабильности
    /// </summary>
    public class TiaPortalTagReader : ITiaPortalTagReader
    {
        private readonly Logger _logger;
        private readonly TiaPortalCommunicationService _tiaService;
        private readonly TiaPortalPlcTagReader _plcTagReader;
        private readonly TiaPortalDbTagReader _dbTagReader;
        private readonly TiaPortalTagReaderFactory.ReaderMode _mode;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="logger">Логгер</param>
        /// <param name="tiaService">Сервис коммуникации с TIA Portal</param>
        /// <param name="mode">Режим чтения тегов</param>
        public TiaPortalTagReader(
            Logger logger,
            TiaPortalCommunicationService tiaService,
            TiaPortalTagReaderFactory.ReaderMode mode = TiaPortalTagReaderFactory.ReaderMode.SafeMode)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tiaService = tiaService ?? throw new ArgumentNullException(nameof(tiaService));
            _mode = mode;

            _logger.Info($"TiaPortalTagReader: Инициализация в режиме {mode}");

            // Создаем специализированные читатели
            _plcTagReader = new TiaPortalPlcTagReader(logger, tiaService);
            _dbTagReader = new TiaPortalDbTagReader(logger, tiaService);
        }

        /// <summary>
        /// Чтение всех тегов из проекта
        /// </summary>
        public PlcData ReadAllTags()
        {
            var plcData = new PlcData();

            try
            {
                _logger.Info($"ReadAllTags: Чтение тегов из проекта TIA Portal в режиме {_mode}...");

                // Получаем программное обеспечение ПЛК
                var plcSoftware = GetPlcSoftware();
                if (plcSoftware == null)
                {
                    return plcData;
                }

                // Сначала читаем теги ПЛК
                try
                {
                    _logger.Info("ReadAllTags: Начало чтения тегов ПЛК...");
                    int plcTagCount = ReadPlcTags(plcData);
                    _logger.Info($"ReadAllTags: Теги ПЛК прочитаны успешно, найдено {plcTagCount} тегов");
                }
                catch (Exception ex)
                {
                    _logger.Error($"ReadAllTags: Ошибка при чтении тегов ПЛК: {ex.Message}");
                }

                // Делаем паузу между операциями для снижения нагрузки на TIA Portal
                Thread.Sleep(500);

                // Проверяем соединение
                if (!CheckConnection())
                {
                    _logger.Error("ReadAllTags: Соединение с TIA Portal потеряно после чтения тегов ПЛК");
                    return plcData;
                }

                // Обновляем PlcSoftware после проверки соединения
                plcSoftware = GetPlcSoftware();
                if (plcSoftware == null)
                {
                    return plcData;
                }

                // Читаем блоки данных с учетом выбранного режима
                try
                {
                    _logger.Info($"ReadAllTags: Начало чтения тегов DB в режиме {_mode}...");

                    // Адаптируем чтение блоков данных в зависимости от режима
                    int dbTagCount = 0;

                    switch (_mode)
                    {
                        case TiaPortalTagReaderFactory.ReaderMode.Standard:
                            // Стандартный режим без ограничений
                            dbTagCount = ReadDataBlocks(plcData);
                            break;

                        case TiaPortalTagReaderFactory.ReaderMode.SafeMode:
                            // Безопасный режим с контролем ошибок
                            dbTagCount = ReadDataBlocksSafe(plcData);
                            break;

                        case TiaPortalTagReaderFactory.ReaderMode.MinimalMode:
                            // Минимальный режим с базовой информацией
                            dbTagCount = ReadDataBlocksMinimal(plcData);
                            break;

                        default:
                            // По умолчанию используем безопасный режим
                            _logger.Warn($"ReadAllTags: Неизвестный режим {_mode}, используем SafeMode");
                            dbTagCount = ReadDataBlocksSafe(plcData);
                            break;
                    }

                    _logger.Info($"ReadAllTags: Теги DB прочитаны успешно, найдено {dbTagCount} тегов");
                }
                catch (Exception ex)
                {
                    _logger.Error($"ReadAllTags: Ошибка при чтении тегов DB: {ex.Message}");
                }

                // Сообщаем о завершении чтения
                _logger.Info($"ReadAllTags: Чтение завершено. Всего тегов: {plcData.AllTags.Count}");
            }
            catch (Exception ex)
            {
                _logger.Error($"ReadAllTags: Общая ошибка при чтении тегов: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.Error($"ReadAllTags: Внутренняя ошибка: {ex.InnerException.Message}");
                }
            }
            finally
            {
                // Запускаем сборщик мусора для освобождения COM-объектов
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            return plcData;
        }

        /// <summary>
        /// Чтение тегов ПЛК
        /// </summary>
        public int ReadPlcTags(PlcData plcData)
        {
            if (plcData == null)
                throw new ArgumentNullException(nameof(plcData));

            try
            {
                _logger.Info($"ReadPlcTags: Делегирование чтения тегов ПЛК специализированному читателю...");
                return _plcTagReader.ReadPlcTags(plcData);
            }
            catch (Exception ex)
            {
                _logger.Error($"ReadPlcTags: Ошибка при делегировании: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Чтение блоков данных - стандартный метод, делегирующий вызов специализированному читателю
        /// </summary>
        public int ReadDataBlocks(PlcData plcData)
        {
            if (plcData == null)
                throw new ArgumentNullException(nameof(plcData));

            try
            {
                _logger.Info($"ReadDataBlocks: Делегирование чтения блоков данных специализированному читателю...");
                return _dbTagReader.ReadDataBlocks(plcData);
            }
            catch (Exception ex)
            {
                _logger.Error($"ReadDataBlocks: Ошибка при делегировании: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Безопасное чтение блоков данных с дополнительными проверками и восстановлением
        /// </summary>
        private int ReadDataBlocksSafe(PlcData plcData)
        {
            if (plcData == null)
                throw new ArgumentNullException(nameof(plcData));

            int initialCount = plcData.DbTags.Count;
            int retryCount = 0;
            const int maxRetries = 3;

            try
            {
                _logger.Info("ReadDataBlocksSafe: Начало безопасного чтения блоков данных...");

                // Пытаемся выполнить чтение DB с несколькими попытками в случае ошибок
                while (retryCount < maxRetries)
                {
                    try
                    {
                        // Стандартный вызов, но с обработкой ошибок
                        int result = _dbTagReader.ReadDataBlocks(plcData);
                        _logger.Info($"ReadDataBlocksSafe: Чтение блоков данных выполнено успешно. Прочитано {result} блоков.");

                        // Если успешно прочитано хотя бы несколько блоков, выходим из цикла
                        if (result > 0)
                        {
                            return result;
                        }

                        // Если не удалось прочитать ни одного блока, повторяем попытку
                        retryCount++;
                        _logger.Warn($"ReadDataBlocksSafe: Не удалось прочитать блоки данных, попытка {retryCount} из {maxRetries}");

                        // Делаем паузу перед следующей попыткой
                        Thread.Sleep(1000);
                    }
                    catch (Exception ex)
                    {
                        retryCount++;
                        _logger.Error($"ReadDataBlocksSafe: Ошибка при чтении блоков данных (попытка {retryCount}): {ex.Message}");

                        // Делаем паузу перед следующей попыткой
                        Thread.Sleep(1000);

                        // Запускаем сборщик мусора после ошибки
                        GC.Collect();
                        GC.WaitForPendingFinalizers();

                        // Проверяем соединение после ошибки
                        if (!CheckConnection())
                        {
                            _logger.Error("ReadDataBlocksSafe: Соединение с TIA Portal потеряно после ошибки");
                            break;
                        }
                    }
                }

                // Если все попытки неудачны, возвращаем 0
                return plcData.DbTags.Count - initialCount;
            }
            catch (Exception ex)
            {
                _logger.Error($"ReadDataBlocksSafe: Общая ошибка при безопасном чтении блоков данных: {ex.Message}");
                return plcData.DbTags.Count - initialCount;
            }
        }

        /// <summary>
        /// Минимальное чтение блоков данных - только базовая информация без структуры
        /// </summary>
        private int ReadDataBlocksMinimal(PlcData plcData)
        {
            if (plcData == null)
                throw new ArgumentNullException(nameof(plcData));

            try
            {
                // Получаем программное обеспечение ПЛК
                var plcSoftware = GetPlcSoftware();
                if (plcSoftware == null)
                {
                    return 0;
                }

                // Получаем список блоков
                var blockGroup = plcSoftware.BlockGroup;
                if (blockGroup == null)
                {
                    _logger.Error("ReadDataBlocksMinimal: BlockGroup не найдена в PlcSoftware");
                    return 0;
                }

                // Создаем временный список для блоков данных
                var dataBlocks = new List<Siemens.Engineering.SW.Blocks.DataBlock>();

                // Ищем блоки данных без углубления в иерархию
                CollectDataBlocksMinimal(blockGroup, dataBlocks, 100); // Ограничиваем до 100 блоков

                _logger.Info($"ReadDataBlocksMinimal: Найдено {dataBlocks.Count} блоков данных");

                // Обрабатываем только основную информацию о каждом блоке
                foreach (var db in dataBlocks)
                {
                    try
                    {
                        if (db == null) continue;

                        // Получаем только имя блока данных
                        string dbName = db.Name;

                        // Определяем оптимизацию, если возможно
                        bool isOptimized = false;
                        try { 
                            isOptimized = db.MemoryLayout == Siemens.Engineering.SW.Blocks.MemoryLayout.Optimized; 
                        }
                        catch { /* Игнорируем ошибки */ }

                        // Создаем тег для блока данных
                        var dbTag = new TagDefinition
                        {
                            Id = Guid.NewGuid(),
                            Name = dbName,
                            Address = isOptimized ? "Optimized" : "Standard",
                            DataType = TagDataType.UDT,
                            Comment = "Data Block",
                            GroupName = "DB",
                            IsOptimized = isOptimized,
                            IsUDT = true,
                            IsSafety = false
                        };

                        // Добавляем тег в коллекцию
                        plcData.DbTags.Add(dbTag);

                        // Важно: удерживаем COM-объект в памяти до завершения работы с ним
                        GC.KeepAlive(db);
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug($"ReadDataBlocksMinimal: Ошибка при обработке блока данных: {ex.Message}");
                        // Продолжаем с другими блоками
                    }
                }

                _logger.Info($"ReadDataBlocksMinimal: Обработано {plcData.DbTags.Count} блоков данных");
                return plcData.DbTags.Count;
            }
            catch (Exception ex)
            {
                _logger.Error($"ReadDataBlocksMinimal: Общая ошибка при минимальном чтении блоков данных: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Минимальный сбор блоков данных без углубления в иерархию
        /// </summary>
        private void CollectDataBlocksMinimal(PlcBlockGroup group, List<Siemens.Engineering.SW.Blocks.DataBlock> dataBlocks, int maxCount)
        {
            try
            {
                if (group == null || dataBlocks.Count >= maxCount)
                    return;

                // Собираем блоки текущей группы
                if (group.Blocks != null)
                {
                    foreach (var block in group.Blocks)
                    {
                        try
                        {
                            if (block is Siemens.Engineering.SW.Blocks.DataBlock db)
                            {
                                dataBlocks.Add(db);

                                // Если достигли максимального количества, прекращаем сбор
                                if (dataBlocks.Count >= maxCount)
                                    return;
                            }
                        }
                        catch { /* Игнорируем ошибки */ }
                    }
                }

                // Проверяем соединение после каждой группы
                if (!CheckConnection())
                    return;
            }
            catch (Exception ex)
            {
                _logger.Debug($"CollectDataBlocksMinimal: Ошибка при сборе блоков данных: {ex.Message}");
            }
        }

        /// <summary>
        /// Получение программного обеспечения ПЛК
        /// </summary>
        private PlcSoftware GetPlcSoftware()
        {
            try
            {
                var plcSoftware = _tiaService.GetPlcSoftware();

                if (plcSoftware == null)
                {
                    _logger.Error("GetPlcSoftware: Не удалось получить PlcSoftware из проекта");
                    return null;
                }

                _logger.Info($"GetPlcSoftware: PlcSoftware получен успешно: {plcSoftware.Name}");
                return plcSoftware;
            }
            catch (Exception ex)
            {
                _logger.Error($"GetPlcSoftware: Ошибка при получении PlcSoftware: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Проверка соединения с TIA Portal
        /// </summary>
        private bool CheckConnection()
        {
            if (!_tiaService.IsConnected || _tiaService.CurrentProject == null)
            {
                _logger.Error("CheckConnection: Соединение с TIA Portal потеряно");
                return false;
            }
            return true;
        }
    }
}