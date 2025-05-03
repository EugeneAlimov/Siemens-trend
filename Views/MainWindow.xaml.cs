using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SiemensTrend.Core.Logging;
using SiemensTrend.Core.Models;
using SiemensTrend.ViewModels;

namespace SiemensTrend.Views
{
    /// <summary>
    /// ?????? ?????????????? ??? MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// ?????? ?????????????
        /// </summary>
        private readonly MainViewModel _viewModel;

        /// <summary>
        /// ?????? ??? ?????? ???????
        /// </summary>
        private readonly Logger _logger;

        /// <summary>
        /// ??????????? ???????? ????
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            // ??????? ?????
            _logger = new Logger();

            // ??????? ? ????????????? ?????? ?????????????
            _viewModel = new MainViewModel(_logger);
            DataContext = _viewModel;

            // ?????????????? TagBrowserViewModel
            _viewModel.InitializeTagBrowser();

            // ????????????? DataContext ??? TagBrowserView
            tagBrowser.DataContext = _viewModel.TagBrowserViewModel;

            // ???????? ????????? ?????? ??? ????????????? ?????????? UI-?????????
            InitializeUI();

            // ?????????????? ????????? ?????????
            UpdateConnectionState();
        }
        private void InitializeUI()
        {
            try
            {
                // ?????????????? ?????????? ???????? ??? ?????? ? ?????? DB
                InitializeEnhancedTagReading();

                // ?????????????? ????? ????????? ????? DB ?? ??????????
                OverrideGetDbTagsButton();

                // ????????? ?????? ??? ???????? ? ?????????? ????????
                AddEnhancedExportButton();

                // ????????? ???????? ?????? ??? ???????
                AddTestButton();

                _logger.Info("MainWindow: UI ??????????????? ? ??????????? ?????????? ??? ?????? ? DB");
            }
            catch (Exception ex)
            {
                _logger.Error($"InitializeUI: ?????? ??? ????????????? ?????????? ????????? UI: {ex.Message}");
            
        /// <summary>
        /// Add tag button click handler
        /// </summary>
        private void BtnAddTag_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("BtnAddTag_Click: Calling tag add dialog");
                
                // Create dialog for adding tag
                var dialog = new Dialogs.TagEditorDialog();
                dialog.Owner = this;
                
                // Show dialog
                if (dialog.ShowDialog() == true)
                {
                    // Get created tag
                    var newTag = dialog.Tag;
                    
                    // Add tag to model
                    _viewModel.AddNewTag(newTag);
                    
                    _logger.Info($"BtnAddTag_Click: Added new tag: {newTag.Name}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"BtnAddTag_Click: Error: {ex.Message}");
                MessageBox.Show($"Error adding tag: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            
        /// <summary>
        /// Add tag button click handler
        /// </summary>
        private void BtnAddTag_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("BtnAddTag_Click: Calling tag add dialog");
                
                // Create dialog for adding tag
                var dialog = new Dialogs.TagEditorDialog();
                dialog.Owner = this;
                
                // Show dialog
                if (dialog.ShowDialog() == true)
                {
                    // Get created tag
                    var newTag = dialog.Tag;
                    
                    // Add tag to model
                    _viewModel.AddNewTag(newTag);
                    
                    _logger.Info($"BtnAddTag_Click: Added new tag: {newTag.Name}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"BtnAddTag_Click: Error: {ex.Message}");
                MessageBox.Show($"Error adding tag: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Edit tag button click handler
        /// </summary>
        private void BtnEditTag_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("BtnEditTag_Click: Calling tag edit dialog");
                
                // Get selected tag from PLC or DB table
                TagDefinition selectedTag = null;
                
                // Check if there's a selected tag in PLC table
                var plcDataGrid = this.FindName("dgPlcTags") as DataGrid;
                if (plcDataGrid != null && plcDataGrid.SelectedItem is TagDefinition)
                {
                    selectedTag = plcDataGrid.SelectedItem as TagDefinition;
                }
                
                // If nothing selected in PLC table, check DB table
                if (selectedTag == null)
                {
                    var dbDataGrid = this.FindName("dgDbTags") as DataGrid;
                    if (dbDataGrid != null && dbDataGrid.SelectedItem is TagDefinition)
                    {
                        selectedTag = dbDataGrid.SelectedItem as TagDefinition;
                    }
                }
                
                if (selectedTag == null)
                {
                    _logger.Warn("BtnEditTag_Click: No tag selected for editing");
                    MessageBox.Show("Please select a tag to edit",
                        "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                // Create dialog for editing tag
                var dialog = new Dialogs.TagEditorDialog(selectedTag);
                dialog.Owner = this;
                
                // Show dialog
                if (dialog.ShowDialog() == true)
                {
                    // Get edited tag
                    var updatedTag = dialog.Tag;
                    
                    // Update tag in model
                    _viewModel.EditTag(selectedTag, updatedTag);
                    
                    _logger.Info($"BtnEditTag_Click: Edited tag: {updatedTag.Name}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"BtnEditTag_Click: Error: {ex.Message}");
                MessageBox.Show($"Error editing tag: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Delete tag button click handler
        /// </summary>
        private void BtnRemoveTag_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("BtnRemoveTag_Click: Deleting tag");
                
                // Get selected tag from PLC or DB table
                TagDefinition selectedTag = null;
                
                // Check if there's a selected tag in PLC table
                var plcDataGrid = this.FindName("dgPlcTags") as DataGrid;
                if (plcDataGrid != null && plcDataGrid.SelectedItem is TagDefinition)
                {
                    selectedTag = plcDataGrid.SelectedItem as TagDefinition;
                }
                
                // If nothing selected in PLC table, check DB table
                if (selectedTag == null)
                {
                    var dbDataGrid = this.FindName("dgDbTags") as DataGrid;
                    if (dbDataGrid != null && dbDataGrid.SelectedItem is TagDefinition)
                    {
                        selectedTag = dbDataGrid.SelectedItem as TagDefinition;
                    }
                }
                
                if (selectedTag == null)
                {
                    _logger.Warn("BtnRemoveTag_Click: No tag selected for deletion");
                    MessageBox.Show("Please select a tag to delete",
                        "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                // Ask for confirmation
                var result = MessageBox.Show($"Are you sure you want to delete tag {selectedTag.Name}?",
                    "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    
                if (result == MessageBoxResult.Yes)
                {
                    // Remove tag from model
                    _viewModel.RemoveTag(selectedTag);
                    
                    _logger.Info($"BtnRemoveTag_Click: Deleted tag: {selectedTag.Name}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"BtnRemoveTag_Click: Error: {ex.Message}");
                MessageBox.Show($"Error deleting tag: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Import tags button click handler
        /// </summary>
        private void BtnImportTags_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("BtnImportTags_Click: Importing tags from CSV");
                
                // Create file selection dialog
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    Title = "Select file with tags to import"
                };
                
                // Show dialog
                if (openFileDialog.ShowDialog() == true)
                {
                    string filePath = openFileDialog.FileName;
                    _logger.Info($"BtnImportTags_Click: Selected file: {filePath}");
                    
                    // Import tags
                    var tagManager = new Storage.TagManagement.TagManager(_logger);
                    var importedTags = tagManager.ImportTagsFromCsv(filePath);
                    
                    // Ask what to do with existing tags
                    var result = MessageBox.Show(
                        "Do you want to replace existing tags with imported ones? " + 
                        "Click 'Yes' to replace, 'No' to add to existing.",
                        "Import tags", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                        
                    if (result == MessageBoxResult.Cancel)
                    {
                        _logger.Info("BtnImportTags_Click: Import canceled by user");
                        return;
                    }
                    
                    // If replacement selected, clear existing tags
                    if (result == MessageBoxResult.Yes)
                    {
                        _viewModel.PlcTags.Clear();
                        _viewModel.DbTags.Clear();
                        _viewModel.AvailableTags.Clear();
                        _logger.Info("BtnImportTags_Click: Existing tags cleared");
                    }
                    
                    // Add imported tags
                    foreach (var tag in importedTags)
                    {
                        _viewModel.AddNewTag(tag);
                    }
                    
                    // Save changes
                    _viewModel.SaveTagsToStorage();
                    
                    _logger.Info($"BtnImportTags_Click: Imported {importedTags.Count} tags");
                    MessageBox.Show($"Imported {importedTags.Count} tags",
                        "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"BtnImportTags_Click: Error: {ex.Message}");
                MessageBox.Show($"Error importing tags: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Export tags button click handler
        /// </summary>
        private void BtnExportTags_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("BtnExportTags_Click: Exporting tags to CSV");
                
                // Check if there are tags to export
                int tagCount = _viewModel.PlcTags.Count + _viewModel.DbTags.Count;
                if (tagCount == 0)
                {
                    _logger.Warn("BtnExportTags_Click: No tags to export");
                    MessageBox.Show("No tags to export",
                        "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                // Create file save dialog
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    Title = "Export tags",
                    DefaultExt = ".csv",
                    AddExtension = true
                };
                
                // Show dialog
                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;
                    _logger.Info($"BtnExportTags_Click: Selected file: {filePath}");
                    
                    // Collect all tags for export
                    var tags = new List<TagDefinition>();
                    tags.AddRange(_viewModel.PlcTags);
                    tags.AddRange(_viewModel.DbTags);
                    
                    // Export tags
                    var tagManager = new Storage.TagManagement.TagManager(_logger);
                    bool success = tagManager.ExportTagsToCsv(tags, filePath);
                    
                    if (success)
                    {
                        _logger.Info($"BtnExportTags_Click: Exported {tags.Count} tags");
                        MessageBox.Show($"Exported {tags.Count} tags",
                            "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        _logger.Error("BtnExportTags_Click: Error exporting tags");
                        MessageBox.Show("Error exporting tags",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"BtnExportTags_Click: Error: {ex.Message}");
                MessageBox.Show($"Error exporting tags: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Save tags button click handler
        /// </summary>
        private void BtnSaveTags_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("BtnSaveTags_Click: Saving tags");
                
                // Save tags
                _viewModel.SaveTagsToStorage();
                
                _logger.Info("BtnSaveTags_Click: Tags saved successfully");
                MessageBox.Show("Tags saved successfully",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.Error($"BtnSaveTags_Click: Error: {ex.Message}");
                MessageBox.Show($"Error saving tags: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
}
        }
        
        /// <summary>
        /// Edit tag button click handler
        /// </summary>
        private void BtnEditTag_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("BtnEditTag_Click: Calling tag edit dialog");
                
                // Get selected tag from PLC or DB table
                TagDefinition selectedTag = null;
                
                // Check if there's a selected tag in PLC table
                var plcDataGrid = this.FindName("dgPlcTags") as DataGrid;
                if (plcDataGrid != null && plcDataGrid.SelectedItem is TagDefinition)
                {
                    selectedTag = plcDataGrid.SelectedItem as TagDefinition;
                }
                
                // If nothing selected in PLC table, check DB table
                if (selectedTag == null)
                {
                    var dbDataGrid = this.FindName("dgDbTags") as DataGrid;
                    if (dbDataGrid != null && dbDataGrid.SelectedItem is TagDefinition)
                    {
                        selectedTag = dbDataGrid.SelectedItem as TagDefinition;
                    }
                }
                
                if (selectedTag == null)
                {
                    _logger.Warn("BtnEditTag_Click: No tag selected for editing");
                    MessageBox.Show("Please select a tag to edit",
                        "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                // Create dialog for editing tag
                var dialog = new Dialogs.TagEditorDialog(selectedTag);
                dialog.Owner = this;
                
                // Show dialog
                if (dialog.ShowDialog() == true)
                {
                    // Get edited tag
                    var updatedTag = dialog.Tag;
                    
                    // Update tag in model
                    _viewModel.EditTag(selectedTag, updatedTag);
                    
                    _logger.Info($"BtnEditTag_Click: Edited tag: {updatedTag.Name}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"BtnEditTag_Click: Error: {ex.Message}");
                MessageBox.Show($"Error editing tag: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Delete tag button click handler
        /// </summary>
        private void BtnRemoveTag_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("BtnRemoveTag_Click: Deleting tag");
                
                // Get selected tag from PLC or DB table
                TagDefinition selectedTag = null;
                
                // Check if there's a selected tag in PLC table
                var plcDataGrid = this.FindName("dgPlcTags") as DataGrid;
                if (plcDataGrid != null && plcDataGrid.SelectedItem is TagDefinition)
                {
                    selectedTag = plcDataGrid.SelectedItem as TagDefinition;
                }
                
                // If nothing selected in PLC table, check DB table
                if (selectedTag == null)
                {
                    var dbDataGrid = this.FindName("dgDbTags") as DataGrid;
                    if (dbDataGrid != null && dbDataGrid.SelectedItem is TagDefinition)
                    {
                        selectedTag = dbDataGrid.SelectedItem as TagDefinition;
                    }
                }
                
                if (selectedTag == null)
                {
                    _logger.Warn("BtnRemoveTag_Click: No tag selected for deletion");
                    MessageBox.Show("Please select a tag to delete",
                        "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                // Ask for confirmation
                var result = MessageBox.Show($"Are you sure you want to delete tag {selectedTag.Name}?",
                    "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    
                if (result == MessageBoxResult.Yes)
                {
                    // Remove tag from model
                    _viewModel.RemoveTag(selectedTag);
                    
                    _logger.Info($"BtnRemoveTag_Click: Deleted tag: {selectedTag.Name}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"BtnRemoveTag_Click: Error: {ex.Message}");
                MessageBox.Show($"Error deleting tag: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Import tags button click handler
        /// </summary>
        private void BtnImportTags_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("BtnImportTags_Click: Importing tags from CSV");
                
                // Create file selection dialog
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    Title = "Select file with tags to import"
                };
                
                // Show dialog
                if (openFileDialog.ShowDialog() == true)
                {
                    string filePath = openFileDialog.FileName;
                    _logger.Info($"BtnImportTags_Click: Selected file: {filePath}");
                    
                    // Import tags
                    var tagManager = new Storage.TagManagement.TagManager(_logger);
                    var importedTags = tagManager.ImportTagsFromCsv(filePath);
                    
                    // Ask what to do with existing tags
                    var result = MessageBox.Show(
                        "Do you want to replace existing tags with imported ones? " + 
                        "Click 'Yes' to replace, 'No' to add to existing.",
                        "Import tags", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                        
                    if (result == MessageBoxResult.Cancel)
                    {
                        _logger.Info("BtnImportTags_Click: Import canceled by user");
                        return;
                    }
                    
                    // If replacement selected, clear existing tags
                    if (result == MessageBoxResult.Yes)
                    {
                        _viewModel.PlcTags.Clear();
                        _viewModel.DbTags.Clear();
                        _viewModel.AvailableTags.Clear();
                        _logger.Info("BtnImportTags_Click: Existing tags cleared");
                    }
                    
                    // Add imported tags
                    foreach (var tag in importedTags)
                    {
                        _viewModel.AddNewTag(tag);
                    }
                    
                    // Save changes
                    _viewModel.SaveTagsToStorage();
                    
                    _logger.Info($"BtnImportTags_Click: Imported {importedTags.Count} tags");
                    MessageBox.Show($"Imported {importedTags.Count} tags",
                        "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"BtnImportTags_Click: Error: {ex.Message}");
                MessageBox.Show($"Error importing tags: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Export tags button click handler
        /// </summary>
        private void BtnExportTags_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("BtnExportTags_Click: Exporting tags to CSV");
                
                // Check if there are tags to export
                int tagCount = _viewModel.PlcTags.Count + _viewModel.DbTags.Count;
                if (tagCount == 0)
                {
                    _logger.Warn("BtnExportTags_Click: No tags to export");
                    MessageBox.Show("No tags to export",
                        "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                // Create file save dialog
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    Title = "Export tags",
                    DefaultExt = ".csv",
                    AddExtension = true
                };
                
                // Show dialog
                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;
                    _logger.Info($"BtnExportTags_Click: Selected file: {filePath}");
                    
                    // Collect all tags for export
                    var tags = new List<TagDefinition>();
                    tags.AddRange(_viewModel.PlcTags);
                    tags.AddRange(_viewModel.DbTags);
                    
                    // Export tags
                    var tagManager = new Storage.TagManagement.TagManager(_logger);
                    bool success = tagManager.ExportTagsToCsv(tags, filePath);
                    
                    if (success)
                    {
                        _logger.Info($"BtnExportTags_Click: Exported {tags.Count} tags");
                        MessageBox.Show($"Exported {tags.Count} tags",
                            "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        _logger.Error("BtnExportTags_Click: Error exporting tags");
                        MessageBox.Show("Error exporting tags",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"BtnExportTags_Click: Error: {ex.Message}");
                MessageBox.Show($"Error exporting tags: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Save tags button click handler
        /// </summary>
        private void BtnSaveTags_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("BtnSaveTags_Click: Saving tags");
                
                // Save tags
                _viewModel.SaveTagsToStorage();
                
                _logger.Info("BtnSaveTags_Click: Tags saved successfully");
                MessageBox.Show("Tags saved successfully",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.Error($"BtnSaveTags_Click: Error: {ex.Message}");
                MessageBox.Show($"Error saving tags: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
}
        }
        /// <summary>
        /// ?????????? ??????? ?????? "????????? ???"
        /// </summary>
        private void BtnSaveLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // ??????? ?????? ?????????? ?????
                Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "????????? ????? (*.txt)|*.txt",
                    Title = "?????????? ????",
                    DefaultExt = ".txt",
                    AddExtension = true
                };

                // ???? ???????????? ?????? ????
                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;
                    _logger.Info($"?????????? ???? ? ????: {filePath}");

                    // ????????? ?????????? ???? ? ????
                    System.IO.File.WriteAllText(filePath, txtLog.Text);

                    _logger.Info("??? ??????? ????????");
                    MessageBox.Show("??? ??????? ????????",
                        "??????????", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"?????? ??? ?????????? ????: {ex.Message}");
                MessageBox.Show($"?????? ??? ?????????? ????: {ex.Message}",
                    "??????", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// ?????????? ??????? ?????? "???????? ???"
        /// </summary>
        private void BtnClearLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("??????? ????");

                // ??????? ?????????? ????
                txtLog.Clear();

                _logger.Info("??? ??????");
            }
            catch (Exception ex)
            {
                _logger.Error($"?????? ??? ??????? ????: {ex.Message}");
                MessageBox.Show($"?????? ??? ??????? ????: {ex.Message}",
                    "??????", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddTestButton()
        {
            try
            {
                ToolBarTray toolbarTray = null;

                // ????? ?? ?????
                toolbarTray = FindName("toolBarTray") as ToolBarTray;

                // ???? ?? ????? ?? ?????, ?? ?????? ?? ???? ? ?????????? ??????
                if (toolbarTray == null)
                {
                    var toolbarTrays = FindVisualChildren<ToolBarTray>(this);
                    if (toolbarTrays.Count > 0)
                    {
                        toolbarTray = toolbarTrays[0];
                    }
                }

                if (toolbarTray != null && toolbarTray.ToolBars.Count > 0)
                {
                    var toolbar = toolbarTray.ToolBars[0];

                    // ??????? ???????? ??????
                    var btnTest = new Button
                    {
                        Content = "???? DB (???????)",
                        Margin = new Thickness(3),
                        Padding = new Thickness(5, 3, 5, 3),
                        Background = Brushes.LightYellow
                    };

                    btnTest.Click += (s, e) => TestDbTagsLoading();

                    // ????????? ??????????? ? ??????
                    toolbar.Items.Add(new Separator());
                    toolbar.Items.Add(btnTest);

                    _logger.Info("AddTestButton: ???????? ?????? ?????????");
                }
                else
                {
                    _logger.Warn("AddTestButton: ?????? ???????????? ?? ???????");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"AddTestButton: ?????? ??? ?????????? ???????? ??????: {ex.Message}");
            }
        }

        /// <summary>
        /// ???????? ???????? ????? DB ??? ???????
        /// </summary>
        private List<TagDefinition> CreateTestDbTags()
        {
            var tags = new List<TagDefinition>();

            // ??????? ????????? ???????? ?????
            tags.Add(new TagDefinition
            {
                Id = Guid.NewGuid(),
                Name = "DB1",
                Address = "Standard",
                DataType = TagDataType.UDT,
                Comment = "???????? ???? ??????",
                GroupName = "DB",
                IsOptimized = false,
                IsUDT = true
            });

            tags.Add(new TagDefinition
            {
                Id = Guid.NewGuid(),
                Name = "DB2",
                Address = "Optimized",
                DataType = TagDataType.UDT,
                Comment = "???????? ???????????????? ???? ??????",
                GroupName = "DB",
                IsOptimized = true,
                IsUDT = true
            });

            tags.Add(new TagDefinition
            {
                Id = Guid.NewGuid(),
                Name = "DB_Motor",
                Address = "Standard",
                DataType = TagDataType.UDT,
                Comment = "???????? ???? ?????? ??? ?????????? ??????????",
                GroupName = "DB",
                IsOptimized = false,
                IsUDT = true
            });

            // ????????? ?????????? ??? ?????? ??????
            tags.Add(new TagDefinition
            {
                Id = Guid.NewGuid(),
                Name = "DB1.Value",
                Address = "DB1.DBD0",
                DataType = TagDataType.Real,
                Comment = "????????",
                GroupName = "DB1",
                IsOptimized = false
            });

            tags.Add(new TagDefinition
            {
                Id = Guid.NewGuid(),
                Name = "DB1.Status",
                Address = "DB1.DBW4",
                DataType = TagDataType.Int,
                Comment = "??????",
                GroupName = "DB1",
                IsOptimized = false
            });

            tags.Add(new TagDefinition
            {
                Id = Guid.NewGuid(),
                Name = "DB2.Value",
                Address = "DB2.Value",
                DataType = TagDataType.Real,
                Comment = "???????? (???????????????? ????)",
                GroupName = "DB2",
                IsOptimized = true
            });

            tags.Add(new TagDefinition
            {
                Id = Guid.NewGuid(),
                Name = "DB_Motor.Speed",
                Address = "DB_Motor.DBD0",
                DataType = TagDataType.Real,
                Comment = "???????? ?????????",
                GroupName = "DB_Motor",
                IsOptimized = false
            });

            tags.Add(new TagDefinition
            {
                Id = Guid.NewGuid(),
                Name = "DB_Motor.Start",
                Address = "DB_Motor.DBX4.0",
                DataType = TagDataType.Bool,
                Comment = "?????? ?????????",
                GroupName = "DB_Motor",
                IsOptimized = false
            });

            tags.Add(new TagDefinition
            {
                Id = Guid.NewGuid(),
                Name = "DB_Motor.Stop",
                Address = "DB_Motor.DBX4.1",
                DataType = TagDataType.Bool,
                Comment = "??????? ?????????",
                GroupName = "DB_Motor",
                IsOptimized = false
            });

            return tags;
        }

        /// <summary>
        /// ?????????? ????? ??? ???????????? ?????? ????? DB
        /// </summary>
        private async void TestDbTagsLoading()
        {
            try
            {
                _logger.Info("TestDbTagsLoading: ?????? ???????????? ???????? ????? DB");
                _viewModel.StatusMessage = "???????????? ???????? ????? DB...";
                _viewModel.IsLoading = true;

                // ????????? ??????????? ? TIA Portal
                if (!_viewModel.IsConnected)
                {
                    _logger.Warn("TestDbTagsLoading: ??? ??????????? ? TIA Portal");
                    MessageBox.Show("?????????? ??????? ???????????? ? TIA Portal.",
                        "??????????????", MessageBoxButton.OK, MessageBoxImage.Warning);
                    _viewModel.IsLoading = false;
                    return;
                }

                // ??????? ???????? ???? DB ??? ???????????
                var testTags = CreateTestDbTags();
                _logger.Info($"TestDbTagsLoading: ??????? {testTags.Count} ???????? ????? DB");

                // ??????? ? ????????? ?????????
                _viewModel.DbTags.Clear();
                foreach (var tag in testTags)
                {
                    _viewModel.DbTags.Add(tag);
                }

                _logger.Info("TestDbTagsLoading: ???????? ???? DB ????????? ???????");
                _viewModel.StatusMessage = $"????????? {testTags.Count} ???????? ????? DB";
            }
            catch (Exception ex)
            {
                _logger.Error($"TestDbTagsLoading: ??????: {ex.Message}");
                _viewModel.StatusMessage = "?????? ??? ???????????? ???????? ????? DB";
            }
            finally
            {
                _viewModel.IsLoading = false;
            }
        }
    }
}

