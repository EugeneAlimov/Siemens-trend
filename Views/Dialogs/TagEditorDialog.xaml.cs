using System;
using System.Windows;
using SiemensTrend.Core.Models;

namespace SiemensTrend.Views.Dialogs
{
    /// <summary>
    /// Логика взаимодействия для TagEditorDialog.xaml
    /// </summary>
    public partial class TagEditorDialog : Window
    {
        /// <summary>
        /// Режим диалога (создание или редактирование)
        /// </summary>
        public enum DialogMode
        {
            Create,
            Edit
        }

        private readonly DialogMode _mode;
        private TagDefinition _originalTag;

        /// <summary>
        /// Результирующий тег
        /// </summary>
        public new TagDefinition Tag { get; private set; }

        /// <summary>
        /// Конструктор для создания нового тега
        /// </summary>
        public TagEditorDialog()
        {
            InitializeComponent();
            _mode = DialogMode.Create;
            Title = "Создание нового тега";

            // Устанавливаем значения по умолчанию
            cmbDataType.SelectedIndex = 0; // Bool
        }

        /// <summary>
        /// Конструктор для редактирования существующего тега
        /// </summary>
        /// <param name="tag">Тег для редактирования</param>
        public TagEditorDialog(TagDefinition tag)
        {
            InitializeComponent();
            _mode = DialogMode.Edit;
            _originalTag = tag;
            Title = "Редактирование тега";

            // Заполняем поля данными тега
            txtName.Text = tag.Name;
            txtAddress.Text = tag.Address;
            txtGroup.Text = tag.GroupName;
            txtComment.Text = tag.Comment;

            // Выбираем тип данных
            switch (tag.DataType)
            {
                case TagDataType.Bool:
                    cmbDataType.SelectedIndex = 0;
                    break;
                case TagDataType.Int:
                    cmbDataType.SelectedIndex = 1;
                    break;
                case TagDataType.DInt:
                    cmbDataType.SelectedIndex = 2;
                    break;
                case TagDataType.Real:
                    cmbDataType.SelectedIndex = 3;
                    break;
                default:
                    cmbDataType.SelectedIndex = 4; // Other
                    break;
            }

            // Выбираем тип тега на основе свойства IsDbTag
            rbPlcTag.IsChecked = !tag.IsDbTag;
            rbDbTag.IsChecked = tag.IsDbTag;
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Отмена"
        /// </summary>
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Сохранить"
        /// </summary>
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверяем обязательные поля
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show("Пожалуйста, введите имя тега", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtName.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtAddress.Text))
                {
                    MessageBox.Show("Пожалуйста, введите адрес тега", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtAddress.Focus();
                    return;
                }

                // Получаем значения из полей
                string name = txtName.Text.Trim();
                string address = txtAddress.Text.Trim();
                string group = txtGroup.Text.Trim();
                string comment = txtComment.Text.Trim();
                bool isDbTag = rbDbTag.IsChecked ?? false;

                // Получаем выбранный тип данных
                TagDataType dataType;
                switch (cmbDataType.SelectedIndex)
                {
                    case 0:
                        dataType = TagDataType.Bool;
                        break;
                    case 1:
                        dataType = TagDataType.Int;
                        break;
                    case 2:
                        dataType = TagDataType.DInt;
                        break;
                    case 3:
                        dataType = TagDataType.Real;
                        break;
                    default:
                        dataType = TagDataType.Other;
                        break;
                }

                // Создаем новый тег или обновляем существующий
                if (_mode == DialogMode.Create)
                {
                    Tag = new TagDefinition
                    {
                        Id = Guid.NewGuid(),
                        Name = name,
                        Address = address,
                        DataType = dataType,
                        GroupName = group,
                        Comment = comment,
                        IsDbTag = isDbTag  // Явно устанавливаем значение
                    };
                }
                else // Редактирование
                {
                    Tag = new TagDefinition
                    {
                        Id = _originalTag.Id,
                        Name = name,
                        Address = address,
                        DataType = dataType,
                        GroupName = group,
                        Comment = comment,
                        IsDbTag = isDbTag  // Явно устанавливаем значение
                    };
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении тега: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Обработчик изменения типа тега
        /// </summary>
        private void RbTagType_Checked(object sender, RoutedEventArgs e)
        {
            // Если выбран DB тег, то автоматически добавляем префикс "DB" в GroupName
            if (rbDbTag != null && rbDbTag.IsChecked == true && !string.IsNullOrEmpty(txtGroup.Text) && !txtGroup.Text.StartsWith("DB"))
            {
                txtGroup.Text = "DB" + txtGroup.Text;
            }


            // Если выбран PLC тег и текущая группа начинается с "DB", 
            // то предлагаем пользователю изменить группу
            if (rbPlcTag.IsChecked == true && txtGroup.Text != null && txtGroup.Text.StartsWith("DB"))
            {
                var result = MessageBox.Show(
                    "Группа начинается с 'DB', что обычно указывает на DB-тег. Хотите изменить группу?",
                    "Предупреждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    txtGroup.Text = txtGroup.Text.Substring(2); // Удаляем "DB"
                }
            }
        }
    }
}