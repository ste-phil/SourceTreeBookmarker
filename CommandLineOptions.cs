using CommandLine;
using CommandLine.Text;

namespace SourceTreeBookmarker;

[Verb("add", HelpText = "Add folder contents to bookmarks")]
public class AddVerbOptions 
{
    public AddVerbOptions()
    {
        
    }
    
    [Option('f', "folder", Required = true, HelpText = "Folder path to add to bookmarks.")]
    public string? FolderPath { get; set; }

    [Option('r', "recurse", Required = false, HelpText = "Recursive search for git repositories in subfolders.", Default = false)]
    public bool Recursive { get; set; }
    
    
    [Usage(ApplicationAlias = "stcli")]
    public static IEnumerable<Example> Examples =>
        new List<Example>() {
            new Example("Add git repositories to sourcetree bookmarks", new AddVerbOptions { FolderPath = "<folder-to-git-repo>" })
        };
}

