using System;

namespace BMTP3.Core2.NetBackupFlow.Models
{
    // Attribut til at specificere streng-repræsentationen af en nøgle
    public class KeyStringValueAttribute : Attribute
    {
        public string StringValue { get; }
        public KeyStringValueAttribute(string value) { StringValue = value; }
    }
}
