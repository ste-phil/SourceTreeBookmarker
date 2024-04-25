using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Xml;
using System.Xml.Serialization;
using CommandLine;

namespace SourceTreeBookmarker;

public class Program
{
    private static ArrayOfTreeViewNode existingNodes = new();
    
    public static int Main(string[] args)
    {
        var userAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string xmlFilePath = Path.Combine(userAppData, "Atlassian\\SourceTree\\bookmarks.xml");
     
        return Parser.Default.ParseArguments<AddVerbOptions>(args)
            .MapResult(
                (AddVerbOptions addOpts) => Run(addOpts.Recursive, xmlFilePath, addOpts.FolderPath),
                _ => 1);
    }


    public static int Run(bool recursive, string xmlFilePath, string folderPath)
    {
        folderPath = Path.GetFullPath(folderPath);
        
        KillSourceTree();
        return AppendBookmarksToXml(recursive, xmlFilePath, folderPath);
    }

    public static void KillSourceTree()
    {
        var processes = Process.GetProcessesByName("SourceTree");
        foreach (var p in processes)
        {
            p.Kill();
        }
    }
    
    public static int AppendBookmarksToXml(bool recursive, string xmlFilePath, string folderPath)
    {
        if (!File.Exists(xmlFilePath))
        {
            Console.WriteLine($"Couldn't find bookmarks.xml in folder '{xmlFilePath}'.");
            return 1;
        }
      
        // Load existing bookmark XML
        using (FileStream fileStream = new FileStream(xmlFilePath, FileMode.Open))
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ArrayOfTreeViewNode));
            existingNodes = (ArrayOfTreeViewNode)serializer.Deserialize(fileStream);
        }

        if (recursive)
        {
            // Add bookmarks for each git repository found in the folder structure
            var rootFolderNode = new BookmarkFolderNode
            {
                Level = 0,
                IsExpanded = false,
                IsLeaf = false,
                Name = new DirectoryInfo(folderPath).Name,
            };
            AddBookmarksRecursively(rootFolderNode, folderPath);
            existingNodes.Add(rootFolderNode);
        }
        else
        {
            if (!IsGitFolder(folderPath))
            {
                Console.WriteLine($"Specified folder '{folderPath}' is NOT a git repository. If you want to search subfolders add -r to search recursively.");
                return 1;
            }

            RemoveFromExistingIfExists(existingNodes, folderPath);
            AddBookmark(folderPath);
        }
        
        
        // Write the updated XML back to the file
        using (FileStream fileStream = new FileStream(xmlFilePath, FileMode.Create))
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ArrayOfTreeViewNode));
            serializer.Serialize(fileStream, existingNodes);
        }
        
        return 0; 
    }

    /// <summary>
    /// Adds all git repositories found in the folder structure to the bookmarks.
    /// </summary>
    /// <param name="parentNode"></param>
    /// <param name="folderPath"></param>
    public static void AddBookmarksRecursively(BookmarkFolderNode parentNode, string folderPath)
    {
        foreach (string directoryPath in Directory.GetDirectories(folderPath))
        {
            var directoryInfo = new DirectoryInfo(directoryPath);
            if (IsGitFolder(directoryPath))
            {
                var bookmarkNode = new BookmarkNode
                {
                    Level = parentNode.Level + 1,
                    IsExpanded = false,
                    IsLeaf = true,
                    Name = directoryInfo.Name,
                    Path = directoryPath,
                    RepoType = "Git"
                };
                RemoveFromExistingIfExists(existingNodes, bookmarkNode.Path);
                
                parentNode.Children.Add(bookmarkNode);
            }
            else
            {
                var subRepositories = Directory.GetDirectories(directoryPath, ".git", SearchOption.AllDirectories);
                if (subRepositories.Length == 0)
                    continue;
                
                var folderNode = new BookmarkFolderNode
                {
                    Level = parentNode.Level + 1,
                    IsExpanded = false,
                    IsLeaf = false,
                    Name = directoryInfo.Name,
                };
                parentNode.Children.Add(folderNode);
                AddBookmarksRecursively(folderNode, directoryPath);
            }
        }
    }

    /// <summary>
    /// Add git repository to bookmarks in correct folder structure.
    /// </summary>
    /// <param name="folderPath"></param>
    public static void AddBookmark(string folderPath)
    {
        var parentFolder = new DirectoryInfo(folderPath).Parent;
        var parentBookmark = FindBookmarkFolder(parentFolder.FullName);

        var newNode = new BookmarkNode
        {
            IsExpanded = false,
            IsLeaf = true,
            Name = new DirectoryInfo(folderPath).Name,
            Path = folderPath,
            RepoType = "Git"
        };
        
        // If parent bookmark is null, that means that the parent folder has not been added as a bookmark
        // so we add the new bookmark to the root level
        if (parentBookmark == null)
        {
            newNode.Level = 0;
            existingNodes.Add(newNode);
        }
        else
        {
            newNode.Level = parentBookmark.Level + 1; 
            parentBookmark.Children.Add(newNode);
        }
    }

    private static BookmarkFolderNode? FindBookmarkFolder(string folderPath)
    {        
        var split = folderPath.Split('\\').ToList();

        foreach (var rootNode in existingNodes)
        {
            if (rootNode is BookmarkFolderNode folderNode)
            {
                var idx = split.IndexOf(folderNode.Name);
                if (idx != -1)
                {
                    return FindBookmarkFolder(folderNode, split, idx);
                }
            }
        }

        return null;
    }
    
    private static BookmarkFolderNode? FindBookmarkFolder(BookmarkFolderNode folderNode, List<string> folderPathSplit, int idx)
    {        
        // If we are at the last folder in the path, return empty since we did not find anything
        if (idx == folderPathSplit.Count - 1)
            return folderNode;
        
        var nextFolderName = folderPathSplit[idx + 1];
        foreach (var rootNode in folderNode.Children)
        {
            if (rootNode is BookmarkFolderNode childFolderNode && nextFolderName == childFolderNode.Name)
            {
                return FindBookmarkFolder(folderNode, folderPathSplit, idx+1);
            }
        }

        return folderNode;
    }
    
    public static void RemoveFromExistingIfExists(ArrayOfTreeViewNode tree, string folderPath)
    {
        BookmarkNode? foundNode = null;
        foreach (var node in tree)
        {
            if (node is BookmarkNode bookmarkNode)
            {
                if (FolderPathsEqual(bookmarkNode.Path, folderPath))
                {
                    foundNode = bookmarkNode;
                    break;
                }
            }
            else if (node is BookmarkFolderNode folderNode)
            {
                RemoveFromExistingIfExists(folderNode, folderPath);
            }
        }
        
        if (foundNode != null)
        {
            tree.Remove(foundNode);
        }
    }
    
    public static void RemoveFromExistingIfExists(BookmarkFolderNode root, string folderPath)
    {
        BookmarkNode? foundNode = null;
        foreach (var node in root.Children)
        {
            if (node is BookmarkNode bookmarkNode)
            {
                if (FolderPathsEqual(bookmarkNode.Path, folderPath))
                {
                    foundNode = bookmarkNode;
                    break;
                }
            }
            else if (node is BookmarkFolderNode folderNode)
            {
                RemoveFromExistingIfExists(folderNode, folderPath);
            }
        }
        
        if (foundNode != null)
        {
            root.Children.Remove(foundNode);
        }
    }


    private static bool FolderPathsEqual(string a, string b)
    {
        a = a.Replace('/', '\\');
        b = b.Replace('/', '\\');

        if (a.EndsWith("\\"))
            a = a.Substring(0, a.Length-1);
        if (b.EndsWith("\\"))
            b = b.Substring(0, b.Length-1);
        
        return a == b;
    }
    
    private static bool IsGitFolder(string folderPath)
    {
        return Directory.Exists(Path.Combine(folderPath, ".git"));
    }
}

