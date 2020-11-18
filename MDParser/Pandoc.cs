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
        private Dictionary<string, Tuple<string, ReplacementType>> dic = new Dictionary<string, Tuple<string, ReplacementType>>();
        public Pandoc()
        {
            dic[@"\\and"] = Tuple.Create(@"\land", ReplacementType.Suffix );
            dic[@"\\or"] = Tuple.Create(@"\lor",ReplacementType.Suffix);
            dic[@"\\exist"] = Tuple.Create(@"\exists", ReplacementType.Prefix);
            dic[@"\\N"] = Tuple.Create(@"\mathbb{N}",ReplacementType.None);
            dic[@"\\Q"] = Tuple.Create(@"\mathbb{Q}",ReplacementType.None);
            dic[@"\\R"] = Tuple.Create(@"\mathbb{R}",ReplacementType.None);
            dic[@"\\Z"] = Tuple.Create(@"\mathbb{Z}",ReplacementType.None);
            dic[@"\\C"] = Tuple.Create(@"\mathbb{C}",ReplacementType.None);
            dic[@"\\sub"] = Tuple.Create(@"\subset", ReplacementType.Prefix);
            dic[@"\\empty"] = Tuple.Create(@"\emptyset",ReplacementType.Prefix);
            /*
             * overli
             * emptyset
             */

            css = File.ReadAllText(cssPath);
        }

        public void ProcessDocument(string path)
        {
            FileInfo file = new FileInfo(path);

            string src = file.FullName;

            var fs = File.ReadAllText(src);

            foreach (var pair in dic)
            {
                var matches = Regex.Matches(fs,pair.Key);
                int offset = 0;
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

                File.WriteAllText(src,fs);
            }

            fs = fs.Replace(".md", ".html");

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

            File.WriteAllText(src,fs);
        }


        public async Task ConvertDocument(string path)
        {
            FileInfo file = new FileInfo(path);

            string src = file.FullName;
            string dest = file.FullName.Replace(".md", ".html");

            
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

        private enum ReplacementType
        {
            Suffix,
            Prefix,
            None
        }
    }




}
