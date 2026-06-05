namespace System.Trees.Parsing;

using FileSystemNodes = System.Trees.Nodes.FileSystem;

public static class FileTreeParser {
	public static FileSystemNodes.FolderTreeNode Parse(IEnumerable<FileInfo> files, FileTreeParseOptions? options = null) {
		return ParseTree(files, options).Root;
	}

	public static FileTree ParseTree(IEnumerable<FileInfo> files, FileTreeParseOptions? options = null) {
		ArgumentNullException.ThrowIfNull(files);

		options ??= new();

		if (options.SortInputPaths) {
			files = files.OrderBy(file => file.FullName, options.PathSortComparer).ToList();
		}

		var basePath = Path.GetFullPath(options.BasePath ?? files.FindCommonBasePath(options.PathComparison));
		var rootName = options.RootName ?? Path.GetFileNameOrPath(basePath);

		var root = FileSystemNodes.FolderTreeNode.Parse(rootName);
		var nodeParserResolver = new TreeNodeParserResolver(options.NodeParsers);

		foreach (var file in files) {
			if (options.IgnoreFilesOutsideBasePath && Path.IsOutsideBasePath(basePath, file.FullName, options.PathComparison)) {
				continue;
			}

			var relativePath = file.GetRelativeNormalizedPath(basePath);

			var parts = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length == 0) {
				continue;
			}

			var folder = root;

			foreach (var part in parts[..^1]) {
				folder = folder.GetOrAddFolder(part);
			}

			var context = new TreeNodeParseContext(file, relativePath, options);

			folder.AddOrUpdateNode(parts[^1], nodeParserResolver.Parse(context));
		}

		root.ComputeTotals();

		return new(basePath, root);
	}
}
