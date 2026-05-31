public static class DetectGitDirectory {
	extension(DirectoryInfo directory) {
		public bool IsGitRootDirectory
			=> directory.GetDirectories(".git").Length != 0;

		public DirectoryInfo? GitRootDirectory {
            get {
				// recursively check parent directories for a .git directory
				var dir = directory;

				while (dir is not null) {
					if (dir.IsGitRootDirectory) {
						return dir;
					}

					dir = dir.Parent;
				}

				return null;
            }
		}
	}

	extension(Util) {
		public static DirectoryInfo? GitRootDirectory {
			get {
				var file = new FileInfo(Util.CurrentQuery.FilePath);
				return file.Directory?.GitRootDirectory;
			}
		}
	}
}