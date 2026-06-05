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
			var common = files
				.Select(file => new DirectoryInfo(
					Path.GetDirectoryName(file.FullName) ?? Directory.GetCurrentDirectory()
				))
				.FindCommonAncestor(
					static directory => directory.Parent,
					new DirectoryInfoSegmentComparer(StringComparer.FromComparison(comparison))
				);

			return common?.FullName ?? Directory.GetCurrentDirectory();
		}
	}

	private sealed class DirectoryInfoSegmentComparer(
		StringComparer segmentComparer
	) : IEqualityComparer<DirectoryInfo> {
		public bool Equals(DirectoryInfo? x, DirectoryInfo? y) {
			if (ReferenceEquals(x, y)) {
				return true;
			}

			if (x is null || y is null) {
				return false;
			}

			return x.GetSegments().SequenceEqual(y.GetSegments(), segmentComparer);
		}

		public int GetHashCode(DirectoryInfo obj) {
			var hash = new HashCode();

			foreach (var segment in obj.GetSegments()) {
				hash.Add(segment, segmentComparer);
			}

			return hash.ToHashCode();
		}
	}
}
