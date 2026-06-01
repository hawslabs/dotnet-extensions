<Query Kind="Program">
  <Reference Relative="..\..\..\.artifacts\bin\HawsLabs.Extensions.System\debug\HawsLabs.Extensions.System.dll">D:\hawslabs\extensions\.artifacts\bin\HawsLabs.Extensions.System\debug\HawsLabs.Extensions.System.dll</Reference>
  <Reference Relative="..\..\..\.artifacts\bin\HawsLabs.Extensions.LINQPad\debug\HawsLabs.Extensions.LINQPad.dll">D:\hawslabs\extensions\.artifacts\bin\HawsLabs.Extensions.LINQPad\debug\HawsLabs.Extensions.LINQPad.dll</Reference>
  <Namespace>HawsLabs.Extensions.LINQPad</Namespace>
  <Namespace>HawsLabs.Extensions.LINQPad.Formatting.AsciiTree</Namespace>
	<Namespace>System.Collections</Namespace>
	<Namespace>System.Formatting</Namespace>
	<Namespace>System.Formatting.AsciiTree</Namespace>
	<Namespace>System.IO</Namespace>
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
		SortOrder = TreeSortOrder.Alphabetical,
		ShowIcons = true,
		ShowLabels = false,
		ShowLineCounts = false,
		AlignColumns = true,
	});
}