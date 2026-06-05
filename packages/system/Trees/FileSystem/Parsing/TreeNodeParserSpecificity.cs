namespace System.Trees.FileSystem.Parsing;

public static class TreeNodeParserSpecificity {
	public const int Fallback = 0;
	public const int Extension = 100;
	public const int Filename = 200;
	public const int PathPattern = 300;
	public const int FileSignature = 500;
	public const int ContentShape = 700;
	public const int ExactFormat = 1000;
}
