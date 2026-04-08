namespace BMTP3.Core2.BackupNew.Domain.Item;

/// <summary>
///     Attribute to specify the string representation of a key
///     Attribute that makes it possible to map the enum value to a readable string during serialization.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class KeyStringValueAttribute : Attribute
{
	public KeyStringValueAttribute(string value)
	{
		StringValue = value;
	}

	public string StringValue { get; }
}