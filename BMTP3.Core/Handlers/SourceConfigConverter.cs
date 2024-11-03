using BMTP3.Core.BackupSource;
using BMTP3.Core.Configs;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core.Handlers {
	public class SourceConfigConverter : JsonConverter<ISourceConfig?> {
		public override ISourceConfig? ReadJson(JsonReader reader, Type objectType, ISourceConfig? existingValue, bool hasExistingValue, JsonSerializer serializer) {
			JObject jo = JObject.Load(reader);
			SourceType? sourceType = jo["SourceType"]?.ToObject<SourceType>(serializer); // Uses SourceTypeConverter for ReadJson

			if(sourceType == null) {
				throw new ArgumentException("Invalid source type", nameof(sourceType));
			}

			switch(sourceType) {
				case SourceType.Drive:
					return jo.ToObject<DriveSourceConfig>();
				case SourceType.Device:
					return jo.ToObject<DeviceSourceConfig>();
				default:
					throw new ArgumentException("Invalid source type", nameof(sourceType));
			}
		}
		public override void WriteJson(JsonWriter writer, ISourceConfig? value, JsonSerializer serializer) {
			if(value == null) {
				// Handle the case when value is null
				// For example, you can throw an exception or write a default value
				throw new ArgumentNullException(nameof(value));
			}

			JObject jo = JObject.FromObject(value);

			// Check if the property already exists
			jo.Remove("SourceType");
			if(!jo.ContainsKey("SourceType")) {
				jo.Add("SourceType", JToken.FromObject(value.SourceType.ToString()));
			}

			jo.WriteTo(writer);
		}
	}
}
