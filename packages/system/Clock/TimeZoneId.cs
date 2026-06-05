namespace System.Clock;

public sealed record TimeZoneId {
	private TimeZoneId(
		string value,
		WindowsTimeZoneId? windowsId,
		IanaTimeZoneId? ianaId
	) {
		Value = value;
		WindowsId = windowsId;
		IanaId = ianaId;
	}

	public string Value { get; }
	public WindowsTimeZoneId? WindowsId { get; }
	public IanaTimeZoneId? IanaId { get; }
	public bool IsWindows => WindowsId is not null;
	public bool IsIana => IanaId is not null;
	public TimeZoneInfo Info => TimeZoneInfo.FindSystemTimeZoneById(Value);
	public static IReadOnlyCollection<TimeZoneId> All { get; } =
		TimeZoneInfo.TimeZones.ToArray();

	public override string ToString() => Value;

	public WindowsTimeZoneId ToWindowsId() {
		return WindowsId ?? IanaId!.ToWindowsId();
	}

	public IanaTimeZoneId ToIanaId(string? region = null) {
		return IanaId ?? WindowsId!.ToIanaId(region);
	}

	public static TimeZoneId From(WindowsTimeZoneId windowsId) {
		var ianaId = TimeZoneInfo.TryConvertWindowsIdToIanaId(windowsId.Value, out var value)
			? IanaTimeZoneId.FromCustomTimeZone(TimeZoneInfo.FindSystemTimeZoneById(value))
			: null;

		return new(windowsId.Value, windowsId, ianaId);
	}

	public static TimeZoneId From(IanaTimeZoneId ianaId) {
		var windowsId = TimeZoneInfo.TryConvertIanaIdToWindowsId(ianaId.Value, out var value)
			? WindowsTimeZoneId.CreateUnchecked(value)
			: null;

		return new(ianaId.Value, windowsId, ianaId);
	}

	internal static TimeZoneId FromSystemTimeZone(TimeZoneInfo timeZone) {
		WindowsTimeZoneId? windowsId = null;
		IanaTimeZoneId? ianaId = null;

		if (TimeZoneInfo.TryConvertWindowsIdToIanaId(timeZone.Id, out var ianaValue)) {
			windowsId = WindowsTimeZoneId.CreateUnchecked(timeZone.Id);
			ianaId = IanaTimeZoneId.FromCustomTimeZone(TimeZoneInfo.FindSystemTimeZoneById(ianaValue));
		} else if (TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZone.Id, out var windowsValue)) {
			windowsId = WindowsTimeZoneId.CreateUnchecked(windowsValue);
			ianaId = IanaTimeZoneId.FromCustomTimeZone(timeZone);
		}

		return new(timeZone.Id, windowsId, ianaId);
	}

	public static implicit operator TimeZoneId(WindowsTimeZoneId id) => From(id);
	public static implicit operator TimeZoneId(IanaTimeZoneId id) => From(id);
	public static implicit operator string(TimeZoneId id) => id.Value;

	[UsedByLinqPad]
	private object ToDump() {
		return new {
            Value,
			IanaTimeZoneId = IanaId?.Value,
			WindowsTimeZoneId = WindowsId?.Value,
			Info.DisplayName,
			Info.BaseUtcOffset,
		};
	}
}