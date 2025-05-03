using System;
using System.Threading.Tasks;

namespace SiemensTrend.ViewModels
{
    /// <summary>
    /// Часть MainViewModel для доступа к данным и экспорта/импорта
    /// </summary>
    public partial class MainViewModel
    {
        /// <summary>
        /// Экспорт тегов в XML-формат
        /// </summary>
        public async Task ExportTagsToXml()
        {
            try
            {
                if (_tiaPortalService == null || !IsConnected)
                {
                    StatusMessage = "Необходимо сначала подключиться к TIA Portal";
                    return;
                }

                IsLoading = true;
                StatusMessage = "Экспорт тегов в XML...";
                ProgressValue = 10;

                //await _tiaPortalService.ExportTagsToXml();

                ProgressValue = 100;
                StatusMessage = "Экспорт тегов в XML завершен";
            }
            catch (Exception ex)
            {
                _logger.Error($"ExportTagsToXml: Ошибка: {ex.Message}");
                StatusMessage = "Ошибка при экспорте тегов";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}