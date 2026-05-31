using System.Diagnostics.CodeAnalysis;

namespace System.IO;

public static class ParentDirectoryExtensions {
    extension(DirectoryInfo directory) {
        DirectoryInfo GetParentDirectory(int levels = 1) {
            if (!directory.TryGetParentDirectory(levels, out var parentDirectory)) {
                throw new DirectoryNotFoundException(
                    $"Path '{directory.FullName}' does not have {levels} parent directories."
                );
            }

            return parentDirectory;
        }

        DirectoryInfo GetParentDirectoryOrRoot(int levels = 1) {
            return directory.TryGetParentDirectory(levels, out var parentDirectory)
                ? parentDirectory
                : directory.Root;
        }

        bool TryGetParentDirectory(
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
