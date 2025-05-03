using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using SiemensTrend.Core.Logging;
using SiemensTrend.Core.Models;

namespace SiemensTrend.Storage.TagManagement
{
    /// <summary>
    /// Class for tag management (manual adding, editing, saving)
    /// </summary>
    public class TagManager
    {
        private readonly Logger _logger;
        private readonly string _tagsFilePath;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger">Logger</param>
        public TagManager(Logger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Define path to tag storage file
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SiemensTrend");
                
            // Create directory if it doesn't exist
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }
            
            _tagsFilePath = Path.Combine(appDataPath, "Tags.xml");
            _logger.Info($"TagManager: Initialized with tags file path: {_tagsFilePath}");
        }

        /// <summary>
        /// Load tags from XML file
        /// </summary>
        /// <returns>List of tags</returns>
        public List<TagDefinition> LoadTags()
        {
            try
            {
                if (!File.Exists(_tagsFilePath))
                {
                    _logger.Info("LoadTags: Tags file doesn't exist, returning empty list");
                    return new List<TagDefinition>();
                }

                _logger.Info($"LoadTags: Loading tags from file {_tagsFilePath}");
                
                using (var fileStream = new FileStream(_tagsFilePath, FileMode.Open, FileAccess.Read))
                {
                    var serializer = new XmlSerializer(typeof(List<TagDefinition>));
                    var tags = (List<TagDefinition>)serializer.Deserialize(fileStream);
                    
                    _logger.Info($"LoadTags: Successfully loaded {tags.Count} tags");
                    return tags;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"LoadTags: Error loading tags: {ex.Message}");
                return new List<TagDefinition>();
            }
        }

        /// <summary>
        /// Save tags to XML file
        /// </summary>
        /// <param name="tags">List of tags to save</param>
        /// <returns>True if save is successful</returns>
        public bool SaveTags(List<TagDefinition> tags)
        {
            try
            {
                _logger.Info($"SaveTags: Saving {tags.Count} tags to file {_tagsFilePath}");
                
                using (var fileStream = new FileStream(_tagsFilePath, FileMode.Create, FileAccess.Write))
                {
                    var serializer = new XmlSerializer(typeof(List<TagDefinition>));
                    serializer.Serialize(fileStream, tags);
                }
                
                _logger.Info("SaveTags: Tags saved successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"SaveTags: Error saving tags: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Import tags from CSV file
        /// </summary>
        /// <param name="filePath">Path to CSV file</param>
        /// <returns>List of imported tags</returns>
        public List<TagDefinition> ImportTagsFromCsv(string filePath)
        {
            try
            {
                _logger.Info($"ImportTagsFromCsv: Importing tags from file {filePath}");
                
                if (!File.Exists(filePath))
                {
                    _logger.Error($"ImportTagsFromCsv: File doesn't exist: {filePath}");
                    return new List<TagDefinition>();
                }
                
                var tags = new List<TagDefinition>();
                var lines = File.ReadAllLines(filePath);
                
                // Skip header if exists
                bool hasHeader = lines.Length > 0 && 
                    (lines[0].Contains("Name") || lines[0].Contains("Address") || 
                     lines[0].Contains("DataType") || lines[0].Contains("Group"));
                
                int startLine = hasHeader ? 1 : 0;
                
                for (int i = startLine; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line))
                        continue;
                    
                    string[] parts = line.Split(',');
                    if (parts.Length < 3)
                        continue; // Not enough data
                    
                    string name = parts[0].Trim();
                    string address = parts[1].Trim();
                    string dataTypeStr = parts.Length > 2 ? parts[2].Trim() : "Unknown";
                    string groupName = parts.Length > 3 ? parts[3].Trim() : string.Empty;
                    string comment = parts.Length > 4 ? parts[4].Trim() : string.Empty;
                    
                    // Determine data type
                    TagDataType dataType;
                    switch (dataTypeStr.ToLower())
                    {
                        case "bool":
                            dataType = TagDataType.Bool;
                            break;
                        case "int":
                            dataType = TagDataType.Int;
                            break;
                        case "dint":
                            dataType = TagDataType.DInt;
                            break;
                        case "real":
                            dataType = TagDataType.Real;
                            break;
                        default:
                            dataType = TagDataType.Other;
                            break;
                    }
                    
                    // Create new tag
                    var tag = new TagDefinition
                    {
                        Id = Guid.NewGuid(),
                        Name = name,
                        Address = address,
                        DataType = dataType,
                        GroupName = groupName,
                        Comment = comment,
                        IsDbTag = address.Contains("DB") || name.Contains("DB")
                    };
                    
                    tags.Add(tag);
                }
                
                _logger.Info($"ImportTagsFromCsv: Successfully imported {tags.Count} tags");
                return tags;
            }
            catch (Exception ex)
            {
                _logger.Error($"ImportTagsFromCsv: Error importing tags: {ex.Message}");
                return new List<TagDefinition>();
            }
        }

        /// <summary>
        /// Export tags to CSV file
        /// </summary>
        /// <param name="tags">List of tags</param>
        /// <param name="filePath">Path to file</param>
        /// <returns>True if export is successful</returns>
        public bool ExportTagsToCsv(List<TagDefinition> tags, string filePath)
        {
            try
            {
                _logger.Info($"ExportTagsToCsv: Exporting {tags.Count} tags to file {filePath}");
                
                // Create header
                var lines = new List<string>
                {
                    "Name,Address,DataType,Group,Comment"
                };
                
                // Add data
                foreach (var tag in tags)
                {
                    // Escape fields with commas
                    string name = EscapeCsvField(tag.Name);
                    string address = EscapeCsvField(tag.Address);
                    string dataType = tag.DataType.ToString();
                    string group = EscapeCsvField(tag.GroupName ?? string.Empty);
                    string comment = EscapeCsvField(tag.Comment ?? string.Empty);
                    
                    string line = $"{name},{address},{dataType},{group},{comment}";
                    lines.Add(line);
                }
                
                // Save file
                File.WriteAllLines(filePath, lines);
                
                _logger.Info("ExportTagsToCsv: Tags exported successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"ExportTagsToCsv: Error exporting tags: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Escape CSV field
        /// </summary>
        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return string.Empty;
                
            // If the field contains a comma or a quote, wrap it in quotes
            if (field.Contains(",") || field.Contains("\""))
            {
                // Escape internal quotes
                field = field.Replace("\"", "\"\"");
                return $"{field}";
            }
            
            return field;
        }
    }
}
