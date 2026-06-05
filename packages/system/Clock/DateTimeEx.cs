using System.Clock.Conversions;

namespace System.Clock;

public static class DateTimeEx {
	extension(DateTime value) {
		public DateTime ToUtc(
			UnspecifiedDateTimeHandling? unspecifiedHandling = null
		) {
			unspecifiedHandling ??= UnspecifiedDateTimeHandling.Throw;

			return value.Kind switch {
				DateTimeKind.Utc => value,
				DateTimeKind.Local => value.ToUniversalTime(),
				DateTimeKind.Unspecified => unspecifiedHandling.ToUtc(value),
				_ => throw new ArgumentOutOfRangeException(nameof(value)),
			};
		}

		public ZonedInstant WithTimeZone(TimeZoneId timeZone) {
			return ZonedInstant.FromDateTime(value, timeZone.Info);
		}

		public ZonedInstant WithTimeZone(WindowsTimeZoneId timeZone) {
			return ZonedInstant.FromDateTime(value, timeZone.Info);
		}

		public ZonedInstant WithTimeZone(IanaTimeZoneId timeZone) {
			return ZonedInstant.FromDateTime(value, timeZone.Info);
		}
	}
}