using HawsLabs.Extensions.LINQPad.Collections;
using HawsLabs.Extensions.LINQPad.Formatting;
using OrderedTreeNodeDictionary = HawsLabs.Extensions.LINQPad.Collections.OrderedDictionary<string, HawsLabs.Extensions.LINQPad.Trees.TreeNode>;

namespace HawsLabs.Extensions.LINQPad.Trees;

public sealed class TreeNode
{
    private readonly IEqualityComparer<string> _childNameComparer;

    public string Name { get; }

    public string? FullPath { get; private set; }

    public int LineCount { get; private set; }

    public int LabelCount { get; private set; }

    public int TotalLines { get; private set; }

    public int TotalLabelCount { get; private set; }

    public OrderedTreeNodeDictionary Children { get; }

    public bool IsFile => FullPath is not null;

    public bool IsDirectory => !IsFile;

    public TreeNode(string name)
        : this(name, StringComparer.OrdinalIgnoreCase)
    {
    }

    private TreeNode(string name, IEqualityComparer<string> childNameComparer)
    {
        Name = name;
        _childNameComparer = childNameComparer;
        Children = new OrderedTreeNodeDictionary(childNameComparer);
    }

    public static TreeNode Parse(IEnumerable<string> filePaths)
    {
        return Parse(filePaths, null);
    }

    public static TreeNode Parse(IEnumerable<string> filePaths, TreeNodeParseOptions? options)
    {
        options ??= new TreeNodeParseOptions();

        var paths = filePaths
            .Select(Path.GetFullPath)
            .ToList();

        if (options.SortInputPaths)
            paths = paths.OrderBy(path => path, options.PathSortComparer).ToList();

        var basePath = Path.GetFullPath(options.BasePath ?? FindCommonBasePath(paths, options.PathComparison));
        var rootName = options.RootName ?? GetDefaultRootName(basePath);

        var root = new TreeNode(rootName, options.ChildNameComparer);

        foreach (var fullPath in paths)
        {
            var relativePath = Path.GetRelativePath(basePath, fullPath).Replace('\\', '/');

            if (options.IgnoreFilesOutsideBasePath && IsOutsideBasePath(relativePath))
                continue;

            var parts = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                continue;

            var node = root;

            foreach (var part in parts)
                node = node.GetOrAdd(part);

            var metrics = options.ReadFileMetrics
                ? ReadMetrics(fullPath, options)
                : new FileMetrics(0, 0);

            node.SetFileMetrics(fullPath, metrics.LineCount, metrics.LabelCount);
        }

        root.ComputeTotals();

        return root;
    }

    public string Format(ITreeNodeFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        return formatter.Format(this);
    }

    public TreeNode GetOrAdd(string name)
    {
        if (!Children.TryGetValue(name, out var child))
        {
            child = new TreeNode(name, _childNameComparer);
            Children[name] = child;
        }

        return child;
    }

    private void SetFileMetrics(string fullPath, int lineCount, int labelCount)
    {
        FullPath = fullPath;
        LineCount = lineCount;
        LabelCount = labelCount;
    }

    private void ComputeTotals()
    {
        foreach (var child in Children.Values)
            child.ComputeTotals();

        TotalLines = LineCount + Children.Values.Sum(child => child.TotalLines);
        TotalLabelCount = LabelCount + Children.Values.Sum(child => child.TotalLabelCount);
    }

    private static FileMetrics ReadMetrics(string fullPath, TreeNodeParseOptions options)
    {
        try
        {
            var lineCount = 0;
            var labelCount = 0;

            foreach (var line in options.ReadLines(fullPath))
            {
                lineCount++;

                if (options.LabelPredicate(line))
                    labelCount++;
            }

            return new FileMetrics(lineCount, labelCount);
        }
        catch when (options.IgnoreFileReadErrors)
        {
            return new FileMetrics(0, 0);
        }
    }

    private static bool IsOutsideBasePath(string relativePath)
    {
        return relativePath == ".."
            || relativePath.StartsWith("../", StringComparison.Ordinal)
            || Path.IsPathRooted(relativePath);
    }

    private static string FindCommonBasePath(IReadOnlyList<string> paths, StringComparison comparison)
    {
        if (paths.Count == 0)
            return Directory.GetCurrentDirectory();

        var common = Path.GetDirectoryName(paths[0]) ?? Directory.GetCurrentDirectory();

        foreach (var path in paths.Skip(1))
        {
            var directory = Path.GetDirectoryName(path) ?? Directory.GetCurrentDirectory();

            while (!IsSameOrChildPath(directory, common, comparison))
            {
                var parent = Directory.GetParent(common);
                if (parent is null)
                    return common;

                common = parent.FullName;
            }
        }

        return common;
    }

    private static bool IsSameOrChildPath(string path, string candidateParent, StringComparison comparison)
    {
        var normalizedPath = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedParent = candidateParent.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return normalizedPath.Equals(normalizedParent, comparison)
            || normalizedPath.StartsWith(normalizedParent + Path.DirectorySeparatorChar, comparison)
            || normalizedPath.StartsWith(normalizedParent + Path.AltDirectorySeparatorChar, comparison);
    }

    private static string GetDefaultRootName(string basePath)
    {
        var trimmed = basePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var name = Path.GetFileName(trimmed);

        return string.IsNullOrWhiteSpace(name)
            ? trimmed
            : name;
    }

    private readonly record struct FileMetrics(int LineCount, int LabelCount);
}
