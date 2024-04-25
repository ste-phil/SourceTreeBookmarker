using System.Xml.Serialization;

namespace SourceTreeBookmarker;

[XmlRoot("ArrayOfTreeViewNode")]
[XmlInclude(typeof(BookmarkNode))]
[XmlInclude(typeof(BookmarkFolderNode))]
public class ArrayOfTreeViewNode : System.Collections.Generic.List<TreeViewNode>
{
}

public class TreeViewNode
{
    public int Level { get; set; }
    public bool IsExpanded { get; set; }
    public bool IsLeaf { get; set; }
    public string Name { get; set; }
}

public class BookmarkNode : TreeViewNode
{
    public string Path { get; set; }
    public string RepoType { get; set; }
}

public class BookmarkFolderNode : TreeViewNode
{
    public List<TreeViewNode> Children { get; set; } = new ();
}