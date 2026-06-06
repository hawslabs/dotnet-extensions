using System.Runtime.CompilerServices;

namespace System.Guards;

public static class PropertyGuards {
	/// <summary>
	/// Assigns <paramref name="value"/> to <paramref name="field"/> at most once per non-null value.
	/// Clearing (setting to null) is permitted from any state so retry / rescue paths can reset the field.
	/// </summary>
	public static void SetOnceOrClear<T>(
		ref T? field,
		T? value,
		[CallerMemberName] string name = ""
	) where T : class {
		if (field is not null && value is not null) {
			throw new InvalidOperationException($"{name} has already been set.");
		}

		field = value;
	}

	/// <summary>
	/// Assigns <paramref name="value"/> to <paramref name="field"/> exactly once.
	/// Clearing is not allowed; callers must supply a non-null value, and subsequent calls throw.
	/// </summary>
	public static void SetOnce<T>(
		ref T? field,
		T? value,
		[CallerMemberName] string name = ""
	) where T : class {
		ArgumentNullException.ThrowIfNull(value);
		if (field is not null) {
			throw new InvalidOperationException($"{name} has already been set.");
		}

		field = value;
	}
}
