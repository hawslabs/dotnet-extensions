namespace System.IO;

using Collections.Generic;
using Microsoft.Extensions.FileSystemGlobbing;

public sealed record GlobsOptions(
	StringComparison PatternComparison,
	FilterOptions FilterOptions,
	SortOptions SortOptions
) {
	public static readonly GlobsOptions Default = new(
		PatternComparison: StringComparison.OrdinalIgnoreCase,
		FilterOptions: FilterOptions.Default,
		SortOptions: SortOptions.Default
	);

	public GlobPatternMode PatternMode { get; init; } = GlobPatternMode.Merge;
	public IReadOnlyCollection<string>? IncludePatterns { get; init; }
	public IReadOnlyCollection<string>? ExcludePatterns { get; init; }
	public IReadOnlyCollection<string> DefaultIncludePatterns { get; init; } = ["**/*"];
	public IReadOnlyCollection<string> DefaultExcludePatterns { get; init; } = [
		".git",
		"**/node_modules/",
		"**/{artifacts,.artifacts,obj,bin,.vs}/",
	];
	public IReadOnlyCollection<string> EffectiveIncludePatterns => GetEffectivePatterns(IncludePatterns, DefaultIncludePatterns);
	public IReadOnlyCollection<string> EffectiveExcludePatterns => GetEffectivePatterns(ExcludePatterns, DefaultExcludePatterns);

	private IReadOnlyCollection<string> GetEffectivePatterns(
		IReadOnlyCollection<string>? patterns,
		IReadOnlyCollection<string> defaultPatterns
	) {
		if (patterns is null) {
			return defaultPatterns
				.SelectMany(BraceExpander.Expander.Expand)
				.ToArray();
		}

		if (PatternMode is GlobPatternMode.Merge) {
			return defaultPatterns
				.Concat(patterns)
				.SelectMany(BraceExpander.Expander.Expand)
				.ToArray();
		}

		return patterns
			.SelectMany(BraceExpander.Expander.Expand)
			.ToArray();
	}
}

public enum GlobPatternMode {
	Fallback,
	Merge,
}

public sealed record SortOptions(
	bool Enabled,
	StringComparison Comparison
) {
	public static readonly SortOptions Default = new(
		Enabled: true,
		Comparison: StringComparison.OrdinalIgnoreCase
	);
}

public sealed record FilterOptions(
	bool PreserveFilterOrder = false
) {
	public static readonly FilterOptions Default = new();
}

public static class GlobExtensions {

	extension(DirectoryInfo directory) {
		public IEnumerable<FileInfo> Glob(
			GlobsOptions? options = null
		) {
			options ??= GlobsOptions.Default;

			var matcher = new Matcher(
				comparisonType: options.PatternComparison,
				preserveFilterOrder: options.FilterOptions.PreserveFilterOrder
			);

			matcher.AddIncludePatterns(options.EffectiveIncludePatterns);
			matcher.AddExcludePatterns(options.EffectiveExcludePatterns);

			var filePaths = matcher.GetResultsInFullPath(directory.FullName);

			if (!options.SortOptions.Enabled) {
				return filePaths.Select(path => new FileInfo(path));
			}

			return filePaths
				.Order(StringComparer.FromComparison(options.SortOptions.Comparison))
				.Select(path => new FileInfo(path));
		}
	}
}