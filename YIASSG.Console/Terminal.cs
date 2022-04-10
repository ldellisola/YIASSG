using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using CommandLine;
using YIASSG.Models;
using YIASSG.Utils;

namespace YIASSG.Console;

public static class Terminal
{
    public static async Task Main(string[] args)
    {
        await Parser.Default.ParseArguments<Options>(args)
            .WithNotParsed(ParseError)
            .WithParsedAsync(RunOptionsAsync);
    }

    private class Options
    {
        [Option('s', "source", Required = true, HelpText = "source of the markdown documents")]
        public string Source { get; set; }

        [Option('d', "destination", Required = true, HelpText = "Folder to publish the converted documents")]
        public string Destination { get; set; }

        [Option('c', "config", Required = true, HelpText = "Path to the config file")]
        public string Settings { get; set; }
    }

    private static async Task RunOptionsAsync(Options opts)
    {
        var settings = JsonSerializer.Deserialize<AppSettings>(File.OpenText(opts.Settings.FormatAsPath()).ReadToEnd());

        var md = new Markdown(settings);
        
        await new MDParser(
            opts.Source.FormatAsPath(),
            opts.Destination.FormatAsPath(),
            settings
        ).Run();
    }

    private static void ParseError(IEnumerable<Error> errors)
    {
        System.Console.WriteLine("Error processing inputs!");
        System.Console.WriteLine("Shutting down.");
        return;
    }
}