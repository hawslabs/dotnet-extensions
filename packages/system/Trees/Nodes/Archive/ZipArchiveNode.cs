namespace System.Trees.Nodes.Archive;

public record ZipArchiveNode(
	FileInfo File,
	string FullPath,
	string RelativePath
) : TreeNode(File.Name) {
	public override string? Icon { get; init; } = "🗜️";

	public static ZipArchiveNode Parse(FileInfo file, string relativePath) {
		ArgumentNullException.ThrowIfNull(file);
		ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

		return new(file, file.FullName, relativePath);
	}
}
