using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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

    private static readonly string[] ForbiddenTypes = {".avi", ".mp4", ".mkv", ".mpg", ".mpeg", ".mov"};

    public static void Copy(string src, string dest)
    {
        src = src.FormatAsPath();
        dest = dest.FormatAsPath();

        if (!Directory.Exists(src))
            throw new SourceNotFoundException(src);

        if (Directory.Exists(dest))
            Directory.Delete(dest, true);

        Directory.CreateDirectory(dest);

        //Now Create all of the directories
        foreach (var dirPath in Directory.EnumerateDirectories(src, "*", Options)
                     .Where(t => t != dest))
            Directory.CreateDirectory(dirPath.Replace(src, dest));
        //Copy all the files & Replaces any files with the same name
        foreach (var newPath in Directory.EnumerateFiles(src, "*.*", Options)
                     .Where(t => !ForbiddenTypes.Any(t.EndsWith)))
            File.Copy(newPath, newPath.Replace(src, dest), true);
    }

    public static string CreateIndex(List<FileInfo> mdFiles, string title)
    {
        var fileTrees = new List<TitleNode>();
        mdFiles.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        foreach (var file in mdFiles)
        {
            var lines = File.ReadAllLines(file.FullName)
                .Where(t =>
                {
                    var reg = Regex.Match(t, "^(#+).+$");
                    return reg.Success && reg.Groups[1].Value.Length > 0;
                }).ToList();

            var text = file.Name.Replace(".md", "");
            var ind = text.IndexOf('-');
            if (ind != -1) text = text.Substring(ind + 1);

            var root = new TitleNode
            {
                Text = text.Trim(),
                ParentNode = null,
                Level = 0,
                FileName = file.Name,
                RealLevel = 0
            };

            var curr = root;

            lines.ForEach(t =>
            {
                var reg = Regex.Match(t, "^(#+).+$");
                var level = reg.Groups[1].Value.Length;

                while (level <= curr.Level) curr = curr.ParentNode;

                var child = new TitleNode
                {
                    ParentNode = curr,
                    Level = level,
                    Text = t.TrimStart('#').Trim(),
                    FileName = file.Name,
                    RealLevel = level
                };
                curr.ChildNodes.Add(child);

                curr = child;
            });

            if (root.ChildNodes.Count == 1)
            {
                var removedChild = root.ChildNodes.First();
                root.ChildNodes = removedChild.ChildNodes;
                root.RearrangeTree();
            }

            fileTrees.Add(root);
        }


        var bld = new MarkdownDocument()
            .AddHeading()
            .AddLine(title);

        var index = 1;
        foreach (var tree in fileTrees) tree.WriteMarkDown(bld, index++);

        return bld.ToString();
    }

    public static void RunInEveryDirectory(Action<DirectoryInfo> convert, string dir)
    {
        convert(new DirectoryInfo(dir));

        Directory.GetDirectories(dir).ToList().ForEach((t) => { RunInEveryDirectory(convert, t); });
    }

    public static void RunInAllFiles(Action<string> func, string dir,
        string searchPattern = "*.md")
    {
        foreach (var file in Directory.GetFiles(dir, searchPattern)) func(file);

        Directory.GetDirectories(dir)
            .ToList()
            .ForEach(item => RunInAllFiles(func, item, searchPattern));
    }
}