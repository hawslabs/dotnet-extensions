namespace System.Formatting.AsciiTree;

public static class AsciiTreeExtensions {
	extension(IEnumerable<FileInfo> paths) {
		public string ToAsciiTree(string basePath, AsciiTreeNodeFormatterOptions? options = null) {
			options ??= new AsciiTreeNodeFormatterOptions {
				SortOrder = TreeSortOrder.Alphabetical,
				ShowIcons = true,
				ShowLabels = true,
				AlignColumns = true,
			};

			var rootNode = Trees.TreeNode.Parse(
				paths,
				new Trees.TreeNodeParseOptions {
					BasePath = basePath,
					LabelPredicate = static line => line.TrimStart().StartsWith("label_", StringComparison.Ordinal),
				}
			);

			return rootNode.Format(
				new AsciiTreeNodeFormatter(options)
			);
		}
	}
}