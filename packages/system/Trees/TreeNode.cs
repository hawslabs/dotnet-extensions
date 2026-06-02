namespace System.Trees;

using Formatting;

using OrderedTreeNodeDictionary = Collections.OrderedDictionary<string, TreeNode>;

public sealed class TreeNode {
	private readonly IEqualityComparer<string> _childNameComparer;

	public string Name { get; }
	public string? FullPath { get; private set; }
	public string? RelativePath { get; private set; }
	public FileInfo? File { get; private set; }
	public int LineCount { get; private set; }
	public int LabelCount { get; private set; }
	public int TotalLines { get; private set; }
	public int TotalLabelCount { get; private set; }

	public OrderedTreeNodeDictionary Children { get; }

	public bool IsFile => FullPath is not null;
	public bool IsDirectory => !IsFile;

	public TreeNode(string name)
		: this(name, StringComparer.OrdinalIgnoreCase) {
	}

	private TreeNode(string name, IEqualityComparer<string> childNameComparer) {
		Name = name;
		_childNameComparer = childNameComparer;
		Children = new OrderedTreeNodeDictionary(childNameComparer);
	}

	public static TreeNode Parse(IEnumerable<FileInfo> files, TreeNodeParseOptions? options = null) {
		options ??= new TreeNodeParseOptions();

		if (options.SortInputPaths) {
			files = files.OrderBy(file => file.FullName, options.PathSortComparer).ToList();
		}

		var basePath = Path.GetFullPath(options.BasePath ?? files.FindCommonBasePath(options.PathComparison));
		var rootName = options.RootName ?? Path.GetFileNameOrPath(basePath);

		var root = new TreeNode(rootName, options.ChildNameComparer);

		foreach (var file in files) {
			if (options.IgnoreFilesOutsideBasePath && Path.IsOutsideBasePath(basePath, file.FullName, options.PathComparison)) {
				continue;
			}

			var relativePath = file.GetRelativeNormalizedPath(basePath);

			var parts = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length == 0) {
				continue;
			}

			var node = root;

			foreach (var part in parts) {
				node = node.GetOrAdd(part);
			}

			var metadata = options.ReadFileMetrics
				? ReadMetadata(file, relativePath, options)
				: new FileMetadata(file, file.FullName, relativePath, 0, 0);

			node.SetFileMetadata(metadata);
		}

		root.ComputeTotals();

		return root;
	}

	public string Format(ITreeNodeFormatter formatter) {
		ArgumentNullException.ThrowIfNull(formatter);
		return formatter.Format(this);
	}

	public TreeNode GetOrAdd(string name) {
		if (Children.TryGetValue(name, out var child)) {
			return child;
		}

		child = new TreeNode(name, _childNameComparer);
		Children[name] = child;

		return child;
	}

	private void SetFileMetadata(FileMetadata metadata) {
		File = metadata.File;
		FullPath = metadata.FullPath;
		RelativePath = metadata.RelativePath;
		LineCount = metadata.LineCount;
		LabelCount = metadata.LabelCount;
	}

	private void ComputeTotals() {
		foreach (var child in Children.Values) {
			child.ComputeTotals();
		}

		TotalLines = LineCount + Children.Values.Sum(child => child.TotalLines);
		TotalLabelCount = LabelCount + Children.Values.Sum(child => child.TotalLabelCount);
	}

	private static FileMetadata ReadMetadata(FileInfo file, string relativePath, TreeNodeParseOptions options) {
		try {
			var lineCount = 0;
			var labelCount = 0;

			foreach (var line in options.ReadLines(file.FullName)) {
				lineCount++;

				if (options.LabelPredicate(line)) {
					labelCount++;
				}
			}

			return new FileMetadata(file, file.FullName, relativePath, lineCount, labelCount);
		} catch when (options.IgnoreFileReadErrors) {
			return new FileMetadata(file, file.FullName, relativePath, 0, 0);
		}
	}

	private readonly record struct FileMetadata(
		FileInfo File,
		string FullPath,
		string RelativePath,
		int LineCount,
		int LabelCount
	);
}