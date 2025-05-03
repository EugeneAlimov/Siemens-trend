# script4_update_main_window.ps1
# Script to update MainWindow.xaml and add handlers

# Project path
$projectPath = Get-Location

# Path to MainWindow.xaml
$mainWindowPath = Join-Path -Path $projectPath -ChildPath "Views\MainWindow.xaml"

# Load current content
$mainWindowContent = Get-Content -Path $mainWindowPath -Raw

# Replace toolbar
$newToolbar = @"
            <ToolBar>
                <Button x:Name="btnConnect" Content="Connect" Margin="3" Padding="5,3" Click="BtnConnect_Click"/>
                <Button x:Name="btnDisconnect" Content="Disconnect" Margin="3" Padding="5,3" Click="BtnDisconnect_Click"/>
                <Separator/>
                <Button x:Name="btnAddTag" Content="Add Tag" Margin="3" Padding="5,3" Click="BtnAddTag_Click"/>
                <Button x:Name="btnEditTag" Content="Edit Tag" Margin="3" Padding="5,3" Click="BtnEditTag_Click"/>
                <Button x:Name="btnRemoveTag" Content="Delete Tag" Margin="3" Padding="5,3" Click="BtnRemoveTag_Click"/>
                <Separator/>
                <Button x:Name="btnImportTags" Content="Import Tags" Margin="3" Padding="5,3" Click="BtnImportTags_Click"/>
                <Button x:Name="btnExportTags" Content="Export Tags" Margin="3" Padding="5,3" Click="BtnExportTags_Click"/>
                <Button x:Name="btnSaveTags" Content="Save Tags" Margin="3" Padding="5,3" Click="BtnSaveTags_Click"/>
                <Separator/>
                <Button x:Name="btnStartMonitoring" Content="Start Monitoring" Margin="3" Padding="5,3" Click="BtnStartMonitoring_Click"/>
                <Button x:Name="btnStopMonitoring" Content="Stop Monitoring" Margin="3" Padding="5,3" Click="BtnStopMonitoring_Click"/>
                <Separator/>
                <Button x:Name="btnSaveLog" Content="Save Log" Margin="3" Padding="5,3" Click="BtnSaveLog_Click"/>
                <Button x:Name="btnClearLog" Content="Clear Log" Margin="3" Padding="5,3" Click="BtnClearLog_Click"/>
            </ToolBar>
"@

# Regular expression for replacing toolbar
$pattern = '(?s)(<ToolBarTray Grid\.Row="0">).*?(</ToolBarTray>)'
$replacement = "`$1`r`n$newToolbar`r`n        `$2"

# Perform replacement
$mainWindowContent = $mainWindowContent -replace $pattern, $replacement

# Add names to DataGrid for tags
$mainWindowContent = $mainWindowContent -replace '<DataGrid Grid\.Row="1" Margin="5" ItemsSource="{Binding PlcTags}"', '<DataGrid x:Name="dgPlcTags" Grid.Row="1" Margin="5" ItemsSource="{Binding PlcTags}"'
$mainWindowContent = $mainWindowContent -replace '<DataGrid ItemsSource="{Binding DbTags}"', '<DataGrid x:Name="dgDbTags" ItemsSource="{Binding DbTags}"'

# Save updated file
Set-Content -Path $mainWindowPath -Value $mainWindowContent

# Add handlers to MainWindow.xaml.cs
$mainWindowCodePath = Join-Path -Path $projectPath -ChildPath "Views\MainWindow.xaml.cs"
$mainWindowCodeContent = Get-Content -Path $mainWindowCodePath -Raw

# New methods to add
$newMethods = @"

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
                    
                    _logger.Info(\$"BtnAddTag_Click: Added new tag: {newTag.Name}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(\$"BtnAddTag_Click: Error: {ex.Message}");
                MessageBox.Show(\$"Error adding tag: {ex.Message}",
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
                    
                    _logger.Info(\$"BtnEditTag_Click: Edited tag: {updatedTag.Name}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(\$"BtnEditTag_Click: Error: {ex.Message}");
                MessageBox.Show(\$"Error editing tag: {ex.Message}",
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
                var result = MessageBox.Show(\$"Are you sure you want to delete tag {selectedTag.Name}?",
                    "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    
                if (result == MessageBoxResult.Yes)
                {
                    // Remove tag from model
                    _viewModel.RemoveTag(selectedTag);
                    
                    _logger.Info(\$"BtnRemoveTag_Click: Deleted tag: {selectedTag.Name}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(\$"BtnRemoveTag_Click: Error: {ex.Message}");
                MessageBox.Show(\$"Error deleting tag: {ex.Message}",
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
                    _logger.Info(\$"BtnImportTags_Click: Selected file: {filePath}");
                    
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
                    
                    _logger.Info(\$"BtnImportTags_Click: Imported {importedTags.Count} tags");
                    MessageBox.Show(\$"Imported {importedTags.Count} tags",
                        "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(\$"BtnImportTags_Click: Error: {ex.Message}");
                MessageBox.Show(\$"Error importing tags: {ex.Message}",
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
                    _logger.Info(\$"BtnExportTags_Click: Selected file: {filePath}");
                    
                    // Collect all tags for export
                    var tags = new List<TagDefinition>();
                    tags.AddRange(_viewModel.PlcTags);
                    tags.AddRange(_viewModel.DbTags);
                    
                    // Export tags
                    var tagManager = new Storage.TagManagement.TagManager(_logger);
                    bool success = tagManager.ExportTagsToCsv(tags, filePath);
                    
                    if (success)
                    {
                        _logger.Info(\$"BtnExportTags_Click: Exported {tags.Count} tags");
                        MessageBox.Show(\$"Exported {tags.Count} tags",
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
                _logger.Error(\$"BtnExportTags_Click: Error: {ex.Message}");
                MessageBox.Show(\$"Error exporting tags: {ex.Message}",
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
                _logger.Error(\$"BtnSaveTags_Click: Error: {ex.Message}");
                MessageBox.Show(\$"Error saving tags: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

"@

# Find place to insert methods
$pattern = '(?s)(namespace SiemensTrend\.Views\s*{.*?public partial class MainWindow\s*:\s*Window\s*{.*?)(}(?:\s*//.*?)*?\s*}(?:\s*//.*?)*?\s*$)'
$replacement = "`$1$newMethods`$2"

# Perform replacement
$mainWindowCodeContent = $mainWindowCodeContent -replace $pattern, $replacement

# Save updated file
Set-Content -Path $mainWindowCodePath -Value $mainWindowCodeContent

Write-Host "Script 4 completed successfully. Updated MainWindow interface and added handlers."