namespace System.IO;

/// <summary>
/// Provides extension methods for <see cref="FileInfo" /> instances.
/// </summary>
public static class FileInfoExtensions {
	extension(FileInfo file) {
		/// <summary>
		/// Gets a relative path to the file with directory separators normalized to forward slashes.
		/// </summary>
		/// <param name="basePath">The base path used to calculate the relative path.</param>
		/// <returns>The relative path from <paramref name="basePath" /> to the file.</returns>
		public string GetRelativeNormalizedPath(string basePath) {
			return Path.GetRelativePath(basePath, file.FullName).Replace('\\', '/');
		}
	}
}