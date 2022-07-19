using System.Collections.Generic;

namespace SourceGenerator.Models
{
    public class ClassDefinition
    {
        public string ClassName { get; set; }
        public List<PropertyDefinition> Properties { get; set; }
    }

    public class PropertyDefinition
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool Nullable { get; set; } = false;
    }
}

