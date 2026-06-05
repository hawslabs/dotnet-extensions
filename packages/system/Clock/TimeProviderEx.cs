namespace System.Clock;

public static class TimeProviderEx {
	extension(TimeProvider time) {
		public ZonedInstant GetInstantInTimeZone(TimeZoneInfo timeZone) {
			return ZonedInstant.Now(timeZone, time);
		}

		public ZonedInstant GetInstantInTimeZone(TimeZoneId timeZone) {
			return time.GetInstantInTimeZone(timeZone.Info);
		}

		public ZonedInstant GetInstantInTimeZone(WindowsTimeZoneId timeZone) {
			return time.GetInstantInTimeZone(timeZone.Info);
		}

		public ZonedInstant GetInstantInTimeZone(IanaTimeZoneId timeZone) {
			return time.GetInstantInTimeZone(timeZone.Info);
		}

		public Instant GetInstant() {
			return Instant.FromDateTimeOffset(time.GetUtcNow());
		}
	}
}