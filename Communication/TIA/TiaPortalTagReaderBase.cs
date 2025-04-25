using System;
using Siemens.Engineering.SW;
using SiemensTrend.Core.Logging;
using SiemensTrend.Core.Models;

namespace SiemensTrend.Communication.TIA
{
    /// <summary>
    /// Базовый абстрактный класс для всех читателей тегов TIA Portal
    /// </summary>
    public abstract class TiaPortalTagReaderBase : ITiaPortalTagReader
    {
        /// <summary>
        /// Логгер для записи событий
        /// </summary>
        protected readonly Logger _logger;

        /// <summary>
        /// Сервис коммуникации с TIA Portal
        /// </summary>
        protected readonly TiaPortalCommunicationService _tiaService;

        /// <summary>
        /// Конструктор базового класса
        /// </summary>
        /// <param name="logger">Логгер</param>
        /// <param name="tiaService">Сервис коммуникации с TIA Portal</param>
        protected TiaPortalTagReaderBase(Logger logger, TiaPortalCommunicationService tiaService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tiaService = tiaService ?? throw new ArgumentNullException(nameof(tiaService));
        }

        /// <summary>
        /// Получение программного обеспечения ПЛК
        /// </summary>
        /// <returns>Объект PlcSoftware или null</returns>
        protected PlcSoftware GetPlcSoftware()
        {
            try
            {
                var plcSoftware = _tiaService.GetPlcSoftware();

                if (plcSoftware == null)
                {
                    _logger.Error("Не удалось получить PlcSoftware из проекта");
                    return null;
                }

                _logger.Info($"PlcSoftware получен успешно: {plcSoftware.Name}");
                return plcSoftware;
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении PlcSoftware: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Проверка соединения с TIA Portal
        /// </summary>
        /// <returns>True, если соединение активно</returns>
        protected bool CheckConnection()
        {
            if (!_tiaService.IsConnected || _tiaService.CurrentProject == null)
            {
                _logger.Error("Соединение с TIA Portal потеряно");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Реализация метода чтения всех тегов
        /// </summary>
        public virtual PlcData ReadAllTags()
        {
            var plcData = new PlcData();

            try
            {
                _logger.Info("ReadAllTags: Чтение тегов из проекта TIA Portal...");

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

                // Проверяем соединение
                if (!CheckConnection())
                {
                    return plcData;
                }

                // Обновляем PlcSoftware после проверки соединения
                plcSoftware = GetPlcSoftware();
                if (plcSoftware == null)
                {
                    return plcData;
                }

                // Читаем блоки данных
                try
                {
                    _logger.Info("ReadAllTags: Начало чтения тегов DB...");
                    int dbTagCount = ReadDataBlocks(plcData);
                    _logger.Info($"ReadAllTags: Теги DB прочитаны успешно, найдено {dbTagCount} тегов");
                }
                catch (Exception ex)
                {
                    _logger.Error($"ReadAllTags: Ошибка при чтении тегов DB: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"ReadAllTags: Общая ошибка при чтении тегов: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.Error($"ReadAllTags: Внутренняя ошибка: {ex.InnerException.Message}");
                }
            }

            return plcData;
        }

        /// <summary>
        /// Абстрактный метод чтения тегов ПЛК
        /// </summary>
        public abstract int ReadPlcTags(PlcData plcData);

        /// <summary>
        /// Абстрактный метод чтения блоков данных
        /// </summary>
        public abstract int ReadDataBlocks(PlcData plcData);
    }
}