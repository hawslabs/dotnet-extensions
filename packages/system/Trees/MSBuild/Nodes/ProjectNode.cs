namespace System.Trees.MSBuild.Nodes;

using System.Trees;

public sealed record ProjectNode(
	FileInfo File,
	string FullPath,
	string RelativePath
) : TreeNode(File.Name) {
	public override string? Icon { get; init; } = "⚙️";

	public static ProjectNode Parse(FileInfo file, string relativePath) {
		ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

		return new(file, file.FullName, relativePath);
	}
}