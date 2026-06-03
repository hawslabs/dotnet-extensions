namespace System.Trees.Parsing;

using Nodes;

/// <summary>
/// Represents a parsed file tree and the root path used to create it.
/// </summary>
/// <param name="RootPath">The full root path used when parsing the tree.</param>
/// <param name="Root">The root folder node for the parsed tree.</param>
public sealed record FileTree(
	string RootPath,
	FolderTreeNode Root
);