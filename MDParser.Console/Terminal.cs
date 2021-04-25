using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CommandLine;
using MDParser.Utils;

namespace MDParser.Console
{
    public class Terminal
    {

        public static async Task Main(string[] args)
        {
            await CommandLine.Parser.Default.ParseArguments<Options>(args)
                .WithNotParsed(ParseError)
                .WithParsedAsync(RunOptionsAsync);

        }

        public class Options
        {
            [Option('s',"source",Required = true, HelpText = "source of the markdown documents")]
            public string Source { get; set; }

            [Option('d',"destination",Required = true, HelpText = "Folder to publish the converted documents")]
            public string Destination { get; set; }
        }

        public static async Task RunOptionsAsync(Options opts)
        {
            string dest = opts.Destination.FormatAsPath();
            string src = opts.Source.FormatAsPath();
            
            Directory.Delete(dest,true);
            DirectoryStructure.Copy(new DirectoryInfo(src), new DirectoryInfo(dest),true);
            var pandocDictionary = Pandoc.GetDictionary();

            // Process

            // This function will create the indexes of courses
                
            List<Metadata> coursesMedatada = new List<Metadata>();

            DirectoryStructure.RunInEveryDirectory(t =>
            {
                System.Console.WriteLine($"In Folder {t.FullName}");
                
                var metadataFiles = t.EnumerateFiles( "courseMetadata.stp",new EnumerationOptions{ ReturnSpecialDirectories = true, AttributesToSkip = FileAttributes.Directory });
                
                if(metadataFiles.Count() != 1)
                { 
                    System.Console.WriteLine("FILES");
                    t.EnumerateFiles("*", new EnumerationOptions { ReturnSpecialDirectories = true, AttributesToSkip = FileAttributes.Directory }).ToList().ForEach(a=> System.Console.WriteLine($"\t {a.Name}"));
                    System.Console.WriteLine("SYSTEM FILES");
                    t.EnumerateFileSystemInfos("*", new EnumerationOptions { ReturnSpecialDirectories = true, AttributesToSkip = FileAttributes.Directory }).ToList().ForEach(a=> System.Console.WriteLine($"\t {a.Name}"));
                    System.Console.WriteLine("No METADATA FOUND");
                    return;
                }
                Metadata metadata = JsonSerializer.Deserialize<Metadata>(metadataFiles.First().OpenText().ReadToEnd());
                coursesMedatada.Add(metadata);
                var mdFiles = t.GetFiles("*.md").ToList();
                if (mdFiles.Count != 0)
                {
                    string indexFile = DirectoryStructure.CreateIndex(mdFiles, metadata.CourseName);
                    File.WriteAllText((t.FullName + @"/index.md").FormatAsPath(),indexFile);
                }

            },dest);

            new Pandoc(pandocDictionary,null).CreateIndex(coursesMedatada,dest);


            // This function will process all the documents to translate latex and convert links
            await DirectoryStructure.RunInAllFiles(async t =>
            {
                await new Pandoc(pandocDictionary,null).ProcessDocument(t);
            }, dest);



            // Export
            await DirectoryStructure.RunInAllFiles(async t =>
            {
                await new Pandoc(pandocDictionary,null).ConvertDocument(t);
            }, dest);
        }

        public static void ParseError(IEnumerable<Error> errors)
        {
            System.Console.WriteLine("Error processing inputs!");
            System.Console.WriteLine("Shutting down.");
            return; 
        }
    }
}