namespace System.Trees;

using Nodes;

using Parsing;

public static class TreeExtensions {
	extension(IEnumerable<FileInfo> paths) {
		public FolderTreeNode ToFileTree(string basePath) {
			return FileTreeParser.Parse(paths, new() {
				BasePath = basePath,
				LabelPredicate = static line => line.TrimStart().StartsWith("label_", StringComparison.Ordinal),
			});
		}
	}
}