using System;
using System.IO;
using System.Linq;
using YIASSG.Exceptions;
using YIASSG.Utils;

namespace YIASSG;

public static class DirectoryStructure
{
    private static readonly EnumerationOptions Options = new()
    {
        ReturnSpecialDirectories = false,
        AttributesToSkip = FileAttributes.Hidden,
        RecurseSubdirectories = true
    };

    private static readonly string[] ForbiddenTypes = {".avi", ".mp4", ".mkv", ".mpg", ".mpeg", ".mov",".pdf"};
    private static readonly string[] ForbiddenFolders = {"docs"};

    public static void Copy(string? src, string? dest)
    {
        ArgumentNullException.ThrowIfNull(src);
        ArgumentNullException.ThrowIfNull(dest);
        
        src = src.FormatAsPath();
        dest = dest.FormatAsPath();

        if (!Directory.Exists(src))
            throw new SourceNotFoundException(src);

        if (Directory.Exists(dest))
            Directory.Delete(dest, true);

        Directory.CreateDirectory(dest);

        //Now Create all of the directories
        foreach (var dirPath in Directory.EnumerateDirectories(src, "*", Options)
                     .Where(t => !t.Split(Path.DirectorySeparatorChar).Intersect(ForbiddenFolders).Any()))
            Directory.CreateDirectory(dirPath.Replace(src, dest));
        //Copy all the files & Replaces any files with the same name
        foreach (var newPath in Directory.EnumerateFiles(src, "*.*", Options)
                     .Where(t => !t.Split(Path.DirectorySeparatorChar).SkipLast(1).Intersect(ForbiddenFolders).Any() &&  !ForbiddenTypes.Any(t.EndsWith)))
            File.Copy(newPath, newPath.Replace(src, dest), true);
    }
    
    public static void RunOnAllFiles(Action<string> func, string dir, string searchPattern = "*.md")
    {
        foreach (var file in Directory.GetFiles(dir, searchPattern)) 
            func(file);

        Directory.GetDirectories(dir)
            .ToList()
            .ForEach(item => RunOnAllFiles(func, item, searchPattern));
    }
}