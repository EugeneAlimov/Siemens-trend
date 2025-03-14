using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Siemens.Engineering;
using Siemens.Engineering.SW;
using SiemensTagExporter.Utils;

namespace SiemensTagExporter.ViewModel
{
    /// <summary>
    /// Аргументы события изменения статуса
    /// </summary>
    public class StatusEventArgs : EventArgs
    {
        public string Message { get; }
        public int ProgressValue { get; }

        public StatusEventArgs(string message, int progressValue = 0)
        {
            Message = message;
            ProgressValue = progressValue;
        }
    }

    /// <summary>
    /// Основная модель представления для главного окна
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        #region Поля

        private readonly TiaPortalHelper _tiaHelper;
        private readonly ILogger _logger;

        private bool _isConnected;
        private bool _isLoading;
        private string _projectName;
        private PlcInfo _selectedPlc;
        private DbInfo _selectedDb;
        private int _progressValue;
        private string _statusMessage;

        #endregion

        #region Свойства

        /// <summary>
        /// Статус подключения к TIA Portal
        /// </summary>
        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    OnPropertyChanged(nameof(IsConnected));
                }
            }
        }

        /// <summary>
        /// Статус загрузки (выполнения операции)
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged(nameof(IsLoading));
                }
            }
        }

        /// <summary>
        /// Имя текущего проекта
        /// </summary>
        public string ProjectName
        {
            get => _projectName;
            set
            {
                if (_projectName != value)
                {
                    _projectName = value;
                    OnPropertyChanged(nameof(ProjectName));
                }
            }
        }

        /// <summary>
        /// Выбранный ПЛК
        /// </summary>
        public PlcInfo SelectedPlc
        {
            get => _selectedPlc;
            set
            {
                if (_selectedPlc != value)
                {
                    _selectedPlc = value;
                    OnPropertyChanged(nameof(SelectedPlc));

                    // Сбрасываем выбранный DB и списки тегов при смене ПЛК
                    if (value != null)
                    {
                        SelectedDb = null;
                        PlcTags = new ObservableCollection<PlcTagInfo>();
                        DbTags = new ObservableCollection<DbTagInfo>();
                    }
                }
            }
        }

        /// <summary>
        /// Выбранный блок данных
        /// </summary>
        public DbInfo SelectedDb
        {
            get => _selectedDb;
            set
            {
                if (_selectedDb != value)
                {
                    _selectedDb = value;
                    OnPropertyChanged(nameof(SelectedDb));

                    // Сбрасываем список тегов DB при смене DB
                    if (value != null)
                    {
                        DbTags = new ObservableCollection<DbTagInfo>();
                    }
                }
            }
        }

        /// <summary>
        /// Значение прогресса операции (0-100)
        /// </summary>
        public int ProgressValue
        {
            get => _progressValue;
            set
            {
                if (_progressValue != value)
                {
                    _progressValue = value;
                    OnPropertyChanged(nameof(ProgressValue));
                }
            }
        }

        /// <summary>
        /// Сообщение о текущем статусе
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged(nameof(StatusMessage));
                    OnStatusChanged(new StatusEventArgs(value, ProgressValue));
                }
            }
        }

        /// <summary>
        /// Список доступных ПЛК
        /// </summary>
        public ObservableCollection<PlcInfo> Plcs { get; set; } = new ObservableCollection<PlcInfo>();

        /// <summary>
        /// Список блоков данных
        /// </summary>
        public ObservableCollection<DbInfo> DataBlocks { get; set; } = new ObservableCollection<DbInfo>();

        /// <summary>
        /// Список тегов ПЛК
        /// </summary>
        public ObservableCollection<PlcTagInfo> PlcTags { get; set; } = new ObservableCollection<PlcTagInfo>();

        /// <summary>
        /// Список тегов DB
        /// </summary>
        public ObservableCollection<DbTagInfo> DbTags { get; set; } = new ObservableCollection<DbTagInfo>();

        #endregion

        #region События

        /// <summary>
        /// Событие изменения свойства
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Событие изменения статуса
        /// </summary>
        public event EventHandler<StatusEventArgs> StatusChanged;

        #endregion

        #region Конструктор

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="logger">Логгер</param>
        public MainViewModel(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tiaHelper = new TiaPortalHelper(_logger);

            // Подписываемся на события TiaPortalHelper
            _tiaHelper.ProgressChanged += TiaHelper_ProgressChanged;
            _tiaHelper.Connected += TiaHelper_Connected;
            _tiaHelper.Disconnected += TiaHelper_Disconnected;

            // Инициализация начальных значений
            IsConnected = false;
            IsLoading = false;
            ProjectName = "Нет подключения";
            StatusMessage = "Готово к работе";
        }

        #endregion

        #region Обработчики событий TiaPortalHelper

        /// <summary>
        /// Обработчик события изменения прогресса
        /// </summary>
        private void TiaHelper_ProgressChanged(int percent, string message)
        {
            ProgressValue = percent;
            StatusMessage = message;
        }

        /// <summary>
        /// Обработчик события подключения
        /// </summary>
        private void TiaHelper_Connected(object sender, EventArgs e)
        {
            IsConnected = true;
            ProjectName = _tiaHelper.ProjectName;
            StatusMessage = $"Подключено к проекту: {ProjectName}";
        }

        /// <summary>
        /// Обработчик события отключения
        /// </summary>
        private void TiaHelper_Disconnected(object sender, EventArgs e)
        {
            IsConnected = false;
            ProjectName = "Нет подключения";
            StatusMessage = "Отключено от TIA Portal";

            // Очищаем все коллекции
            Plcs.Clear();
            DataBlocks.Clear();
            PlcTags.Clear();
            DbTags.Clear();

            // Сбрасываем выбранные элементы
            SelectedPlc = null;
            SelectedDb = null;
        }

        #endregion

        #region Публичные методы

        /// <summary>
        /// Асинхронное подключение к TIA Portal
        /// </summary>
        public async Task ConnectAsync()
        {
            if (IsLoading || IsConnected) return;

            IsLoading = true;
            StatusMessage = "Подключение к TIA Portal...";

            try
            {
                bool result = await Task.Run(() => _tiaHelper.Connect(true));

                if (!result)
                {
                    MessageBox.Show("Не удалось подключиться к TIA Portal. Проверьте, запущен ли TIA Portal и открыт ли проект.",
                        "Ошибка подключения", MessageBoxButton.OK, MessageBoxImage.Warning);

                    StatusMessage = "Ошибка подключения";
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при подключении: {ex.Message}");
                MessageBox.Show($"Ошибка при подключении: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

                StatusMessage = "Ошибка подключения";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Отключение от TIA Portal
        /// </summary>
        public void Disconnect()
        {
            if (IsLoading || !IsConnected) return;

            try
            {
                _tiaHelper.Disconnect();
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при отключении: {ex.Message}");
                MessageBox.Show($"Ошибка при отключении: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Асинхронное получение списка ПЛК
        /// </summary>
        public async Task GetPlcsAsync()
        {
            if (IsLoading || !IsConnected) return;

            IsLoading = true;
            StatusMessage = "Получение списка ПЛК...";
            ProgressValue = 0;

            try
            {
                var plcList = await Task.Run(() => _tiaHelper.GetAvailablePlcs());

                Plcs.Clear();
                foreach (var plc in plcList)
                {
                    Plcs.Add(plc);
                }

                if (Plcs.Count > 0)
                {
                    SelectedPlc = Plcs[0];
                    StatusMessage = $"Найдено ПЛК: {Plcs.Count}";
                }
                else
                {
                    StatusMessage = "Не найдено ни одного ПЛК";
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении списка ПЛК: {ex.Message}");
                MessageBox.Show($"Ошибка при получении списка ПЛК: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

                StatusMessage = "Ошибка при получении списка ПЛК";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Асинхронное получение тегов ПЛК
        /// </summary>
        public async Task GetPlcTagsAsync()
        {
            if (IsLoading || SelectedPlc == null) return;

            IsLoading = true;
            StatusMessage = $"Получение тегов ПЛК {SelectedPlc.Name}...";
            ProgressValue = 0;

            try
            {
                var tagsList = await _tiaHelper.GetAllPlcTagsAsync(SelectedPlc.Software);

                PlcTags.Clear();
                foreach (var tag in tagsList)
                {
                    PlcTags.Add(tag);
                }

                StatusMessage = $"Получено тегов ПЛК: {PlcTags.Count}";
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении тегов ПЛК: {ex.Message}");
                MessageBox.Show($"Ошибка при получении тегов ПЛК: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

                StatusMessage = "Ошибка при получении тегов ПЛК";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Асинхронное получение блоков данных
        /// </summary>
        public async Task GetDataBlocksAsync()
        {
            if (IsLoading || SelectedPlc == null) return;

            IsLoading = true;
            StatusMessage = $"Получение блоков данных ПЛК {SelectedPlc.Name}...";
            ProgressValue = 0;

            try
            {
                var dbList = await _tiaHelper.GetAllDataBlocksAsync(SelectedPlc.Software);

                DataBlocks.Clear();
                foreach (var db in dbList)
                {
                    DataBlocks.Add(db);
                }

                if (DataBlocks.Count > 0)
                {
                    SelectedDb = DataBlocks[0];
                    StatusMessage = $"Получено блоков данных: {DataBlocks.Count}";
                }
                else
                {
                    StatusMessage = "Не найдено ни одного блока данных";
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении блоков данных: {ex.Message}");
                MessageBox.Show($"Ошибка при получении блоков данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

                StatusMessage = "Ошибка при получении блоков данных";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Асинхронное получение тегов DB
        /// </summary>
        public async Task GetDbTagsAsync()
        {
            if (IsLoading || SelectedDb == null) return;

            IsLoading = true;
            StatusMessage = $"Получение тегов DB {SelectedDb.Name}...";
            ProgressValue = 0;

            try
            {
                var tagsList = await _tiaHelper.GetDataBlockTagsAsync(SelectedDb.Instance);

                DbTags.Clear();
                foreach (var tag in tagsList)
                {
                    DbTags.Add(tag);
                }

                StatusMessage = $"Получено тегов DB: {DbTags.Count}";
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении тегов DB: {ex.Message}");
                MessageBox.Show($"Ошибка при получении тегов DB: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

                StatusMessage = "Ошибка при получении тегов DB";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Асинхронный экспорт тегов в файл
        /// </summary>
        public async Task ExportTagsAsync(string filePath)
        {
            if (IsLoading) return;

            IsLoading = true;
            StatusMessage = "Экспорт тегов...";
            ProgressValue = 0;

            try
            {
                // Определяем тип экспорта (PLC теги, DB теги или оба)
                bool hasPlcTags = PlcTags.Count > 0;
                bool hasDbTags = DbTags.Count > 0;

                if (hasPlcTags && !hasDbTags)
                {
                    // Экспорт только тегов ПЛК
                    await Task.Run(() => _tiaHelper.ExportPlcTagsToCsv(PlcTags.ToList(), filePath));
                    StatusMessage = $"Экспортировано тегов ПЛК: {PlcTags.Count}";
                }
                else if (!hasPlcTags && hasDbTags)
                {
                    // Экспорт только тегов DB
                    await Task.Run(() => _tiaHelper.ExportDbTagsToCsv(DbTags.ToList(), filePath));
                    StatusMessage = $"Экспортировано тегов DB: {DbTags.Count}";
                }
                else if (hasPlcTags && hasDbTags)
                {
                    // Экспорт обоих типов тегов в разные файлы
                    string plcTagsPath = filePath.Replace(".csv", "_PlcTags.csv");
                    string dbTagsPath = filePath.Replace(".csv", "_DbTags.csv");

                    await Task.Run(() =>
                    {
                        _tiaHelper.ExportPlcTagsToCsv(PlcTags.ToList(), plcTagsPath);
                        ProgressValue = 50;
                        _tiaHelper.ExportDbTagsToCsv(DbTags.ToList(), dbTagsPath);
                    });

                    StatusMessage = $"Экспортировано тегов ПЛК: {PlcTags.Count}, тегов DB: {DbTags.Count}";
                    MessageBox.Show($"Теги ПЛК экспортированы в файл: {plcTagsPath}\n" +
                                  $"Теги DB экспортированы в файл: {dbTagsPath}",
                        "Экспорт завершен", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Нет тегов для экспорта.",
                        "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                    StatusMessage = "Нет тегов для экспорта";
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при экспорте тегов: {ex.Message}");
                MessageBox.Show($"Ошибка при экспорте тегов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

                StatusMessage = "Ошибка при экспорте тегов";
            }
            finally
            {
                IsLoading = false;
                ProgressValue = 100;
            }
        }

        #endregion

        #region Вспомогательные методы

        /// <summary>
        /// Вызов события изменения свойства
        /// </summary>
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Вызов события изменения статуса
        /// </summary>
        protected void OnStatusChanged(StatusEventArgs e)
        {
            StatusChanged?.Invoke(this, e);
        }

        #endregion
    }
}