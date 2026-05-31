namespace System.IO;

using System.Diagnostics.CodeAnalysis;

public static class ParentDirectoryExtensions {
	extension(DirectoryInfo directory) {
		public DirectoryInfo GetParentDirectory(int levels = 1) {
			if (!directory.TryGetParentDirectory(levels, out var parentDirectory)) {
				throw new DirectoryNotFoundException(
					$"Path '{directory.FullName}' does not have {levels} parent directories."
				);
			}

			return parentDirectory;
		}

		public DirectoryInfo GetParentDirectoryOrRoot(int levels = 1) {
			return directory.TryGetParentDirectory(levels, out var parentDirectory)
				? parentDirectory
				: directory.Root;
		}

		public bool TryGetParentDirectory(
			int levels,
			[NotNullWhen(true)] out DirectoryInfo? parentDirectory
		) {
			parentDirectory = null;

			for (var i = 0; i < levels; i++) {
				if (directory.Parent is null) {
					return false;
				}

				directory = directory.Parent;
			}

			parentDirectory = directory;
			return true;
		}
	}
}
