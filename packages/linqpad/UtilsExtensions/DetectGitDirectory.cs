public static class DetectGitDirectory {
	extension(Util) {
		public static DirectoryInfo? GitRootDirectory {
			get {
				var filePath = Util.CurrentQuery.FilePath;
				if (string.IsNullOrWhiteSpace(filePath)) {
					return null;
				}

				var file = new FileInfo(filePath);
				return file.Directory?.GitRootDirectory;
			}
		}
	}
}