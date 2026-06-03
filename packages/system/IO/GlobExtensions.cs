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
			string[] includePatterns,
			string[] excludePatterns,
			GlobsOptions? options = null
		) {
			options ??= GlobsOptions.Default;

			var matcher = new Matcher(
				comparisonType: options.PatternComparison,
				preserveFilterOrder: options.FilterOptions.PreserveFilterOrder
			);

			matcher.AddIncludePatterns(includePatterns.SelectMany(BraceExpander.Expander.Expand));
			matcher.AddExcludePatterns(excludePatterns.SelectMany(BraceExpander.Expander.Expand));

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