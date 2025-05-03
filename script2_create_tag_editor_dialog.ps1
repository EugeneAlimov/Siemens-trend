# script2_create_tag_editor_dialog.ps1
# Script to create tag editor dialog

# Project path
$projectPath = Get-Location

# Create directory for dialogs
$dialogsDir = Join-Path -Path $projectPath -ChildPath "Views\Dialogs"
if (-not (Test-Path $dialogsDir)) {
    New-Item -Path $dialogsDir -ItemType Directory -Force
}

# Create XAML for TagEditorDialog
$tagEditorDialogPath = Join-Path -Path $dialogsDir -ChildPath "TagEditorDialog.xaml"
$tagEditorDialogContent = @"
<Window x:Class="SiemensTrend.Views.Dialogs.TagEditorDialog"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Tag Editor" Height="350" Width="500"
        WindowStartupLocation="CenterOwner" ResizeMode="NoResize">
    <Grid Margin="10">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Header -->
        <TextBlock Grid.Row="0" Text="Edit Tag" FontWeight="Bold" FontSize="16" Margin="0,0,0,10"/>

        <!-- Edit form -->
        <Grid Grid.Row="1">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="*"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="120"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <!-- Tag name -->
            <TextBlock Grid.Row="0" Grid.Column="0" Text="Name:" VerticalAlignment="Center" Margin="0,5"/>
            <TextBox Grid.Row="0" Grid.Column="1" x:Name="txtName" Margin="0,5"/>

            <!-- Address -->
            <TextBlock Grid.Row="1" Grid.Column="0" Text="Address:" VerticalAlignment="Center" Margin="0,5"/>
            <TextBox Grid.Row="1" Grid.Column="1" x:Name="txtAddress" Margin="0,5"/>

            <!-- Data type -->
            <TextBlock Grid.Row="2" Grid.Column="0" Text="Data Type:" VerticalAlignment="Center" Margin="0,5"/>
            <ComboBox Grid.Row="2" Grid.Column="1" x:Name="cmbDataType" Margin="0,5">
                <ComboBoxItem Content="Bool"/>
                <ComboBoxItem Content="Int"/>
                <ComboBoxItem Content="DInt"/>
                <ComboBoxItem Content="Real"/>
                <ComboBoxItem Content="Other"/>
            </ComboBox>

            <!-- Group -->
            <TextBlock Grid.Row="3" Grid.Column="0" Text="Group:" VerticalAlignment="Center" Margin="0,5"/>
            <TextBox Grid.Row="3" Grid.Column="1" x:Name="txtGroup" Margin="0,5"/>

            <!-- Tag type -->
            <TextBlock Grid.Row="4" Grid.Column="0" Text="Tag Type:" VerticalAlignment="Center" Margin="0,5"/>
            <StackPanel Grid.Row="4" Grid.Column="1" Orientation="Horizontal" Margin="0,5">
                <RadioButton x:Name="rbPlcTag" Content="PLC Tag" Margin="0,0,10,0" IsChecked="True"/>
                <RadioButton x:Name="rbDbTag" Content="DB Tag"/>
            </StackPanel>

            <!-- Comment -->
            <TextBlock Grid.Row="5" Grid.Column="0" Text="Comment:" VerticalAlignment="Top" Margin="0,5"/>
            <TextBox Grid.Row="5" Grid.Column="1" x:Name="txtComment" Margin="0,5" Height="60" TextWrapping="Wrap"/>
        </Grid>

        <!-- Buttons -->
        <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,10,0,0">
            <Button x:Name="btnCancel" Content="Cancel" Width="80" Height="30" Margin="0,0,10,0" Click="BtnCancel_Click"/>
            <Button x:Name="btnSave" Content="Save" Width="80" Height="30" Click="BtnSave_Click" IsDefault="True"/>
        </StackPanel>
    </Grid>
</Window>
"@
Set-Content -Path $tagEditorDialogPath -Value $tagEditorDialogContent

# Create code for TagEditorDialog
$tagEditorDialogCodePath = Join-Path -Path $dialogsDir -ChildPath "TagEditorDialog.xaml.cs"
$tagEditorDialogCodeContent = @"
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
                MessageBox.Show(\$"Error saving tag: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
"@
Set-Content -Path $tagEditorDialogCodePath -Value $tagEditorDialogCodeContent

Write-Host "Script 2 completed successfully. Tag editor dialog created."