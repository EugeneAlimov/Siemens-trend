using System;
using SiemensTrend.Core.Models;

namespace SiemensTrend.Helpers
{
    /// <summary>
    /// Вспомогательные методы для TiaPortalXmlManager
    /// </summary>
    public partial class TiaPortalXmlManager
    {
        /// <summary>
        /// Конвертация строкового типа данных в TagDataType
        /// </summary>
        private TagDataType ConvertStringToTagDataType(string dataTypeStr)
        {
            if (string.IsNullOrEmpty(dataTypeStr))
                return TagDataType.Other;

            switch (dataTypeStr.ToLower())
            {
                case "bool": return TagDataType.Bool;
                case "int": return TagDataType.Int;
                case "dint": return TagDataType.DInt;
                case "real": return TagDataType.Real;
                case "string": return TagDataType.String;
                case "udt":
                case "udt_":
                    return TagDataType.UDT;
                default:
                    // Дополнительные проверки для сложных случаев
                    if (dataTypeStr.ToLower().Contains("bool")) return TagDataType.Bool;
                    if (dataTypeStr.ToLower().Contains("int") && !dataTypeStr.ToLower().Contains("dint")) return TagDataType.Int;
                    if (dataTypeStr.ToLower().Contains("dint")) return TagDataType.DInt;
                    if (dataTypeStr.ToLower().Contains("real")) return TagDataType.Real;
                    if (dataTypeStr.ToLower().Contains("string")) return TagDataType.String;
                    if (dataTypeStr.ToLower().StartsWith("udt_") || dataTypeStr.ToLower().Contains("type")) return TagDataType.UDT;
                    return TagDataType.Other; // Для неизвестных типов
            }
        }
    }
}