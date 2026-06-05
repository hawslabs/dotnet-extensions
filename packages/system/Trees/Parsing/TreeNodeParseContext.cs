namespace System.Trees.Parsing;

public sealed record TreeNodeParseContext(
	FileInfo File,
	string RelativePath,
	FileTreeParseOptions Options
) {
	public string FullPath => File.FullName;
}
