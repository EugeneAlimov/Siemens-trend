using System;
using System.Collections.Generic;
using System.Linq;
using SiemensTrend.Core.Models;

namespace SiemensTrend.Communication.TIA
{
    /// <summary>
    /// Часть сервиса TiaPortalCommunicationService для работы с тегами
    /// </summary>
    public partial class TiaPortalCommunicationService
    {
        /// <summary>
        /// Метод для поиска тегов по строке имени
        /// </summary>
        /// <param name="tagNames">Список имен тегов</param>
        /// <returns>Список найденных тегов</returns>
        public List<TagDefinition> SearchTagsByNames(List<string> tagNames)
        {
            try
            {
                _logger.Info($"Поиск тегов по именам ({tagNames.Count} тегов)");

                if (!_isConnected || _tiaPortal == null || _project == null)
                {
                    _logger.Error("Попытка поиска тегов без подключения к TIA Portal");
                    return new List<TagDefinition>();
                }

                // Создаем объект для поиска тегов
                var tagFinder = new TiaPortalTagFinder(_logger, _tiaPortal);

                // Выполняем поиск
                var foundTags = tagFinder.FindTags(tagNames);

                _logger.Info($"Найдено {foundTags.Count} тегов");

                return foundTags;
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при поиске тегов: {ex.Message}");
                return new List<TagDefinition>();
            }
        }

        /// <summary>
        /// Метод для получения всех тегов из проекта
        /// </summary>
        /// <returns>Список всех тегов</returns>
        public PlcData GetAllTags()
        {
            try
            {
                _logger.Info("Получение всех тегов из проекта");

                if (!_isConnected || _tiaPortal == null || _project == null)
                {
                    _logger.Error("Попытка получения тегов без подключения к TIA Portal");
                    return new PlcData();
                }

                PlcData result = new PlcData();

                // Получаем все теги PLC
                var plcTags = GetAllPlcTags();
                result.PlcTags.AddRange(plcTags);

                // Получаем все теги DB
                var dbTags = GetAllDbTags();
                result.DbTags.AddRange(dbTags);

                _logger.Info($"Получено {result.PlcTags.Count} PLC тегов и {result.DbTags.Count} DB тегов");

                return result;
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении всех тегов: {ex.Message}");
                return new PlcData();
            }
        }

        /// <summary>
        /// Метод для получения всех тегов PLC
        /// </summary>
        private List<TagDefinition> GetAllPlcTags()
        {
            List<TagDefinition> result = new List<TagDefinition>();

            try
            {
                // Перебираем все устройства в проекте
                foreach (var device in _project.Devices)
                {
                    // Перебираем все элементы устройства
                    foreach (var deviceItem in device.DeviceItems)
                    {
                        var softwareContainer = deviceItem.GetService<Siemens.Engineering.HW.Features.SoftwareContainer>();
                        if (softwareContainer != null)
                        {
                            var plcSoftware = softwareContainer.Software as Siemens.Engineering.SW.PlcSoftware;
                            if (plcSoftware != null)
                            {
                                // Перебираем все таблицы тегов
                                foreach (var tagTable in plcSoftware.TagTableGroup.TagTables)
                                {
                                    // Пропускаем системные таблицы
                                    if (tagTable.Name.StartsWith("System", StringComparison.OrdinalIgnoreCase))
                                        continue;

                                    // Перебираем все теги в таблице
                                    foreach (var tag in tagTable.Tags)
                                    {
                                        // Преобразуем тип данных
                                        var dataType = TiaTagTypeUtility.ConvertToTagDataType(tag.DataTypeName);

                                        // Проверяем, поддерживается ли тип тега
                                        if (!TiaTagTypeUtility.IsSupportedTagType(dataType))
                                            continue;

                                        // Создаем объект TagDefinition
                                        var tagDefinition = new TagDefinition
                                        {
                                            Id = Guid.NewGuid(),
                                            Name = tag.Name,
                                            GroupName = tagTable.Name,
                                            Address = tag.LogicalAddress,
                                            DataType = dataType,
                                            IsDbTag = false,
                                            Comment = tag.Comment?.ToString()
                                        };

                                        // Добавляем тег в результат
                                        result.Add(tagDefinition);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении тегов PLC: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Метод для получения всех тегов DB
        /// </summary>
        private List<TagDefinition> GetAllDbTags()
        {
            List<TagDefinition> result = new List<TagDefinition>();

            try
            {
                // Перебираем все устройства в проекте
                foreach (var device in _project.Devices)
                {
                    // Перебираем все элементы устройства
                    foreach (var deviceItem in device.DeviceItems)
                    {
                        var softwareContainer = deviceItem.GetService<Siemens.Engineering.HW.Features.SoftwareContainer>();
                        if (softwareContainer != null)
                        {
                            var plcSoftware = softwareContainer.Software as Siemens.Engineering.SW.PlcSoftware;
                            if (plcSoftware != null)
                            {
                                // Перебираем все блоки данных
                                foreach (var block in plcSoftware.BlockGroup.Blocks)
                                {
                                    // Проверяем, что это блок данных
                                    if (block is Siemens.Engineering.SW.Blocks.PlcBlock plcBlock &&
                                        plcBlock.ProgrammingLanguage == Siemens.Engineering.SW.Blocks.ProgrammingLanguage.DB)
                                    {
                                        // Получаем информацию о блоке данных
                                        bool isOptimized = IsDataBlockOptimized(plcBlock);

                                        // Получаем интерфейс блока данных
                                        var blockInterface = plcBlock.GetAttribute("Interface") as Siemens.Engineering.SW.Blocks.Interface.PlcBlockInterface;
                                        if (blockInterface != null)
                                        {
                                            // Перебираем все члены блока данных
                                            foreach (var member in blockInterface.Members)
                                            {
                                                // Обрабатываем члены блока данных
                                                ProcessDbMember(result, plcBlock.Name, "", member, isOptimized);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении тегов DB: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Рекурсивный метод для обработки члена блока данных
        /// </summary>
        private void ProcessDbMember(List<TagDefinition> tags, string dbName, string parentPath,
            Siemens.Engineering.SW.Blocks.Interface.Member member, bool isOptimized)
        {
            try
            {
                // Формируем путь к текущему члену
                string path = string.IsNullOrEmpty(parentPath) ? member.Name : $"{parentPath}.{member.Name}";

                // Получаем тип данных
                string dataTypeName = GetMemberDataTypeName(member);
                var dataType = TiaTagTypeUtility.ConvertToTagDataType(dataTypeName);

                // Проверяем, поддерживается ли тип тега
                if (TiaTagTypeUtility.IsSupportedTagType(dataType))
                {
                    // Создаем объект TagDefinition
                    var tagDefinition = new TagDefinition
                    {
                        Id = Guid.NewGuid(),
                        Name = path,
                        GroupName = dbName,
                        DataType = dataType,
                        IsDbTag = true,
                        IsOptimized = isOptimized,
                        Comment = member.GetAttribute("Comment")?.ToString()
                    };

                    // Добавляем тег в результат
                    tags.Add(tagDefinition);
                }

                // Рекурсивно обрабатываем вложенные члены
                // В реальном API получение дочерних элементов может отличаться
                foreach (var childMember in GetChildMembers(member))
                {
                    ProcessDbMember(tags, dbName, path, childMember, isOptimized);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при обработке члена блока данных {member.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Вспомогательный метод для получения дочерних элементов Member
        /// </summary>
        /// <summary>
        /// Вспомогательный метод для получения дочерних элементов Member
        /// </summary>
        private IEnumerable<Siemens.Engineering.SW.Blocks.Interface.Member> GetChildMembers(
            Siemens.Engineering.SW.Blocks.Interface.Member member)
        {
            try
            {
                // Check if the member has a method or property to retrieve child members directly
                var childMembers = member.GetAttribute("ChildMembers") as IEnumerable<Siemens.Engineering.SW.Blocks.Interface.Member>;
                if (childMembers != null)
                {
                    return childMembers;
                }

                _logger.Error($"Member does not have accessible child members: {member.Name}");
                return new List<Siemens.Engineering.SW.Blocks.Interface.Member>();
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении дочерних элементов Member: {ex.Message}");
                return new List<Siemens.Engineering.SW.Blocks.Interface.Member>();
            }
        }



        /// <summary>
        /// Интерфейс для доступа к иерархии Member (определяется в зависимости от API)
        /// </summary>
        private interface IMemberHierarchy
        {
            IEnumerable<Siemens.Engineering.SW.Blocks.Interface.Member> GetChildMembers();
        }

        /// <summary>
        /// Получение имени типа данных для члена блока данных
        /// </summary>
        private string GetMemberDataTypeName(Siemens.Engineering.SW.Blocks.Interface.Member member)
        {
            try
            {
                var dataTypeAttr = member.GetAttribute("DataType");
                return dataTypeAttr?.ToString() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Определение, оптимизирован ли блок данных
        /// </summary>
        private bool IsDataBlockOptimized(Siemens.Engineering.SW.Blocks.PlcBlock block)
        {
            try
            {
                var attr = block.GetAttribute("Optimized block access");
                return attr is bool optimized && optimized;
            }
            catch
            {
                return false;
            }
        }
    }
}