namespace System.Trees.Formatting.Ascii;

public static class AsciiTreeExtensions {
	extension(IEnumerable<FileInfo> paths) {
		public string ToAsciiTree(AsciiTreeFormatterOptions? options = null) {
			options ??= new() {
				SortOrder = TreeSortOrder.Alphabetical,
				ShowIcons = true,
				ShowLabels = true,
				AlignColumns = true,
			};

			var basePath = paths.FindCommonBasePath(StringComparison.OrdinalIgnoreCase);
			var tree = paths.ToFileTree(basePath);

			return tree.Format(
				new AsciiTreeFormatter(options)
			);
		}
	}
}