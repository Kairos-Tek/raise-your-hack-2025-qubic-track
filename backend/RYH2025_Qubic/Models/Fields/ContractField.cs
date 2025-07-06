namespace RYH2025_Qubic.Models.Fields
{
    public class ContractField
    {
        public string Name { get; set; } = string.Empty;
        public string QubicType { get; set; } = string.Empty;
        public string TypeScriptType { get; set; } = string.Empty;
        public int ByteSize { get; set; }
        public int Order { get; set; } // Position in the struct for serialization
        public bool IsArray { get; set; }
        public int? ArraySize { get; set; }
        public string? Description { get; set; }
        public bool IsRequired { get; set; } = true;
        public object? DefaultValue { get; set; }
        public List<string> Validations { get; set; } = new();
    }

}
