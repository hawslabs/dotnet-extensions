namespace System.Trees.Formatting.Ascii;

using Parsing;

public static class AsciiTreeExtensions {
	extension(IEnumerable<FileInfo> paths) {
        public FileTree ToFileTree() {
	        ArgumentNullException.ThrowIfNull(paths);

	        return FileTreeParser.ParseTree(paths, new() {
		        BasePath = paths.FindCommonBasePath(StringComparison.OrdinalIgnoreCase),
		        PathComparison = StringComparison.OrdinalIgnoreCase,
		        LabelPredicate = static line => line.TrimStart().StartsWith("label_", StringComparison.Ordinal),
	        });
        }

		public AsciiTree ToAsciiTree(AsciiTreeFormatterOptions? options = null) {
			options ??= new() {
				SortOrder = TreeSortOrder.Alphabetical,
				ShowIcons = true,
				ShowLabels = true,
				AlignColumns = true,
			};

			var fileTree = paths.ToFileTree();

			return new(
				fileTree.RootPath,
				fileTree.Root.Format(new AsciiTreeFormatter(options))
			);
		}
	}
}