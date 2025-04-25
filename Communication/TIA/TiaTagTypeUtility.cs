using SiemensTrend.Core.Models;

namespace SiemensTrend.Communication.TIA
{
    /// <summary>
    /// Утилитный класс для работы с типами тегов TIA Portal
    /// </summary>
    public static class TiaTagTypeUtility
    {
        /// <summary>
        /// Конвертация строкового типа данных в TagDataType
        /// </summary>
        /// <param name="dataTypeString">Строковое представление типа данных</param>
        /// <returns>Соответствующий тип TagDataType</returns>
        public static TagDataType ConvertToTagDataType(string dataTypeString)
        {
            if (string.IsNullOrEmpty(dataTypeString))
                return TagDataType.Other;

            string lowerType = dataTypeString.ToLower();

            if (lowerType.Contains("bool"))
                return TagDataType.Bool;
            else if (lowerType.Contains("int") && !lowerType.Contains("dint"))
                return TagDataType.Int;
            else if (lowerType.Contains("dint"))
                return TagDataType.DInt;
            else if (lowerType.Contains("real"))
                return TagDataType.Real;
            else if (lowerType.Contains("string"))
                return TagDataType.String;
            else if (lowerType.StartsWith("udt_") || lowerType.Contains("type"))
                return TagDataType.UDT;
            else
                return TagDataType.Other;
        }

        /// <summary>
        /// Проверка, поддерживается ли тип тега
        /// </summary>
        /// <param name="dataType">Тип тега</param>
        /// <returns>True, если тип поддерживается</returns>
        public static bool IsSupportedTagType(TagDataType dataType)
        {
            // По требованиям только bool, int, dint, real
            return dataType == TagDataType.Bool ||
                   dataType == TagDataType.Int ||
                   dataType == TagDataType.DInt ||
                   dataType == TagDataType.Real;
        }
    }
}