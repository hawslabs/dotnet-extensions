namespace System.Trees.Nodes;

public sealed record FolderTreeNode(string Name) : TreeNode(Name) {
	public int TotalLines { get; private set; }
	public int TotalLabelCount { get; private set; }

	public FolderTreeNode GetOrAddFolder(string name) {
		if (Children.TryGetValue(name, out var child)) {
			return child as FolderTreeNode
			   ?? throw new InvalidOperationException($"A non-folder node named '{name}' already exists under '{Name}'.");
		}

		var newFolder = new FolderTreeNode(name);
		Children[name] = newFolder;

		return newFolder;
	}

	internal void AddOrUpdateFile(string name, FileTreeNode file) {
		if (Children.TryGetValue(name, out var child) && child is not FileTreeNode) {
			throw new InvalidOperationException($"A non-file node named '{name}' already exists under '{Name}'.");
		}

		Children[name] = file;
	}

	internal void ComputeTotals() {
		var totalLines = 0;
		var totalLabelCount = 0;

		foreach (var child in Children.Values) {
			switch (child) {
				case FileTreeNode file:
					totalLines += file.LineCount;
					totalLabelCount += file.LabelCount;
					break;

				case FolderTreeNode folder:
					folder.ComputeTotals();
					totalLines += folder.TotalLines;
					totalLabelCount += folder.TotalLabelCount;
					break;
			}
		}

		TotalLines = totalLines;
		TotalLabelCount = totalLabelCount;
	}
}