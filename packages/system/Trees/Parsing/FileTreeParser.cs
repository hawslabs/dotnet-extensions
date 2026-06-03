namespace System.Trees.Parsing;

using Nodes;

public static class FileTreeParser {
	public static FolderTreeNode Parse(IEnumerable<FileInfo> files, FileTreeParseOptions? options = null) {
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

		var root = new FolderTreeNode(rootName);

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

			var metadata = options.ReadFileMetrics
				? ReadMetadata(file, relativePath, options)
				: new(file, file.FullName, relativePath, 0, 0);

			folder.AddOrUpdateFile(parts[^1], metadata.ToFileTreeNode());
		}

		root.ComputeTotals();

		return new(basePath, root);
	}

	private static FileMetadata ReadMetadata(FileInfo file, string relativePath, FileTreeParseOptions options) {
		try {
			var lineCount = 0;
			var labelCount = 0;

			foreach (var line in options.ReadLines(file.FullName)) {
				lineCount++;

				if (options.LabelPredicate(line)) {
					labelCount++;
				}
			}

			return new(file, file.FullName, relativePath, lineCount, labelCount);
		} catch when (options.IgnoreFileReadErrors) {
			return new(file, file.FullName, relativePath, 0, 0);
		}
	}

	private readonly record struct FileMetadata(
		FileInfo File,
		string FullPath,
		string RelativePath,
		int LineCount,
		int LabelCount
	) {
		public FileTreeNode ToFileTreeNode() => new(
			File,
			FullPath,
			RelativePath,
			LineCount,
			LabelCount
		);
	}
}