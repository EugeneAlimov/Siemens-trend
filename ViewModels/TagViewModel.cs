using System;
using System.Windows.Input;
using SiemensTrend.Core.Commands;
using SiemensTrend.Core.Models;

namespace SiemensTrend.ViewModels
{
    /// <summary>
    /// ViewModel для представления тега в пользовательском интерфейсе
    /// </summary>
    public class TagViewModel : ViewModelBase
    {
        private TagType _tagType;
        private string _group;
        private string _name;
        private TagDataType _dataType;
        private bool _isOptimized;
        private string _comment;
        private bool _isMonitored;

        /// <summary>
        /// Тип тега (PLC или DB)
        /// </summary>
        public TagType TagType
        {
            get => _tagType;
            set => SetProperty(ref _tagType, value);
        }

        /// <summary>
        /// Группа/DB тега
        /// </summary>
        public string Group
        {
            get => _group;
            set => SetProperty(ref _group, value);
        }

        /// <summary>
        /// Имя тега
        /// </summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// Полное имя тега, включая путь
        /// </summary>
        public string FullName
        {
            get => TagType == TagType.DB
                ? $"\"{Group}\".{Name}"
                : $"\"{Group}\"";
        }

        /// <summary>
        /// Тип данных
        /// </summary>
        public TagDataType DataType
        {
            get => _dataType;
            set => SetProperty(ref _dataType, value);
        }

        /// <summary>
        /// Флаг оптимизации (для DB)
        /// </summary>
        public bool IsOptimized
        {
            get => _isOptimized;
            set => SetProperty(ref _isOptimized, value);
        }

        /// <summary>
        /// Комментарий
        /// </summary>
        public string Comment
        {
            get => _comment;
            set => SetProperty(ref _comment, value);
        }

        /// <summary>
        /// Флаг добавления в мониторинг
        /// </summary>
        public bool IsMonitored
        {
            get => _isMonitored;
            set => SetProperty(ref _isMonitored, value);
        }

        /// <summary>
        /// Ссылка на исходный тег
        /// </summary>
        public TagDefinition Tag { get; set; }

        /// <summary>
        /// Команда для добавления в мониторинг
        /// </summary>
        public ICommand AddToMonitoringCommand { get; set; }

        /// <summary>
        /// Команда для удаления из мониторинга
        /// </summary>
        public ICommand RemoveFromMonitoringCommand { get; set; }

        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        public TagViewModel()
        {
        }

        /// <summary>
        /// Конструктор с параметрами
        /// </summary>
        public TagViewModel(TagDefinition tag)
        {
            if (tag == null)
                throw new ArgumentNullException(nameof(tag));

            TagType = tag.IsDbTag ? TagType.DB : TagType.PLC;
            Group = tag.GroupName;
            Name = tag.Name;
            DataType = tag.DataType;
            IsOptimized = tag.IsOptimized;
            Comment = tag.Comment;
            Tag = tag;
        }
    }
}