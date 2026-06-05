using System.Clock.Conversions;

namespace System.Clock;

public readonly record struct ZonedInstant(
	Instant Instant,
	TimeZoneInfo TimeZone
) {
	public string TimeZoneId => TimeZone.Id;

	public DateTime LocalDateTime {
		get {
			var local = TimeZoneInfo.ConvertTimeFromUtc(Instant.Value, TimeZone);

			// This is local to TimeZone, not necessarily local to the machine.
			return DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
		}
	}

	public DateTimeOffset LocalDateTimeOffset =>
		TimeZoneInfo.ConvertTime(Instant.OffsetValue, TimeZone);

	public TimeSpan Offset => TimeZone.GetUtcOffset(Instant.Value);

	public static ZonedInstant Now(TimeZoneInfo timeZone) {
		return Now(timeZone, TimeProvider.System);
	}

	public static ZonedInstant Now(TimeZoneInfo timeZone, TimeProvider timeProvider) {
		return timeProvider.GetInstant().InZone(timeZone);
	}

	public static ZonedInstant FromInstant(
		Instant instant,
		TimeZoneInfo timeZone
	) {
		return new(instant, timeZone);
	}

	public static ZonedInstant FromDateTime(
		DateTime value,
		TimeZoneInfo timeZone,
		UnspecifiedDateTimeHandling? unspecifiedHandling = null
	) {
		return new(
			Instant.FromDateTime(value, unspecifiedHandling),
			timeZone
		);
	}

	public static ZonedInstant FromDateTimeOffset(
		DateTimeOffset value,
		TimeZoneInfo timeZone
	) {
		return new(
			Instant.FromDateTimeOffset(value),
			timeZone
		);
	}

	public static ZonedInstant FromLocalDateTime(
		DateTime localDateTime,
		TimeZoneInfo timeZone,
		AmbiguousTimeHandling? ambiguousTimeHandling = null
	) {
		ambiguousTimeHandling ??= AmbiguousTimeHandling.Throw;

		var local = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);

		if (timeZone.IsInvalidTime(local)) {
			throw new ArgumentException(
				"The supplied local date/time does not exist in the given time zone, probably because of a daylight-saving transition.",
				nameof(localDateTime)
			);
		}

		TimeSpan offset;

		if (timeZone.IsAmbiguousTime(local)) {
			var offsets = timeZone.GetAmbiguousTimeOffsets(local);
			offset = ambiguousTimeHandling.GetOffset(localDateTime, offsets);
		} else {
			offset = timeZone.GetUtcOffset(local);
		}

		var dateTimeOffset = new DateTimeOffset(local, offset);

		return new(
			Instant.FromDateTimeOffset(dateTimeOffset),
			timeZone
		);
	}

	public ZonedInstant WithTimeZone(TimeZoneInfo timeZone) {
		return new(Instant, timeZone);
	}

	public ZonedInstant ToTimeZone(TimeZoneId timeZone) {
		return WithTimeZone(timeZone.Info);
	}

	public ZonedInstant ToTimeZone(WindowsTimeZoneId timeZone) {
		return WithTimeZone(timeZone.Info);
	}

	public ZonedInstant ToTimeZone(IanaTimeZoneId timeZone) {
		return WithTimeZone(timeZone.Info);
	}

	public override string ToString() {
		return $"{LocalDateTime:O} {TimeZone.Id} ({Offset})";
	}
}