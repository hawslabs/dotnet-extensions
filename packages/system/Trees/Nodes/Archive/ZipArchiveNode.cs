namespace System.Trees.Nodes.Archive;

public record ZipArchiveNode(
	FileInfo File,
	string FullPath,
	string RelativePath
) : TreeNode(File.Name) {
	public static bool CanParse(FileInfo file) {
		ArgumentNullException.ThrowIfNull(file);

		return string.Equals(file.Extension, ".zip", StringComparison.OrdinalIgnoreCase);
	}

	public static ZipArchiveNode Parse(FileInfo file, string relativePath) {
		ArgumentNullException.ThrowIfNull(file);
		ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

		return new(file, file.FullName, relativePath);
	}
}
