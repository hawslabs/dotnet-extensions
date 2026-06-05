namespace System.Trees.FileSystem;

using System.Trees.FileSystem.Parsing;

public static class FileTreeExtensions {
	extension(IEnumerable<FileInfo> files) {
		public FileTree ToFileTree() {
			return FileTreeParser.ParseTree(files, new() {
				BasePath = files.FindCommonBasePath(StringComparison.OrdinalIgnoreCase),
				PathComparison = StringComparison.OrdinalIgnoreCase,
				LabelPredicate = static line => line.TrimStart().StartsWith("label_", StringComparison.Ordinal),
			});
		}
	}
}