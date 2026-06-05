namespace System.Clock;

internal static class DateTimeNormalization {
	public static DateTime ToUtc(
		DateTime value,
		UnspecifiedDateTimeHandling unspecifiedHandling = UnspecifiedDateTimeHandling.Throw
	) {
		return value.Kind switch {
			DateTimeKind.Utc => value,
			DateTimeKind.Local => value.ToUniversalTime(),
			DateTimeKind.Unspecified => unspecifiedHandling switch {
				UnspecifiedDateTimeHandling.AssumeUtc => DateTime.SpecifyKind(value, DateTimeKind.Utc),
				UnspecifiedDateTimeHandling.AssumeLocal => DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime(),
				_ => throw new ArgumentException("DateTime.Kind is Unspecified. Specify whether to assume UTC or local time.", nameof(value)),
			},
			_ => throw new ArgumentOutOfRangeException(nameof(value)),
		};
	}
}

public enum UnspecifiedDateTimeHandling {
	Throw,
	AssumeUtc,
	AssumeLocal,
}

public enum AmbiguousTimeHandling {
	Throw,
	EarlierInstant,
	LaterInstant,
}