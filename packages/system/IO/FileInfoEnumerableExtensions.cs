namespace System.IO;

/// <summary>
/// Provides extension methods for sequences of <see cref="FileInfo" /> instances.
/// </summary>
public static class FileInfoEnumerableExtensions {
	extension(IEnumerable<FileInfo> files) {
		/// <summary>
		/// Finds the deepest directory that contains all files in the sequence.
		/// </summary>
		/// <param name="comparison">One of the enumeration values that specifies how paths are compared.</param>
		/// <returns>The full path of the common base directory.</returns>
		/// <remarks>
		/// Returns the current directory when <paramref name="files" /> is empty.
		/// </remarks>
		public string FindCommonBasePath(StringComparison comparison) {
			using var enumerator = files.GetEnumerator();

			if (!enumerator.MoveNext()) {
				return Directory.GetCurrentDirectory();
			}

			var common = Path.GetDirectoryName(enumerator.Current.FullName) ?? Directory.GetCurrentDirectory();

			while (enumerator.MoveNext()) {
				var directory = Path.GetDirectoryName(enumerator.Current.FullName) ?? Directory.GetCurrentDirectory();

				while (!Path.IsSameOrChildPath(directory, common, comparison)) {
					var parent = Directory.GetParent(common);
					if (parent is null) {
						return common;
					}

					common = parent.FullName;
				}
			}

			return common;
		}
	}
}