using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MDParser.Utils;
using Microsoft.Extensions.Logging;

namespace MDParser
{
    
    public class Pandoc
    {
        private string css { get; set; }

        private const string cssPath = "style.css";
        static string dictionaryPath = "Dictionary.json";
        private readonly ILogger<Pandoc> _logger;
        private Dictionary<string, Tuple<string, ReplacementType>> dic;
        public Pandoc(Dictionary<string, Tuple<string, ReplacementType>> dictionary, ILogger<Pandoc> _logger)
        {
            //dic[@"\\and"] = Tuple.Create(@"\land", ReplacementType.Suffix );
            //dic[@"\\or"] = Tuple.Create(@"\lor",ReplacementType.Suffix);
            //dic[@"\\exist"] = Tuple.Create(@"\exists", ReplacementType.Prefix);
            //dic[@"\\N"] = Tuple.Create(@"\mathbb{N}",ReplacementType.None);
            //dic[@"\\Q"] = Tuple.Create(@"\mathbb{Q}",ReplacementType.None);
            //dic[@"\\R"] = Tuple.Create(@"\mathbb{R}",ReplacementType.None);
            //dic[@"\\Z"] = Tuple.Create(@"\mathbb{Z}",ReplacementType.None);
            //dic[@"\\C"] = Tuple.Create(@"\mathbb{C}",ReplacementType.None);
            //dic[@"\\sub"] = Tuple.Create(@"\subset", ReplacementType.Prefix);
            //dic[@"\\empty"] = Tuple.Create(@"\emptyset",ReplacementType.Prefix);
            //dic[@"\\rarr"] = Tuple.Create(@"\rightarrow", ReplacementType.None);
            //dic[@"\\Rarr"] = Tuple.Create(@"\Rightarrow", ReplacementType.None);
            //dic[@"\\larr"] = Tuple.Create(@"\leftarrow", ReplacementType.None);
            //dic[@"\\Larr"] = Tuple.Create(@"\Leftarrow", ReplacementType.None);
            this._logger = _logger;
            dic = dictionary;
            css = File.ReadAllText(cssPath);
        }

        public static Dictionary<string, Tuple<string, ReplacementType>> GetDictionary()
        {
            string text = File.ReadAllText(dictionaryPath);
            
            var a = JsonSerializer.Deserialize<Dictionary<string, Tuple<string, ReplacementType>>>(text);
            return a;
        }

        public async Task ProcessDocument(string path)
        {
            FileInfo file = new FileInfo(path);

            string src = file.FullName;

            var fs = await File.ReadAllTextAsync(src);
            int offset = 0;

            foreach (var pair in dic)
            {
                var matches = Regex.Matches(fs,pair.Key);
                offset = 0;
                foreach (var match in matches.ToList())
                {

                    // If the string I'm loooking for is a suffix of the string I want to replace it with
                    if (pair.Value.Item2 == ReplacementType.Suffix)
                    {
                        var temp = fs.Substring(match.Index + offset,pair.Value.Item1.Length);
                        bool wasAlreadySubstituted = temp.Contains(pair.Value.Item1);

                        if (!wasAlreadySubstituted)
                        {
                            fs = fs.Remove(match.Index+ offset, match.Length);
                            fs = fs.Insert(match.Index+ offset, pair.Value.Item1);

                            offset += pair.Value.Item1.Length - match.Length;

                        }
                    }
                    else if (pair.Value.Item2 == ReplacementType.Prefix)
                    {
                        var temp = fs.Substring(match.Index + offset,pair.Value.Item1.Length);
                        bool wasAlreadySubstituted = temp.Contains(pair.Value.Item1);

                        if (!wasAlreadySubstituted)
                        {
                            fs = fs.Remove(match.Index+ offset, match.Length);
                            fs = fs.Insert(match.Index+ offset, pair.Value.Item1);

                            offset += pair.Value.Item1.Length - match.Length;

                        }
                    }
                    else if (pair.Value.Item2 == ReplacementType.None)
                    {
                        var ch = fs[match.Index + offset + match.Length];

                        if (!Char.IsLetter(ch) || ch == '$')
                        {
                            fs = fs.Remove(match.Index+ offset, match.Length);
                            fs = fs.Insert(match.Index+ offset, pair.Value.Item1);

                            offset += pair.Value.Item1.Length - match.Length;
                        }
                        
                        
                    }
                    
                }
            }


            offset = 0;
            foreach (var match in Regex.Matches(fs, @"\[(.+)\]\((.*)\.md(#*)(.*)\)").ToList())
            {
                string text = match.Groups[1].Value;
                string address = match.Groups[2].Value;
                string hashtag = match.Groups[3].Value;

                var reg = new Regex(@"[^a-zA-Z0-9\u00C0-\u00FF -_]");
                string titleLink = reg.Replace(match.Groups[4].Value,"").
                    Replace(' ','-').ToLowerInvariant();

                fs = fs.Remove(match.Index + offset,match.Length);

                if (hashtag.Length == 0)
                {
                    fs = fs.Insert(match.Index + offset, $"[{text}]({address}.html)");
                    offset += $"[{text}]({address}.html)".Length - match.Length;
                }
                else
                {
                    fs = fs.Insert(match.Index + offset, $"[{text}]({address}.html#{titleLink})");
                    offset +=$"[{text}]({address}.html#{titleLink})".Length - match.Length;
                }
            }



            int baseIndex = 0;

            do
            {
                int startIndex = fs.IndexOf('$',baseIndex);
                if (startIndex == -1)
                    break;
                int endIndex = fs.IndexOf('$', startIndex + 1);
                if (endIndex == -1)
                    break;

                var temp = fs.Substring(startIndex+1, endIndex - startIndex-1);

                fs = fs.Remove(startIndex + 1, endIndex - startIndex - 1);
                fs = fs.Insert(startIndex + 1, temp.Trim());


                baseIndex = endIndex + 1 -(endIndex - startIndex -1) + temp.Trim().Length;

            } while (baseIndex < fs.Length && baseIndex >= 0);

            await File.WriteAllTextAsync(src,fs);
        }


        public async Task ConvertDocument(string path)
        {
            FileInfo file = new FileInfo(path);

            string src = file.FullName;
            string dest = file.FullName.Replace(".md", ".html");

            var process = new PandocProcess(src, dest, file.Name.Replace(".md", ""));

            await process.Execute();

            if (!await process.HasError())
            {
                var html = await File.ReadAllTextAsync(dest);
                var insert = html.Insert(html.IndexOf("</head>", StringComparison.Ordinal), css);
                await File.WriteAllTextAsync(dest,insert);
                File.Delete(path);
            }
            else
            {
                var err = await process.GetError();
                Console.WriteLine($"Error running Pandoc Proces {src}: {err}");
            }
        }

        public enum ReplacementType
        {
            Suffix,
            Prefix,
            None
        }

        public void CreateIndex(List<Metadata> coursesMedatada, string dest)
        {
            StringBuilder bld = new StringBuilder();

            bld.AppendLine($"# Materias");

            coursesMedatada.ForEach(t =>
            {
                bld.AppendLine($"- [{t.CourseCode} - {t.CourseName}]({t.CourseCode}/index.md)");
            });

            File.WriteAllText((dest + @"/index.md").FormatAsPath(),bld.ToString());
        }
    }




}
