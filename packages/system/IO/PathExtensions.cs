namespace System.IO;

/// <summary>
/// Provides extension methods for <see cref="Path" />.
/// </summary>
public static class PathExtensions {
	extension(Path) {
		/// <summary>
		/// Determines whether a path is outside a base path.
		/// </summary>
		/// <param name="basePath">The base path the candidate path must stay within.</param>
		/// <param name="path">The path to evaluate.</param>
		/// <param name="comparison">One of the enumeration values that specifies how paths are compared.</param>
		/// <returns><see langword="true" /> if <paramref name="path" /> is outside <paramref name="basePath" />; otherwise, <see langword="false" />.</returns>
		public static bool IsOutsideBasePath(string basePath, string path, StringComparison comparison) {
			var normalizedBasePath = Path.GetFullPath(NormalizeDirectorySeparators(basePath));
			var normalizedPath = Path.GetFullPath(NormalizeDirectorySeparators(path), normalizedBasePath);

			return !Path.IsSameOrChildPath(normalizedPath, normalizedBasePath, comparison);
		}

		/// <summary>
		/// Determines whether a path is the same as, or is contained by, another path.
		/// </summary>
		/// <param name="path">The path to evaluate.</param>
		/// <param name="candidateParent">The path to compare as the candidate parent path.</param>
		/// <param name="comparison">One of the enumeration values that specifies how paths are compared.</param>
		/// <returns><see langword="true" /> if the path is the same as, or is contained by, <paramref name="candidateParent" />; otherwise, <see langword="false" />.</returns>
		public static bool IsSameOrChildPath(string path, string candidateParent, StringComparison comparison) {
			var normalizedPath = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			var normalizedParent = candidateParent.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

			return normalizedPath.Equals(normalizedParent, comparison)
				|| normalizedPath.StartsWith(normalizedParent + Path.DirectorySeparatorChar, comparison)
				|| normalizedPath.StartsWith(normalizedParent + Path.AltDirectorySeparatorChar, comparison);
		}

		/// <summary>
		/// Gets the file name from a path, or the trimmed path when no file name is available.
		/// </summary>
		/// <param name="path">The path to inspect.</param>
		/// <returns>The file name, or the trimmed path when the file name is empty.</returns>
		public static string GetFileNameOrPath(string path) {
			var trimmed = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			var name = Path.GetFileName(trimmed);

			return string.IsNullOrWhiteSpace(name)
				? trimmed
				: name;
		}

		private static string NormalizeDirectorySeparators(string path) => path
			.Replace('\\', Path.DirectorySeparatorChar)
			.Replace('/', Path.DirectorySeparatorChar);
	}
}