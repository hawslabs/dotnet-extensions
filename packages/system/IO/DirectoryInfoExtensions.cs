namespace System.IO;

/// <summary>
/// Provides extension methods for <see cref="DirectoryInfo" /> instances.
/// </summary>
public static class DirectoryInfoExtensions {
	extension(DirectoryInfo directory) {
		/// <summary>
		/// Gets the path segments from the directory's full path.
		/// </summary>
		/// <returns>The directory path segments.</returns>
		public string[] GetSegments() {
			return directory.FullName.Split(
				[Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
				StringSplitOptions.RemoveEmptyEntries
			);
		}
	}
}
