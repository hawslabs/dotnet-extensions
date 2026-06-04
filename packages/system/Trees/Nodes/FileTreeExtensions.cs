using System.Trees.Parsing;

public static class FileTreeExtensions {
	extension(IEnumerable<FileInfo> files) {
		public FileTree ToFileTree() {
			ArgumentNullException.ThrowIfNull(files);

			return FileTreeParser.ParseTree(files, new() {
				BasePath = files.FindCommonBasePath(StringComparison.OrdinalIgnoreCase),
				PathComparison = StringComparison.OrdinalIgnoreCase,
				LabelPredicate = static line => line.TrimStart().StartsWith("label_", StringComparison.Ordinal),
			});
		}
	}
}
