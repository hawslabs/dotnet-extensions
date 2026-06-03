namespace System.Trees.Formatting.Ascii;

using Parsing;

public static class AsciiTreeExtensions {
	extension(IEnumerable<FileInfo> paths) {
		public AsciiTree ToAsciiTree(AsciiTreeFormatterOptions? options = null) {
			ArgumentNullException.ThrowIfNull(paths);

			options ??= new() {
				SortOrder = TreeSortOrder.Alphabetical,
				ShowIcons = true,
				ShowLabels = true,
				AlignColumns = true,
			};

			var fileTree = FileTreeParser.ParseTree(paths, new() {
				BasePath = paths.FindCommonBasePath(StringComparison.OrdinalIgnoreCase),
				PathComparison = StringComparison.OrdinalIgnoreCase,
				LabelPredicate = static line => line.TrimStart().StartsWith("label_", StringComparison.Ordinal),
			});

			return new(
				fileTree.RootPath,
				fileTree.Root.Format(new AsciiTreeFormatter(options))
			);
		}
	}
}