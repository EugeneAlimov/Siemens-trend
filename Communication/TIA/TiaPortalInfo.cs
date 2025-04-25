using Siemens.Engineering;

namespace SiemensTrend.Communication.TIA
{
    /// <summary>
    /// Информация о проекте TIA Portal
    /// </summary>
    public class TiaProjectInfo
    {
        /// <summary>
        /// Имя проекта
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Путь к проекту
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Процесс TIA Portal
        /// </summary>
        public TiaPortalProcess TiaProcess { get; set; }

        /// <summary>
        /// Экземпляр TIA Portal
        /// </summary>
        public TiaPortal TiaPortalInstance { get; set; }

        /// <summary>
        /// Объект проекта
        /// </summary>
        public Project Project { get; set; }

        /// <summary>
        /// Строковое представление
        /// </summary>
        public override string ToString()
        {
            return Name ?? "Unnamed Project";
        }
    }
}