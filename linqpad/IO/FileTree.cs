using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace HawsLabs.Extensions.LINQPad.IO;

public static class FileTree {
    public static IEnumerable<FileInfo> EnumerateFiles(
        string basePath,
        IEnumerable<string> includePatterns,
        IEnumerable<string> excludePatterns,
        FileEnumerationOptions? options = null) {
        options ??= new FileEnumerationOptions();

        var fullBasePath = Path.GetFullPath(basePath);

        if (options.EnsureBasePathExists && !Directory.Exists(fullBasePath)) {
            throw new DirectoryNotFoundException($"Base path does not exist: {fullBasePath}");
        }

        var matcher = new Matcher(options.PatternComparison);

        var includes = NormalizeIncludePatterns(includePatterns, options).ToList();
        if (includes.Count == 0) {
            includes.Add("**/*");
        }

        foreach (var include in includes) {
            matcher.AddInclude(include);
        }

        foreach (var exclude in NormalizeExcludePatterns(excludePatterns, options)) {
            matcher.AddExclude(exclude);
        }

        var result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(fullBasePath)));

        var filePaths = result.Files
            .Select(match => Path.GetFullPath(Path.Combine(fullBasePath, match.Path)));

        if (options.SortResults) {
            filePaths = filePaths.OrderBy(path => path, options.ResultSortComparer);
        }

        return filePaths.Select(path => new FileInfo(path));
    }

    private static IEnumerable<string> NormalizeIncludePatterns(
        IEnumerable<string> patterns,
        FileEnumerationOptions options) {
        foreach (var pattern in ExpandPatterns(patterns, options)) {
            yield return NormalizePattern(pattern);
        }
    }

    private static IEnumerable<string> NormalizeExcludePatterns(
        IEnumerable<string> patterns,
        FileEnumerationOptions options
    ) {
        foreach (var rawPattern in ExpandPatterns(patterns, options)) {
            var pattern = NormalizePattern(rawPattern);

            if (string.IsNullOrWhiteSpace(pattern)) {
                continue;
            }

            if (pattern.EndsWith("/", StringComparison.Ordinal)) {
                yield return pattern + "**";
            } else {
                yield return pattern;
            }

            if (options.TreatBareExcludeAsDirectoryName && IsBarePathSegment(pattern)) {
                yield return pattern + "/**";
                yield return "**/" + pattern + "/**";
            }
        }
    }

    private static IEnumerable<string> ExpandPatterns(
        IEnumerable<string> patterns,
        FileEnumerationOptions options) {
        foreach (var pattern in patterns.Where(p => !string.IsNullOrWhiteSpace(p))) {
            var normalized = NormalizePattern(pattern);

            if (!options.ExpandBracePatterns) {
                yield return normalized;
                continue;
            }

            foreach (var expanded in ExpandBracePattern(normalized)) {
                yield return expanded;
            }
        }
    }

    private static IEnumerable<string> ExpandBracePattern(string pattern) {
        var open = pattern.IndexOf('{');
        if (open < 0) {
            yield return pattern;
            yield break;
        }

        var close = pattern.IndexOf('}', open + 1);
        if (close < 0) {
            yield return pattern;
            yield break;
        }

        var prefix = pattern[..open];
        var suffix = pattern[(close + 1)..];

        var values = pattern[(open + 1)..close]
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var value in values) {
            foreach (var expanded in ExpandBracePattern(prefix + value + suffix)) {
                yield return expanded;
            }
        }
    }

    private static string NormalizePattern(string pattern) {
        var normalized = pattern.Trim().Replace('\\', '/').TrimStart('/');

        while (normalized.StartsWith("./", StringComparison.Ordinal)) {
            normalized = normalized[2..];
        }

        return normalized;
    }

    private static bool IsBarePathSegment(string pattern) {
        return !pattern.Contains('/')
            && !pattern.Contains('*')
            && !pattern.Contains('?')
            && !pattern.Contains('[')
            && !pattern.Contains(']')
            && !pattern.Contains('{')
            && !pattern.Contains('}');
    }
}
