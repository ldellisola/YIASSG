using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using YIASSG.Utils;
using YIASSG.Models;

namespace YIASSG
{
    public class MDParser
    {
        private readonly AppSettings _settings;
        private readonly string _destination;
        private readonly string _source;
        private readonly List<Metadata> _courses;
        private readonly Markdown _markdown;

        public MDParser(string src, string dest, AppSettings settings)
        {
            this._source = src;
            this._destination = dest;
            this._settings = settings;
            this._courses = FindCoursesMetadata();
            this._markdown = new Markdown(settings);
        }

        private List<Metadata> FindCoursesMetadata()
        {
            return Directory.EnumerateFiles(_destination, "courseMetadata.stp", SearchOption.AllDirectories)
                .ToList()
                .ConvertAll(Metadata.LoadFromFile);
        }

        private void CreateIndexForCourse(Metadata course)
        {
            var files = Directory.EnumerateFiles(course.Path, "*.md")
                .ToList()
                .ConvertAll(t => new FileInfo(t));

            string indexFile = DirectoryStructure.CreateIndex(files, course.CourseName);
            File.WriteAllText((course.Path + @"/index.md").FormatAsPath(), indexFile);
        }

        private Task CreateIndexFileAsync(CancellationToken token = default)
        {
            StringBuilder bld = new StringBuilder();

            bld.AppendLine($"# Materias");
            _courses.ForEach(t => bld.AppendLine($"- [{t.CourseCode} - {t.CourseName}]({t.CourseCode}/index.md)"));

            return File.WriteAllTextAsync(($"{_destination}/index.md").FormatAsPath(), bld.ToString(), token);
        }

        private string PrepareMarkdownDocument(string content, string filename)
        {
            content = _markdown.FixLatex(content);
            content = _markdown.FixLinks(content);
            content = _markdown.FixImageLinks(content, filename);

            // _markdown.CheckCodeSegments(content, filename);
            // _markdown.CheckLatexSegments(content, filename);
            
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
            File.WriteAllText(filename.Replace(".md",".html"), content, Encoding.UTF8);
            File.Delete(filename);
        }

        public async Task Run(CancellationToken token = default)
        {
            Directory.Delete(_destination, true);
            DirectoryStructure.Copy(new DirectoryInfo(_source), new DirectoryInfo(_destination), true);

            _courses.ForEach(this.CreateIndexForCourse);
            await CreateIndexFileAsync(token);
            DirectoryStructure.RunInAllFiles(ConvertDocument, _destination, token: token);
        }
    }
}