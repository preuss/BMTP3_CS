using BMTP3.Core.BackupSource;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BMTP3.Core.Handlers {
	public class SourceTypeConverter : JsonConverter<SourceType> {
		public override SourceType ReadJson(JsonReader reader, Type objectType, SourceType existingValue, bool hasExistingValue, JsonSerializer serializer) {
			JToken token = JToken.Load(reader);
			if(token.Type == JTokenType.String && token.Value<string>() is string value) {
				return (SourceType)Enum.Parse(typeof(SourceType), value);
			} else {
				throw new Exception("Invalid JSON value for SourceType");
			}
		}
		public override void WriteJson(JsonWriter writer, SourceType value, JsonSerializer serializer) {
			// Not really used
			// TODO: Find out why not used, and why this code has to be added to SourceConfigConverter.WriteJson
			// TODO: Find out how to use this in SourceConfigConverter
			writer.WriteValue(value.ToString());
		}
	}
}
