namespace System.Trees.NuGet.Nodes;

using System.Trees.Archive.Nodes;

public sealed record NuGetPackageNode(
	string PackageId,
	string PackageVersion,
	FileInfo File,
	string FullPath,
	string RelativePath
) : ZipArchiveNode(File, FullPath, RelativePath) {
	public override string? Icon { get; init; } = "📦";

	public static new NuGetPackageNode Parse(FileInfo file, string relativePath) {
		ArgumentNullException.ThrowIfNull(file);
		ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

		var fileName = Path.GetFileNameWithoutExtension(file.Name);
		var versionStartIndex = FindVersionStartIndex(fileName);

		var packageId = versionStartIndex is null
			? fileName
			: fileName[..(versionStartIndex.Value - 1)];
		var packageVersion = versionStartIndex is null
			? ""
			: fileName[versionStartIndex.Value..];

		return new(packageId, packageVersion, file, file.FullName, relativePath);
	}

	private static int? FindVersionStartIndex(string fileName) {
		for (var index = 0; index < fileName.Length; index++) {
			if (fileName[index] != '.') {
				continue;
			}

			var segmentStartIndex = index + 1;
			if (segmentStartIndex < fileName.Length && IsPackageVersion(fileName.AsSpan(segmentStartIndex))) {
				return segmentStartIndex;
			}
		}

		return null;
	}

	private static bool IsPackageVersion(ReadOnlySpan<char> value) {
		var index = 0;
		var numericSegmentCount = 0;

		if (!TryReadNumericSegment(value, ref index)) {
			return false;
		}

		numericSegmentCount++;

		while (index < value.Length && value[index] == '.') {
			index++;

			if (!TryReadNumericSegment(value, ref index)) {
				return false;
			}

			numericSegmentCount++;
		}

		if (numericSegmentCount < 2) {
			return false;
		}

		if (index == value.Length) {
			return true;
		}

		if (value[index] is not ('-' or '+')) {
			return false;
		}

		index++;
		if (index == value.Length) {
			return false;
		}

		for (; index < value.Length; index++) {
			if (!char.IsAsciiLetterOrDigit(value[index]) && value[index] is not ('.' or '-' or '+')) {
				return false;
			}
		}

		return true;
	}

	private static bool TryReadNumericSegment(ReadOnlySpan<char> value, ref int index) {
		var startIndex = index;

		while (index < value.Length && char.IsDigit(value[index])) {
			index++;
		}

		return index > startIndex;
	}
}
