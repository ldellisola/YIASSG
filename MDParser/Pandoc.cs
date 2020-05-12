using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MDParser
{
    
    public class Pandoc
    {
        private string css { get; set; }

        private const string args = "/C Pandoc \"{0}\" -s -c https://raw.githubusercontent.com/slashfoo/lmweb/master/style/latinmodern-mono-light.css --mathjax --highlight-style tango --metadata pagetitle=\"{1}\" -o \"{2}\"";
        private const string cssPath = "style.css";
        private Dictionary<string, Tuple<string, string>> dic = new Dictionary<string, Tuple<string, string>>();
        public Pandoc()
        {
            dic[@"\\and"] = Tuple.Create(@"\land", @"");
            dic[@"\\or"] = Tuple.Create(@"\lor","");
            dic[@"\\exist"] = Tuple.Create(@"\exists", @"\exists");
            dic[@"\\N "] = Tuple.Create(@"\mathbb{N}","");
            dic[@"\\Q "] = Tuple.Create(@"\mathbb{Q}","");
            dic[@"\\R "] = Tuple.Create(@"\mathbb{R}","");
            dic[@"\\sub"] = Tuple.Create(@"\subset", @"\subset");
            dic[@"\\empty"] = Tuple.Create(@"\emptyset", @"\emptyset");

            css = File.ReadAllText(cssPath);
        }


        public async Task ConvertDocument(string path)
        {
            FileInfo file = new FileInfo(path);

            string src = file.FullName;
            string dest = file.FullName.Replace(".md", ".html");

            var fs = await File.ReadAllTextAsync(src);

            foreach (var pair in dic)
            {
                var matches = Regex.Matches(fs,pair.Key);

                foreach (var match in matches.ToList())
                {
                    if (!fs.Substring(match.Index, pair.Value.Item2.Length).Contains(pair.Value.Item2))
                    {
                        fs = fs.Remove(match.Index, match.Length).Insert(match.Index, pair.Value.Item1);
                        
                    }
                }
            }

            fs = fs.Replace(".md", ".html");

            File.WriteAllText(src,fs);


            var argsComplete = String.Format(args,src, file.Name.Replace(".md",""), dest);

            var CMD = new Process
            {
                StartInfo =
                {
                    FileName = "CMD.exe",
                    Arguments = argsComplete,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    UseShellExecute = false
                }
            };

            CMD.Start();

            CMD.WaitForExit();

            string err = CMD.StandardError.ReadToEnd();

            Debug.WriteLineIf(err != "", err);

            var html = File.ReadAllText(dest);

            var insert = html.Insert(html.IndexOf("</head>", StringComparison.Ordinal), css);

            File.WriteAllText(dest,insert);

            File.Delete(path);



        }
    }




}
