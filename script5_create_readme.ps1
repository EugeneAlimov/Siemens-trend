# script5_create_readme.ps1
# Script to create README with description of changes

# Project path
$projectPath = Get-Location

# Path to README
$readmePath = Join-Path -Path $projectPath -ChildPath "README_CHANGES.md"

# README content
$readmeContent = @"
# Changes in SiemensTrend project

## Removed:
- Automatic tag retrieval from TIA Portal project
- All classes and methods related to automatic tag parsing

## Added:
- Manual tag management (adding, editing, deleting)
- Dialog window for tag editing
- Import/export of tags from CSV
- Saving tags to XML file

## Preserved:
- Functionality for connecting to TIA Portal
- TIA Portal project selection
- Tag monitoring and trend plotting

## How to use:
1. Connect to TIA Portal by selecting an existing project
2. Add tags manually through the application interface
3. Save tags for future use
4. Start monitoring selected tags

## New code structure:
1. `TagManager` - class for tag management (saving, loading, import, export)
2. `TagEditorDialog` - dialog window for creating and editing tags
3. Updated `MainViewModel` with methods for manual tag management
4. New handlers in `MainWindow` for working with tags

## CSV format for import/export:
Name,Address,DataType,Group,Comment
Tag1,DB1.DBX0.0,Bool,Motor,Example tag
Tag2,M0.0,Bool,Inputs,Another tag

## Note:
Tags are saved to an XML file in the AppData folder and automatically loaded when the application starts.
"@

# Save file
Set-Content -Path $readmePath -Value $readmeContent

Write-Host "Script 5 completed successfully. Created README file with description of changes."