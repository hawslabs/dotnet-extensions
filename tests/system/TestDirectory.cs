namespace HawsLabs.Extensions.Tests;

using System.IO;

public sealed class TestDirectory : IDisposable {
	public DirectoryInfo Root { get; }

	public TestDirectory() {
		Root = Directory.CreateDirectory(
			Path.Combine(
				Path.GetTempPath(),
				"HawsLabs.Extensions.System.Tests",
				Guid.NewGuid().ToString("N")
			)
		);
	}

	public DirectoryInfo CreateDirectory(string relativePath) {
		return Directory.CreateDirectory(GetFullPath(relativePath));
	}

	public FileInfo CreateFile(string relativePath, string contents = "") {
		var fullPath = GetFullPath(relativePath);
		var directoryPath = Path.GetDirectoryName(fullPath);
		if (directoryPath is not null) {
			Directory.CreateDirectory(directoryPath);
		}

		File.WriteAllText(fullPath, contents);

		return new(fullPath);
	}

	public void Dispose() {
		Root.Refresh();
		if (Root.Exists) {
			Root.Delete(recursive: true);
		}
	}

	private string GetFullPath(string relativePath) {
		return Path.Combine(Root.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
	}
}