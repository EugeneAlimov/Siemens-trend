using System;
using System.Text.RegularExpressions;
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

            // Детальное сопоставление типов
            // Логические типы
            if (lowerType.Contains("bool"))
                return TagDataType.Bool;

            // Целочисленные типы - 16 бит
            else if (lowerType == "int" || lowerType == "int16" || lowerType == "uint16" ||
                     lowerType == "word" || lowerType.Contains("_int"))
                return TagDataType.Int;

            // Целочисленные типы - 32 бит
            else if (lowerType == "dint" || lowerType == "int32" || lowerType == "uint32" ||
                     lowerType == "dword" || lowerType == "udint")
                return TagDataType.DInt;

            // Типы с плавающей точкой
            else if (lowerType.Contains("real") || lowerType.Contains("float"))
                return TagDataType.Real;

            // Строковые типы
            else if (lowerType.Contains("string") || lowerType.Contains("wstring") ||
                     lowerType.Contains("char"))
                return TagDataType.String;

            // Пользовательские типы данных
            else if (lowerType.StartsWith("udt") || lowerType.StartsWith("\"udt") ||
                     lowerType.Contains("struct") || lowerType.Contains("tag_udt"))
                return TagDataType.UDT;

            // По умолчанию
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

        /// <summary>
        /// Проверка, является ли имя контейнера блоком данных
        /// </summary>
        /// <param name="containerName">Имя контейнера</param>
        /// <returns>True, если это блок данных</returns>
        public static bool IsDbContainer(string containerName)
        {
            // Проверяем варианты форматов имени DB
            if (string.IsNullOrEmpty(containerName))
                return false;

            // Точные совпадения с префиксом DB
            if (containerName.StartsWith("DB", StringComparison.OrdinalIgnoreCase))
                return true;

            // Совпадения с _DB в имени
            if (containerName.Contains("_DB", StringComparison.OrdinalIgnoreCase))
                return true;

            // Регулярное выражение для проверки DB+цифры
            if (Regex.IsMatch(containerName, @"DB\d+"))
                return true;

            // Специфичные имена контейнеров, которые являются DB
            if (containerName == "S1" || containerName == "Exchanger_DB")
                return true;

            // По умолчанию считаем, что это не DB
            return false;
        }

        /// <summary>
        /// Разбор имени тега на составные части
        /// </summary>
        /// <param name="fullTagName">Полное имя тега</param>
        /// <returns>Кортеж с контейнером, именем тега и признаком DB</returns>
        public static (string Container, string TagName, bool IsDB) ParseTagName(string fullTagName)
        {
            // Проверка на null или пустую строку
            if (string.IsNullOrEmpty(fullTagName))
            {
                return (string.Empty, string.Empty, false);
            }

            // Формат с кавычками "ContainerName".TagPath или "TagName"
            if (fullTagName.Contains("\""))
            {
                int startQuoteIndex = fullTagName.IndexOf("\"");
                int endQuoteIndex = fullTagName.IndexOf("\"", startQuoteIndex + 1);

                if (startQuoteIndex >= 0 && endQuoteIndex > startQuoteIndex)
                {
                    // Извлекаем имя контейнера (без кавычек)
                    string container = fullTagName.Substring(startQuoteIndex + 1, endQuoteIndex - startQuoteIndex - 1);

                    // Проверяем, есть ли что-то после закрывающей кавычки
                    if (fullTagName.Length > endQuoteIndex + 1)
                    {
                        // Ищем точку после закрывающей кавычки
                        if (fullTagName[endQuoteIndex + 1] == '.')
                        {
                            // Это формат DB - есть путь после контейнера
                            string tagName = fullTagName.Substring(endQuoteIndex + 2); // Пропускаем кавычку и точку
                            return (container, tagName, true);
                        }
                        else
                        {
                            // Необычный формат, возвращаем как есть
                            return (container, fullTagName, false);
                        }
                    }
                    else
                    {
                        // Только имя контейнера в кавычках - это PLC тег
                        return (container, container, false);
                    }
                }
            }
            // Формат с точкой, но без кавычек: ContainerName.TagPath
            else if (fullTagName.Contains("."))
            {
                var parts = fullTagName.Split(new[] { '.' }, 2);

                if (parts.Length >= 2)
                {
                    string container = parts[0];
                    string tagName = parts[1];

                    // Проверяем, является ли контейнер блоком данных
                    bool isDb = IsDbContainer(container);

                    return (container, tagName, isDb);
                }
            }

            // Формат PLC тега без точек и кавычек
            return ("Default", fullTagName, false);
        }
    }
}