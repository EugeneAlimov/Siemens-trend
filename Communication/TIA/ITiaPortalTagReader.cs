using SiemensTrend.Core.Models;

namespace SiemensTrend.Communication.TIA
{
    /// <summary>
    /// Интерфейс для всех читателей тегов TIA Portal
    /// </summary>
    public interface ITiaPortalTagReader
    {
        /// <summary>
        /// Чтение всех тегов из проекта
        /// </summary>
        /// <returns>Объект с данными ПЛК</returns>
        PlcData ReadAllTags();

        /// <summary>
        /// Чтение только тегов ПЛК
        /// </summary>
        /// <param name="plcData">Объект для сохранения тегов</param>
        /// <returns>Количество прочитанных тегов</returns>
        int ReadPlcTags(PlcData plcData);

        /// <summary>
        /// Чтение только блоков данных
        /// </summary>
        /// <param name="plcData">Объект для сохранения тегов</param>
        /// <returns>Количество прочитанных блоков данных</returns>
        int ReadDataBlocks(PlcData plcData);
    }
}