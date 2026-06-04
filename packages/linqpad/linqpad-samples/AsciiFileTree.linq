<Query Kind="Program">
  <Reference Relative="..\..\..\.artifacts\bin\HawsLabs.Extensions.LINQPad\debug\HawsLabs.Extensions.LINQPad.dll">D:\hawslabs\dotnet-extensions\.artifacts\bin\HawsLabs.Extensions.LINQPad\debug\HawsLabs.Extensions.LINQPad.dll</Reference>
  <Reference Relative="..\..\..\.artifacts\bin\HawsLabs.Extensions.LINQPad\debug\HawsLabs.Extensions.System.dll">D:\hawslabs\dotnet-extensions\.artifacts\bin\HawsLabs.Extensions.LINQPad\debug\HawsLabs.Extensions.System.dll</Reference>
  <Namespace>System.Trees.Formatting</Namespace>
  <Namespace>System.Trees.Formatting.Ascii</Namespace>
  <Namespace>System.Trees.Nodes</Namespace>
  <Namespace>System.Trees.Parsing</Namespace>
</Query>

void Main() {
	Util.GetFileTree(
		rootDirectory: Util.GitRootDirectory!,
		options: GlobsOptions.Default with {
			IncludePatterns = [
				"**/*.{csproj,sln,slnx,props,targets}",
				"**/*.{cs,json}",
			],
			ExcludePatterns = [
				"**/*.Designer.cs",
				"**/*DbContextModelSnapshot.cs",
			],
		}
	).ToAsciiTree().Dump();
}
