using BMTP3.Core2.NetBackupFlow.Models;
using System;
using System.Reflection;
using System.Linq;

namespace BMTP3.Core2.NetBackupFlow.Extensions
{
    public static class MetadataKeyExtensions
    {
        // Metode til at læse KeyStringValueAttribute via reflection
        public static string ToKeyString(this MetadataKey key)
        {
            return key.GetType()
                      .GetMember(key.ToString())
                      .First()
                      .GetCustomAttribute<KeyStringValueAttribute>()
                      ?.StringValue ?? key.ToString(); // Fallback til enum navn hvis attribut mangler
        }
    }
}
