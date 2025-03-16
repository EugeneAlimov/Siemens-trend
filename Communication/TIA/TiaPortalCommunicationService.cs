// Добавьте эти методы в существующий класс TiaPortalCommunicationService

using Siemens.Engineering.HW.Features;
using Siemens.Engineering.SW;
using Siemens.Engineering;
using SiemensTrend.Communication.TIA;

using SiemensTrend.Core.Models;

/// <summary>
/// Получение PlcSoftware из проекта
/// </summary>
/// <returns>Объект PlcSoftware или null, если не найден</returns>
public PlcSoftware GetPlcSoftware()
{
    if (_project == null)
    {
        _logger.Error("Ошибка: Проект TIA Portal не открыт.");
        return null;
    }

    _logger.Info($"Поиск PLC Software в проекте {_project.Name}...");

    try
    {
        foreach (var device in _project.Devices)
        {
            _logger.Info($"Проверка устройства: {device.Name}");

            foreach (var deviceItem in device.DeviceItems)
            {
                var softwareContainer = deviceItem.GetService<SoftwareContainer>();

                if (softwareContainer?.Software is PlcSoftware plcSoftware)
                {
                    _logger.Info($"✅ Найден PLC Software в устройстве: {device.Name}");
                    return plcSoftware;
                }
            }
        }
    }
    catch (Exception ex)
    {
        _logger.Error($"Ошибка при поиске PLC Software: {ex.Message}");
    }

    _logger.Error("❌ Ошибка: PLC Software не найдено в проекте.");
    return null;
}

/// <summary>
/// Загрузка и возврат всех тегов проекта
/// </summary>
/// <returns>Объект PlcData, содержащий теги ПЛК и DB</returns>
public async Task<PlcData> GetAllProjectTagsAsync()
{
    if (!IsConnected || _project == null)
    {
        _logger.Error("Попытка получения тегов без подключения к TIA Portal");
        return new PlcData();
    }

    try
    {
        _logger.Info("Запуск чтения всех тегов проекта...");

        // Создаем считыватель тегов
        var reader = new TiaPortalTagReader(_logger, this);

        // Получаем все теги
        var plcData = await reader.ReadAllTagsAsync();

        _logger.Info($"Загружено {plcData.PlcTags.Count} тегов ПЛК и {plcData.DbTags.Count} тегов DB");

        return plcData;
    }
    catch (Exception ex)
    {
        _logger.Error($"Ошибка при получении всех тегов проекта: {ex.Message}");
        if (ex.InnerException != null)
        {
            _logger.Error($"Внутренняя ошибка: {ex.InnerException.Message}");
        }
        return new PlcData();
    }
}

/// <summary>
/// Получение списка тегов для отображения в TagBrowserViewModel
/// </summary>
/// <returns>Список тегов для отображения</returns>
public async Task<List<TagDefinition>> GetTagsFromProjectAsync()
{
    try
    {
        var plcData = await GetAllProjectTagsAsync();
        return plcData.AllTags;
    }
    catch (Exception ex)
    {
        _logger.Error($"Ошибка при получении тегов для отображения: {ex.Message}");
        return new List<TagDefinition>();
    }
}

/// <summary>
/// Получение только тегов ПЛК из проекта
/// </summary>
/// <returns>Список тегов ПЛК</returns>
public async Task<List<TagDefinition>> GetPlcTagsAsync()
{
    try
    {
        var plcData = await GetAllProjectTagsAsync();
        return plcData.PlcTags;
    }
    catch (Exception ex)
    {
        _logger.Error($"Ошибка при получении тегов ПЛК: {ex.Message}");
        return new List<TagDefinition>();
    }
}

/// <summary>
/// Получение только тегов блоков данных из проекта
/// </summary>
/// <returns>Список тегов блоков данных</returns>
public async Task<List<TagDefinition>> GetDbTagsAsync()
{
    try
    {
        var plcData = await GetAllProjectTagsAsync();
        return plcData.DbTags;
    }
    catch (Exception ex)
    {
        _logger.Error($"Ошибка при получении тегов блоков данных: {ex.Message}");
        return new List<TagDefinition>();
    }
}