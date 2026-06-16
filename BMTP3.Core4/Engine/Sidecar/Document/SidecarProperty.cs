namespace BMTP3.Core4.Engine.Sidecar.Document;

internal sealed record SidecarProperty(string Key, string? Value, int Weight = 100)
{
	public string Key { get; } = Key ?? throw new ArgumentNullException(nameof(Key));
	public string? Comment { get; init; }

	public static SidecarProperty From(string key, DateTime? value, int weight = 100, string? comment = null) =>
		new(key, value?.ToString("o"), weight) { Comment = comment };

	public static SidecarProperty From(string key, DateTimeOffset? value, int weight = 100, string? comment = null) =>
		new(key, value?.ToString("o"), weight) { Comment = comment };

	public static SidecarProperty From(string key, long? value, int weight = 100, string? comment = null) =>
		new(key, value?.ToString(), weight) { Comment = comment };

	public static SidecarProperty From(string key, int? value, int weight = 100, string? comment = null) =>
		new(key, value?.ToString(), weight) { Comment = comment };

	public static SidecarProperty From(string key, bool? value, int weight = 100, string? comment = null) =>
		new(key, value?.ToString().ToLowerInvariant(), weight) { Comment = comment };

	public static SidecarProperty From(string key, string? value, int weight = 100, string? comment = null) =>
		new(key, value, weight) { Comment = comment };
}
