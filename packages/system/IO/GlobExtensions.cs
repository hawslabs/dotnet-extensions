namespace System.IO;

using Collections.Generic;

using Microsoft.Extensions.FileSystemGlobbing;

public sealed record GlobsOptions {
	public StringComparison PatternComparison { get; init; } = StringComparison.OrdinalIgnoreCase;
	public bool PreserveFilterOrder { get; init; } = false;
	public bool SortResults { get; init; } = true;
	public StringComparer SortComparer { get; init; } = StringComparer.OrdinalIgnoreCase;
}

public static class GlobExtensions {
	extension(DirectoryInfo directory) {
		public IEnumerable<FileInfo> Glob(
			string[] includePatterns,
			string[] excludePatterns,
			GlobsOptions? options = null
		) {
			options ??= new GlobsOptions();

			var matcher = new Matcher(
				comparisonType: options.PatternComparison,
				preserveFilterOrder: options.PreserveFilterOrder
			);

			matcher.AddIncludePatterns(includePatterns.SelectMany(BraceExpander.Expander.Expand));
			matcher.AddExcludePatterns(excludePatterns.SelectMany(BraceExpander.Expander.Expand));

			var filePaths = matcher.GetResultsInFullPath(directory.FullName);

			if (options.SortResults) {
				filePaths = filePaths.Order(options.SortComparer);
			}

			return filePaths.Select(path => new FileInfo(path));
		}
	}
}