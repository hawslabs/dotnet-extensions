namespace System.Clock;

public static class TimeZoneInfoEx {
	extension(TimeZoneInfo) {
		public static IEnumerable<TimeZoneId> TimeZones =>
			TimeZoneInfo.GetSystemTimeZones()
				.Select(TimeZoneId.FromSystemTimeZone);

		public static IEnumerable<WindowsTimeZoneId> WindowsTimeZones =>
			TimeZoneInfo.GetSystemTimeZones()
				.Select(WindowsTimeZoneId.TryFromSystemTimeZone)
				.OfType<WindowsTimeZoneId>()
				.DistinctBy(id => id.Value);

		public static IEnumerable<IanaTimeZoneId> IanaTimeZones =>
			TimeZoneInfo.GetSystemTimeZones()
				.Select(IanaTimeZoneId.TryFromSystemTimeZone)
				.OfType<IanaTimeZoneId>()
				.DistinctBy(id => id.Value);
	}
}