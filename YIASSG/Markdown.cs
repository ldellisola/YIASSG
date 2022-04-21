using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Markdig;
using YIASSG.Utils;
using YIASSG.Exceptions;
using YIASSG.Models;

namespace YIASSG;

public class Markdown
{
    private readonly Dictionary<string, string> _dic;
    private readonly MarkdownPipeline _mdPipeline;
    private readonly string _htmlTemplate;
    private readonly IEnumerable<string> _css;
    private readonly IEnumerable<(string Id, string Content)> _js;

    public Markdown(AppSettings settings)
    {
        _dic = settings.Dictionary;
        _mdPipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().UseBootstrap().Build();
        _htmlTemplate = File.ReadAllText(settings.Template);
        _js = settings.JsAssets.Select(t => (t.Replace(".js",""),File.ReadAllText(t)));
        _css = settings.CssAssets.Select(t => File.ReadAllText(t));
    }

    /// <summary>
    /// It replaces the non-standard latex formulas for standard ones
    /// </summary>
    /// <param name="document"></param>
    /// <returns></returns>
    public string FixLatex(string document)
    {
        // TODO: Hardcode key, replacement definitions
        foreach (var (key, replacement) in _dic)
            document = Regex.Replace(
                document,
                $"(?<key>{key})(?<extra>[^a-zA-Z])",
                match => $"{replacement}{match.Groups["extra"].Value}"
            );

        return document;
    }

    private readonly Regex _linksPattern =
        new(
            @"\[(?<title>.+)\]\((?<address>.*)\.md(?<reference>#*)(?<fragment>.*)\)",
            RegexOptions.Compiled
        );

    /// <summary>
    /// It transforms links directed to md files their HTML equivalent files
    /// </summary>
    /// <param name="document"></param>
    /// <returns></returns>
    public string FixLinks(string document)
    {
        return _linksPattern.Replace(document, match =>
        {
            var title = match.Groups["title"].Value;
            var (address, file) = match.Groups["address"].Value.SplitDirectoryFromFile();

            var completeAddress = string.IsNullOrWhiteSpace(address)
                ? UrlEncoder.Default.Encode(file)
                : $"{address}/{UrlEncoder.Default.Encode(file)}".FormatAsPath();

            var hashtag = match.Groups["reference"].Value;
            var reg = new Regex(@"[^a-zA-Z0-9\u00C0-\u00FF -_]");

            // TODO: Work on this. See if theres a way to autoscroll to any fragment
            var fragment = reg.Replace(match.Groups["fragment"].Value, "")
                .Replace(' ', '-')
                .ToLowerInvariant();


            return hashtag.Length == 0
                ? $"[{title}]({completeAddress}.html)"
                : $"[{title}]({completeAddress}.html#{fragment})";
        });
    }

    private readonly Regex _latexSegmentsPattern =
        new(
            "^(?<indentation>.*)\\${2,}",
            RegexOptions.Multiline | RegexOptions.Compiled
        );

    public void CheckLatexSegments(string document, string filename)
    {
        var errors = _latexSegmentsPattern.Matches(document)
            .Chunk(2)
            .Where(t => t.Length != 2 || t.DistinctBy(r => r.Groups["indentation"].Length).Count() != 1)
            .Select(t => (t.First().Index, t.Last().Index + t.Last().Length))
            .ToList();

        if (errors.Count != 0)
        {
            var (start, end) = errors.First();
            throw new InvalidLatexSegmentException(filename, document, start, end);
        }
    }

    private readonly Regex _codeSegmentsPattern =
        new(
            "^(?<indentation>.*)`{3,}",
            RegexOptions.Multiline | RegexOptions.Compiled
        );

    public void CheckCodeSegments(string document, string filename)
    {
        var errors = _codeSegmentsPattern.Matches(document)
            .Chunk(2)
            .Where(t => t.Length != 2 || t.DistinctBy(r => r.Groups["indentation"].Length).Count() != 1)
            .Select(t => (t.First().Index, t.Last().Index + t.Last().Length))
            .ToList();

        if (errors.Count != 0)
        {
            var (start, end) = errors.First();
            throw new InvalidCodeSegmentException(filename, document, start, end);
        }
    }

    private readonly Regex _markdownImagePattern =
        new(
            @"!\[(?<name>[^\]]+)\]\((?<resource>[^)]+)\)",
            RegexOptions.Compiled
        );

    private readonly Regex _htmlImagePattern =
        new(
            "<img\\s*src=\"(?<src>[^\"]+)\"\\s*alt=\"(?<alt>[^\"]+)\"\\s*style=\"zoom:(?<scale>\\d+)%;\"\\s*/>",
            RegexOptions.Compiled
        );
    
    public void CheckImageLinks(string document, string documentName)
    {
        var (directory, _) = documentName.SplitDirectoryFromFile();
        foreach (Match match in _markdownImagePattern.Matches(document))
        {
            var imageFile = Paths.FormatAsPath(directory, match.Groups["resource"].Value);
            if (!File.Exists(imageFile))
                throw new InvalidImageLinkException(
                    documentName,
                    imageFile,
                    document,
                    match.Index,
                    match.Index + match.Length
                );
        }
        
        foreach (Match match in _htmlImagePattern.Matches(document))
        {
            var imageFile = Paths.FormatAsPath(directory, match.Groups["src"].Value);
            if (!File.Exists(imageFile))
                throw new InvalidImageLinkException(
                    documentName,
                    imageFile,
                    document,
                    match.Index,
                    match.Index + match.Length
                );
        }
    }

    public string FixImageLinks(string document, string filename)
    {
        var file = new FileInfo(filename);

        document = _markdownImagePattern.Replace(document, (match) =>
        {
            var name = match.Groups["name"].Value;
            var originalRelativeResourceName = match.Groups["resource"].Value;

            var (originalRelativeDirectory, _) = originalRelativeResourceName.SplitDirectoryFromFile();
            var originalResource = new FileInfo($"{file.Directory}/{originalRelativeResourceName}".FormatAsPath());

            var newRelativeResourceName =
                $"{originalRelativeDirectory}/{Guid.NewGuid()}{originalResource.Extension}".FormatAsPath();
            
            try{
                originalResource.MoveTo($"{file.Directory}/{newRelativeResourceName}".FormatAsPath(), true);
            }
            catch (Exception e)
            {
                throw new InvalidImageLinkException(
                    filename, 
                    originalResource.FullName,
                    document, 
                    match.Index,
                    match.Index + match.Length
                    );
            }

            return $"![{name}]({newRelativeResourceName})";
        });

        document = _htmlImagePattern.Replace(document, (match) =>
        {
            var src = match.Groups["src"].Value;
            var alt = Guid.NewGuid();
            var scale = match.Groups["scale"].Value;

            var (originalRelativeDirectory, _) = src.SplitDirectoryFromFile();
            var originalResource = new FileInfo($"{file.Directory}/{src}".FormatAsPath());

            var newRelativeResourceName =
                $"{originalRelativeDirectory}/{alt}{originalResource.Extension}".FormatAsPath();
            try
            {
                
                originalResource.MoveTo($"{file.Directory}/{newRelativeResourceName}".FormatAsPath(), true);
            }
            catch (Exception e)
            {
                throw new InvalidImageLinkException(
                    filename, 
                    originalResource.FullName,
                    document, 
                    match.Index,
                    match.Index + match.Length
                );
            }


            return $"<img src=\"{newRelativeResourceName}\" alt=\"{alt}\" style=\"zoom:{scale}%;\" />";
        });

        return document;
    }

    public string ToHTML(string document, string title)
    {
        return _htmlTemplate
            .Replace("{{title}}", title)
            .Replace("{{css}}", string.Join('\n', _css.Select(t=> $"<style>{t}</style>")))
            .Replace("{{js}}", string.Join('\n', _js.Select(t=> $"<script id=\"{t.Id}-script\">{t.Content}</script>")))
            .Replace("{{body}}", Markdig.Markdown.ToHtml(document, _mdPipeline));
    }


}