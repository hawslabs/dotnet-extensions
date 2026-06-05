using System.Trees.Formatting.Ascii;
using System.Trees.FileSystem;
using System.Trees.Parsing;

public static class AsciiTreeExtensions {
	extension(IEnumerable<FileInfo> paths) {
		public AsciiTree ToAsciiTree(AsciiTreeFormatterOptions? options = null) {
			return paths.ToFileTree().ToAsciiTree(options);
		}
	}

    extension(FileTree fileTree) {
	    public AsciiTree ToAsciiTree(AsciiTreeFormatterOptions? options = null) {
		    return new(
			    RootPath: fileTree.RootPath,
			    Text: new(() => fileTree.Root.Format(
				    formatter: options switch {
					    not null => new(options),
                        _ => AsciiTreeFormatter.Default,
				    }
			    ))
		    );
	    }
    }
}
