namespace System.Trees;

using FileSystemNodes = System.Trees.FileSystem.Nodes;

using System.Trees.FileSystem.Parsing;

public static class TreeExtensions {
	extension(IEnumerable<FileInfo> paths) {
		public FileSystemNodes.FolderTreeNode ToFileTree(string basePath) {
			return FileTreeParser.Parse(paths, new() {
				BasePath = basePath,
				LabelPredicate = static line => line.TrimStart().StartsWith("label_", StringComparison.Ordinal),
			});
		}
	}
}
