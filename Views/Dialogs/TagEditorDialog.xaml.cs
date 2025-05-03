using System;
using System.Windows;
using SiemensTrend.Core.Models;

namespace SiemensTrend.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for TagEditorDialog.xaml
    /// </summary>
    public partial class TagEditorDialog : Window
    {
        /// <summary>
        /// Dialog mode (create or edit)
        /// </summary>
        public enum DialogMode
        {
            Create,
            Edit
        }

        private readonly DialogMode _mode;
        private TagDefinition _originalTag;

        /// <summary>
        /// Result tag
        /// </summary>
        public TagDefinition Tag { get; private set; }

        /// <summary>
        /// Constructor for creating a new tag
        /// </summary>
        public TagEditorDialog()
        {
            InitializeComponent();
            _mode = DialogMode.Create;
            Title = "Create New Tag";
            
            // Set default values
            cmbDataType.SelectedIndex = 0; // Bool
        }

        /// <summary>
        /// Constructor for editing an existing tag
        /// </summary>
        /// <param name="tag">Tag to edit</param>
        public TagEditorDialog(TagDefinition tag)
        {
            InitializeComponent();
            _mode = DialogMode.Edit;
            _originalTag = tag;
            Title = "Edit Tag";
            
            // Fill fields with tag data
            txtName.Text = tag.Name;
            txtAddress.Text = tag.Address;
            txtGroup.Text = tag.GroupName;
            txtComment.Text = tag.Comment;
            
            // Select data type
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
            
            // Select tag type
            rbPlcTag.IsChecked = !tag.IsDbTag;
            rbDbTag.IsChecked = tag.IsDbTag;
        }

        /// <summary>
        /// Cancel button click handler
        /// </summary>
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Save button click handler
        /// </summary>
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Check required fields
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show("Please enter tag name", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtName.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtAddress.Text))
                {
                    MessageBox.Show("Please enter tag address", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtAddress.Focus();
                    return;
                }

                // Get values from fields
                string name = txtName.Text.Trim();
                string address = txtAddress.Text.Trim();
                string group = txtGroup.Text.Trim();
                string comment = txtComment.Text.Trim();
                bool isDbTag = rbDbTag.IsChecked ?? false;

                // Get selected data type
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

                // Create new tag or update existing
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
                        IsDbTag = isDbTag
                    };
                }
                else // Edit
                {
                    Tag = new TagDefinition
                    {
                        Id = _originalTag.Id,
                        Name = name,
                        Address = address,
                        DataType = dataType,
                        GroupName = group,
                        Comment = comment,
                        IsDbTag = isDbTag
                    };
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving tag: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
