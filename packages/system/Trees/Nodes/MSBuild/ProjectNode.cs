namespace System.Trees.Nodes.MSBuild;

public sealed record ProjectNode(
	FileInfo File,
	string FullPath,
	string RelativePath
) : TreeNode(File.Name) {
	private static readonly string[] ProjectExtensions = [
		".csproj",
		".fsproj",
		".vbproj",
	];

	public static bool CanParse(FileInfo file) {
		ArgumentNullException.ThrowIfNull(file);

		return ProjectExtensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase);
	}

	public static ProjectNode Parse(FileInfo file, string relativePath) {
		ArgumentNullException.ThrowIfNull(file);
		ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

		return new(file, file.FullName, relativePath);
	}
}
