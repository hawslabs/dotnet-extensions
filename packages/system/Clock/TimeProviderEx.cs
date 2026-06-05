namespace System.Clock;

public static class TimeProviderEx {
	extension(TimeProvider time) {
		public ZonedInstant GetZoned(TimeZoneInfo timeZone) {
			return ZonedInstant.Now(timeZone, time);
		}

		public Instant GetInstant() {
			return Instant.FromDateTimeOffset(time.GetUtcNow());
		}
	}
}