namespace System.Trees.Archive.Nodes;

using System.Trees;

public record ZipArchiveNode(
	FileInfo File,
	string FullPath,
	string RelativePath
) : TreeNode(File.Name) {
	public override string? Icon { get; init; } = "🗜️";

	public static ZipArchiveNode Parse(FileInfo file, string relativePath) {
		ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

		return new(file, file.FullName, relativePath);
	}
}