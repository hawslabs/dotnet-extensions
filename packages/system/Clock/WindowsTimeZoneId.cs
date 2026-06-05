namespace System.Clock;

public sealed record WindowsTimeZoneId {
	public string Value { get; }

	private WindowsTimeZoneId(string value) {
		Value = value;
	}

	public static IEnumerable<WindowsTimeZoneId> All => TimeZoneInfo.WindowsTimeZones;

	public static WindowsTimeZoneId Utc { get; } = new("UTC");

	public TimeZoneInfo Info => TimeZoneInfo.FindSystemTimeZoneById(Value);

	public override string ToString() => Value;

	public IanaTimeZoneId ToIanaId(string? region = null) {
		return IanaTimeZoneId.FromWindowsId(this, region);
	}

	public static WindowsTimeZoneId From(string value) {
		if (!TimeZoneInfo.TryConvertWindowsIdToIanaId(value, out _)) {
			throw new TimeZoneNotFoundException(
				$"No IANA timezone mapping found for Windows timezone '{value}'."
			);
		}

		return new(value);
	}

	public static WindowsTimeZoneId FromIanaId(IanaTimeZoneId ianaId) {
		if (!TimeZoneInfo.TryConvertIanaIdToWindowsId(ianaId.Value, out var windowsId)) {
			throw new TimeZoneNotFoundException(
				$"No Windows timezone mapping found for IANA timezone '{ianaId.Value}'."
			);
		}

		return new(windowsId);
	}

	internal static WindowsTimeZoneId CreateUnchecked(string value) => new(value);

	internal static WindowsTimeZoneId? TryFromSystemTimeZone(TimeZoneInfo timeZone) {
		if (TimeZoneInfo.TryConvertWindowsIdToIanaId(timeZone.Id, out _)) {
			return new(timeZone.Id);
		}

		return TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZone.Id, out var windowsId)
			? new(windowsId)
			: null;
	}

	public static implicit operator string(WindowsTimeZoneId id) => id.Value;

	[UsedByLinqPad]
	private object ToDump() {
		return new {
			WindowsTimeZoneId = Value,
			Info.DisplayName,
			Info.BaseUtcOffset,
		};
	}
}