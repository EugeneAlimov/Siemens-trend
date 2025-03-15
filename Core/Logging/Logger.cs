using System;
using System.IO;

namespace SiemensTrend.Core.Logging
{
    /// <summary>
    /// Простой логер для записи сообщений в файл и консоль
    /// </summary>
    public class Logger
    {
        // Путь к файлу лога
        private readonly string _logFilePath;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="logFileName">Имя файла лога (по умолчанию "log.txt")</param>
        public Logger(string logFileName = "log.txt")
        {
            // Создаем путь к файлу в папке программы
            _logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logFileName);
        }

        /// <summary>
        /// Записывает отладочное сообщение
        /// </summary>
        public void Debug(string message)
        {
            WriteLog("DEBUG", message);
        }

        /// <summary>
        /// Записывает информационное сообщение
        /// </summary>
        public void Info(string message)
        {
            WriteLog("INFO", message);
        }

        /// <summary>
        /// Записывает предупреждение
        /// </summary>
        public void Warn(string message)
        {
            WriteLog("WARN", message);
        }

        /// <summary>
        /// Записывает сообщение об ошибке
        /// </summary>
        public void Error(string message)
        {
            WriteLog("ERROR", message);
        }

        /// <summary>
        /// Внутренний метод для записи сообщения в лог
        /// </summary>
        private void WriteLog(string level, string message)
        {
            try
            {
                // Формируем сообщение с временем и уровнем
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string logMessage = $"[{timestamp}] [{level}] {message}";

                // Записываем в файл
                File.AppendAllText(_logFilePath, logMessage + Environment.NewLine);

                // Выводим в консоль
                Console.WriteLine(logMessage);
            }
            catch (Exception ex)
            {
                // В случае ошибки записи в файл, хотя бы выводим в консоль
                Console.WriteLine($"Ошибка записи в лог: {ex.Message}");
            }
        }
    }
}