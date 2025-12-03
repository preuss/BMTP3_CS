namespace BMTP3.Core2.BackupNew2.Models;
[AttributeUsage(AttributeTargets.Field)]
// Attribute to specify the string representation of a key
public class KeyStringValueAttribute : Attribute
{
	public string StringValue { get; }
	public KeyStringValueAttribute(string value) { StringValue = value; }
}
