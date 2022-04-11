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

namespace YIASSG
{
    public class Markdown
    {
        private readonly Dictionary<string, string> _dic;
        private readonly MarkdownPipeline _mdPipeline;
        private readonly string _htmlTemplate;
        private readonly string _cssStyle;

        private readonly Regex _markdownImagePattern = 
            new (
                @"!\[(?<name>[^\]]+)\]\((?<resource>[^)]+)\)", 
                RegexOptions.Compiled
            );
        private readonly Regex _htmlImagePattern = 
            new (
                "<img\\s*src=\"(?<src>[^\"]+)\"\\s*alt=\"(?<alt>[^\"]+)\"\\s*style=\"zoom:(?<scale>\\d+)%;\"\\s*/>", 
                RegexOptions.Compiled
            );
        private readonly Regex _linksPattern = 
            new (
                @"\[(?<title>.+)\]\((?<address>.*)\.md(?<reference>#*)(?<subtitle>.*)\)", 
                RegexOptions.Compiled
            );

        public Markdown(AppSettings settings)
        {
            this._dic = settings.Dictionary;
            this._mdPipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().UseBootstrap().Build();
            this._htmlTemplate = File.ReadAllText(settings.Template);
            _cssStyle = File.ReadAllText("style.css");
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
            {
                document = Regex.Replace(
                    document,
                    $"(?<key>{key})(?<extra>[^a-zA-Z])", 
                    match => $"{replacement}{match.Groups["extra"].Value}"
                );
            }

            return document;
        }

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

                var completeAddress = string.IsNullOrWhiteSpace(address) ? 
                    UrlEncoder.Default.Encode(file) :
                    $"{address}/{UrlEncoder.Default.Encode(file)}".FormatAsPath();
                
                var hashtag = match.Groups["reference"].Value;
                var reg = new Regex(@"[^a-zA-Z0-9\u00C0-\u00FF -_]");
                
                string subtitle = reg.Replace(match.Groups["subtitle"].Value,"")
                    .Replace(' ','-')
                    .ToLowerInvariant();
                
                
                return hashtag.Length == 0 ? $"[{title}]({completeAddress}.html)" : $"[{title}]({completeAddress}.html#{subtitle})";
            });
            
        }

        public void CheckLatexSegments(string document, string filename)
        {
            var isNotValid = Regex.Matches(document, "^[\t| ]*\\${2,}", RegexOptions.Multiline)
                .Select(t => t.Value.Replace("\n",""))
                .Chunk(2)
                .Any(t=> t.Length != 2 || t.Distinct().Count() != 1);
            
            if (isNotValid)
                throw new InvalidLatexSegmentException(filename);
        }

        public void CheckCodeSegments(string document, string filename)
        {
            var isNotValid = Regex.Matches(document, "^[\t| ]*`{2,}", RegexOptions.Multiline)
                .Select(t => t.Value.Replace("\n",""))
                .Chunk(2)
                .Any(t=> t.Length != 2 || t.Distinct().Count() != 1);
            
            if (isNotValid)
                throw new InvalidCodeSegmentException(filename);

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

                var newRelativeResourceName = $"{originalRelativeDirectory}/{Guid.NewGuid()}{originalResource.Extension}".FormatAsPath();   
                
                // TODO: Throw exception if the file does not exists with line

                // TODO: Some files are not working
                try{
                    originalResource.MoveTo($"{file.Directory}/{newRelativeResourceName}".FormatAsPath(), true);
                }
                catch (Exception e)
                {
                    
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

                var newRelativeResourceName = $"{originalRelativeDirectory}/{alt}{originalResource.Extension}".FormatAsPath();
                // TODO: Throw exception if the file does not exists with line
                try
                {
                    originalResource.MoveTo($"{file.Directory}/{newRelativeResourceName}".FormatAsPath(), true);
                }
                catch (Exception e)
                {
                    
                }


                return $"<img src=\"{newRelativeResourceName}\" alt=\"{alt}\" style=\"scale:0.{scale};\" />";
            });
            
            return document;
        }

        public string ToHTML(string document, string title)
        {
            return _htmlTemplate
                .Replace("{{title}}", title)
                .Replace("{{style}}",_cssStyle)
                .Replace("{{body}}", Markdig.Markdown.ToHtml(document, _mdPipeline));

        }
        
    }
}