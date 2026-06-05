namespace System.Clock;

public static class TimeProviderEx {
	extension(TimeProvider time) {
		public ZonedDateTime GetZoned(TimeZoneInfo timeZone) {
			return ZonedDateTime.Now(timeZone, time);
		}

		public Instant GetInstant() {
			return Instant.FromDateTimeOffset(time.GetUtcNow());
		}
	}
}