using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using YIASSG.Utils;
using YIASSG.Models;

namespace YIASSG;

public class YIASSG
{
    private readonly AppSettings _settings;
    private readonly string _destination;
    private readonly string _source;
    private IEnumerable<Metadata>? _courses;
    private readonly Markdown _markdown;

    public YIASSG(string? src, string? dest, AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(src);
        ArgumentNullException.ThrowIfNull(dest);

        _source = src;
        _destination = dest;
        _settings = settings;
        _markdown = new Markdown(settings);
    }

    private IEnumerable<Metadata> FindCoursesMetadata()
    {
        return Directory.EnumerateFiles(_destination, _settings.MetadataFileName ?? "courseMetadata.stp",
                SearchOption.AllDirectories)
            .Select(Metadata.LoadFromFile)
            .OrderBy(t => t.Code);
    }

    private void CreateIndexForCourse(Metadata course)
    {
        var doc = new MarkdownDocument()
            .AddHeading()
            .AddLine(course.Name);

        foreach (var filename in Directory.EnumerateFiles(course.Path, "*.md").OrderBy(t => t))
        {
            var text = File.ReadAllText(filename);
            var headings = Regex.Matches(text, @"^(?<depth>\#+) (?<title>.+)", RegexOptions.Multiline)
                .AsEnumerable()
                .OrderBy(t => t.Index);
            int? previousLevel = null;

            foreach (var heading in headings)
            {
                var level = heading.Groups["depth"].Value.Length;
                var content = heading.Groups["title"].Value.Trim(' ', '\t');
                var nLev =
                    (int) (previousLevel is not null && level - 1 > previousLevel ? previousLevel + 1 : level - 1);
                doc.AddUnorderedListElement(nLev)
                    .AddLink(
                        content,
                        filename.Replace(course.Path + Path.DirectorySeparatorChar, ""),
                        level,
                        content
                    )
                    .AddNewLine();
                previousLevel = nLev;
            }
        }

        File.WriteAllText((course.Path + @"/index.md").FormatAsPath(), doc.Build());
    }

    private Task CreateIndexFileAsync(CancellationToken token = default)
    {
        var doc = new MarkdownDocument();

        doc.AddHeading()
            .AddLine("Materias");

        foreach (var course in _courses!)
            doc.AddUnorderedListElement()
                .AddLink($"{course.Code} - {course.Name}", $"{course.Code}/index.md")
                .AddNewLine();

        return File.WriteAllTextAsync($"{_destination}/index.md".FormatAsPath(), doc.Build(), token);
    }

    private string PrepareMarkdownDocument(string content, string filename)
    {
        content = _markdown.FixLatex(content);
        _markdown.CheckCodeSegments(content, filename);
        _markdown.CheckLatexSegments(content, filename);

        content = _markdown.FixLinks(content);
        content = _markdown.FixImageLinks(content, filename);

        return content;
    }


    private void ConvertDocument(string filename)
    {
        var (_, title) = filename.SplitDirectoryFromFile();

        var document = PrepareMarkdownDocument(File.OpenText(filename).ReadToEnd(), filename);
        var content = _markdown.ToHtml(document, title!);

        File.WriteAllText(filename.Replace(".md", ".html"), content, Encoding.UTF8);
        // File.Delete(filename);
    }

    public async Task Run(CancellationToken token = default)
    {
        DirectoryStructure.Copy(_source, _destination);

        _courses = FindCoursesMetadata();
        foreach (var course in _courses)
            CreateIndexForCourse(course);

        await CreateIndexFileAsync(token);
        DirectoryStructure.RunOnAllFiles(ConvertDocument, _destination);
    }
}