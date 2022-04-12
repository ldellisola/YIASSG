using CommandLine;
using YIASSG;
using YIASSG.Models;
using YIASSG.Utils;

namespace MDParser.Hooks;

public class Hooks
{
    private class Options
    {
        [Option('s', "source", Required = true, HelpText = "source of the markdown documents")]
        public string Source { get; set; }
    }

    public static async Task Main(string[] args)
    {
        await Parser.Default.ParseArguments<Options>(args)
            .WithNotParsed(ParseError)
            .WithParsedAsync(RunOptionsAsync);
    }


    private static async Task RunOptionsAsync(Options arg)
    {
        var src = new DirectoryInfo(arg.Source.FormatAsPath());
        var md = new Markdown(new AppSettings());

        foreach (var file in src.EnumerateFiles("*.md", SearchOption.AllDirectories))
        {
            var content = await file.OpenText().ReadToEndAsync();
            md.CheckCodeSegments(content, file.FullName);
            md.CheckLatexSegments(content, file.FullName);
            md.CheckImageLinks(content, file.FullName);
        }
    }

    private static void ParseError(IEnumerable<Error> obj)
    {
        Console.WriteLine("ERROR");
    }
}