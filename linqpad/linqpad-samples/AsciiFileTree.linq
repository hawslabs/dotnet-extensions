<Query Kind="Program">
  <Reference Relative="..\..\.artifacts\bin\HawsLabs.Extensions.LINQPad\debug\HawsLabs.Extensions.LINQPad.dll">D:\hawslabs\extensions\.artifacts\bin\HawsLabs.Extensions.LINQPad\debug\HawsLabs.Extensions.LINQPad.dll</Reference>
  <Reference Relative="..\..\.artifacts\bin\HawsLabs.Extensions.LINQPad\debug\HawsLabs.Extensions.System.dll">D:\hawslabs\extensions\.artifacts\bin\HawsLabs.Extensions.LINQPad\debug\HawsLabs.Extensions.System.dll</Reference>
  <Namespace>System.Formatting</Namespace>
  <Namespace>System.Formatting.AsciiTree</Namespace>
  <Namespace>System.Trees</Namespace>
</Query>

void Main() {
	Util.GitRootDirectory?.Glob(
		includePatterns: [
			"**/*.{csproj,sln,slnx,props,targets}",
			"**/*.{cs,json}",
		],
		excludePatterns: [
			".git",
			"**/*.Designer.cs",
			"**/*DbContextModelSnapshot.cs",
			"**/node_modules/",
			"**/{artifacts,.artifacts,obj,bin,.vs}/",
		]
	).DumpAsAsciiTree(Util.GitRootDirectory!.FullName, new() {
		SortOrder = TreeSortOrder.AlphabeticalDirectoriesFirst,
		ShowIcons = true,
		ShowLabels = false,
		ShowLineCounts = false,
		AlignColumns = true,
	});
}
