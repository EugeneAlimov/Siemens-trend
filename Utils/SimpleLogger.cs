using System;
using System.IO;
using System.Text;

namespace SiemensTagExporter.Utils
{
    /// <summary>
    /// Тип сообщения лога
    /// </summary>
    public enum LogLevel
    {
        Debug,
        Info,
        Warn,
        Error
    }

    /// <summary>
    /// Аргументы события логирования
    /// </summary>
    public class LogEventArgs : EventArgs
    {
        public string Message { get; }
        public LogLevel Level { get; }
        public DateTime Timestamp { get; }

        public LogEventArgs(string message, LogLevel level)
        {
            Message = message;
            Level = level;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Простой класс для логирования
    /// </summary>
    public class SimpleLogger : ILogger
    {
        private readonly object _lockObj = new object();
        private readonly StringBuilder _logBuffer = new StringBuilder();
        private string _logFilePath;

        /// <summary>
        /// Событие логирования
        /// </summary>
        public event EventHandler<LogEventArgs> LogEvent;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="logToFile">Логировать ли в файл</param>
        public SimpleLogger(bool logToFile = false)
        {
            if (logToFile)
            {
                SetupFileLogging();
            }
        }

        /// <summary>
        /// Настройка логирования в файл
        /// </summary>
        private void SetupFileLogging()
        {
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            _logFilePath = Path.Combine(logDir, $"Log_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");
        }

        /// <summary>
        /// Логирование сообщения уровня Debug
        /// </summary>
        public void Debug(string message)
        {
            Log(message, LogLevel.Debug);
        }

        /// <summary>
        /// Логирование сообщения уровня Info
        /// </summary>
        public void Info(string message)
        {
            Log(message, LogLevel.Info);
        }

        /// <summary>
        /// Логирование сообщения уровня Warn
        /// </summary>
        public void Warn(string message)
        {
            Log(message, LogLevel.Warn);
        }

        /// <summary>
        /// Логирование сообщения уровня Error
        /// </summary>
        public void Error(string message)
        {
            Log(message, LogLevel.Error);
        }

        /// <summary>
        /// Логирование сообщения с указанным уровнем
        /// </summary>
        private void Log(string message, LogLevel level)
        {
            if (string.IsNullOrEmpty(message))
                return;

            string formattedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";

            // Добавляем сообщение в буфер
            lock (_lockObj)
            {
                _logBuffer.AppendLine(formattedMessage);

                // Если буфер стал слишком большим, записываем его в файл и очищаем
                if (_logBuffer.Length > 10000 && !string.IsNullOrEmpty(_logFilePath))
                {
                    try
                    {
                        File.AppendAllText(_logFilePath, _logBuffer.ToString());
                        _logBuffer.Clear();
                    }
                    catch
                    {
                        // Игнорируем ошибки записи в файл
                    }
                }
            }

            // Генерируем событие логирования
            OnLogEvent(new LogEventArgs(message, level));
        }

        /// <summary>
        /// Вызов события логирования
        /// </summary>
        protected virtual void OnLogEvent(LogEventArgs e)
        {
            LogEvent?.Invoke(this, e);
        }

        /// <summary>
        /// Сохранение лога в файл
        /// </summary>
        public void SaveLogToFile(string filePath = null)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = _logFilePath;
            }

            if (string.IsNullOrEmpty(filePath))
            {
                string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                filePath = Path.Combine(logDir, $"Log_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");
            }

            lock (_lockObj)
            {
                try
                {
                    File.AppendAllText(filePath, _logBuffer.ToString());
                    _logBuffer.Clear();
                }
                catch (Exception ex)
                {
                    OnLogEvent(new LogEventArgs($"Ошибка при сохранении лога в файл: {ex.Message}", LogLevel.Error));
                }
            }
        }
    }
}