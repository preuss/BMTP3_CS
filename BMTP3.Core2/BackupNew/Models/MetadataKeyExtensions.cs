using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Models;
// Extra helper method – that does make it easy to get the string representation
public static class MetadataKeyExtensions
{
	public static string GetKey(this MetadataKey key)
	{
		var field = key.GetType().GetField(key.ToString())
					?? throw new ArgumentException($"No field found for {key}");

		var attribute = field.GetCustomAttributes(typeof(MetadataKeyInfoAttribute), false)
							 .Cast<MetadataKeyInfoAttribute>()
							 .FirstOrDefault();

		return attribute?.Key ?? key.ToString();
	}
}