using BMTP3.Core2.BackupNew.Domain.Item;
using System.Reflection;

namespace BMTP3.Core2.NetBackupFlow.Extensions;

public static class MetadataKeyExtensions
{
	// Metode til at læse KeyStringValueAttribute via reflection
	public static string ToKeyString(this MetadataKey key)
	{
		FieldInfo field = key.GetType().GetField(key.ToString())
					?? throw new ArgumentException($"No field found for {key}");

		KeyStringValueAttribute? attribute = field.GetCustomAttribute<KeyStringValueAttribute>()
				?? throw new ArgumentException($"No KeyStringValueAttribute found for {key}");

		return attribute.StringValue;
	}
}
