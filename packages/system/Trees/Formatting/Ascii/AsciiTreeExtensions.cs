namespace System.Trees.Formatting.Ascii;

using Parsing;

public static class AsciiTreeExtensions {
	extension(IEnumerable<FileInfo> paths) {
		public string ToAsciiTree(string basePath, AsciiTreeNodeFormatterOptions? options = null) {
			options ??= new() {
				SortOrder = TreeSortOrder.Alphabetical,
				ShowIcons = true,
				ShowLabels = true,
				AlignColumns = true,
			};

			var rootNode = FileTreeParser.Parse(paths, new() {
				BasePath = basePath,
				LabelPredicate = static line => line.TrimStart().StartsWith("label_", StringComparison.Ordinal),
			});

			return rootNode.Format(
				new AsciiTreeNodeFormatter(options)
			);
		}
	}
}