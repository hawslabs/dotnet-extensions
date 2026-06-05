namespace System.Clock;


public abstract class AmbiguousTimeHandling {
	public static readonly AmbiguousTimeHandling Throw = new ThrowAmbiguousTimeHandling();
	public static readonly AmbiguousTimeHandling EarlierInstant = new EarlierInstantAmbiguousTimeHandling();
	public static readonly AmbiguousTimeHandling LaterInstant = new LaterInstantAmbiguousTimeHandling();

	private AmbiguousTimeHandling() {
	}

	public abstract TimeSpan GetOffset(DateTime localDateTime, TimeSpan[] ambiguousTimeOffsets);

	private sealed class ThrowAmbiguousTimeHandling : AmbiguousTimeHandling {
		public override TimeSpan GetOffset(DateTime localDateTime, TimeSpan[] ambiguousTimeOffsets) {
			throw new ArgumentException(
				"The supplied local date/time is ambiguous in the given time zone, probably because of a daylight-saving transition.",
				nameof(localDateTime)
			);
		}
	}

	private sealed class EarlierInstantAmbiguousTimeHandling : AmbiguousTimeHandling {
		public override TimeSpan GetOffset(DateTime localDateTime, TimeSpan[] ambiguousTimeOffsets) {
			return ambiguousTimeOffsets.Max();
		}
	}

	private sealed class LaterInstantAmbiguousTimeHandling : AmbiguousTimeHandling {
		public override TimeSpan GetOffset(DateTime localDateTime, TimeSpan[] ambiguousTimeOffsets) {
			return ambiguousTimeOffsets.Min();
		}
	}
}