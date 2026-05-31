namespace HawsLabs.Extensions.LINQPad.IO;

public sealed record FileEnumerationOptions
{
    public bool EnsureBasePathExists { get; init; } = true;

    public bool SortResults { get; init; } = true;

    public StringComparer ResultSortComparer { get; init; } = StringComparer.OrdinalIgnoreCase;

    public StringComparison PatternComparison { get; init; } = StringComparison.OrdinalIgnoreCase;

    public bool ExpandBracePatterns { get; init; } = true;

    public bool TreatBareExcludeAsDirectoryName { get; init; } = true;
}
