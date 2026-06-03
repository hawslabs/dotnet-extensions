namespace System.Trees.Formatting.Ascii;

public sealed record AsciiTreeNodeFormatterOptions {
	public TreeSortOrder SortOrder { get; init; } = TreeSortOrder.Alphabetical;
	public StringComparer NameComparer { get; init; } = StringComparer.OrdinalIgnoreCase;
	public bool AlignColumns { get; init; } = true;
	public bool ShowIcons { get; init; } = true;
	public bool ShowLineCounts { get; init; } = true;
	public bool ShowLabels { get; init; } = false;
	public bool ShowZeroLabelCounts { get; init; } = false;
	public string DirectoryIcon { get; init; } = "📁";
	public string FileIcon { get; init; } = "📄";
	public string LabelIcon { get; init; } = "🏷️";
	public string ColumnSeparator { get; init; } = "  ";
	public string LabelSeparator { get; init; } = "  ";
	public string NumberFormat { get; init; } = "N0";
	public CultureInfo Culture { get; init; } = CultureInfo.CurrentCulture;
}