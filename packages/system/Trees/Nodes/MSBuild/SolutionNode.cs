namespace System.Trees.Nodes.MSBuild;

public sealed record SolutionNode(
	FileInfo File,
	string FullPath,
	string RelativePath
) : TreeNode(File.Name) {
	private static readonly string[] SolutionExtensions = [
		".sln",
		".slnx",
	];

	public static bool CanParse(FileInfo file) {
		ArgumentNullException.ThrowIfNull(file);

		return SolutionExtensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase);
	}

	public static SolutionNode Parse(FileInfo file, string relativePath) {
		ArgumentNullException.ThrowIfNull(file);
		ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

		return new(file, file.FullName, relativePath);
	}
}
