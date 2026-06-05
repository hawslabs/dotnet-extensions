namespace System.Trees.Parsing;

using FileSystemNodes = System.Trees.Nodes.FileSystem;

/// <summary>
/// Represents a parsed file tree and the root path used to create it.
/// </summary>
/// <param name="RootPath">The full root path used when parsing the tree.</param>
/// <param name="Root">The root folder node for the parsed tree.</param>
public sealed record FileTree(
	string RootPath,
	FileSystemNodes.FolderTreeNode Root
);