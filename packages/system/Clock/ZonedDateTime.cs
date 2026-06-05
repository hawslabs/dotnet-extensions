namespace System.Clock;

public readonly record struct ZonedDateTime {
	public Instant Instant { get; }
	public TimeZoneInfo TimeZone { get; }
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

	public ZonedDateTime(Instant instant, TimeZoneInfo timeZone) {
		Instant = instant;
		TimeZone = timeZone;
	}

	public static ZonedDateTime Now(TimeZoneInfo timeZone) {
		return Now(timeZone, TimeProvider.System);
	}

	public static ZonedDateTime Now(TimeZoneInfo timeZone, TimeProvider timeProvider) {
		return timeProvider.GetInstant().InZone(timeZone);
	}

	public static ZonedDateTime FromInstant(
		Instant instant,
		TimeZoneInfo timeZone
	) {
		return new(instant, timeZone);
	}

	public static ZonedDateTime FromDateTime(
		DateTime value,
		TimeZoneInfo timeZone,
		UnspecifiedDateTimeHandling unspecifiedHandling = UnspecifiedDateTimeHandling.Throw
	) {
		return new(
			Instant.FromDateTime(value, unspecifiedHandling),
			timeZone
		);
	}

	public static ZonedDateTime FromDateTimeOffset(
		DateTimeOffset value,
		TimeZoneInfo timeZone
	) {
		return new(
			Instant.FromDateTimeOffset(value),
			timeZone
		);
	}

	public static ZonedDateTime FromLocalDateTime(
		DateTime localDateTime,
		TimeZoneInfo timeZone,
		AmbiguousTimeHandling ambiguousTimeHandling = AmbiguousTimeHandling.Throw
	) {
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

			offset = ambiguousTimeHandling switch {
				AmbiguousTimeHandling.EarlierInstant => offsets.Max(),
				AmbiguousTimeHandling.LaterInstant => offsets.Min(),

				_ => throw new ArgumentException(
					"The supplied local date/time is ambiguous in the given time zone, probably because of a daylight-saving transition.",
					nameof(localDateTime)
				),
			};
		} else {
			offset = timeZone.GetUtcOffset(local);
		}

		var dateTimeOffset = new DateTimeOffset(local, offset);

		return new(
			Instant.FromDateTimeOffset(dateTimeOffset),
			timeZone
		);
	}

	public ZonedDateTime WithTimeZone(TimeZoneInfo timeZone) {
		return new(Instant, timeZone);
	}

	public override string ToString() {
		return $"{LocalDateTime:O} {TimeZone.Id} ({Offset})";
	}
}