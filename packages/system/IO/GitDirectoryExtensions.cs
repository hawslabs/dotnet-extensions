namespace System.IO;

public static class GitDirectoryExtensions {
	extension(DirectoryInfo directory) {
		public bool IsGitRootDirectory
			=> directory.Exists && directory.GetDirectories(".git").Length != 0;

		public DirectoryInfo? GitRootDirectory {
			get {
				var currentDirectory = directory;

				while (currentDirectory is not null) {
					if (currentDirectory.IsGitRootDirectory) {
						return currentDirectory;
					}

					currentDirectory = currentDirectory.Parent;
				}

				return null;
			}
		}
	}
}