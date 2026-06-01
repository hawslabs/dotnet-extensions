namespace System.Trees;

public sealed record TreeNodeParseOptions {
	public string? BasePath { get; init; }
	public string? RootName { get; init; }
	public bool SortInputPaths { get; init; } = true;
	public StringComparer PathSortComparer { get; init; } = StringComparer.OrdinalIgnoreCase;
	public StringComparison PathComparison { get; init; } = StringComparison.OrdinalIgnoreCase;
	public IEqualityComparer<string> ChildNameComparer { get; init; } = StringComparer.OrdinalIgnoreCase;
	public bool ReadFileMetrics { get; init; } = true;
	public bool IgnoreFileReadErrors { get; init; } = false;
	public bool IgnoreFilesOutsideBasePath { get; init; } = false;
	public Func<string, IEnumerable<string>> ReadLines { get; init; } = File.ReadLines;
	public Func<string, bool> LabelPredicate { get; init; }
		= static line => line.TrimStart().StartsWith("label_", StringComparison.Ordinal);
}