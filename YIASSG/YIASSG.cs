using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;
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
    private IEnumerable<Metadata> _courses;
    private readonly Markdown _markdown;

    public YIASSG(string src, string dest, AppSettings settings)
    {
        _source = src;
        _destination = dest;
        _settings = settings;
        _markdown = new Markdown(settings);
    }

    private IEnumerable<Metadata> FindCoursesMetadata()
    {
        return Directory.EnumerateFiles(_destination, "courseMetadata.stp", SearchOption.AllDirectories)
            .Select(Metadata.LoadFromFile)
            .OrderBy(t => t.Code);
    }

    private void CreateIndexForCourse(Metadata course)
    {
        var files = Directory.EnumerateFiles(course.Path, "*.md")
            .ToList()
            .ConvertAll(t => new FileInfo(t));

        var indexFile = DirectoryStructure.CreateIndex(files, course.Name);
        File.WriteAllText((course.Path + @"/index.md").FormatAsPath(), indexFile);
    }

    private Task CreateIndexFileAsync(CancellationToken token = default)
    {
        var doc = new MarkdownDocument();

        doc.AddHeading()
            .AddLine("Materias");

        foreach (var course in _courses)
            doc.AddUnorderedListElement()
                .AddLink($"{course.Code} - {course.Name}", $"{course.Code}/index.md")
                .AddNewLine();

        return File.WriteAllTextAsync($"{_destination}/index.md".FormatAsPath(), doc.Build(), token);
    }

    private string PrepareMarkdownDocument(string content, string filename)
    {
        content = _markdown.FixLatex(content);
        content = _markdown.FixLinks(content);
        content = _markdown.FixImageLinks(content, filename);

        _markdown.CheckCodeSegments(content, filename);
        _markdown.CheckLatexSegments(content, filename);

        // TODO: Add fix links tests
        // TODO: Add fix latex tests
        // TODO: Add docker support
        // TODO: Add checks for **, <u><\u> y otros
        return content;
    }


    private void ConvertDocument(string filename)
    {
        var (_, title) = filename.SplitDirectoryFromFile();
        var content = PrepareMarkdownDocument(File.OpenText(filename).ReadToEnd(), filename);
        content = _markdown.ToHTML(content, title);
        File.WriteAllText(filename.Replace(".md", ".html"), content, Encoding.UTF8);
        File.Delete(filename);
    }

    public async Task Run(CancellationToken token = default)
    {
        DirectoryStructure.Copy(_source, _destination);

        _courses = FindCoursesMetadata();
        foreach (var course in _courses)
            CreateIndexForCourse(course);

        await CreateIndexFileAsync(token);
        DirectoryStructure.RunInAllFiles(ConvertDocument, _destination);
    }
}