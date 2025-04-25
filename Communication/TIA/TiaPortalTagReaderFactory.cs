using System;
using SiemensTrend.Core.Logging;
using SiemensTrend.Core.Models;

namespace SiemensTrend.Communication.TIA
{
    /// <summary>
    /// Фабрика для создания читателей тегов TIA Portal
    /// </summary>
    public class TiaPortalTagReaderFactory
    {
        /// <summary>
        /// Создать комбинированный читатель тегов
        /// </summary>
        /// <param name="logger">Логгер</param>
        /// <param name="tiaService">Сервис коммуникации с TIA Portal</param>
        /// <returns>Экземпляр комбинированного читателя тегов</returns>
        public static TiaPortalTagReader CreateTagReader(
            Logger logger,
            TiaPortalCommunicationService tiaService)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            if (tiaService == null)
                throw new ArgumentNullException(nameof(tiaService));

            return new TiaPortalTagReader(logger, tiaService);
        }

        /// <summary>
        /// Создать специализированный читатель тегов ПЛК
        /// </summary>
        /// <param name="logger">Логгер</param>
        /// <param name="tiaService">Сервис коммуникации с TIA Portal</param>
        /// <returns>Экземпляр читателя тегов ПЛК</returns>
        public static TiaPortalPlcTagReader CreatePlcTagReader(
            Logger logger,
            TiaPortalCommunicationService tiaService)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            if (tiaService == null)
                throw new ArgumentNullException(nameof(tiaService));

            return new TiaPortalPlcTagReader(logger, tiaService);
        }

        /// <summary>
        /// Создать специализированный читатель блоков данных
        /// </summary>
        /// <param name="logger">Логгер</param>
        /// <param name="tiaService">Сервис коммуникации с TIA Portal</param>
        /// <returns>Экземпляр читателя блоков данных</returns>
        public static TiaPortalDbTagReader CreateDbTagReader(
            Logger logger,
            TiaPortalCommunicationService tiaService)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            if (tiaService == null)
                throw new ArgumentNullException(nameof(tiaService));

            return new TiaPortalDbTagReader(logger, tiaService);
        }
    }
}