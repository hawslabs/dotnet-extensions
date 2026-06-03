namespace System.Trees.Nodes;

public sealed record FileTreeNode(
	FileInfo File,
	string FullPath,
	string RelativePath,
	int LineCount = 0,
	int LabelCount = 0
) : TreeNode(File.Name);