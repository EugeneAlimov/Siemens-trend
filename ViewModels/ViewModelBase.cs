using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SiemensTrend.ViewModels
{
    /// <summary>
    /// Базовый класс для всех моделей представления
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        /// <summary>
        /// Событие изменения свойства
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Метод вызова события PropertyChanged
        /// </summary>
        /// <param name="propertyName">Имя свойства (подставляется автоматически)</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Метод для установки значения свойства с вызовом события PropertyChanged
        /// </summary>
        /// <typeparam name="T">Тип свойства</typeparam>
        /// <param name="field">Ссылка на поле</param>
        /// <param name="value">Новое значение</param>
        /// <param name="propertyName">Имя свойства (подставляется автоматически)</param>
        /// <returns>True, если значение изменилось</returns>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            // Если значение не изменилось, возвращаем false
            if (Equals(field, value))
                return false;

            // Устанавливаем новое значение
            field = value;

            // Вызываем событие PropertyChanged
            OnPropertyChanged(propertyName);

            return true;
        }
    }
}