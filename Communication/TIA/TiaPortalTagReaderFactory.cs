using System;
using SiemensTrend.Core.Logging;

namespace SiemensTrend.Communication.TIA
{
    /// <summary>
    /// Улучшенная фабрика для создания читателей тегов TIA Portal
    /// с поддержкой отказоустойчивого режима работы
    /// </summary>
    public class TiaPortalTagReaderFactory
    {
        /// <summary>
        /// Режимы чтения тегов из TIA Portal
        /// </summary>
        public enum ReaderMode
        {
            /// <summary>
            /// Стандартный режим чтения с полной обработкой
            /// </summary>
            Standard,

            /// <summary>
            /// Защищенный режим с ограничениями для повышения стабильности
            /// </summary>
            SafeMode,

            /// <summary>
            /// Минимальный режим, только базовая информация
            /// </summary>
            MinimalMode
        }

        /// <summary>
        /// Создать комбинированный читатель тегов
        /// </summary>
        /// <param name="logger">Логгер</param>
        /// <param name="tiaService">Сервис коммуникации с TIA Portal</param>
        /// <param name="mode">Режим работы читателя тегов</param>
        /// <returns>Экземпляр комбинированного читателя тегов</returns>
        public static ITiaPortalTagReader CreateTagReader(
            Logger logger,
            TiaPortalCommunicationService tiaService,
            ReaderMode mode = ReaderMode.SafeMode)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            if (tiaService == null)
                throw new ArgumentNullException(nameof(tiaService));

            logger.Info($"TiaPortalTagReaderFactory: Создание читателя тегов в режиме {mode}");

            // Для всех режимов используем одинаковый класс, но с разными настройками
            return new TiaPortalTagReader(logger, tiaService, mode);
        }

        /// <summary>
        /// Создать специализированный читатель тегов ПЛК
        /// </summary>
        /// <param name="logger">Логгер</param>
        /// <param name="tiaService">Сервис коммуникации с TIA Portal</param>
        /// <param name="mode">Режим работы читателя тегов</param>
        /// <returns>Экземпляр читателя тегов ПЛК</returns>
        public static TiaPortalPlcTagReader CreatePlcTagReader(
            Logger logger,
            TiaPortalCommunicationService tiaService,
            ReaderMode mode = ReaderMode.SafeMode)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            if (tiaService == null)
                throw new ArgumentNullException(nameof(tiaService));

            logger.Info($"TiaPortalTagReaderFactory: Создание читателя тегов ПЛК в режиме {mode}");

            return new TiaPortalPlcTagReader(logger, tiaService);
        }

        /// <summary>
        /// Создать специализированный читатель блоков данных
        /// </summary>
        /// <param name="logger">Логгер</param>
        /// <param name="tiaService">Сервис коммуникации с TIA Portal</param>
        /// <param name="mode">Режим работы читателя тегов</param>
        /// <returns>Экземпляр читателя блоков данных</returns>
        public static TiaPortalDbTagReader CreateDbTagReader(
            Logger logger,
            TiaPortalCommunicationService tiaService,
            ReaderMode mode = ReaderMode.SafeMode)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            if (tiaService == null)
                throw new ArgumentNullException(nameof(tiaService));

            logger.Info($"TiaPortalTagReaderFactory: Создание читателя блоков данных в режиме {mode}");

            return new TiaPortalDbTagReader(logger, tiaService);
        }
    }
}