using System.Collections.Generic;

namespace Siemens_trend.Helpers
{
    public class PlcTag
    {
        public string Name { get; set; } = "";
        public string DataType { get; set; } = "";
        public string LogicalAddress { get; set; } = "";
        public string TableName { get; set; } = ""; // Название таблицы тегов (если есть)
        public bool IsOptimized { get; set; } = false; // ✅ Новое свойство
    }

    public class PlcDbVariable
    {
        public string Name { get; set; } = "";
        public string DataType { get; set; } = "";
        public string LogicalAddress { get; set; } = "";
    }

    public class PlcDb
    {
        public string Name { get; set; } = "";
        public bool IsOptimized { get; set; } = false;
        public bool IsSafety { get; set; } = false;  // Safety DB
        public bool IsUDT { get; set; } = false;     // UDT DB
        public List<PlcDbVariable> Variables { get; set; } = new List<PlcDbVariable>();
    }

    public class PlcData
    {
        public List<PlcTag> Tags { get; set; } = new List<PlcTag>();
        public List<PlcDb> DataBlocks { get; set; } = new List<PlcDb>();
    }
}
