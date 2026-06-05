namespace System.Trees.Parsing;

using ArchiveNodes = System.Trees.Nodes.Archive;
using FileSystemNodes = System.Trees.Nodes.FileSystem;
using MSBuildNodes = System.Trees.Nodes.MSBuild;
using NuGetNodes = System.Trees.Nodes.NuGet;
using TreeNodes = System.Trees.Nodes;

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

			folder.AddOrUpdateNode(parts[^1], ParseNode(file, relativePath, options));
		}

		root.ComputeTotals();

		return new(basePath, root);
	}

	private static TreeNodes.TreeNode ParseNode(FileInfo file, string relativePath, FileTreeParseOptions options) {
		if (NuGetNodes.NuGetPackageNode.CanParse(file)) {
			return NuGetNodes.NuGetPackageNode.Parse(file, relativePath);
		}

		if (ArchiveNodes.ZipArchiveNode.CanParse(file)) {
			return ArchiveNodes.ZipArchiveNode.Parse(file, relativePath);
		}

		if (MSBuildNodes.SolutionNode.CanParse(file)) {
			return MSBuildNodes.SolutionNode.Parse(file, relativePath);
		}

		if (MSBuildNodes.ProjectNode.CanParse(file)) {
			return MSBuildNodes.ProjectNode.Parse(file, relativePath);
		}

		return FileSystemNodes.FileTreeNode.Parse(file, relativePath, options);
	}
}
