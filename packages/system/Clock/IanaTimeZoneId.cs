namespace System.Clock;

public sealed record IanaTimeZoneId {
	public string Value { get; }

	private IanaTimeZoneId(string value) {
		Value = value;
	}

	public static IEnumerable<IanaTimeZoneId> All => TimeZoneInfo.IanaTimeZones;

	public static IanaTimeZoneId Utc { get; } = new("Etc/UTC");

	public TimeZoneInfo Info => TimeZoneInfo.FindSystemTimeZoneById(Value);

	public override string ToString() => Value;

	public WindowsTimeZoneId ToWindowsId() {
		return WindowsTimeZoneId.FromIanaId(this);
	}

	public static IanaTimeZoneId From(string value) {
		if (!TimeZoneInfo.TryConvertIanaIdToWindowsId(value, out _)) {
			throw new TimeZoneNotFoundException(
				$"No Windows timezone mapping found for IANA timezone '{value}'."
			);
		}

		return new(value);
	}

	public static IanaTimeZoneId FromWindowsId(WindowsTimeZoneId windowsId, string? region = null) {
		if (region is null) {
			if (TimeZoneInfo.TryConvertWindowsIdToIanaId(windowsId.Value, out var ianaId)) {
				return new(ianaId);
			}
		} else {
			if (TimeZoneInfo.TryConvertWindowsIdToIanaId(windowsId.Value, region, out var ianaId)) {
				return new(ianaId);
			}
		}

		throw new TimeZoneNotFoundException(
			$"No IANA timezone mapping found for Windows timezone '{windowsId.Value}'."
		);
	}

	public static IanaTimeZoneId FromCustomTimeZone(TimeZoneInfo timeZone) => new(timeZone.Id);

	internal static IanaTimeZoneId? TryFromSystemTimeZone(TimeZoneInfo timeZone) {
		if (TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZone.Id, out _)) {
			return new(timeZone.Id);
		}

		return TimeZoneInfo.TryConvertWindowsIdToIanaId(timeZone.Id, out var ianaId)
			? new(ianaId)
			: null;
	}

	public static implicit operator string(IanaTimeZoneId id) => id.Value;

    [UsedByLinqPad]
    private object ToDump() {
		return new {
			IanaTimeZoneId = Value,
			Info.DisplayName,
            Info.BaseUtcOffset,
		};
	}
}